using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using global::System;
using global::System.Collections.Generic;
using global::System.Collections.ObjectModel;
using global::System.Runtime.InteropServices;
using EasyAccess.Core;
using EasyAccess.System;
using EasyAccess.Util;

namespace EasyAccess.UI
{
    public sealed class FolderItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string DisplayPath { get; set; } = string.Empty;
    }

    public sealed class OverlayWindow : Window
    {
        private readonly IntPtr _ownerHwnd;
        private readonly ListView _listView;
        private readonly ObservableCollection<FolderItem> _items = new();
        private IntPtr _hwnd;
        private bool _isVisible;

        public event Action<string>? FolderSelected;

        public OverlayWindow(IntPtr ownerHwnd)
        {
            _ownerHwnd = ownerHwnd;

            _listView = new ListView
            {
                ItemsSource = _items,
                SelectionMode = ListViewSelectionMode.Single,
                IsItemClickEnabled = true,
                MaxHeight = 320,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4)
            };

            _listView.ItemTemplate = CreateItemTemplate();
            _listView.ItemClick += OnItemClick;

            var border = new Border
            {
                Child = _listView,
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 0, 16, 8)
            };

            Content = border;

            var presenter = GetAppWindow().Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsAlwaysOnTop = true;
            }

            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);

            Activated += OnActivated;
        }

        private DataTemplate CreateItemTemplate()
        {
            var xaml = @"
<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel Spacing=""2"" Padding=""12,8"">
        <TextBlock Text=""{Binding Name}"" FontSize=""14"" FontWeight=""SemiBold"" TextTrimming=""CharacterEllipsis""/>
        <TextBlock Text=""{Binding DisplayPath}"" FontSize=""12"" Foreground=""Gray"" TextTrimming=""CharacterEllipsis""/>
    </StackPanel>
</DataTemplate>";

            return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FolderItem item)
            {
                FolderSelected?.Invoke(item.Path);
            }
        }

        private void OnActivated(object sender, WindowActivatedEventArgs args)
        {
            if (_hwnd == IntPtr.Zero)
            {
                _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

                var exStyle = NativeMethods.GetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE,
                    new IntPtr(exStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW));

                NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_HWNDPARENT, _ownerHwnd);
            }
        }

        internal void UpdateFolders(List<ExplorerFolder> folders)
        {
            _items.Clear();

            foreach (var folder in folders)
            {
                _items.Add(new FolderItem
                {
                    Name = folder.DisplayName,
                    Path = folder.Path,
                    DisplayPath = TruncatePath(folder.Path)
                });
            }
        }

        public void ShowOverlay(IntPtr dialogHwnd)
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

            if (!_isVisible)
            {
                NativeMethods.ShowWindow(windowHandle, NativeMethods.SW_SHOW);
                _isVisible = true;
            }

            PositionWindow(dialogHwnd);
        }

        public void HideOverlay()
        {
            if (_isVisible)
            {
                var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
                NativeMethods.ShowWindow(windowHandle, NativeMethods.SW_HIDE);
                _isVisible = false;
            }
        }

        private void PositionWindow(IntPtr dialogHwnd)
        {
            NativeMethods.GetWindowRect(dialogHwnd, out var dialogRect);

            var dpi = NativeMethods.GetDpiForWindow(dialogHwnd);
            var scale = dpi / 96.0;

            var overlayWidth = (int)(dialogRect.Width * scale);
            var overlayHeight = (int)(_items.Count <= 3 ? _items.Count * 60 + 20 : 320);
            var x = dialogRect.Left;
            var y = dialogRect.Bottom - overlayHeight;

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            NativeMethods.SetWindowPos(windowHandle, IntPtr.Zero, x, y, overlayWidth, overlayHeight,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        }

        private static string TruncatePath(string path)
        {
            if (path.Length <= 50)
                return path;

            var parts = path.Split('\\');
            if (parts.Length <= 3)
                return path;

            return $"{parts[0]}\\...\\{parts[parts.Length - 1]}";
        }

        private AppWindow GetAppWindow()
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            return AppWindow.GetFromWindowId(
                Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle));
        }
    }
}
