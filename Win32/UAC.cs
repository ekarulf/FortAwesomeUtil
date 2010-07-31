using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using FortAwesomeUtil.Win32.NativeWrappers;

namespace FortAwesomeUtil.Win32
{
    public class UAC
    {
        public static void AddShieldToButtonHandle(IntPtr hWnd)
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                User32.SendMessage(hWnd, User32.BCM_SETSHIELD, 0, 0xFFFFFFFF);
            }
        }

        public static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool RunAsAdminUser(string path, string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = path;
            startInfo.Arguments = args;
            startInfo.Verb = "runas";
            try
            {
                Process p = Process.Start(startInfo);
                return true;
            }
            catch (Win32Exception)
            {
                // User Cancelled UAC
                return false;
            }
        }

        public static Process RunAsDesktopUser(string path, string args)
        {
            int lastError;
            IntPtr shellWindow = User32.GetShellWindow();

            if (shellWindow == IntPtr.Zero)
            {
                throw new InvalidOperationException("Unable to get shell window.");
            }

            uint processID = 0;
            User32.GetWindowThreadProcessId(shellWindow, out processID);
            if (processID == 0)
            {
                lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            IntPtr processHandle = Kernel32.OpenProcess(Windows_h.ProcessAccessFlags.QueryInformation, true, processID);

            if (processHandle == IntPtr.Zero)
            {
                lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            IntPtr shellProcessToken;

            Windows_h.TokenAccessFlags tokenAccess = Windows_h.TokenAccessFlags.TOKEN_QUERY | Windows_h.TokenAccessFlags.TOKEN_ASSIGN_PRIMARY |
                Windows_h.TokenAccessFlags.TOKEN_DUPLICATE | Windows_h.TokenAccessFlags.TOKEN_ADJUST_DEFAULT |
                Windows_h.TokenAccessFlags.TOKEN_ADJUST_SESSIONID;

            if (!Advapi32.OpenProcessToken(processHandle, tokenAccess, out shellProcessToken))
            {
                lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            IntPtr newPrimaryToken;

            if (!Advapi32.DuplicateTokenEx(shellProcessToken, tokenAccess, IntPtr.Zero,
                Windows_h.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, Windows_h.TOKEN_TYPE.TokenPrimary, out newPrimaryToken))
            {
                lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            Windows_h.STARTUPINFO startupInfo = new Windows_h.STARTUPINFO();
            startupInfo.cb = System.Runtime.InteropServices.Marshal.SizeOf(startupInfo);
            startupInfo.lpDesktop = "";

            Windows_h.PROCESS_INFORMATION processInfo = new Windows_h.PROCESS_INFORMATION();

            if (!Advapi32.CreateProcessAsUserW(newPrimaryToken, path, path + " " + args, IntPtr.Zero, IntPtr.Zero, false, 0,
                IntPtr.Zero, null, ref startupInfo, out processInfo))
            {
                lastError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            Kernel32.CloseHandle(processInfo.hProcess);
            Kernel32.CloseHandle(processInfo.hThread);

            return Process.GetProcessById(processInfo.dwProcessId);
        }
    }
}
