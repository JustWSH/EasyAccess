using System;
using System.Threading;

namespace EasyAccess.Infra
{
    internal sealed class SingleInstance : IDisposable
    {
        private const string MutexName = @"Global\EasyAccess_SingleInstance";

        private Mutex? _mutex;
        private bool _isFirstInstance;

        public bool IsFirstInstance => _isFirstInstance;

        public SingleInstance()
        {
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);
        }

        public void Dispose()
        {
            _mutex?.Dispose();
            _mutex = null;
        }
    }
}
