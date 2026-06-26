using System;
using System.Text;
using EasyAccess.Infra;
using EasyAccess.Util;

namespace EasyAccess.Core
{
    internal sealed class DialogDetector
    {
        private const string StandardDialogClass = "#32770";

        private readonly Logger _logger;

        public DialogDetector(Logger logger)
        {
            _logger = logger;
        }

        public bool IsFileDialog(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
                return false;

            var className = GetClassName(hwnd);
            if (className != StandardDialogClass)
                return false;

            var result = HasFileDialogControls(hwnd);
            _logger.Debug($"IsFileDialog result: {result}");
            return result;
        }

        private bool HasFileDialogControls(IntPtr hwnd)
        {
            int matchCount = 0;
            bool hasEdit = false;
            bool hasComboBox = false;
            bool hasButton = false;

            NativeMethods.EnumChildWindows(hwnd, (childHwnd, _) =>
            {
                var childClass = GetClassName(childHwnd);

                if (childClass == "Edit")
                    hasEdit = true;

                if (childClass == "ComboBoxEx32" || childClass == "ComboBox")
                    hasComboBox = true;

                if (childClass == "Button")
                {
                    var text = GetWindowText(childHwnd);
                    if (text.Contains("打开") || text.Contains("Open") ||
                        text.Contains("保存") || text.Contains("Save") ||
                        text.Contains("另存"))
                        hasButton = true;
                }

                return true;
            }, IntPtr.Zero);

            if (hasEdit) matchCount++;
            if (hasComboBox) matchCount++;
            if (hasButton) matchCount++;

            return matchCount >= 2;
        }

        private static string GetClassName(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            NativeMethods.GetClassNameW(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetWindowText(IntPtr hwnd)
        {
            var length = (int)NativeMethods.SendMessage(hwnd, NativeMethods.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            if (length == 0)
                return string.Empty;

            var sb = new StringBuilder(length + 1);
            NativeMethods.SendMessage(hwnd, NativeMethods.WM_GETTEXT, (IntPtr)sb.Capacity, sb);
            return sb.ToString();
        }
    }
}
