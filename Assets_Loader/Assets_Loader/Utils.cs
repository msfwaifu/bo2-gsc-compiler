using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Assets_Loader
{
    internal static class Utils
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, int lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, [Out] int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, int lpBaseAddress, byte[] lpBuffer, uint nSize, [Out] int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, [Out] uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        private static IntPtr handle;
        private const int bytes = 0;

        public static bool ConnectToGame()
        {
            Process t6zm = Process.GetProcessesByName("t6zm").FirstOrDefault();
            Process t6mp = Process.GetProcessesByName("t6mp").FirstOrDefault();
            if (t6zm != null)
            {
                handle = OpenProcess(0x1f0fff, false, t6zm.Id);
                return true;
            }
            if (t6mp != null)
            {
                handle = OpenProcess(0x1f0fff, false, t6mp.Id);
                return true;
            }
            return false;
        }

        public static void Free(int pointer, int length)
        {
            VirtualFreeEx(handle, (IntPtr) pointer, (uint) length, 0x8000);
        }

        public static void CreateRemoteThread(int pointer)
        {
            CreateRemoteThread(handle, IntPtr.Zero, 0, (IntPtr) pointer, IntPtr.Zero, 0, bytes);
        }

        public static int Malloc(int length)
        {
            return (int) VirtualAllocEx(handle, IntPtr.Zero, (uint) length, 0x3000, 0x40);
        }

        public static void WriteInt(int pointer, int value)
        {
            Write(pointer, BitConverter.GetBytes(value));
        }

        public static int ReadInt(int pointer)
        {
            var buffer = new byte[4];
            ReadProcessMemory(handle, pointer, buffer, 4, bytes);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static byte[] Read(int pointer, int length)
        {
            var buffer = new byte[length];
            ReadProcessMemory(handle, pointer, buffer, buffer.Length, bytes);
            return buffer;
        }

        public static string ReadString(int pointer)
        {
            var strPointer = ReadInt(pointer);
            var sb = new StringBuilder();
            while (Read(strPointer, 1)[0] != 0)
            {
                sb.Append(Convert.ToChar((Read(strPointer, 1)[0])));
                strPointer++;
            }
            return sb.ToString();
        }

        public static void Write(int pointer, byte[] b)
        {
            uint oldprotect;
            VirtualProtectEx(handle, (IntPtr)pointer, (uint)b.Length, 0x40, out oldprotect);
            WriteProcessMemory(handle, pointer, b, (uint) b.Length, bytes);
        }
    }
}
