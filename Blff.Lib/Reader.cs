using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace blff
{
    public class Reader
    {
        private List<Win32.MemoryBasicInformation> MemBasicInfo { get; set; }

        public IntPtr GetPointer(System.Diagnostics.Process process, byte[] pattern)
        {
            if (process == null)
                throw new Exception("Process cannot be null");

            MemBasicInfo = new List<Win32.MemoryBasicInformation>();
            GetMemoryInfo(process.Handle);

            for (var i = 0; i < MemBasicInfo.Count; i++)
            {
                var buffer = new byte[MemBasicInfo[i].RegionSize];
                Win32.ReadProcessMemory(process.Handle, MemBasicInfo[i].BaseAddress, buffer, MemBasicInfo[i].RegionSize, 0);

                var result = Search(buffer, pattern);

                if (result != IntPtr.Zero)
                    return new IntPtr(MemBasicInfo[i].BaseAddress.ToInt32() + result.ToInt32());
            }

            return IntPtr.Zero;
        }

        private void GetMemoryInfo(IntPtr pHandle)
        {
            var address = new IntPtr();

            while (true)
            {
                var memInfo = new Win32.MemoryBasicInformation();
                var dump = Win32.VirtualQueryEx(pHandle, address, out memInfo, Marshal.SizeOf(memInfo));

                if (dump == 0)
                    break;

                if ((memInfo.State & 0x1000) != 0 && (memInfo.Protect & 0x100) == 0)
                    MemBasicInfo.Add(memInfo);

                address = new IntPtr(memInfo.BaseAddress.ToInt32() + memInfo.RegionSize);
            }
        }

        private IntPtr Search(byte[] sIn, byte[] sFor)
        {
            var sBytes = new int[256];
            var pool = 0;
            var end = sFor.Length - 1;

            for (var i = 0; i < 256; i++)
                sBytes[i] = sFor.Length;

            for (var i = 0; i < end; i++)
                sBytes[sFor[i]] = end - i;

            while (pool <= sIn.Length - sFor.Length)
            {
                for (var i = end; sIn[pool + i] == sFor[i]; i--)
                    if (i == 0) return new IntPtr(pool);

                pool += sBytes[sIn[pool + end]];
            }

            return IntPtr.Zero;
        }
    }
}