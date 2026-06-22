using global::System;
using global::System.Threading;
using EasyAccess.Util;

namespace EasyAccess.Infra
{
    internal sealed class SingleInstance : IDisposable
    {
        private const string MutexName = @"Global\EasyAccess_SingleInstance";
        private const string WindowClassName = "EasyAccess_Hidden";
        private const int WM_USER_ACTIVATE = NativeMethods.WM_USER + 1;

        private Mutex? _mutex;
        private bool _isFirstInstance;

        public bool IsFirstInstance => _isFirstInstance;

        public SingleInstance()
        {
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);
        }

        public bool TryActivateExisting()
        {
            if (_isFirstInstance)
                return false;

            var existing = NativeMethods.FindWindow(WindowClassName, null);
            if (existing != IntPtr.Zero)
            {
                NativeMethods.PostMessage(existing, WM_USER_ACTIVATE, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            _mutex?.Dispose();
            _mutex = null;
        }
    }
}
