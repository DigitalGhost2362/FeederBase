using UnityEditor;
using UnityEngine;

namespace Feeder
{
    [InitializeOnLoad]
    internal static class FRepackFbxModule
    {
        public const string RepackModelsToolPath = "Optimize/Repack Models Tool";

        static FRepackFbxModule()
        {
            FToolRegistry.Register(
                RepackModelsToolPath,
                "repackfbx",
                () => ScriptableObject.CreateInstance<FRepackModelsTool>());
        }
    }
}
