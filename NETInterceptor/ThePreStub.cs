using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public abstract class ThePreStub
    {
        private static readonly Lazy<ThePreStub> _instance = new Lazy<ThePreStub>(Create);
        private readonly IntPtr _preStubPtr;

        protected ThePreStub(IntPtr preStubPtr)
        {
            _preStubPtr = preStubPtr;
        }

        public static ThePreStub Instance 
        {
            get { return _instance.Value; }
        }

        public IntPtr ThePreStubPtr { get { return _preStubPtr; } }

        public abstract IntPtr PreStubWorker { get; }

        private static ThePreStub Create()
        {
            var precode = Precode.Create(Precode.DetectPrecode.GetPrecodePtr());

            switch (Utils.CurrentArchitecture) {
                case Architecture.X86:
                    return new ThePreStubX86(precode.TargetPtr);
                case Architecture.X64:
                    return new ThePreStubX64(precode.TargetPtr);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        /*
               0:   55                      push   ebp
               1:   8b ec                   mov    ebp,esp
               3:   53                      push   ebx
               4:   56                      push   esi
               5:   57                      push   edi
               6:   51                      push   ecx
               7:   52                      push   edx
               8:   8b f4                   mov    esi,esp
               a:   50                      push   eax
               b:   56                      push   esi
               c:   e8 ac 62 0b 00          call   0xb62bd ; PreStubWorker
              11:   5a                      pop    edx
              12:   59                      pop    ecx
              13:   5f                      pop    edi
              14:   5e                      pop    esi
              15:   5b                      pop    ebx
              16:   5d                      pop    ebp
              17:   ff e0                   jmp    eax
              19:   c3                      ret
         */
        private unsafe class ThePreStubX86 : ThePreStub
        {
            public ThePreStubX86(IntPtr preStubPtr)
                : base(preStubPtr)
            {
            }

            public override IntPtr PreStubWorker
            {
                get
                {
                    var ptr = ThePreStubPtr.ToBytePtr() + 0xC;
                    var offset = *(int*)(ptr + 1);
                    return new IntPtr(ptr + offset + 5);
                }
            }
        }

        private unsafe class ThePreStubX64 : ThePreStub
        {
            public ThePreStubX64(IntPtr preStubPtr)
                : base(preStubPtr)
            {
            }

            public override IntPtr PreStubWorker
            {
                get {
                    if (Utils.CurrentRuntime == Runtime.CLR2) {
                        throw new NotImplementedException();
                    }

                    if (Utils.CurrentRuntime == Runtime.CLR4) {
                        throw new NotImplementedException();
                    }

                    if (Utils.CurrentRuntime == Runtime.CLR46) {
                        #region listing
                        /*
                        0:   41 57                   push   r15
                        2:   41 56                   push   r14
                        4:   41 55                   push   r13
                        6:   41 54                   push   r12
                        8:   55                      push   rbp
                        9:   53                      push   rbx
                        a:   56                      push   rsi
                        b:   57                      push   rdi
                        c:   48 83 ec 68             sub    rsp,0x68
                        10:   48 89 8c 24 b0 00 00    mov    QWORD PTR [rsp+0xb0],rcx
                        17:   00
                        18:   48 89 94 24 b8 00 00    mov    QWORD PTR [rsp+0xb8],rdx
                        1f:   00
                        20:   4c 89 84 24 c0 00 00    mov    QWORD PTR [rsp+0xc0],r8
                        27:   00
                        28:   4c 89 8c 24 c8 00 00    mov    QWORD PTR [rsp+0xc8],r9
                        2f:   00
                        30:   66 0f 7f 44 24 20       movdqa XMMWORD PTR [rsp+0x20],xmm0
                        36:   66 0f 7f 4c 24 30       movdqa XMMWORD PTR [rsp+0x30],xmm1
                        3c:   66 0f 7f 54 24 40       movdqa XMMWORD PTR [rsp+0x40],xmm2
                        42:   66 0f 7f 5c 24 50       movdqa XMMWORD PTR [rsp+0x50],xmm3
                        48:   48 8d 4c 24 68          lea    rcx,[rsp+0x68]
                        4d:   49 8b d2                mov    rdx,r10
                        50:   e8 eb c3 00 00          call   0xc440 ; <------------ PreStubWorker
                        55:   66 0f 6f 44 24 20       movdqa xmm0,XMMWORD PTR [rsp+0x20]
                        5b:   66 0f 6f 4c 24 30       movdqa xmm1,XMMWORD PTR [rsp+0x30]
                        61:   66 0f 6f 54 24 40       movdqa xmm2,XMMWORD PTR [rsp+0x40]
                        67:   66 0f 6f 5c 24 50       movdqa xmm3,XMMWORD PTR [rsp+0x50]
                        6d:   48 8b 8c 24 b0 00 00    mov    rcx,QWORD PTR [rsp+0xb0]
                        74:   00
                        75:   48 8b 94 24 b8 00 00    mov    rdx,QWORD PTR [rsp+0xb8]
                        7c:   00
                        7d:   4c 8b 84 24 c0 00 00    mov    r8,QWORD PTR [rsp+0xc0]
                        84:   00
                        85:   4c 8b 8c 24 c8 00 00    mov    r9,QWORD PTR [rsp+0xc8]
                        8c:   00
                        8d:   48 83 c4 68             add    rsp,0x68
                        91:   5f                      pop    rdi
                        92:   5e                      pop    rsi
                        93:   5b                      pop    rbx
                        94:   5d                      pop    rbp
                        95:   41 5c                   pop    r12
                        97:   41 5d                   pop    r13
                        99:   41 5e                   pop    r14
                        9b:   41 5f                   pop    r15
                        9d:   48 ff e0                rex.W jmp rax
                        a0:   a9 22 00 00 00          test   eax,0x22
                        a5:   c3                      ret
                        */
                        #endregion
                        var ptr = ThePreStubPtr.ToBytePtr() + 0xC;
                        var offset = *(int*)(ptr + 1);
                        return new IntPtr(ptr + offset + 5);
                    }

                    throw new NotSupportedException("Unsupported runtime.");
                }
            }
        }
    }
}
