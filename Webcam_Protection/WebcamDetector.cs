using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Webcam_Protection
{
    public class WebcamDetector
    {
        [DllImport("ntdll.dll")]
        public static extern int NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, ref int ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_INFORMATION
        {
            public ushort ProcessId;
            public byte ObjectTypeNumber;
            public byte Flags;
            public ushort Handle;
            public IntPtr Object;
            public uint GrantedAccess;
        }

        const int SystemHandleInformation = 16;

        public static List<int> GetProcessesUsingDevice(string devicePath)
        {
            int length = 0;
            NtQuerySystemInformation(SystemHandleInformation, IntPtr.Zero, 0, ref length);

            IntPtr ptr = Marshal.AllocHGlobal(length);
            NtQuerySystemInformation(SystemHandleInformation, ptr, length, ref length);

            int handleCount = Marshal.ReadInt32(ptr);
            IntPtr currentPtr = ptr + 4;

            List<int> processIds = new List<int>();

            for (int i = 0; i < handleCount; i++)
            {
                SYSTEM_HANDLE_INFORMATION handleInfo = Marshal.PtrToStructure<SYSTEM_HANDLE_INFORMATION>(currentPtr);
                currentPtr += Marshal.SizeOf(typeof(SYSTEM_HANDLE_INFORMATION));

                try
                {
                    Process process = Process.GetProcessById(handleInfo.ProcessId);
                    string processName = process.ProcessName;

                    // Kiểm tra nếu process giữ handle đúng với đường dẫn thiết bị (webcam)
                    if (GetDevicePath(handleInfo.Handle, handleInfo.ProcessId).Contains(devicePath))
                    {
                        Console.WriteLine($"🎯 Process đang dùng webcam: {processName} (PID: {handleInfo.ProcessId})");
                        processIds.Add(handleInfo.ProcessId);
                    }
                }
                catch { }
            }

            Marshal.FreeHGlobal(ptr);
            return processIds;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll")]
        private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, ushort hSourceHandle,
            IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern uint GetFinalPathNameByHandle(IntPtr hFile, StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        private static string GetDevicePath(ushort handle, int processId)
        {
            IntPtr processHandle = OpenProcess(0x0010, false, processId);
            if (processHandle == IntPtr.Zero) return string.Empty;

            if (!DuplicateHandle(processHandle, handle, Process.GetCurrentProcess().Handle,
                                 out IntPtr duplicatedHandle, 0, false, 2))
            {
                CloseHandle(processHandle);
                return string.Empty;
            }

            StringBuilder path = new StringBuilder(1024);
            GetFinalPathNameByHandle(duplicatedHandle, path, 1024, 0);

            CloseHandle(duplicatedHandle);
            CloseHandle(processHandle);

            return path.ToString();
        }
    }
}
