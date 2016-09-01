using System;
using System.Collections.Generic;
using System.Text;

namespace NETInterceptor
{
    public struct MemoryBlock
    {
        public readonly IntPtr Address;
        public readonly int Size;

        public MemoryBlock(IntPtr address, int size)
        {
            Address = address;
            Size = size;
        }
    }
}
