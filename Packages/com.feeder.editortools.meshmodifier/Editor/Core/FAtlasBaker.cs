#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Feeder
{
    public sealed class FAtlasSlot
    {
        public Rect AtlasRect;
        public Rect Region;
        public Vector2 Tiling = Vector2.one;
        public Vector2 Offset = Vector2.zero;
        public bool Solid;
    }

    public sealed class FAtlasBakeOutput
    {
        public readonly Dictionary<Material, FAtlasSlot> Slots = new Dictionary<Material, FAtlasSlot>();
        public readonly List<FMeshBakeResults.Atlas> Atlases = new List<FMeshBakeResults.Atlas>();
        public Vector2Int AtlasSize;
        public FAtlasPackingAlgorithm Algorithm;

        public Vector2 Remap(Material material, Vector2 uv)
        {
            FAtlasSlot slot = Slots[material];
            Rect r = slot.AtlasRect;
            if (slot.Solid)
                return r.center;

            Vector2 t = uv * slot.Tiling + slot.Offset;
            float lx = (t.x - slot.Region.xMin) / slot.Region.width;
            float ly = (t.y - slot.Region.yMin) / slot.Region.height;
            float x = Algorithm == FAtlasPackingAlgorithm.HorizontalStrips ? t.x : r.xMin + lx * r.width;
            float y = Algorithm == FAtlasPackingAlgorithm.VerticalStrips ? t.y : r.yMin + ly * r.height;
            return new Vector2(x, y);
        }
    }

    public static class FAtlasBaker
    {
        private const string BlitShaderName = "Hidden/Feeder/AtlasBlit";
        private const int SolidTileSize = 16;
        private const int MinTileSize = 4;
        private const int ColorPass = 0;
        private const int NormalPass = 1;

        private static readonly string[] StandardProperties =
        {
            "_BaseMap", "_MainTex", "_BumpMap", "_NormalMap", "_MetallicGlossMap", "_SpecGlossMap",
            "_OcclusionMap", "_EmissionMap", "_ParallaxMap", "_DetailMask"
        };

        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "_BaseMap", "_MainTex" },
            { "_MainTex", "_BaseMap" }
        };

        private sealed class PropertyPlan
        {
            public string Name;
            public int Index;
            public bool IsNormal;
            public bool IsColor;
        }

        private sealed class Tile
        {
            public readonly List<Material> Materials = new List<Material>();
            public Material Representative;
            public Rect Region;
            public Vector2 Tiling;
            public Vector2 Offset;
            public bool Solid;
            public Vector2Int Size;
            public RectInt Pixels;
        }

        #region Bake

        public static FAtlasBakeOutput Bake(FMeshBakeSettings settings, Material resultMaterial,
            IReadOnlyDictionary<Material, Rect> meshUVBounds, Action<string, float> progress)
        {
            List<PropertyPlan> properties = PlanProperties(settings, resultMaterial, meshUVBounds.Keys);
            List<Tile> tiles = BuildTiles(settings, properties, meshUVBounds);

            var output = new FAtlasBakeOutput { Algorithm = settings.packingAlgorithm };
            Vector2Int[] sizes = tiles.Select(t => t.Size).ToArray();
            FAtlasLayout layout = FAtlasLayout.Build(sizes, settings.atlasPadding, settings.maxAtlasSize, settings.packingAlgorithm);
            output.AtlasSize = layout.Size;

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                tile.Pixels = layout.Rects[i];
                var slot = new FAtlasSlot
                {
                    AtlasRect = new Rect((float)tile.Pixels.x / layout.Size.x, (float)tile.Pixels.y / layout.Size.y,
                        (float)tile.Pixels.width / layout.Size.x, (float)tile.Pixels.height / layout.Size.y),
                    Region = tile.Region,
                    Tiling = tile.Tiling,
                    Offset = tile.Offset,
                    Solid = tile.Solid
                };
                foreach (Material material in tile.Materials)
                    output.Slots[material] = slot;
            }

            var blit = new Material(Shader.Find(BlitShaderName)) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                for (int p = 0; p < properties.Count; p++)
                {
                    progress?.Invoke("Baking atlas " + properties[p].Name, (float)p / properties.Count);
                    Texture2D atlas = Compose(blit, properties[p], tiles, layout, settings);
                    output.Atlases.Add(new FMeshBakeResults.Atlas { propertyName = properties[p].Name, texture = atlas });
                }
            }
            finally
            {
                Object.DestroyImmediate(blit);
            }

            return output;
        }

        #endregion

        #region Planning

        private static List<PropertyPlan> PlanProperties(FMeshBakeSettings settings, Material resultMaterial,
            IEnumerable<Material> sources)
        {
            var names = new List<string>(StandardProperties);
            foreach (string custom in settings.customShaderPropNames)
                if (!string.IsNullOrWhiteSpace(custom) && !names.Contains(custom.Trim()))
                    names.Add(custom.Trim());
            names.RemoveAll(n => settings.texturePropNamesToIgnore.Contains(n));
            if (names.Contains("_BaseMap") && names.Contains("_MainTex") && HasTextureProperty(resultMaterial, "_BaseMap"))
                names.Remove("_MainTex");

            var plans = new List<PropertyPlan>();
            List<Material> sourceList = sources.Where(m => m != null).ToList();
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                if (!HasTextureProperty(resultMaterial, name))
                    continue;
                if (!sourceList.Any(m => GetSourceTexture(m, name) != null))
                    continue;

                bool isNormal = IsNormalProperty(name);
                plans.Add(new PropertyPlan
                {
                    Name = name,
                    Index = i,
                    IsNormal = isNormal,
                    IsColor = !isNormal && IsColorProperty(name)
                });
            }
            return plans;
        }

        private static List<Tile> BuildTiles(FMeshBakeSettings settings, List<PropertyPlan> properties,
            IReadOnlyDictionary<Material, Rect> meshUVBounds)
        {
            var byKey = new Dictionary<string, Tile>();
            var tiles = new List<Tile>();

            foreach (KeyValuePair<Material, Rect> pair in meshUVBounds)
            {
                Material material = pair.Key;
                var textures = properties.Select(p => GetSourceTexture(material, p.Name)).ToList();
                bool solid = textures.All(t => t == null);

                Vector2 tiling = MainTiling(material, out Vector2 offset);
                Rect region = solid ? new Rect(0f, 0f, 1f, 1f) : ComputeRegion(settings, pair.Value, tiling, offset);
                string key = BuildKey(settings, properties, material, textures, region, solid);

                if (!byKey.TryGetValue(key, out Tile tile))
                {
                    tile = new Tile
                    {
                        Representative = material,
                        Region = region,
                        Tiling = tiling,
                        Offset = offset,
                        Solid = solid,
                        Size = solid ? new Vector2Int(SolidTileSize, SolidTileSize) : ComputeTileSize(settings, textures, region)
                    };
                    byKey.Add(key, tile);
                    tiles.Add(tile);
                }
                tile.Materials.Add(material);
            }

            return tiles;
        }

        private static Rect ComputeRegion(FMeshBakeSettings settings, Rect meshUV, Vector2 tiling, Vector2 offset)
        {
            Rect uv = settings.considerMeshUVs ? meshUV : new Rect(0f, 0f, 1f, 1f);
            Vector2 a = uv.min * tiling + offset;
            Vector2 b = uv.max * tiling + offset;
            Rect region = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

            // strip atlases keep the original coordinate on the strip axis and let the sampler wrap it
            if (settings.packingAlgorithm == FAtlasPackingAlgorithm.HorizontalStrips)
                region = new Rect(0f, region.y, 1f, region.height);
            if (settings.packingAlgorithm == FAtlasPackingAlgorithm.VerticalStrips)
                region = new Rect(region.x, 0f, region.width, 1f);

            // a collapsed uv range (all vertices on one texel) still needs a non-zero region to divide by
            if (region.width < 1e-5f)
                region = new Rect(region.x - 0.0005f, region.y, 0.001f, region.height);
            if (region.height < 1e-5f)
                region = new Rect(region.x, region.y - 0.0005f, region.width, 0.001f);
            return region;
        }

        private static Vector2Int ComputeTileSize(FMeshBakeSettings settings, List<Texture> textures, Rect region)
        {
            int texW = textures.Where(t => t != null).Max(t => t.width);
            int texH = textures.Where(t => t != null).Max(t => t.height);
            int w = ScaleAxis(texW, region.width, settings.maxTilingBakeSize);
            int h = ScaleAxis(texH, region.height, settings.maxTilingBakeSize);

            bool freePacking = settings.packingAlgorithm == FAtlasPackingAlgorithm.TightPacker ||
                               settings.packingAlgorithm == FAtlasPackingAlgorithm.FastShelfPacker;
            if (settings.resizePowerOfTwoTextures && freePacking && settings.atlasPadding > 0)
            {
                if (Mathf.IsPowerOfTwo(w))
                    w = Mathf.Max(MinTileSize, w - 2 * settings.atlasPadding);
                if (Mathf.IsPowerOfTwo(h))
                    h = Mathf.Max(MinTileSize, h - 2 * settings.atlasPadding);
            }
            return new Vector2Int(w, h);
        }

        private static int ScaleAxis(int textureSize, float regionSize, int maxTilingBakeSize)
        {
            int size = Mathf.RoundToInt(textureSize * regionSize);
            if (regionSize > 1f + 1e-4f)
                size = Mathf.Min(size, maxTilingBakeSize);
            return Mathf.Max(MinTileSize, size);
        }

        private static string BuildKey(FMeshBakeSettings settings, List<PropertyPlan> properties, Material material,
            List<Texture> textures, Rect region, bool solid)
        {
            var sb = new StringBuilder();
            foreach (Texture t in textures)
                sb.Append(t != null ? t.GetInstanceID() : 0).Append('|');
            if (!solid)
                sb.Append(region.xMin.ToString("R")).Append(',').Append(region.yMin.ToString("R")).Append(',')
                  .Append(region.width.ToString("R")).Append(',').Append(region.height.ToString("R"));
            foreach (PropertyPlan p in properties)
                sb.Append('|').Append(SourceTint(settings, material, p, GetSourceTexture(material, p.Name) != null));
            return sb.ToString();
        }

        #endregion

        #region Composition

        private static Texture2D Compose(Material blit, PropertyPlan property, List<Tile> tiles, FAtlasLayout layout,
            FMeshBakeSettings settings)
        {
            bool linearProject = QualitySettings.activeColorSpace == ColorSpace.Linear;
            var descriptor = new RenderTextureDescriptor(layout.Size.x, layout.Size.y, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = property.IsColor && linearProject
            };
            RenderTexture rt = RenderTexture.GetTemporary(descriptor);
            RenderTexture previous = RenderTexture.active;
            Texture2D readback = null;
            try
            {
                RenderTexture.active = rt;
                GL.Clear(false, true, ClearColor(property));
                GL.PushMatrix();
                GL.LoadPixelMatrix(0f, layout.Size.x, 0f, layout.Size.y);
                foreach (Tile tile in tiles)
                    DrawTile(blit, property, tile, layout.Bleed, settings);
                GL.PopMatrix();

                readback = new Texture2D(layout.Size.x, layout.Size.y, TextureFormat.RGBA32, false, !property.IsColor);
                readback.ReadPixels(new Rect(0, 0, layout.Size.x, layout.Size.y), 0, 0);
                readback.Apply();
                return SaveAtlas(readback, property, settings, layout.Size);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                if (readback != null)
                    Object.DestroyImmediate(readback);
            }
        }

        private static void DrawTile(Material blit, PropertyPlan property, Tile tile, int bleed, FMeshBakeSettings settings)
        {
            Texture source = GetSourceTexture(tile.Representative, property.Name);
            bool hasTexture = source != null && !tile.Solid;
            RectInt px = tile.Pixels;

            blit.SetTexture("_MainTex", hasTexture ? source : Texture2D.whiteTexture);
            blit.SetVector("_Region", hasTexture ? new Vector4(tile.Region.x, tile.Region.y, tile.Region.width, tile.Region.height) : new Vector4(0f, 0f, 1f, 1f));
            blit.SetVector("_HalfTexel", new Vector4(0.5f / px.width, 0.5f / px.height, 0f, 0f));
            blit.SetVector("_Tint", SourceTint(settings, tile.Representative, property, hasTexture));
            blit.SetFloat("_PackedNormal", hasTexture && property.IsNormal && IsPackedNormal(source) ? 1f : 0f);
            blit.SetPass(property.IsNormal ? NormalPass : ColorPass);

            float bx = settings.packingAlgorithm == FAtlasPackingAlgorithm.HorizontalStrips ? 0 : bleed;
            float by = settings.packingAlgorithm == FAtlasPackingAlgorithm.VerticalStrips ? 0 : bleed;
            float x0 = px.xMin - bx, x1 = px.xMax + bx, y0 = px.yMin - by, y1 = px.yMax + by;
            float u0 = -bx / px.width, u1 = 1f + bx / px.width, v0 = -by / px.height, v1 = 1f + by / px.height;

            GL.Begin(GL.QUADS);
            GL.TexCoord2(u0, v0); GL.Vertex3(x0, y0, 0f);
            GL.TexCoord2(u0, v1); GL.Vertex3(x0, y1, 0f);
            GL.TexCoord2(u1, v1); GL.Vertex3(x1, y1, 0f);
            GL.TexCoord2(u1, v0); GL.Vertex3(x1, y0, 0f);
            GL.End();
        }

        private static Texture2D SaveAtlas(Texture2D readback, PropertyPlan property, FMeshBakeSettings settings, Vector2Int size)
        {
            string path = $"{settings.outputFolder}/{settings.baseName}-mat-{property.Name}-atlas-{property.Index}.png";
            File.WriteAllBytes(path, readback.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = property.IsNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = property.IsColor;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(size.x, size.y)), 32, 16384);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        #endregion

        #region Material queries

        public static bool HasTextureProperty(Material material, string name)
        {
            if (material == null || material.shader == null)
                return false;
            int index = material.shader.FindPropertyIndex(name);
            return index >= 0 && material.shader.GetPropertyType(index) == ShaderPropertyType.Texture;
        }

        public static Texture GetSourceTexture(Material material, string name)
        {
            if (material == null)
                return null;
            if (HasTextureProperty(material, name) && material.GetTexture(name) != null)
                return material.GetTexture(name);
            if (Aliases.TryGetValue(name, out string alias) && HasTextureProperty(material, alias))
                return material.GetTexture(alias);
            return null;
        }

        private static Vector2 MainTiling(Material material, out Vector2 offset)
        {
            offset = Vector2.zero;
            if (material == null)
                return Vector2.one;
            string main = HasTextureProperty(material, "_BaseMap") ? "_BaseMap" : HasTextureProperty(material, "_MainTex") ? "_MainTex" : null;
            if (main == null)
                return Vector2.one;
            offset = material.GetTextureOffset(main);
            return material.GetTextureScale(main);
        }

        private static Vector4 SourceTint(FMeshBakeSettings settings, Material material, PropertyPlan property, bool hasTexture)
        {
            bool blend = settings.considerNonTextureProperties && material != null;
            string name = property.Name;

            if (property.IsNormal)
                return hasTexture ? Vector4.one : new Vector4(0.5f, 0.5f, 1f, 1f);

            if (name == "_BaseMap" || name == "_MainTex")
                return blend ? ToShaderColor(BaseColor(material)) : Vector4.one;

            if (name == "_EmissionMap")
            {
                if (!blend)
                    return hasTexture ? Vector4.one : new Vector4(0f, 0f, 0f, 1f);
                bool emissive = material.IsKeywordEnabled("_EMISSION") && material.HasProperty("_EmissionColor");
                Color emission = emissive ? material.GetColor("_EmissionColor") : Color.black;
                return ToShaderColor(emission);
            }

            if (name == "_MetallicGlossMap" || name == "_SpecGlossMap")
            {
                if (hasTexture)
                    return Vector4.one;
                if (!blend)
                    return new Vector4(0f, 0f, 0f, 1f);
                float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
                float smoothness = material.HasProperty("_Smoothness") ? material.GetFloat("_Smoothness")
                    : material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;
                return new Vector4(metallic, metallic, metallic, smoothness);
            }

            if (name == "_ParallaxMap")
                return hasTexture ? Vector4.one : new Vector4(0.5f, 0.5f, 0.5f, 1f);

            return Vector4.one;
        }

        private static Color BaseColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color"))
                return material.GetColor("_Color");
            return Color.white;
        }

        private static Vector4 ToShaderColor(Color color)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? (Vector4)color.linear : (Vector4)color;
        }

        private static Color ClearColor(PropertyPlan property)
        {
            if (property.IsNormal)
                return new Color(0.5f, 0.5f, 1f, 1f);
            if (property.Name == "_EmissionMap" || property.Name == "_MetallicGlossMap" || property.Name == "_SpecGlossMap")
                return Color.black;
            return Color.white;
        }

        private static bool IsNormalProperty(string name)
        {
            return name.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsColorProperty(string name)
        {
            return name != "_MetallicGlossMap" && name != "_SpecGlossMap" && name != "_OcclusionMap" &&
                   name != "_ParallaxMap" && name != "_DetailMask" &&
                   name.IndexOf("Mask", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool IsPackedNormal(Texture texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            return AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType == TextureImporterType.NormalMap;
        }

        #endregion
    }
}
#endif
