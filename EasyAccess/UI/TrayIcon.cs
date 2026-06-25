using Microsoft.UI.Xaml;
using global::System;
using global::System.IO;
using global::System.Runtime.InteropServices;
using EasyAccess.Infra;
using EasyAccess.Util;

namespace EasyAccess.UI
{
    public sealed class TrayIcon : IDisposable
    {
        private readonly Window _mainWindow;
        private readonly IntPtr _hwnd;
        private readonly AppConfig _config;
        private readonly Action _saveConfig;
        private NotifyIconData _notifyIconData;
        private bool _disposed;
        private IntPtr _hIcon;

        public event Action? ExitRequested;

        public TrayIcon(Window mainWindow, AppConfig config, Action saveConfig)
        {
            _mainWindow = mainWindow;
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            _config = config;
            _saveConfig = saveConfig;

            _notifyIconData = new NotifyIconData
            {
                cbSize = Marshal.SizeOf<NotifyIconData>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
                uCallbackMessage = WM_TRAYICON,
                szTip = "EasyAccess"
            };

            LoadIcon();
            Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);

            SubclassWindow();
        }

        private void SubclassWindow()
        {
            _wndProc = WndProc;
            _oldWndProc = NativeMethods.GetWindowLongPtrW(_hwnd, NativeMethods.GWL_WNDPROC);
            NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProc));
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                int lParamInt = lParam.ToInt32();
                if (lParamInt == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                }
            }
            else if (msg == WM_COMMAND)
            {
                int commandId = wParam.ToInt32() & 0xFFFF;
                HandleCommand(commandId);
            }

            return NativeMethods.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void LoadIcon()
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "tray_icon.ico");
            if (File.Exists(iconPath))
            {
                _hIcon = LoadIconFromFile(iconPath);
            }
            else
            {
                _hIcon = LoadIcon(IntPtr.Zero, IDI_APPLICATION);
            }

            _notifyIconData.hIcon = _hIcon;
        }

        private void ShowContextMenu()
        {
            var menu = CreatePopupMenu();

            // Show Overlay toggle
            AppendMenu(menu, MF_STRING | (_config.ShowOverlayOnDetect ? MF_CHECKED : 0),
                ID_TOGGLE_OVERLAY, "显示 Overlay(&O)");
            AppendMenu(menu, MF_SEPARATOR, 0, "");

            // Log level submenu
            var logMenu = CreatePopupMenu();
            AppendMenu(logMenu, MF_STRING | (_config.LogLevel == "debug" ? MF_CHECKED : 0), ID_LOG_DEBUG, "Debug");
            AppendMenu(logMenu, MF_STRING | (_config.LogLevel == "info" ? MF_CHECKED : 0), ID_LOG_INFO, "Info");
            AppendMenu(logMenu, MF_STRING | (_config.LogLevel == "warn" ? MF_CHECKED : 0), ID_LOG_WARN, "Warn");
            AppendMenu(logMenu, MF_STRING | (_config.LogLevel == "error" ? MF_CHECKED : 0), ID_LOG_ERROR, "Error");
            AppendMenu(menu, MF_POPUP, (int)logMenu, "日志级别(&L)");

            // Max visible items submenu
            var itemsMenu = CreatePopupMenu();
            AppendMenu(itemsMenu, MF_STRING | (_config.MaxOverlayItems == 1 ? MF_CHECKED : 0), ID_ITEMS_1, "1");
            AppendMenu(itemsMenu, MF_STRING | (_config.MaxOverlayItems == 2 ? MF_CHECKED : 0), ID_ITEMS_2, "2");
            AppendMenu(itemsMenu, MF_STRING | (_config.MaxOverlayItems == 3 ? MF_CHECKED : 0), ID_ITEMS_3, "3");
            AppendMenu(itemsMenu, MF_STRING | (_config.MaxOverlayItems == 4 ? MF_CHECKED : 0), ID_ITEMS_4, "4");
            AppendMenu(itemsMenu, MF_STRING | (_config.MaxOverlayItems == 5 ? MF_CHECKED : 0), ID_ITEMS_5, "5");
            AppendMenu(menu, MF_POPUP, (int)itemsMenu, "最大显示的项目(&M)");

            AppendMenu(menu, MF_SEPARATOR, 0, "");
            AppendMenu(menu, MF_STRING, ID_EXIT, "退出(&X)");

            GetCursorPos(out var point);
            SetForegroundWindow(_hwnd);
            TrackPopupMenu(menu, TPM_RIGHTBUTTON, point.X, point.Y, 0, _hwnd, IntPtr.Zero);
            DestroyMenu(menu);
        }

        private void HandleCommand(int commandId)
        {
            switch (commandId)
            {
                case ID_TOGGLE_OVERLAY:
                    _config.ShowOverlayOnDetect = !_config.ShowOverlayOnDetect;
                    _saveConfig();
                    break;
                case ID_LOG_DEBUG:
                    _config.LogLevel = "debug";
                    _saveConfig();
                    break;
                case ID_LOG_INFO:
                    _config.LogLevel = "info";
                    _saveConfig();
                    break;
                case ID_LOG_WARN:
                    _config.LogLevel = "warn";
                    _saveConfig();
                    break;
                case ID_LOG_ERROR:
                    _config.LogLevel = "error";
                    _saveConfig();
                    break;
                case ID_ITEMS_1:
                    _config.MaxOverlayItems = 1;
                    _saveConfig();
                    break;
                case ID_ITEMS_2:
                    _config.MaxOverlayItems = 2;
                    _saveConfig();
                    break;
                case ID_ITEMS_3:
                    _config.MaxOverlayItems = 3;
                    _saveConfig();
                    break;
                case ID_ITEMS_4:
                    _config.MaxOverlayItems = 4;
                    _saveConfig();
                    break;
                case ID_ITEMS_5:
                    _config.MaxOverlayItems = 5;
                    _saveConfig();
                    break;
                case ID_EXIT:
                    ExitRequested?.Invoke();
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);

            if (_hIcon != IntPtr.Zero)
                DestroyIcon(_hIcon);
        }

        #region Native

        private delegate IntPtr Win32WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private Win32WindowProc? _wndProc;
        private IntPtr _oldWndProc;

        private const int WM_TRAYICON = 0x0400 + 1;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_COMMAND = 0x0111;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIM_ADD = 0x00000000;
        private const int NIM_DELETE = 0x00000002;
        private const int MF_STRING = 0x00000000;
        private const int MF_CHECKED = 0x00000008;
        private const int MF_SEPARATOR = 0x00000800;
        private const int MF_POPUP = 0x00000010;
        private const int TPM_RIGHTBUTTON = 0x0002;
        private const int IDI_APPLICATION = 32512;

        private const int ID_TOGGLE_OVERLAY = 1001;
        private const int ID_LOG_DEBUG = 1010;
        private const int ID_LOG_INFO = 1011;
        private const int ID_LOG_WARN = 1012;
        private const int ID_LOG_ERROR = 1013;
        private const int ID_ITEMS_1 = 1020;
        private const int ID_ITEMS_2 = 1021;
        private const int ID_ITEMS_3 = 1022;
        private const int ID_ITEMS_4 = 1023;
        private const int ID_ITEMS_5 = 1024;
        private const int ID_EXIT = 1099;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconData
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData pnid);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, int lpIconName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadIconFromFile(string lpFileName);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion
    }
}
