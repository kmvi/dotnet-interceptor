using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public class GlobalMemoryBlock : IDisposable
    {
        private readonly IntPtr _address;
        private readonly int _size;
        private bool _disposed;

        private GlobalMemoryBlock(IntPtr address, int size)
        {
            if (address == IntPtr.Zero)
                throw new ArgumentException("");

            if (size <= 0)
                throw new ArgumentException();

            _address = address;
            _size = size;
            _disposed = false;
        }

        public IntPtr Address
        {
            get
            {
                EnsureNotDisposed();
                return _address;
            }
        }

        public int Size
        {
            get
            {
                EnsureNotDisposed();
                return _size;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("MemoryBlock");
        }

        public void Dispose()
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
