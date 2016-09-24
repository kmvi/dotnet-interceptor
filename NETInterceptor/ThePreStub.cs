using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public static class ThePreStub
    {
        private static readonly Lazy<IntPtr> _preStubPtr = new Lazy<IntPtr>(Create);

        public static IntPtr Address
        {
            get { return _preStubPtr.Value; }
        }

        private static IntPtr Create()
        {
            IntPtr precodePtr = Precode.DetectPrecode.GetPrecodePtr();
            Precode precode = Precode.Create(precodePtr);
            return precode.TargetPtr;
        }
    }
}
