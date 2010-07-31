using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FortAwesomeUtil.Win32.NativeWrappers
{
    class User32
    {
        // "Security Shield" button
        internal const int BCM_FIRST = 0x1600;
        internal const int BCM_SETSHIELD = (BCM_FIRST + 0x000C);

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern UInt32 SendMessage(IntPtr hWnd, UInt32 msg, UInt32 wParam, UInt32 lParam);
    }
}
