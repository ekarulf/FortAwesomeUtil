using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FortAwesomeUtil.Win32.NativeWrappers
{
    class Advapi32
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcessAsUserW(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            Windows_h.CreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref Windows_h.STARTUPINFO lpStartupInfo,
            out Windows_h.PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle,
            Windows_h.TokenAccessFlags DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            Windows_h.TokenAccessFlags dwDesiredAccess,
            IntPtr lpThreadAttributes,
            Windows_h.SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            Windows_h.TOKEN_TYPE TokenType,
            out IntPtr phNewToken);
    }
}
