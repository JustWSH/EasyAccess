using global::System;
using global::System.Collections.Generic;
using global::System.IO;
using global::System.Runtime.InteropServices;
using EasyAccess.Infra;

namespace EasyAccess.System
{
    internal sealed class ExplorerFolder
    {
        public string Path { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public IntPtr Hwnd { get; set; }
    }

    internal sealed class ShellWindowsInterop
    {
        private readonly Logger _logger;

        public ShellWindowsInterop(Logger logger)
        {
            _logger = logger;
        }

        public List<ExplorerFolder> GetOpenFolders()
        {
            var folders = new List<ExplorerFolder>();

            try
            {
                var shellWindowsType = Type.GetTypeFromProgID("Shell.Application");
                if (shellWindowsType == null)
                {
                    _logger.Error("Failed to get Shell.Application type");
                    return folders;
                }

                dynamic shell = Activator.CreateInstance(shellWindowsType)!;
                dynamic windows = shell.Windows;

                for (int i = 0; i < windows.Count; i++)
                {
                    try
                    {
                        dynamic window = windows.Item(i);
                        if (window == null)
                            continue;

                        string url = window.LocationURL;
                        string title = window.LocationName;
                        long hwndLong = window.HWND;

                        if (string.IsNullOrEmpty(url))
                            continue;

                        var path = UrlToPath(url);
                        if (!string.IsNullOrEmpty(path) && IsDirectoryExists(path))
                        {
                            folders.Add(new ExplorerFolder
                            {
                                Path = path,
                                DisplayName = title,
                                WindowTitle = title,
                                Hwnd = new IntPtr(hwndLong)
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug($"Failed to get folder from window: {ex.Message}");
                    }
                }

                Marshal.ReleaseComObject(windows);
                Marshal.ReleaseComObject(shell);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to enumerate shell windows", ex);
            }

            return folders;
        }

        private static string UrlToPath(string url)
        {
            if (url.StartsWith("file:///"))
            {
                var path = url.Substring(8).Replace('/', '\\');
                try
                {
                    return Uri.UnescapeDataString(path);
                }
                catch
                {
                    return path;
                }
            }
            return string.Empty;
        }

        private static bool IsDirectoryExists(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
