#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Feeder
{
    /// <summary>
    /// Bakes the targets into one combined mesh: packs source textures into atlases, remaps UVs,
    /// merges geometry, then writes mesh, material, atlases, bake results and a prefab.
    /// Source objects are never modified.
    /// </summary>
    public static class FMeshBakeService
    {
        private const string ProgressTitle = "Mesh Modifier";

        public static FMeshBakeResult Bake(FMeshBakeSession session, FMeshBakeSettings settings, out string error)
        {
            error = Validate(session, settings);
            if (error != null)
                return null;

            string folder = settings.outputFolder.Replace('\\', '/').TrimEnd('/');
            var tempInstances = new List<GameObject>();
            var transientMaterials = new List<Material>();
            FMeshBakeResult result = null;

            try
            {
                EnsureFolder(folder);

                bool anySceneInput;
                List<GameObject> objsToMesh = CollectSourceObjects(session.Targets, tempInstances, out anySceneInput);
                if (objsToMesh.Count == 0)
                {
                    error = "Không tìm thấy MeshRenderer nào có mesh hợp lệ trong các mục tiêu.";
                    return null;
                }

                List<string> unreadable = objsToMesh
                    .Where(o => !o.GetComponent<MeshFilter>().sharedMesh.isReadable)
                    .Select(o => o.name).ToList();
                if (unreadable.Count > 0)
                {
                    string names = string.Join(", ", unreadable.Take(10)) +
                                   (unreadable.Count > 10 ? $" ... (+{unreadable.Count - 10})" : string.Empty);
                    error = "Mesh không đọc được (Read/Write đang tắt): " + names +
                            ". Hãy bật Read/Write trong import settings của model rồi bake lại.";
                    return null;
                }

                Material resultMaterial = CreateCombinedMaterialAsset(settings, objsToMesh);
                Material fallbackMaterial = null;
                List<FMeshCombineSource> sources = BuildSources(objsToMesh, () =>
                {
                    if (fallbackMaterial == null)
                    {
                        fallbackMaterial = new Material(resultMaterial.shader) { name = "(none)", hideFlags = HideFlags.HideAndDontSave };
                        transientMaterials.Add(fallbackMaterial);
                    }
                    return fallbackMaterial;
                });

                FAtlasBakeOutput atlas = FAtlasBaker.Bake(settings, resultMaterial, CollectMeshUVBounds(sources),
                    (msg, progress) => EditorUtility.DisplayProgressBar(ProgressTitle, msg, progress * 0.7f));
                ApplyAtlasesToMaterial(resultMaterial, atlas, settings);

                EditorUtility.DisplayProgressBar(ProgressTitle, "Combining meshes", 0.75f);
                bool withTangents = sources.Any(s => s.Mesh.HasVertexAttribute(VertexAttribute.Tangent));
                Vector3 pivot = FMeshCombiner.ComputePivot(sources, settings.pivotLocation, settings.customPivot);
                Mesh combined = FMeshCombiner.Combine(sources, atlas.Remap, pivot, withTangents, settings.generateLightmapUV2);
                combined.name = settings.baseName + "-mesh";
                SaveAsset(combined, settings.MeshPath);
                Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(settings.MeshPath);

                EditorUtility.DisplayProgressBar(ProgressTitle, "Writing prefab", 0.9f);
                SaveBakeResults(settings, resultMaterial, atlas);
                GameObject prefab = SavePrefab(settings, meshAsset, resultMaterial, pivot);
                if (settings.placeResultInScene && anySceneInput)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    Undo.RegisterCreatedObjectUndo(instance, "Bake Combined Mesh");
                }

                result = new FMeshBakeResult
                {
                    PrefabPath = settings.PrefabPath,
                    MeshPath = settings.MeshPath,
                    MaterialPath = settings.MaterialPath,
                    BakeResultsPath = settings.BakeResultsPath
                };
                CollectAtlasPaths(resultMaterial, result.AtlasPaths);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                error = "Bake gặp lỗi ngoại lệ: " + e.Message + "\n\nXem Console để biết chi tiết.";
                return null;
            }
            finally
            {
                foreach (GameObject temp in tempInstances)
                    if (temp != null)
                        Object.DestroyImmediate(temp);
                foreach (Material temp in transientMaterials)
                    if (temp != null)
                        Object.DestroyImmediate(temp);

                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (result != null)
                    ReportSuccess(result);
            }
        }

        private static string Validate(FMeshBakeSession session, FMeshBakeSettings settings)
        {
            if (session.Targets.Count == 0)
                return "Hãy thêm ít nhất một mục tiêu (object trong scene hoặc prefab).";
            if (string.IsNullOrWhiteSpace(settings.baseName))
                return "Base Name không được để trống.";
            if (settings.baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return "Base Name chứa ký tự không hợp lệ cho tên file.";

            string folder = settings.outputFolder.Replace('\\', '/').TrimEnd('/');
            if (!folder.StartsWith("Assets/") && folder != "Assets")
                return "Output Folder phải nằm trong thư mục Assets của project.";
            return null;
        }

        private static List<GameObject> CollectSourceObjects(List<Object> targets,
            List<GameObject> tempInstances, out bool anySceneInput)
        {
            anySceneInput = false;
            var objsToMesh = new List<GameObject>();
            foreach (Object target in targets)
            {
                var go = target as GameObject;
                if (go == null)
                    continue;

                GameObject root = go;
                if (EditorUtility.IsPersistent(go))
                {
                    // Prefab asset: bake from a transient scene instance, removed in finally.
                    root = (GameObject)PrefabUtility.InstantiatePrefab(go);
                    root.hideFlags = HideFlags.HideInHierarchy;
                    tempInstances.Add(root);
                }
                else
                {
                    anySceneInput = true;
                }

                foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null)
                        continue;
                    if (!objsToMesh.Contains(mr.gameObject))
                        objsToMesh.Add(mr.gameObject);
                }
            }

            return objsToMesh;
        }

        private static List<FMeshCombineSource> BuildSources(List<GameObject> objsToMesh, Func<Material> fallbackMaterial)
        {
            var sources = new List<FMeshCombineSource>();
            foreach (GameObject go in objsToMesh)
            {
                Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
                Material[] shared = go.GetComponent<MeshRenderer>().sharedMaterials;
                var materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = shared.Length > 0 ? shared[Mathf.Min(i, shared.Length - 1)] : null;
                    materials[i] = material != null ? material : fallbackMaterial();
                }
                sources.Add(new FMeshCombineSource { Transform = go.transform, Mesh = mesh, SubMeshMaterials = materials });
            }
            return sources;
        }

        private static Dictionary<Material, Rect> CollectMeshUVBounds(List<FMeshCombineSource> sources)
        {
            var bounds = new Dictionary<Material, Rect>();
            var uvs = new List<Vector2>();
            foreach (FMeshCombineSource source in sources)
            {
                source.Mesh.GetUVs(0, uvs);
                for (int sub = 0; sub < source.Mesh.subMeshCount; sub++)
                {
                    Material material = source.SubMeshMaterials[Mathf.Min(sub, source.SubMeshMaterials.Length - 1)];
                    Rect rect = uvs.Count == source.Mesh.vertexCount
                        ? FMeshUVUtility.GetBounds(uvs, source.Mesh.GetTriangles(sub))
                        : new Rect(0f, 0f, 0f, 0f);
                    bounds[material] = bounds.TryGetValue(material, out Rect existing) ? FMeshUVUtility.Union(existing, rect) : rect;
                }
            }
            return bounds;
        }

        /// <summary>
        /// Clones the first source material so the combined material keeps its shader, keywords and non-texture settings.
        /// </summary>
        private static Material CreateCombinedMaterialAsset(FMeshBakeSettings settings, List<GameObject> objsToMesh)
        {
            Material newMat = null;
            Renderer firstRenderer = objsToMesh[0].GetComponent<Renderer>();
            if (firstRenderer != null && firstRenderer.sharedMaterial != null)
                newMat = new Material(firstRenderer.sharedMaterial);

            if (newMat == null)
            {
                Material pipelineDefault = GraphicsSettings.currentRenderPipeline != null
                    ? GraphicsSettings.currentRenderPipeline.defaultMaterial : null;
                Shader fallback = pipelineDefault != null ? pipelineDefault.shader : Shader.Find("Standard");
                if (fallback == null)
                    fallback = Shader.Find("Diffuse");
                newMat = new Material(fallback);
            }

            SaveAsset(newMat, settings.MaterialPath);
            return AssetDatabase.LoadAssetAtPath<Material>(settings.MaterialPath);
        }

        private static void ApplyAtlasesToMaterial(Material material, FAtlasBakeOutput atlas, FMeshBakeSettings settings)
        {
            foreach (FMeshBakeResults.Atlas entry in atlas.Atlases)
            {
                SetAtlas(material, entry.propertyName, entry.texture);
                if (entry.propertyName == "_BaseMap")
                    SetAtlas(material, "_MainTex", entry.texture);
                if (entry.propertyName == "_MainTex")
                    SetAtlas(material, "_BaseMap", entry.texture);

                switch (entry.propertyName)
                {
                    case "_BumpMap":
                    case "_NormalMap":
                        material.EnableKeyword("_NORMALMAP");
                        break;
                    case "_MetallicGlossMap":
                        material.EnableKeyword("_METALLICSPECGLOSSMAP");
                        material.EnableKeyword("_METALLICGLOSSMAP");
                        break;
                    case "_SpecGlossMap":
                        material.EnableKeyword("_SPECGLOSSMAP");
                        break;
                    case "_OcclusionMap":
                        material.EnableKeyword("_OCCLUSIONMAP");
                        break;
                    case "_ParallaxMap":
                        material.EnableKeyword("_PARALLAXMAP");
                        break;
                    case "_EmissionMap":
                        material.EnableKeyword("_EMISSION");
                        material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                        break;
                }
            }

            if (settings.considerNonTextureProperties)
            {
                // tints are already multiplied into the atlas pixels, so the combined material must not tint again
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", Color.white);
                if (atlas.Atlases.Any(a => a.propertyName == "_EmissionMap") && material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", Color.white);
            }

            EditorUtility.SetDirty(material);
        }

        private static void SetAtlas(Material material, string propertyName, Texture2D texture)
        {
            if (!FAtlasBaker.HasTextureProperty(material, propertyName))
                return;
            material.SetTexture(propertyName, texture);
            material.SetTextureScale(propertyName, Vector2.one);
            material.SetTextureOffset(propertyName, Vector2.zero);
        }

        private static void SaveBakeResults(FMeshBakeSettings settings, Material resultMaterial, FAtlasBakeOutput atlas)
        {
            var results = ScriptableObject.CreateInstance<FMeshBakeResults>();
            results.resultMaterial = resultMaterial;
            results.atlasSize = atlas.AtlasSize;
            results.packingAlgorithm = atlas.Algorithm;
            results.atlases.AddRange(atlas.Atlases);
            foreach (KeyValuePair<Material, FAtlasSlot> pair in atlas.Slots)
            {
                if (!EditorUtility.IsPersistent(pair.Key))
                    continue;
                results.entries.Add(new FMeshBakeResults.Entry
                {
                    sourceMaterial = pair.Key,
                    atlasRect = pair.Value.AtlasRect,
                    sourceRegion = pair.Value.Region
                });
            }
            SaveAsset(results, settings.BakeResultsPath);
        }

        private static GameObject SavePrefab(FMeshBakeSettings settings, Mesh mesh, Material material, Vector3 pivot)
        {
            var root = new GameObject(settings.baseName);
            try
            {
                var child = new GameObject(settings.baseName + "-mesh");
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = pivot;
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.AddComponent<MeshRenderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(root, settings.PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SaveAsset(Object asset, string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void CollectAtlasPaths(Material combinedMaterial, List<string> atlasPaths)
        {
            if (combinedMaterial == null || combinedMaterial.shader == null)
                return;

            int propertyCount = ShaderUtil.GetPropertyCount(combinedMaterial.shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(combinedMaterial.shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                Texture tex = combinedMaterial.GetTexture(ShaderUtil.GetPropertyName(combinedMaterial.shader, i));
                if (tex == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(tex);
                if (!string.IsNullOrEmpty(path) && !atlasPaths.Contains(path))
                    atlasPaths.Add(path);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent))
                return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static void ReportSuccess(FMeshBakeResult result)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
            if (prefab != null)
                EditorGUIUtility.PingObject(prefab);

            var lines = new List<string>
            {
                $"<color=#4CE04C><b>[Feeder Mesh Modifier]</b></color> Bake completed. Created assets:",
                "  " + result.PrefabPath,
                "  " + result.MeshPath,
                "  " + result.MaterialPath,
                "  " + result.BakeResultsPath
            };
            foreach (string atlasPath in result.AtlasPaths)
                lines.Add("  " + atlasPath);

            Debug.Log(string.Join("\n", lines), prefab);
        }
    }
}
#endif
