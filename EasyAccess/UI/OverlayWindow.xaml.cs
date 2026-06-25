using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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

    internal sealed class OverlayWindow : Window
    {
        private readonly IntPtr _ownerHwnd;
        private readonly ListView _listView;
        private readonly Border _border;
        private readonly Grid _root;
        private readonly ObservableCollection<FolderItem> _items = new();
        private readonly EasyAccess.Infra.Logger? _logger;
        private IntPtr _hwnd;
        private bool _isVisible;
        private bool _isDarkTheme;
        private int _maxVisibleItems = 5;

        public event Action<string>? FolderSelected;

        public OverlayWindow(IntPtr ownerHwnd, EasyAccess.Infra.Logger? logger = null)
        {
            _ownerHwnd = ownerHwnd;
            _logger = logger;

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

            _border = new Border
            {
                Child = _listView,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0)
            };

            // 设置根 Grid 背景与 Border 一致，避免圆角后露出黑色
            _root = new Grid
            {
                Children = { _border }
            };
            Content = _root;

            _isDarkTheme = IsSystemDarkTheme();
            ApplyTheme();

            var appWindow = GetAppWindow();
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsAlwaysOnTop = true;
            }

            // 完全隐藏标题栏
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
            appWindow.TitleBar.ButtonInactiveBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);

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
        <TextBlock Text=""{Binding DisplayPath}"" FontSize=""12"" Opacity=""0.6"" TextTrimming=""CharacterEllipsis""/>
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

        // 透明色键 (品红色)
        private const uint TRANSPARENT_COLOR_KEY = 0x00FF00FF;

        private void OnActivated(object sender, WindowActivatedEventArgs args)
        {
            if (_hwnd == IntPtr.Zero)
            {
                _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                _logger?.Debug($"[Overlay] OnActivated: hwnd={_hwnd}");

                // 移除窗口标题栏
                var style = NativeMethods.GetWindowLongPtrW(_hwnd, NativeMethods.GWL_STYLE);
                _logger?.Debug($"[Overlay] Original style=0x{style.ToInt64():X8}");
                NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_STYLE,
                    new IntPtr(style.ToInt64() & ~(NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME)));

                var exStyle = NativeMethods.GetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE);
                _logger?.Debug($"[Overlay] Original exStyle=0x{exStyle.ToInt64():X8}");

                NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE,
                    new IntPtr(exStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_LAYERED));

                var newExStyle = NativeMethods.GetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE);
                _logger?.Debug($"[Overlay] New exStyle=0x{newExStyle.ToInt64():X8}");

                NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_HWNDPARENT, _ownerHwnd);

                // 使用 color key 实现透明背景 (品红色区域会变透明)
                var result1 = NativeMethods.SetLayeredWindowAttributes(_hwnd, TRANSPARENT_COLOR_KEY, 255, NativeMethods.LWA_COLORKEY);
                _logger?.Debug($"[Overlay] SetLayeredWindowAttributes(LWA_COLORKEY) result={result1}");

                // 扩展窗口框架到客户区，移除边框
                var margins = new NativeMethods.MARGINS { leftWidth = -1, rightWidth = -1, topHeight = -1, bottomHeight = -1 };
                var result2 = NativeMethods.DwmExtendFrameIntoClientArea(_hwnd, ref margins);
                _logger?.Debug($"[Overlay] DwmExtendFrameIntoClientArea result={result2}");
            }

            var isDark = IsSystemDarkTheme();
            if (isDark != _isDarkTheme)
            {
                _isDarkTheme = isDark;
                ApplyTheme();
            }
        }

        private static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                        return intValue == 0;
                }
            }
            catch { }
            return false;
        }

        private void ApplyTheme()
        {
            if (_isDarkTheme)
            {
                var darkBg = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 45, G = 45, B = 45 });
                _root.Background = darkBg;
                _border.Background = darkBg;
                _border.BorderBrush = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 64, G = 64, B = 64 });
                _listView.Background = darkBg;
                _listView.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                var lightBg = new SolidColorBrush(Colors.White);
                _root.Background = lightBg;
                _border.Background = lightBg;
                _border.BorderBrush = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 224, G = 224, B = 224 });
                _listView.Background = lightBg;
                _listView.Foreground = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 26, G = 26, B = 26 });
            }

            if (_isDarkTheme)
            {
                var darkBg = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 45, G = 45, B = 45 });
                _border.Background = darkBg;
                _border.BorderBrush = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 64, G = 64, B = 64 });
                _listView.Background = darkBg;
                _listView.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                var lightBg = new SolidColorBrush(Colors.White);
                _border.Background = lightBg;
                _border.BorderBrush = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 224, G = 224, B = 224 });
                _listView.Background = lightBg;
                _listView.Foreground = new SolidColorBrush(new global::Windows.UI.Color { A = 255, R = 26, G = 26, B = 26 });
            }
        }

        internal void UpdateFolders(List<ExplorerFolder> folders)
        {
            // Smart update: skip if data hasn't changed to avoid UI flicker
            if (_items.Count == folders.Count)
            {
                bool same = true;
                for (int i = 0; i < folders.Count; i++)
                {
                    if (_items[i].Path != folders[i].Path)
                    {
                        same = false;
                        break;
                    }
                }
                if (same)
                    return;
            }

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

            UpdateMaxHeight();
        }

        internal void UpdateMaxVisibleItems(int maxItems)
        {
            _maxVisibleItems = maxItems;
            UpdateMaxHeight();
        }

        private void UpdateMaxHeight()
        {
            // 使用固定的高度计算，避免布局问题
            var dpi = _hwnd != IntPtr.Zero ? NativeMethods.GetDpiForWindow(_hwnd) : 96;
            var scale = dpi / 96.0;

            // 每个文件夹项的高度：StackPanel Padding(8+8) + TextBlock1(14*1.4) + Spacing(2) + TextBlock2(12*1.4) = 56.4
            var itemHeight = 56.4 * scale;
            var listViewPadding = 4 * scale;

            var maxHeight = _maxVisibleItems * itemHeight + listViewPadding * 2;
            _listView.MaxHeight = maxHeight;
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

            var padding = (int)(16 * scale);
            var gap = (int)(4 * scale);

            var overlayWidth = dialogRect.Width - (padding * 2);

            // 使用固定的高度计算
            var itemHeight = (int)(56.4 * scale);
            var listViewPadding = (int)(4 * scale);

            var visibleCount = global::System.Math.Min(_items.Count, _maxVisibleItems);
            var overlayHeight = visibleCount * itemHeight + listViewPadding * 2;

            _logger?.Debug($"[Overlay] PositionWindow: items={_items.Count}, maxVisible={_maxVisibleItems}, visible={visibleCount}, height={overlayHeight}");

            var x = dialogRect.Left + padding;
            var y = dialogRect.Bottom + gap;

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _logger?.Debug($"[Overlay] PositionWindow: pos=({x},{y}), size=({overlayWidth},{overlayHeight}), scale={scale}");

            NativeMethods.SetWindowPos(windowHandle, IntPtr.Zero, x, y, overlayWidth, overlayHeight,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_FRAMECHANGED);

            // 裁剪窗口为圆角矩形，圆角半径与 Border 一致
            var cornerRadius = (int)(8 * scale);
            _logger?.Debug($"[Overlay] CreateRoundRectRgn: size=({overlayWidth},{overlayHeight}), radius={cornerRadius}");
            var rgn = NativeMethods.CreateRoundRectRgn(0, 0, overlayWidth, overlayHeight, cornerRadius * 2, cornerRadius * 2);
            _logger?.Debug($"[Overlay] CreateRoundRectRgn result={rgn}");

            if (rgn != IntPtr.Zero)
            {
                var rgnResult = NativeMethods.SetWindowRgn(windowHandle, rgn, true);
                _logger?.Debug($"[Overlay] SetWindowRgn result={rgnResult}");
            }
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
