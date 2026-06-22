using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using global::System;
using global::System.Drawing;
using global::System.IO;
using global::System.Runtime.InteropServices;

namespace EasyAccess.UI
{
    public sealed class TrayIcon : IDisposable
    {
        private readonly Window _mainWindow;
        private NotifyIconData _notifyIconData;
        private bool _disposed;
        private IntPtr _hIcon;

        public event Action? ExitRequested;

        public TrayIcon(Window mainWindow)
        {
            _mainWindow = mainWindow;

            _notifyIconData = new NotifyIconData
            {
                cbSize = Marshal.SizeOf<NotifyIconData>(),
                hWnd = WinRT.Interop.WindowNative.GetWindowHandle(mainWindow),
                uID = 1,
                uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
                uCallbackMessage = WM_TRAYICON,
                szTip = "EasyAccess"
            };

            LoadIcon();
            Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
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

        public void HandleMessage(int msg, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                switch (lParam.ToInt32())
                {
                    case WM_RBUTTONUP:
                        ShowContextMenu();
                        break;
                }
            }
        }

        private void ShowContextMenu()
        {
            var menu = CreatePopupMenu();

            AppendMenu(menu, MF_STRING, ID_EXIT, "退出(&X)");

            GetCursorPos(out var point);
            SetForegroundWindow(_notifyIconData.hWnd);
            TrackPopupMenu(menu, TPM_RIGHTBUTTON, point.X, point.Y, 0, _notifyIconData.hWnd, IntPtr.Zero);
            DestroyMenu(menu);
        }

        public void HandleCommand(int commandId)
        {
            if (commandId == ID_EXIT)
            {
                ExitRequested?.Invoke();
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

        private const int WM_TRAYICON = 0x0400 + 1;
        private const int WM_RBUTTONUP = 0x0205;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIM_ADD = 0x00000000;
        private const int NIM_DELETE = 0x00000002;
        private const int MF_STRING = 0x00000000;
        private const int ID_EXIT = 1001;
        private const int TPM_RIGHTBUTTON = 0x0002;
        private const int IDI_APPLICATION = 32512;

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
