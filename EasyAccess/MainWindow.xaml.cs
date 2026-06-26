using Microsoft.UI.Xaml;
using System;
using EasyAccess.Util;

namespace EasyAccess
{
    public sealed partial class MainWindow : Window
    {
        private IntPtr _hwnd;

        public MainWindow()
        {
            InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            var presenter = NativeMethods.GetAppWindow(_hwnd).Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
            }

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);

            var exStyle = NativeMethods.GetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLongPtrW(_hwnd, NativeMethods.GWL_EXSTYLE,
                new IntPtr(exStyle.ToInt64() | NativeMethods.WS_EX_TOOLWINDOW));

            NativeMethods.SetWindowPos(_hwnd, IntPtr.Zero, -32000, -32000, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }
}
