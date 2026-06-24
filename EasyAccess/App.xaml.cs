using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using global::System;
using global::System.Threading.Tasks;
using EasyAccess.Core;
using EasyAccess.Infra;
using EasyAccess.System;
using EasyAccess.UI;
using EasyAccess.Util;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace EasyAccess
{
    public partial class App : Application
    {
        private Window? _window;
        private SingleInstance? _singleInstance;
        private Logger? _logger;
        private ConfigManager? _configManager;
        private WinEventHook? _winEventHook;
        private DialogDetector? _dialogDetector;
        private FolderCollector? _folderCollector;
        private Navigator? _navigator;
        private OverlayWindow? _overlay;
        private TrayIcon? _trayIcon;
        private IntPtr _currentDialogHwnd;
        private bool _initialized;

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _singleInstance = new SingleInstance();
            if (!_singleInstance.IsFirstInstance)
            {
                _singleInstance.TryActivateExisting();
                Current.Exit();
                return;
            }

            var configDir = ConfigManager.GetDefaultConfigDirectory();
            var logDir = global::System.IO.Path.Combine(configDir, "logs");

            _logger = new Logger(logDir, LogLevel.Debug);
            _logger.Info($"Log directory: {logDir}");
            _logger.Info($"Config directory: {configDir}");
            _configManager = new ConfigManager(configDir);
            _configManager.Load();

            _logger.Info("EasyAccess starting...");

            _window = new MainWindow();
            _window.Activate();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);

            _dialogDetector = new DialogDetector(_logger);
            _folderCollector = new FolderCollector(_logger);
            _navigator = new Navigator(_logger);

            _overlay = new OverlayWindow(hwnd, _logger);
            _overlay.FolderSelected += OnFolderSelected;

            _trayIcon = new TrayIcon(_window, _configManager.Config, () => _configManager.Save());
            _trayIcon.ExitRequested += OnExitRequested;

            _winEventHook = new WinEventHook(_logger);
            _winEventHook.DialogCreated += OnDialogCreated;
            _winEventHook.DialogDestroyed += OnDialogDestroyed;
            _winEventHook.ForegroundChanged += OnForegroundChanged;

            if (!_winEventHook.Install())
            {
                _logger.Error("Failed to install window event hook");
            }

            _initialized = true;
            _logger.Info("EasyAccess initialized");
        }

        private async void OnDialogCreated(IntPtr hwnd)
        {
            if (!_initialized)
                return;

            if (_dialogDetector!.IsFileDialog(hwnd))
            {
                _logger.Info($"File dialog detected: {hwnd}");

                if (CheckUacPermission(hwnd))
                {
                    _currentDialogHwnd = hwnd;
                    await ShowOverlayForDialog(hwnd);
                }
            }
        }

        private bool CheckUacPermission(IntPtr hwnd)
        {
            try
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
                var isElevated = UacHelper.IsProcessElevated(processId);
                var isCurrentElevated = UacHelper.IsCurrentProcessElevated();

                if (isElevated && !isCurrentElevated)
                {
                    _logger.Warn($"Dialog hwnd={hwnd} is elevated (pid={processId}), but EasyAccess is not");
                    ShowUacToast();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("UAC check failed", ex);
                return true;
            }
        }

        private void ShowUacToast()
        {
            try
            {
                var toastXml = new XmlDocument();
                toastXml.LoadXml(@"
<toast>
    <visual>
        <binding template=""ToastGeneric"">
            <text>EasyAccess</text>
            <text>检测到管理员权限的对话框。请以管理员身份运行 EasyAccess 以支持此对话框。</text>
        </binding>
    </visual>
</toast>");

                var toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier("EasyAccess").Show(toast);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to show UAC toast", ex);
            }
        }

        private void OnDialogDestroyed(IntPtr hwnd)
        {
            _logger.Debug($"OnDialogDestroyed called: hwnd={hwnd}, currentDialogHwnd={_currentDialogHwnd}");
            if (hwnd == _currentDialogHwnd)
            {
                _logger.Info($"Dialog destroyed: {hwnd}");
                _currentDialogHwnd = IntPtr.Zero;
                _overlay?.HideOverlay();
                _logger.Info("Overlay hidden");
            }
        }

        private async void OnForegroundChanged(IntPtr hwnd)
        {
            if (!_initialized)
                return;

            if (_dialogDetector!.IsFileDialog(hwnd))
            {
                _logger.Info($"File dialog brought to foreground: {hwnd}");
                _currentDialogHwnd = hwnd;
                await ShowOverlayForDialog(hwnd);
            }
            else if (hwnd != _currentDialogHwnd && _currentDialogHwnd != IntPtr.Zero)
            {
            }
        }

        private async Task ShowOverlayForDialog(IntPtr dialogHwnd)
        {
            var folders = await _folderCollector!.GetOpenFoldersAsync();
            if (folders.Count > 0)
            {
                _overlay!.UpdateFolders(folders);
                _overlay.ShowOverlay(dialogHwnd);
            }
            else
            {
                _logger.Info("No open folders found");
                _overlay?.HideOverlay();
            }
        }

        private async void OnFolderSelected(string path)
        {
            if (_currentDialogHwnd == IntPtr.Zero)
                return;

            _logger.Info($"User selected folder: {path}");

            var success = await _navigator!.NavigateToAsync(_currentDialogHwnd, path);
            if (!success)
            {
                _logger.Warn("Navigation failed");
            }
        }

        private void OnExitRequested()
        {
            _logger.Info("Exit requested");
            _winEventHook?.Dispose();
            _overlay?.Close();
            _trayIcon?.Dispose();
            _singleInstance?.Dispose();
            _logger?.Dispose();
            Current.Exit();
        }
    }
}
