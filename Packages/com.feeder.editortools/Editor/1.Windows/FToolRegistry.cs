using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feeder
{
    /// <summary>
    /// Lets an optional module add its own tools to the Editor Tools menu tree without the
    /// core package referencing the module. A module registers from an [InitializeOnLoad]
    /// static constructor; the registry is also what draws the greyed-out placeholder for a
    /// module that is not installed.
    /// </summary>
    public static class FToolRegistry
    {
        public readonly struct Entry
        {
            public readonly string MenuPath;
            public readonly string ModuleId;
            public readonly Func<ScriptableObject> CreateTool;
            public readonly Texture2D Icon;

            public Entry(string menuPath, string moduleId, Func<ScriptableObject> createTool, Texture2D icon)
            {
                MenuPath = menuPath;
                ModuleId = moduleId;
                CreateTool = createTool;
                Icon = icon;
            }
        }

        private static readonly List<Entry> Entries = new List<Entry>();

        public static IReadOnlyList<Entry> All => Entries;

        public static void Register(string menuPath, string moduleId, Func<ScriptableObject> createTool, Texture2D icon = null)
        {
            if (string.IsNullOrEmpty(menuPath)) throw new ArgumentException("menuPath is empty.", nameof(menuPath));
            if (createTool == null) throw new ArgumentNullException(nameof(createTool));

            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].MenuPath == menuPath)
                {
                    Entries[i] = new Entry(menuPath, moduleId, createTool, icon);
                    return;
                }
            }

            Entries.Add(new Entry(menuPath, moduleId, createTool, icon));
        }

        public static bool IsRegistered(string menuPath)
        {
            for (int i = 0; i < Entries.Count; i++)
                if (Entries[i].MenuPath == menuPath) return true;
            return false;
        }
    }
}
