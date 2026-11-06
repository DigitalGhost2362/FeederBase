using System.Collections.Generic;

namespace Feeder
{
    /// <summary>
    /// The optional modules of Editor Tools. Each one is a separate UPM package so a project
    /// only pulls the heavy dependencies it actually uses. Local-only modules are never
    /// published and show no download button.
    /// </summary>
    public static class FModuleCatalog
    {
        public const string RepositoryUrl = "https://github.com/BingoBoiz/FeederEditorTools.git";

        public sealed class Module
        {
            public string Id;
            public string DisplayName;
            public string Summary;
            public string Requires;
            public string[] MenuPaths;
            public bool LocalOnly;

            public string PackageName => "com.feeder.editortools." + Id;
            public string GitUrl => RepositoryUrl + "?path=/Packages/" + PackageName;
        }

        public static readonly IReadOnlyList<Module> All = new List<Module>
        {
            new Module
            {
                Id = "meshdedup",
                DisplayName = "Mesh Dedup & Align",
                Summary = "So sánh mesh trùng nhau, canh mesh về đúng vị trí bằng thuật toán Kabsch/ICP.",
                Requires = "Kèm thư viện MathNet.Numerics (~2 MB).",
                MenuPaths = new[] { "Optimize/Deduplicate Mesh Tool" }
            },
            new Module
            {
                Id = "repackfbx",
                DisplayName = "Repack Models (FBX)",
                Summary = "Xuất từng mesh ra file FBX cạnh asset gốc rồi trỏ lại tham chiếu trong scene.",
                Requires = "Cần package Unity com.unity.formats.fbx.",
                MenuPaths = new[] { "Optimize/Repack Models Tool" }
            },
            new Module
            {
                Id = "googlesheet",
                DisplayName = "GoogleSheet Importer",
                Summary = "Đọc Google Sheet qua service account, sinh class C# và ScriptableObject.",
                Requires = "Kèm Google Apis DLL và cần com.unity.nuget.newtonsoft-json.",
                MenuPaths = new string[0]
            },
            new Module
            {
                Id = "assetcache",
                DisplayName = "Asset Cache",
                Summary = "Cửa sổ duyệt và lọc nhanh toàn bộ asset trong project.",
                Requires = "Không cần dependency ngoài.",
                MenuPaths = new string[0]
            },
            new Module
            {
                Id = "meshmodifier",
                DisplayName = "Mesh Modifier",
                Summary = "Gộp nhiều mesh thành một và tách mesh thành nhiều phần.",
                Requires = "Không cần dependency ngoài.",
                MenuPaths = new string[0]
            }
        };

        public static Module Find(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }
    }
}
