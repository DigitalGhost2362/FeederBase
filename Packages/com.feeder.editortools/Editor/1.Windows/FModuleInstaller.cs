using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Feeder
{
    /// <summary>Installs and removes the optional Editor Tools modules through the Package Manager.</summary>
    public static class FModuleInstaller
    {
        private static ListRequest _listRequest;
        private static AddRequest _addRequest;
        private static RemoveRequest _removeRequest;
        private static HashSet<string> _installed;

        public static bool IsBusy => _addRequest != null || _removeRequest != null || _listRequest != null;

        public static string BusyLabel { get; private set; }

        public static bool IsInstalled(FModuleCatalog.Module module)
        {
            if (_installed == null)
            {
                RefreshInstalled();
                return false;
            }

            return _installed.Contains(module.PackageName);
        }

        public static void RefreshInstalled()
        {
            if (_listRequest != null) return;

            BusyLabel = "Đang đọc danh sách package...";
            _listRequest = Client.List(true, false);
            EditorApplication.update += PollList;
        }

        public static void Install(FModuleCatalog.Module module)
        {
            if (IsBusy || module.LocalOnly) return;

            BusyLabel = "Đang cài " + module.DisplayName + "...";
            _addRequest = Client.Add(module.GitUrl);
            EditorApplication.update += PollAdd;
        }

        public static void Remove(FModuleCatalog.Module module)
        {
            if (IsBusy) return;

            BusyLabel = "Đang gỡ " + module.DisplayName + "...";
            _removeRequest = Client.Remove(module.PackageName);
            EditorApplication.update += PollRemove;
        }

        private static void PollList()
        {
            if (_listRequest == null || !_listRequest.IsCompleted) return;

            EditorApplication.update -= PollList;
            var found = new HashSet<string>();
            if (_listRequest.Status == StatusCode.Success)
            {
                foreach (var package in _listRequest.Result)
                    found.Add(package.name);
            }
            else if (_listRequest.Error != null)
            {
                Debug.LogError("[Editor Tools] Không đọc được danh sách package: " + _listRequest.Error.message);
            }

            _installed = found;
            _listRequest = null;
            BusyLabel = null;
            Repaint();
        }

        private static void PollAdd()
        {
            if (_addRequest == null || !_addRequest.IsCompleted) return;

            EditorApplication.update -= PollAdd;
            if (_addRequest.Status == StatusCode.Success)
                Debug.Log("[Editor Tools] Đã cài " + _addRequest.Result.displayName + ".");
            else if (_addRequest.Error != null)
                Debug.LogError("[Editor Tools] Cài module thất bại: " + _addRequest.Error.message);

            _addRequest = null;
            BusyLabel = null;
            RefreshInstalled();
        }

        private static void PollRemove()
        {
            if (_removeRequest == null || !_removeRequest.IsCompleted) return;

            EditorApplication.update -= PollRemove;
            if (_removeRequest.Status != StatusCode.Success && _removeRequest.Error != null)
                Debug.LogError("[Editor Tools] Gỡ module thất bại: " + _removeRequest.Error.message);

            _removeRequest = null;
            BusyLabel = null;
            RefreshInstalled();
        }

        private static void Repaint()
        {
            if (EditorWindow.HasOpenInstances<FModuleWindow>())
                EditorWindow.GetWindow<FModuleWindow>().Repaint();
        }
    }
}
