using UnityEditor;
using UnityEngine;

namespace Feeder
{
    /// <summary>Lists the optional modules and installs or removes them.</summary>
    public sealed class FModuleWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Tools/Feeder/Editor Tools/Modules", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<FModuleWindow>();
            window.titleContent = new GUIContent("Modules");
            window.minSize = new Vector2(460f, 320f);
            window.Show();
            FModuleInstaller.RefreshInstalled();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Module tuỳ chọn", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Package lõi chỉ chứa tool không cần thư viện ngoài. Mỗi module dưới đây là một package riêng, cài khi cần.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(FModuleInstaller.IsBusy))
            {
                if (GUILayout.Button("Kiểm tra lại trạng thái", GUILayout.Height(20f)))
                    FModuleInstaller.RefreshInstalled();
            }

            if (FModuleInstaller.IsBusy)
                EditorGUILayout.HelpBox(FModuleInstaller.BusyLabel ?? "Đang xử lý...", MessageType.Info);

            EditorGUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var modules = FModuleCatalog.All;
            for (int i = 0; i < modules.Count; i++)
                DrawModule(modules[i]);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Mở tài liệu", GUILayout.Height(22f)))
                FDocs.Open(FDocs.ModulesPage);
        }

        private static void DrawModule(FModuleCatalog.Module module)
        {
            bool installed = FModuleInstaller.IsInstalled(module);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(module.DisplayName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(
                        module.LocalOnly ? "local only" : installed ? "đã cài" : "chưa cài",
                        EditorStyles.miniLabel,
                        GUILayout.Width(70f));
                }

                EditorGUILayout.LabelField(module.Summary, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(module.Requires, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(2f);

                if (module.LocalOnly)
                {
                    EditorGUILayout.HelpBox(
                        "Module này không phát hành kèm repo. Chép thủ công vào Packages/ sau khi có bản quyền.",
                        MessageType.None);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(FModuleInstaller.IsBusy))
                {
                    if (!installed && GUILayout.Button("Cài", GUILayout.Height(22f)))
                        FModuleInstaller.Install(module);
                    if (installed && GUILayout.Button("Gỡ", GUILayout.Height(22f)))
                        FModuleInstaller.Remove(module);
                    if (GUILayout.Button("Chép git URL", GUILayout.Height(22f), GUILayout.Width(110f)))
                        EditorGUIUtility.systemCopyBuffer = module.GitUrl;
                }
            }

            EditorGUILayout.Space(2f);
        }
    }
}
