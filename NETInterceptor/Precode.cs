using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public abstract class Precode
    {
        protected readonly IntPtr _methodPtr;

        protected Precode(IntPtr methodPtr)
        {
            _methodPtr = methodPtr;
        }

        public abstract IntPtr JmpToTargetPtr { get; }

        /// <summary>
        /// ThePreStub or compiled method address
        /// </summary>
        public abstract IntPtr TargetPtr { get; }

        public abstract IntPtr MethodDescPtr { get; }

        public static Precode Create(IntPtr methodPtr)
        {
            if (StubPrecode.IsStubPrecode(methodPtr)) {
                return StubPrecode.Create(methodPtr);
            }
            
            if (FixupPrecode.IsFixupPrecode(methodPtr)) {
                return FixupPrecode.Create(methodPtr);
            }
            
            if (RemotingPrecode.IsRemotingPrecode(methodPtr)) {
                return RemotingPrecode.Create(methodPtr);
            }

            throw new NotSupportedException("Not supported precode type.");
        }

        public static bool HasPrecode(IntPtr methodPtr)
        {
            return StubPrecode.IsStubPrecode(methodPtr) ||
                FixupPrecode.IsFixupPrecode(methodPtr) ||
                RemotingPrecode.IsRemotingPrecode(methodPtr);
        }

        internal static class DetectPrecode
        {
            public static IntPtr GetPrecodePtr()
            {
                var func = new __EmptyDelegate(__Empty);
                var gc = GCHandle.Alloc(func);
                var ptr = func.Method.MethodHandle.GetFunctionPointer();
                gc.Free();
                return ptr;
            }

            private delegate void __EmptyDelegate();

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private static void __Empty()
            {
            }
        }
    }
}
