using UnityEditor;
using UnityEngine;

namespace Feeder
{
    [InitializeOnLoad]
    internal static class FMeshDedupModule
    {
        public const string DeduplicateMeshToolPath = "Optimize/Deduplicate Mesh Tool";

        static FMeshDedupModule()
        {
            FToolRegistry.Register(
                DeduplicateMeshToolPath,
                "meshdedup",
                () => ScriptableObject.CreateInstance<FDeduplicateMeshTool>());
        }
    }
}
