using global::System;
using global::System.Runtime.InteropServices;

namespace EasyAccess.Util
{
    internal static class UacHelper
    {
        public static bool IsProcessElevated(uint processId)
        {
            IntPtr processHandle = IntPtr.Zero;
            IntPtr tokenHandle = IntPtr.Zero;

            try
            {
                processHandle = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_INFORMATION, false, processId);

                if (processHandle == IntPtr.Zero)
                    return false;

                if (!OpenProcessToken(processHandle, NativeMethods.TOKEN_QUERY, out tokenHandle))
                    return false;

                if (!GetTokenInformation(tokenHandle, TokenInformationClass.TokenElevation,
                    out var elevation, Marshal.SizeOf<TOKEN_ELEVATION>(), out _))
                    return false;

                return elevation.TokenIsElevated != 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                    NativeMethods.CloseHandle(tokenHandle);
                if (processHandle != IntPtr.Zero)
                    NativeMethods.CloseHandle(processHandle);
            }
        }

        public static bool IsCurrentProcessElevated()
        {
            using var process = global::System.Diagnostics.Process.GetCurrentProcess();
            return IsProcessElevated((uint)process.Id);
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, TokenInformationClass TokenInformationClass,
            out TOKEN_ELEVATION TokenInformation, int TokenInformationLength, out int ReturnLength);

        private enum TokenInformationClass
        {
            TokenElevation = 20
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_ELEVATION
        {
            public int TokenIsElevated;
        }
    }
}
