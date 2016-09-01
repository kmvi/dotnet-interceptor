using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETInterceptor
{
    // TODO: cross domain calls
    public abstract class RemotingPrecode : Precode
    {
        protected RemotingPrecode(IntPtr methodPtr)
            : base(methodPtr)
        {
        }

        public new static RemotingPrecode Create(IntPtr methodPtr)
        {
            switch (Utils.CurrentArchitecture) {
                case Architecture.X86:
                    return new RemotingPrecodeX86(methodPtr);
                case Architecture.X64:
                    return new RemotingPrecodeX64(methodPtr);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        public static bool IsRemotingPrecode(IntPtr methodPtr)
        {
            switch (Utils.CurrentArchitecture) {
                case Architecture.X86:
                    return RemotingPrecodeX86.IsRemotingPrecode(methodPtr);
                case Architecture.X64:
                    return RemotingPrecodeX64.IsRemotingPrecode(methodPtr);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        /*
         *    0:   b8 08 79 10 5f          mov    eax,0x5f107908 ; pMethodDesc
         *    5:   90                      nop
         *    6:   e8 59 df ff ff          call   0xffffdf64 ; PrecodeRemotingThunk
         *    b:   e9 74 2d 83 05          jmp    0x5832d84 ; ThePreStub or CompiledMethod
         */
        private unsafe class RemotingPrecodeX86 : RemotingPrecode
        {
            public RemotingPrecodeX86(IntPtr methodPtr)
                : base(methodPtr)
            {
            }

            // TODO: cross-domain call
            public override IntPtr TargetPtr
            {
                get
                {
                    var ptr = JmpToTargetPtr.ToBytePtr();
                    var offset = *(int*)(ptr + 1);
                    return new IntPtr(ptr + offset + 5);
                }
            }

            public override IntPtr MethodDescPtr
            {
                get
                {
                    var ptr = _methodPtr.ToBytePtr();
                    return new IntPtr(*(int*)(ptr + 1));
                }
            }

            public new static bool IsRemotingPrecode(IntPtr methodPtr)
            {
                var ptr = (ulong *)methodPtr.ToPointer();
                var code1 = *ptr++;
                var code2 = *ptr;

                return (code1 & 0x00FFFF00000000FFUL) == 0x00E89000000000B8UL &&
                    (code2 & 0x00000000FF000000UL) == 0x00000000E9000000;
            }

            public override IntPtr JmpToTargetPtr
            {
                get { return _methodPtr.Plus(11); }
            }
        }

        /*
         *     0:   48 85 c9                test   rcx,rcx
         *     3:   74 12                   je     0x17 ; local call
         *     5:   48 8b 01                mov    rax,QWORD PTR [rcx]
         *     8:   49 ba 18 00 17 ff f5    movabs r10,0x7ff5ff170018 ; ProxyAddress
         *     f:   7f 00 00
         *    12:   49 3b c2                cmp    rax,r10
         *    15:   74 0c                   je     0x23 ; remote call
         *    17:   48 b8 d0 ba 1c 58 ff    movabs rax,0x7fff581cbad0 ; ThePreStub
         *    1e:   7f 00 00
         *    21:   ff e0                   jmp    rax
         *    23:   49 ba 50 ca d3 57 ff    movabs r10,0x7fff57d3ca50 ; pMethodDesc
         *    2a:   7f 00 00
         *    2d:   48 b8 10 4a 2c 59 ff    movabs rax,0x7fff592c4a10 ; RemotingCheck
         *    34:   7f 00 00
         *    37:   ff e0                   jmp    rax
         */
        private unsafe class RemotingPrecodeX64 : RemotingPrecode
        {
            public RemotingPrecodeX64(IntPtr methodPtr)
                : base(methodPtr)
            {
            }

            // TODO: cross-domain call
            public override IntPtr TargetPtr
            {
                get
                {
                    var ptr = _methodPtr.ToBytePtr() + 0x17;
                    var absAddr = *(long*)(ptr + 2);
                    return new IntPtr(absAddr);
                }
            }

            public override IntPtr MethodDescPtr
            {
                get
                {
                    var ptr = _methodPtr.ToBytePtr() + 0x23;
                    return new IntPtr(*(long*)(ptr + 2));
                }
            }

            public new static bool IsRemotingPrecode(IntPtr methodPtr)
            {
                var code = *(uint *)methodPtr.ToPointer();
                return code == 0x74C98548U;
            }

            // returns TargetPtr since absolute jmp used
            public override IntPtr JmpToTargetPtr
            {
                get { return TargetPtr; }
            }
        }
    }
}
