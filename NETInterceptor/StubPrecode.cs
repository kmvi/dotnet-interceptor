using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETInterceptor
{
    public abstract class StubPrecode : Precode
    {
        protected StubPrecode(IntPtr methodPtr)
            : base(methodPtr)
        {
        }       

        public new static StubPrecode Create(IntPtr methodPtr)
        {
            switch (Utils.CurrentArchitecture) {
                case Architecture.X86:
                    return new StubPrecodeX86(methodPtr);
                case Architecture.X64:
                    return new StubPrecodeX64(methodPtr);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        public static bool IsStubPrecode(IntPtr methodPtr)
        {
            switch (Utils.CurrentArchitecture) {
                case Architecture.X86:
                    return StubPrecodeX86.IsStubPrecode(methodPtr);
                case Architecture.X64:
                    return StubPrecodeX64.IsStubPrecode(methodPtr);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        /*
         *    0:   b8 c0 79 10 5f          mov    eax,0x5f1079c0 ; pMethodDesc
         *    5:   89 ed                   mov    ebp,ebp
         *    7:   e9 38 eb 00 00          jmp    0xeb44 ; ThePreStub or CompiledMethod
         */
        private unsafe class StubPrecodeX86 : StubPrecode
        {
            public StubPrecodeX86(IntPtr methodPtr)
                : base(methodPtr)
            {
            }

            public override IntPtr TargetPtr
            {
                get
                {
                    var ptr = (byte*)JmpToTargetPtr.ToPointer();
                    var offset = *(int *)(ptr + 1);
                    return new IntPtr(ptr + offset + 5);
                }
            }

            public override IntPtr MethodDescPtr
            {
                get
                {
                    var ptr = (byte*)_methodPtr.ToPointer();
                    return new IntPtr(*(int*)(ptr + 1));
                }
            }

            public new static bool IsStubPrecode(IntPtr methodPtr)
            {
                var code = *(ulong *)methodPtr.ToPointer();
                return (code & 0xFFFFFF00000000FF) == 0xE9ED8900000000B8;
            }

            public override IntPtr JmpToTargetPtr
            {
                get { return IntPtr.Add(_methodPtr, 7); }
            }
        }

        /*
         *    0:   49 ba b0 73 d4 57 ff    movabs r10,0x7fff57d473b0 ; pMethodDesc 
         *    7:   7f 00 00
         *    a:   40 e9 e8 d7 fb ff       rex jmp 0xfffffffffffbd7f8 ; ThePreStub or CompiledMethod
         */
        private unsafe class StubPrecodeX64 : StubPrecode
        {
            public StubPrecodeX64(IntPtr methodPtr)
                : base(methodPtr)
            {
            }

            public override IntPtr TargetPtr
            {
                get
                {
                    var ptr = (byte*)JmpToTargetPtr.ToPointer();
                    var offset = *(int*)(ptr + 2);
                    return new IntPtr(ptr + offset + 6);
                }
            }

            public override IntPtr MethodDescPtr
            {
                get
                {
                    var ptr = (byte*)_methodPtr.ToPointer();
                    return new IntPtr(*(long*)(ptr + 2));
                }
            }

            public new static bool IsStubPrecode(IntPtr methodPtr)
            {
                var ptr = (ulong *)methodPtr.ToPointer();
                var code1 = *ptr++;
                var code2 = *ptr;

                return (code1 & 0x000000000000FFFFUL) == 0x000000000000BA49UL &&
                    (code2 & 0x00000000FFFF0000UL) == 0x00000000E9400000UL;
            }

            public override IntPtr JmpToTargetPtr
            {
                get { return IntPtr.Add(_methodPtr, 10); }
            }
        }
    }
}
