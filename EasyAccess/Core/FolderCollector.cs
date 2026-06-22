using global::System;
using global::System.Collections.Generic;
using global::System.Threading.Tasks;
using EasyAccess.Infra;
using EasyAccess.System;

namespace EasyAccess.Core
{
    internal sealed class FolderCollector
    {
        private readonly ShellWindowsInterop _shellWindows;
        private readonly Logger _logger;

        public FolderCollector(Logger logger)
        {
            _logger = logger;
            _shellWindows = new ShellWindowsInterop(logger);
        }

        public Task<List<ExplorerFolder>> GetOpenFoldersAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var folders = _shellWindows.GetOpenFolders();
                    _logger.Info($"Found {folders.Count} open folders");
                    return folders;
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to collect folders", ex);
                    return new List<ExplorerFolder>();
                }
            });
        }
    }
}
