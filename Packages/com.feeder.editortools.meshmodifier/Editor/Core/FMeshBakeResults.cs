#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feeder
{
    public sealed class FMeshBakeResults : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public Material sourceMaterial;
            public Rect atlasRect;
            public Rect sourceRegion;
        }

        [Serializable]
        public sealed class Atlas
        {
            public string propertyName;
            public Texture2D texture;
        }

        public Material resultMaterial;
        public Vector2Int atlasSize;
        public FAtlasPackingAlgorithm packingAlgorithm;
        public List<Entry> entries = new List<Entry>();
        public List<Atlas> atlases = new List<Atlas>();
    }
}
#endif
