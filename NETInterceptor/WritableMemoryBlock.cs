using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public class WritableMemoryBlock : MemoryBlock, IDisposable
    {
        private readonly MemoryBlock _block;
        private readonly MEMORY_PROTECTION_CONSTANTS _oldProtect;
        private bool _disposed;

        public WritableMemoryBlock(IntPtr address, int size)
            : base(address, size)
        {
            _block = null;

            var result = VirtualProtect(_address, _size,
                MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE,
                out _oldProtect);

            if (!result)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public WritableMemoryBlock(MemoryBlock block)
            : this(block.Address, block.Size)
        {
            _block = block;
        }

        public override void Dispose()
        {
            if (!_disposed) {
                MEMORY_PROTECTION_CONSTANTS tmp;
                VirtualProtect(_address, _size, _oldProtect, out tmp);
                if (_block != null)
                    _block.Dispose();
                _disposed = true;
            }
        }

        [DllImport("kernel32")]
        private static extern unsafe bool VirtualProtect(
            IntPtr lpAddress, int dwSize,
            MEMORY_PROTECTION_CONSTANTS flNewProtect,
            out MEMORY_PROTECTION_CONSTANTS lpflOldProtect);

        [Flags]
        private enum MEMORY_PROTECTION_CONSTANTS
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
