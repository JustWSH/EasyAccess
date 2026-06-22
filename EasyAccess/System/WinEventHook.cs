using global::System;
using EasyAccess.Util;

namespace EasyAccess.System
{
    internal sealed class WinEventHook : IDisposable
    {
        private readonly NativeMethods.WinEventDelegate _delegate;
        private IntPtr _hook = IntPtr.Zero;
        private bool _disposed;
        private readonly Infra.Logger? _logger;

        public event Action<IntPtr>? DialogCreated;
        public event Action<IntPtr>? DialogDestroyed;
        public event Action<IntPtr>? ForegroundChanged;
        public event Action<IntPtr>? LocationChanged;

        public WinEventHook(Infra.Logger? logger = null)
        {
            _delegate = WinEventCallback;
            _logger = logger;
        }

        public bool Install()
        {
            var moduleHandle = NativeMethods.GetModuleHandle(null);

            _hook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_CREATE,
                NativeMethods.EVENT_OBJECT_CREATE,
                moduleHandle,
                _delegate,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

            if (_hook == IntPtr.Zero)
            {
                _logger?.Error("Failed to set EVENT_OBJECT_CREATE hook");
                return false;
            }

            NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_DESTROY,
                NativeMethods.EVENT_OBJECT_DESTROY,
                moduleHandle,
                _delegate,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

            NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                moduleHandle,
                _delegate,
                0, 0,
                NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

            _logger?.Info("WinEventHook installed successfully");
            return true;
        }

        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero || idObject != 0 || idChild != 0)
                return;

            switch (eventType)
            {
                case NativeMethods.EVENT_OBJECT_CREATE:
                    _logger?.Debug($"EVENT_OBJECT_CREATE: hwnd={hwnd}");
                    DialogCreated?.Invoke(hwnd);
                    break;
                case NativeMethods.EVENT_OBJECT_DESTROY:
                    DialogDestroyed?.Invoke(hwnd);
                    break;
                case NativeMethods.EVENT_SYSTEM_FOREGROUND:
                    _logger?.Debug($"EVENT_SYSTEM_FOREGROUND: hwnd={hwnd}");
                    ForegroundChanged?.Invoke(hwnd);
                    break;
                case NativeMethods.EVENT_OBJECT_LOCATIONCHANGE:
                    LocationChanged?.Invoke(hwnd);
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}
