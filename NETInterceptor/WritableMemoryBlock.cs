using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public class WritableMemoryBlock : IDisposable
    {
        private readonly IntPtr _block;
        private readonly MEMORY_PROTECTION_CONSTANTS _oldProtect;
        private readonly uint _size;
        private bool _disposed;

        public WritableMemoryBlock(IntPtr pBlock, int size)
        {
            _block = pBlock;
            _size = (uint)size;

            var r = VirtualProtect(_block, _size, MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE, out _oldProtect);

            if (!r)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public static explicit operator IntPtr(WritableMemoryBlock block)
        {
            if (block._disposed)
                throw new ObjectDisposedException("WritableMemoryBlock");

            return block._block;
        }

        public void Dispose()
        {
            if (!_disposed) {
                MEMORY_PROTECTION_CONSTANTS tmp;
                var r = VirtualProtect(_block, _size, _oldProtect, out tmp);
                _disposed = true;
            }
        }

        [DllImport("kernel32")]
        public static extern unsafe bool VirtualProtect(IntPtr lpAddress, uint dwSize, MEMORY_PROTECTION_CONSTANTS flNewProtect, out MEMORY_PROTECTION_CONSTANTS lpflOldProtect);

        [Flags]
        public enum MEMORY_PROTECTION_CONSTANTS
        {
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
            PAGE_TARGETS_INVALID = 0x40000000,
            PAGE_TARGETS_NO_UPDATE = 0x40000000,
        }
    }
}
