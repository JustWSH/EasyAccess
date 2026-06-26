using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
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
        private NativeMethods.NOTIFYICONDATA _notifyIconData;
        private bool _disposed;
        private IntPtr _hIcon;

        public event Action? ExitRequested;

        public TrayIcon(Window mainWindow, AppConfig config, Action saveConfig)
        {
            _mainWindow = mainWindow;
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow);
            _config = config;
            _saveConfig = saveConfig;

            _notifyIconData = new NativeMethods.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_MESSAGE,
                uCallbackMessage = NativeMethods.WM_TRAYICON,
                szTip = "EasyAccess"
            };

            LoadIcon();
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref _notifyIconData);

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
            if (msg == NativeMethods.WM_TRAYICON)
            {
                int lParamInt = lParam.ToInt32();
                if (lParamInt == NativeMethods.WM_RBUTTONUP)
                {
                    ShowContextMenu();
                }
            }
            else if (msg == NativeMethods.WM_COMMAND)
            {
                int commandId = wParam.ToInt32() & 0xFFFF;
                HandleCommand(commandId);
            }

            return NativeMethods.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void LoadIcon()
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ico.ico");
            if (File.Exists(iconPath))
            {
                _hIcon = NativeMethods.LoadImage(IntPtr.Zero, iconPath, NativeMethods.IMAGE_ICON, 0, 0, NativeMethods.LR_LOADFROMFILE);
            }
            else
            {
                _hIcon = NativeMethods.LoadIcon(IntPtr.Zero, NativeMethods.IDI_APPLICATION);
            }

            _notifyIconData.hIcon = _hIcon;
        }

        private void ShowContextMenu()
        {
            var menu = NativeMethods.CreatePopupMenu();

            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING | NativeMethods.MF_DISABLED, 0, "EasyAccess v1.0");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_SEPARATOR, 0, "");

            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING | (_config.ShowOverlayOnDetect ? NativeMethods.MF_CHECKED : 0),
                ID_TOGGLE_OVERLAY, "显示 Overlay | Show Overlay(&O)");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_SEPARATOR, 0, "");

            var logMenu = NativeMethods.CreatePopupMenu();
            NativeMethods.AppendMenu(logMenu, NativeMethods.MF_STRING | (_config.LogLevel == "debug" ? NativeMethods.MF_CHECKED : 0), ID_LOG_DEBUG, "Debug");
            NativeMethods.AppendMenu(logMenu, NativeMethods.MF_STRING | (_config.LogLevel == "info" ? NativeMethods.MF_CHECKED : 0), ID_LOG_INFO, "Info");
            NativeMethods.AppendMenu(logMenu, NativeMethods.MF_STRING | (_config.LogLevel == "warn" ? NativeMethods.MF_CHECKED : 0), ID_LOG_WARN, "Warn");
            NativeMethods.AppendMenu(logMenu, NativeMethods.MF_STRING | (_config.LogLevel == "error" ? NativeMethods.MF_CHECKED : 0), ID_LOG_ERROR, "Error");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_POPUP, (int)logMenu, "日志级别 | Log Level(&L)");

            var itemsMenu = NativeMethods.CreatePopupMenu();
            NativeMethods.AppendMenu(itemsMenu, NativeMethods.MF_STRING | (_config.MaxOverlayItems == 1 ? NativeMethods.MF_CHECKED : 0), ID_ITEMS_1, "1");
            NativeMethods.AppendMenu(itemsMenu, NativeMethods.MF_STRING | (_config.MaxOverlayItems == 2 ? NativeMethods.MF_CHECKED : 0), ID_ITEMS_2, "2");
            NativeMethods.AppendMenu(itemsMenu, NativeMethods.MF_STRING | (_config.MaxOverlayItems == 3 ? NativeMethods.MF_CHECKED : 0), ID_ITEMS_3, "3");
            NativeMethods.AppendMenu(itemsMenu, NativeMethods.MF_STRING | (_config.MaxOverlayItems == 4 ? NativeMethods.MF_CHECKED : 0), ID_ITEMS_4, "4");
            NativeMethods.AppendMenu(itemsMenu, NativeMethods.MF_STRING | (_config.MaxOverlayItems == 5 ? NativeMethods.MF_CHECKED : 0), ID_ITEMS_5, "5");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_POPUP, (int)itemsMenu, "最大显示项目 | Max Items(&M)");

            NativeMethods.AppendMenu(menu, NativeMethods.MF_SEPARATOR, 0, "");
            NativeMethods.AppendMenu(menu, NativeMethods.MF_STRING, ID_EXIT, "退出 | Exit(&X)");

            NativeMethods.GetCursorPos(out var point);
            NativeMethods.SetForegroundWindow(_hwnd);
            NativeMethods.TrackPopupMenu(menu, NativeMethods.TPM_RIGHTBUTTON, point.X, point.Y, 0, _hwnd, IntPtr.Zero);
            NativeMethods.DestroyMenu(menu);
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

            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _notifyIconData);

            if (_hIcon != IntPtr.Zero)
                NativeMethods.DestroyIcon(_hIcon);
        }

        private NativeMethods.WndProcDelegate? _wndProc;
        private IntPtr _oldWndProc;

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
    }
}
