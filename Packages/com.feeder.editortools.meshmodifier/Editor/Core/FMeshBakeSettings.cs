#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feeder
{
    /// <summary>
    /// Serializable settings for a combined-mesh bake: atlas options, mesh options and output location.
    /// </summary>
    [Serializable]
    public class FMeshBakeSettings
    {
        public static readonly int[] AtlasSizes = { 512, 1024, 2048, 4096, 8192 };
        public static readonly string[] AtlasSizeLabels = { "512", "1024", "2048", "4096", "8192" };

        // ---- Atlas ----
        public int maxAtlasSize = 4096;
        public int atlasPadding = 2;
        public FAtlasPackingAlgorithm packingAlgorithm = FAtlasPackingAlgorithm.TightPacker;
        public bool resizePowerOfTwoTextures = false;
        /// <summary>Bakes only the UV-covered region of each texture, so tiling and out-of-bounds UVs still work in an atlas.</summary>
        public bool considerMeshUVs = true;
        public bool considerNonTextureProperties = false;
        public int maxTilingBakeSize = 1024;
        public List<string> customShaderPropNames = new List<string>();
        public List<string> texturePropNamesToIgnore = new List<string>();

        // ---- Mesh ----
        public bool generateLightmapUV2 = false;
        public FMeshPivotLocation pivotLocation = FMeshPivotLocation.BoundsCenter;
        public Vector3 customPivot = Vector3.zero;

        // ---- Output ----
        public string outputFolder = "Assets/Generated Combined Meshes";
        public string baseName = "CombinedMesh";
        public bool placeResultInScene = true;

        public string PrefabPath => $"{outputFolder}/{baseName}.prefab";
        public string MeshPath => $"{outputFolder}/{baseName}-mesh.asset";
        public string MaterialPath => $"{outputFolder}/{baseName}-mat.mat";
        public string BakeResultsPath => $"{outputFolder}/{baseName}-bakeResults.asset";
    }
}
#endif
