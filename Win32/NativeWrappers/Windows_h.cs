using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FortAwesomeUtil.Win32.NativeWrappers
{
    class Windows_h
    {
        [Flags]
        public enum CreationFlags
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000
        }

        [Flags()]
        public enum ProcessAccessFlags : int
        {
            /// <summary>Specifies all possible access flags for the process object.</summary>
            AllAccess = CreateThread | DuplicateHandle | QueryInformation | SetInformation
                | Terminate | VMOperation | VMRead | VMWrite | Synchronize,
            /// <summary>Enables usage of the process handle in the CreateRemoteThread 
            /// function to create a thread in the process.</summary>
            CreateThread = 0x2,
            /// <summary>Enables usage of the process handle as either the source or target process 
            /// in the DuplicateHandle function to duplicate a handle.</summary>
            DuplicateHandle = 0x40,
            /// <summary>Enables usage of the process handle in the GetExitCodeProcess and 
            /// GetPriorityClass functions to read information from the process object.</summary>
            QueryInformation = 0x400,
            /// <summary>Enables usage of the process handle in the SetPriorityClass function to 
            /// set the priority class of the process.</summary>
            SetInformation = 0x200,
            /// <summary>Enables usage of the process handle in the TerminateProcess function to 
            /// terminate the process.</summary>
            Terminate = 0x1,
            /// <summary>Enables usage of the process handle in the VirtualProtectEx and 
            /// WriteProcessMemory functions to modify the virtual memory of the process.</summary>
            VMOperation = 0x8,
            /// <summary>Enables usage of the process handle in the ReadProcessMemory function to' 
            /// read from the virtual memory of the process.</summary>
            VMRead = 0x10,
            /// <summary>Enables usage of the process handle in the WriteProcessMemory function to 
            /// write to the virtual memory of the process.</summary>
            VMWrite = 0x20,
            /// <summary>Enables usage of the process handle in any of the wait functions to wait 
            /// for the process to terminate.</summary>
            Synchronize = 0x100000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [Flags()]
        public enum TokenAccessFlags : int
        {
            STANDARD_RIGHTS_REQUIRED = 0x000F0000,
            STANDARD_RIGHTS_READ = 0x00020000,
            TOKEN_ASSIGN_PRIMARY = 0x0001,
            TOKEN_DUPLICATE = 0x0002,
            TOKEN_IMPERSONATE = 0x0004,
            TOKEN_QUERY = 0x0008,
            TOKEN_QUERY_SOURCE = 0x0010,
            TOKEN_ADJUST_PRIVILEGES = 0x0020,
            TOKEN_ADJUST_GROUPS = 0x0040,
            TOKEN_ADJUST_DEFAULT = 0x0080,
            TOKEN_ADJUST_SESSIONID = 0x0100,
            TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY),
            TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
                TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
                TOKEN_ADJUST_SESSIONID)
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }
    }
}
