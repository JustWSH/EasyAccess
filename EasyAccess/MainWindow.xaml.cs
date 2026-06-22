using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using global::System;

namespace EasyAccess
{
    public sealed partial class MainWindow : Window
    {
        private IntPtr _hwnd;

        public MainWindow()
        {
            InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            var presenter = GetAppWindow().Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);

            var exStyle = EasyAccess.Util.NativeMethods.GetWindowLongPtrW(_hwnd, EasyAccess.Util.NativeMethods.GWL_EXSTYLE);
            EasyAccess.Util.NativeMethods.SetWindowLongPtrW(_hwnd, EasyAccess.Util.NativeMethods.GWL_EXSTYLE,
                new IntPtr(exStyle.ToInt64() | EasyAccess.Util.NativeMethods.WS_EX_TOOLWINDOW));

            EasyAccess.Util.NativeMethods.SetWindowPos(_hwnd, IntPtr.Zero, -32000, -32000, 0, 0,
                EasyAccess.Util.NativeMethods.SWP_NOSIZE | EasyAccess.Util.NativeMethods.SWP_NOACTIVATE);
        }

        private Microsoft.UI.Windowing.AppWindow GetAppWindow()
        {
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        }
    }
}
