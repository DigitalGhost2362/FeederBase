using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Sirenix.Utilities;

namespace Feeder
{
    public class FMenuEditorWindow : OdinMenuEditorWindow
    {
        [MenuItem("Tools/Feeder/Editor Tools/Tool Hub", priority = 0)]
        private static void OpenWindow()
        {
            var window = GetWindow<FMenuEditorWindow>();
            window.titleContent = FIconCatalog.CreateWindowTitle("Editor Tools", FIconCatalog.WindowMenuTitleIcon);
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 600);
            window.Show();
        }

        protected override void OnDisable()
        {
            FMeshPreviewDrawer.Cleanup();
            base.OnDisable();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = FMenuTreeStyle.Create();
            FMenuCatalog.AddTools(tree);

            return tree;
        }
    }
}
