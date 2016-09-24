using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public class GlobalMemoryBlock : MemoryBlock, IDisposable
    {
        private bool _disposed;

        private GlobalMemoryBlock(IntPtr address, int size)
            : base(address, size)
        {
            _disposed = false;
        }

        public override IntPtr Address
        {
            get
            {
                EnsureNotDisposed();
                return base.Address;
            }
        }

        public override int Size
        {
            get
            {
                EnsureNotDisposed();
                return base.Size;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("MemoryBlock");
        }

        public override void Dispose()
        {
            if (!_disposed) {
                Marshal.FreeHGlobal(_address);
                _disposed = true;
            }
        }

        public static GlobalMemoryBlock Allocate(int size)
        {
            return new GlobalMemoryBlock(Marshal.AllocHGlobal(size), size);
        }
    }
}
