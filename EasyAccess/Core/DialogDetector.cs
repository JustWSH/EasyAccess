using global::System;
using global::System.Text;
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
            _logger.Debug($"Checking window {hwnd}, class: {className}");

            if (className != StandardDialogClass)
                return false;

            _logger.Debug($"Found #32770 dialog, checking controls...");
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

            _logger.Debug($"Dialog {hwnd}: Edit={hasEdit}, ComboBox={hasComboBox}, Button={hasButton}");

            return matchCount >= 2;
        }

        public string? GetDialogFilePath(IntPtr hwnd)
        {
            var editHwnd = NativeMethods.FindWindowExW(hwnd, IntPtr.Zero, "Edit", null);
            if (editHwnd == IntPtr.Zero)
                return null;

            return GetWindowText(editHwnd);
        }

        public bool ClickButton(IntPtr hwnd, string buttonText)
        {
            IntPtr buttonHwnd = IntPtr.Zero;

            NativeMethods.EnumChildWindows(hwnd, (childHwnd, _) =>
            {
                var className = GetClassName(childHwnd);
                if (className == "Button")
                {
                    var text = GetWindowText(childHwnd);
                    if (text.Contains(buttonText))
                    {
                        buttonHwnd = childHwnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (buttonHwnd == IntPtr.Zero)
                return false;

            NativeMethods.SendMessage(buttonHwnd, NativeMethods.BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        public IntPtr GetAddressBarEdit(IntPtr hwnd)
        {
            var workerHwnd = IntPtr.Zero;
            NativeMethods.EnumChildWindows(hwnd, (childHwnd, _) =>
            {
                var className = GetClassName(childHwnd);
                if (className == "WorkerW")
                {
                    workerHwnd = childHwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (workerHwnd == IntPtr.Zero)
                return IntPtr.Zero;

            var rebarHwnd = NativeMethods.FindWindowExW(workerHwnd, IntPtr.Zero, "ReBarWindow32", null);
            if (rebarHwnd == IntPtr.Zero)
                return IntPtr.Zero;

            var addressBandHwnd = IntPtr.Zero;
            NativeMethods.EnumChildWindows(rebarHwnd, (childHwnd, _) =>
            {
                var className = GetClassName(childHwnd);
                if (className == "Address Band Root")
                {
                    addressBandHwnd = childHwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (addressBandHwnd == IntPtr.Zero)
                return IntPtr.Zero;

            var progressHwnd = NativeMethods.FindWindowExW(addressBandHwnd, IntPtr.Zero, "msctls_progress32", null);
            if (progressHwnd == IntPtr.Zero)
                return IntPtr.Zero;

            var breadcrumbParent = NativeMethods.FindWindowExW(progressHwnd, IntPtr.Zero, "Breadcrumb Parent", null);
            if (breadcrumbParent != IntPtr.Zero)
            {
                var breadcrumbHwnd = NativeMethods.FindWindowExW(breadcrumbParent, IntPtr.Zero, null, null);
                if (breadcrumbHwnd != IntPtr.Zero)
                {
                    var className = GetClassName(breadcrumbHwnd);
                    if (className == "ToolbarWindow32")
                        return breadcrumbHwnd;
                }
            }

            var comboBoxExHwnd = NativeMethods.FindWindowExW(progressHwnd, IntPtr.Zero, "ComboBoxEx32", null);
            if (comboBoxExHwnd != IntPtr.Zero)
            {
                var comboBoxHwnd = NativeMethods.FindWindowExW(comboBoxExHwnd, IntPtr.Zero, "ComboBox", null);
                if (comboBoxHwnd != IntPtr.Zero)
                {
                    var editHwnd = NativeMethods.FindWindowExW(comboBoxHwnd, IntPtr.Zero, "Edit", null);
                    if (editHwnd != IntPtr.Zero)
                        return editHwnd;
                }
            }

            return IntPtr.Zero;
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
