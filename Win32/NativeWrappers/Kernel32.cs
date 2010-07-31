using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FortAwesomeUtil.Win32.NativeWrappers
{
    class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(Windows_h.ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] 
            bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool CloseHandle(IntPtr handle);
    }
}
