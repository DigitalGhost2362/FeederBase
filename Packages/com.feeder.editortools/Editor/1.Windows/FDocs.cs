using System.IO;
using UnityEditor;
using UnityEngine;

namespace Feeder
{
    /// <summary>
    /// Opens the Editor Tools documentation. A local copy inside the project wins over the
    /// published site, so a checkout of the repository reads the docs that match its code and
    /// keeps working offline.
    /// </summary>
    public static class FDocs
    {
        public const string SiteUrl = "https://bingoboiz.github.io/FeederEditorTools/";
        public const string IndexPage = "index.html";
        public const string ModulesPage = "modules.html";

        public static string ToolPage(string fileName) => "tools/" + fileName;

        [MenuItem("Tools/Feeder/Editor Tools/Documentation", priority = 2)]
        public static void OpenIndex() => Open(IndexPage);

        public static void Open(string page)
        {
            string local = FindLocal(page);
            Application.OpenURL(local != null ? new System.Uri(local).AbsoluteUri : SiteUrl + page);
        }

        private static string FindLocal(string page)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot)) return null;

            string candidate = Path.Combine(projectRoot, "docs", page.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(candidate) ? candidate : null;
        }
    }
}
