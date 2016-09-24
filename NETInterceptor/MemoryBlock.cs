using System;
using System.Collections.Generic;
using System.Text;

namespace NETInterceptor
{
    public class MemoryBlock : IDisposable
    {
        protected readonly IntPtr _address;
        protected readonly int _size;

        public MemoryBlock(IntPtr address, int size)
        {
            if (address == IntPtr.Zero)
                throw new ArgumentException("");

            if (size <= 0)
                throw new ArgumentException();

            _address = address;
            _size = size;
        }

        public virtual IntPtr Address
        {
            get { return _address; }
        }

        public virtual int Size
        {
            get { return _size; }
        }

        public virtual void Dispose()
        {
        }
    }
}
