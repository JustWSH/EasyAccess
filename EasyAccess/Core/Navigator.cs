using global::System;
using global::System.Runtime.InteropServices;
using global::System.Text;
using global::System.Threading.Tasks;
using EasyAccess.Infra;
using EasyAccess.Util;

namespace EasyAccess.Core
{
    internal sealed class Navigator
    {
        private readonly Logger _logger;

        public Navigator(Logger logger)
        {
            _logger = logger;
        }

        public Task<bool> NavigateToAsync(IntPtr dialogHwnd, string targetPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    var addressEditHwnd = FindAddressEdit(dialogHwnd);
                    if (addressEditHwnd == IntPtr.Zero)
                    {
                        _logger.Warn("Could not find address bar edit control");
                        return false;
                    }

                    if (!SetAddressText(addressEditHwnd, targetPath))
                    {
                        _logger.Warn("Failed to set address text");
                        return false;
                    }

                    _logger.Info($"Navigated to: {targetPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error("Navigation failed", ex);
                    return false;
                }
            });
        }

        private IntPtr FindAddressEdit(IntPtr dialogHwnd)
        {
            var workerHwnd = FindChildByClass(dialogHwnd, "WorkerW");
            if (workerHwnd == IntPtr.Zero)
            {
                _logger.Debug("WorkerW not found");
                return IntPtr.Zero;
            }

            var rebarHwnd = FindChildByClass(workerHwnd, "ReBarWindow32");
            if (rebarHwnd == IntPtr.Zero)
            {
                _logger.Debug("ReBarWindow32 not found");
                return IntPtr.Zero;
            }

            var addressBandHwnd = FindChildByClassName(rebarHwnd, "Address Band Root");
            if (addressBandHwnd == IntPtr.Zero)
            {
                _logger.Debug("Address Band Root not found");
                return IntPtr.Zero;
            }

            var progressHwnd = FindChildByClass(addressBandHwnd, "msctls_progress32");
            if (progressHwnd == IntPtr.Zero)
            {
                _logger.Debug("msctls_progress32 not found");
                return IntPtr.Zero;
            }

            var breadcrumbParent = FindChildByClass(progressHwnd, "Breadcrumb Parent");
            if (breadcrumbParent != IntPtr.Zero)
            {
                var toolbarHwnd = FindChildByClass(breadcrumbParent, "ToolbarWindow32");
                if (toolbarHwnd != IntPtr.Zero)
                {
                    var className = GetClassName(toolbarHwnd);
                    if (className == "ToolbarWindow32")
                    {
                        _logger.Debug("Found breadcrumb ToolbarWindow32");
                        return toolbarHwnd;
                    }
                }
            }

            var comboBoxExHwnd = FindChildByClass(progressHwnd, "ComboBoxEx32");
            if (comboBoxExHwnd != IntPtr.Zero)
            {
                var comboBoxHwnd = FindChildByClass(comboBoxExHwnd, "ComboBox");
                if (comboBoxHwnd != IntPtr.Zero)
                {
                    var editHwnd = FindChildByClass(comboBoxHwnd, "Edit");
                    if (editHwnd != IntPtr.Zero)
                    {
                        _logger.Debug("Found address bar Edit control");
                        return editHwnd;
                    }
                }
            }

            _logger.Debug("Address edit control not found");
            return IntPtr.Zero;
        }

        private bool SetAddressText(IntPtr editHwnd, string path)
        {
            _logger.Debug($"SetAddressText: hwnd={editHwnd}, path={path}");

            var className = GetClassName(editHwnd);
            _logger.Debug($"Control class: {className}");

            if (className == "ToolbarWindow32")
            {
                _logger.Debug("Found breadcrumb, clicking to switch to edit mode...");
                NativeMethods.SendMessage(editHwnd, NativeMethods.WM_LBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
                NativeMethods.SendMessage(editHwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
                global::System.Threading.Thread.Sleep(300);

                var editControl = FindEditInAddressBar(editHwnd);
                if (editControl != IntPtr.Zero)
                {
                    _logger.Debug($"Found edit control after click: {editControl}");
                    editHwnd = editControl;
                }
                else
                {
                    _logger.Warn("Could not find edit control after clicking breadcrumb");
                    return false;
                }
            }

            NativeMethods.SetForegroundWindow(editHwnd);
            global::System.Threading.Thread.Sleep(100);

            NativeMethods.SendMessage(editHwnd, NativeMethods.WM_SETTEXT, IntPtr.Zero, path);
            global::System.Threading.Thread.Sleep(100);

            SendEnterKey(editHwnd);
            _logger.Debug("Enter key sent");

            return true;
        }

        private IntPtr FindEditInAddressBar(IntPtr breadcrumbHwnd)
        {
            var parent = NativeMethods.GetParent(breadcrumbHwnd);
            if (parent == IntPtr.Zero)
                return IntPtr.Zero;

            var grandParent = NativeMethods.GetParent(parent);
            if (grandParent == IntPtr.Zero)
                return IntPtr.Zero;

            var comboBoxExHwnd = FindChildByClass(grandParent, "ComboBoxEx32");
            if (comboBoxExHwnd != IntPtr.Zero)
            {
                var comboBoxHwnd = FindChildByClass(comboBoxExHwnd, "ComboBox");
                if (comboBoxHwnd != IntPtr.Zero)
                {
                    var editHwnd = FindChildByClass(comboBoxHwnd, "Edit");
                    if (editHwnd != IntPtr.Zero)
                        return editHwnd;
                }
            }

            return IntPtr.Zero;
        }

        private void SendEnterKey(IntPtr targetHwnd)
        {
            var inputs = new NativeMethods.INPUT[2];

            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].union.ki.wVk = NativeMethods.VK_RETURN;
            inputs[0].union.ki.dwFlags = 0;

            inputs[1].type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].union.ki.wVk = NativeMethods.VK_RETURN;
            inputs[1].union.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput(2, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        }

        private static IntPtr FindChildByClass(IntPtr parentHwnd, string className)
        {
            return NativeMethods.FindWindowExW(parentHwnd, IntPtr.Zero, className, null);
        }

        private static IntPtr FindChildByClassName(IntPtr parentHwnd, string partialClassName)
        {
            IntPtr result = IntPtr.Zero;

            NativeMethods.EnumChildWindows(parentHwnd, (childHwnd, _) =>
            {
                var childClassName = GetClassName(childHwnd);
                if (childClassName.Contains(partialClassName))
                {
                    result = childHwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return result;
        }

        private static string GetClassName(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            NativeMethods.GetClassNameW(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
