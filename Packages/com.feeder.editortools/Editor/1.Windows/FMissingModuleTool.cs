using Sirenix.OdinInspector;
using UnityEngine;

namespace Feeder
{
    /// <summary>Placeholder shown in the tool tree for a tool whose module is not installed.</summary>
    public sealed class FMissingModuleTool : SerializedScriptableObject
    {
        private FModuleCatalog.Module _module;

        public void Setup(FModuleCatalog.Module module) => _module = module;

        [OnInspectorGUI]
        private void Draw()
        {
            if (_module == null) return;

            GUILayout.Space(4);
            FStylesUtils.DrawDescription(
                _module.DisplayName + " chưa được cài.\n\n" +
                _module.Summary + "\n" +
                _module.Requires);
            GUILayout.Space(8);

            if (_module.LocalOnly)
            {
                FStylesUtils.DrawDescription(
                    "Module này không phát hành kèm repo nên không có nút tải. " +
                    "Xem README của module để biết cách chép vào Packages/.");
                return;
            }

            if (GUILayout.Button("Cài module này", GUILayout.Height(28)))
                FModuleInstaller.Install(_module);

            GUILayout.Space(4);
            if (GUILayout.Button("Mở cửa sổ Modules", GUILayout.Height(22)))
                FModuleWindow.Open();
        }
    }
}
