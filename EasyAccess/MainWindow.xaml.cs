using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace EasyAccess
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var presenter = GetAppWindow().Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var exStyle = EasyAccess.Util.NativeMethods.GetWindowLongPtrW(hwnd, EasyAccess.Util.NativeMethods.GWL_EXSTYLE);
            EasyAccess.Util.NativeMethods.SetWindowLongPtrW(hwnd, EasyAccess.Util.NativeMethods.GWL_EXSTYLE,
                new IntPtr(exStyle.ToInt64() | EasyAccess.Util.NativeMethods.WS_EX_TOOLWINDOW));

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            EasyAccess.Util.NativeMethods.SetWindowPos(windowHandle, IntPtr.Zero, -32000, -32000, 0, 0,
                EasyAccess.Util.NativeMethods.SWP_NOSIZE | EasyAccess.Util.NativeMethods.SWP_NOACTIVATE);
        }

        private Microsoft.UI.Windowing.AppWindow GetAppWindow()
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle));
        }
    }
}
