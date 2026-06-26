using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyAccess.Infra;
using EasyAccess.Interop;

namespace EasyAccess.Core
{
    internal sealed class FolderCollector
    {
        private readonly ShellWindowsInterop _shellWindows;
        private readonly Logger _logger;
        private List<ExplorerFolder>? _cachedFolders;

        public FolderCollector(Logger logger)
        {
            _logger = logger;
            _shellWindows = new ShellWindowsInterop(logger);
        }

        public bool HasCache => _cachedFolders != null;

        public Task<List<ExplorerFolder>> GetOpenFoldersAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_cachedFolders != null)
                    {
                        return new List<ExplorerFolder>(_cachedFolders);
                    }

                    var folders = _shellWindows.GetOpenFolders();
                    _logger.Info($"Found {folders.Count} open folders");

                    _cachedFolders = folders;

                    return new List<ExplorerFolder>(folders);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to collect folders", ex);
                    return new List<ExplorerFolder>();
                }
            });
        }

        public void InvalidateCache()
        {
            _cachedFolders = null;
        }
    }
}
