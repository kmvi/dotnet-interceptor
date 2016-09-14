using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public IntPtr Address { get { return _preStubPtr; } }

        public abstract IntPtr PreStubWorkerAddress { get; }

        private static ThePreStub Create()
        {
            IntPtr precodePtr = Precode.DetectPrecode.GetPrecodePtr();
            Precode precode = Precode.Create(precodePtr);

            switch (Env.CurrentArchitecture) {
                case Architecture.X86:
                    return new ThePreStubX86(precode.TargetPtr);
                case Architecture.X64:
                    return new ThePreStubX64(precode.TargetPtr);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        
        private unsafe class ThePreStubX86 : ThePreStub
        {
            public ThePreStubX86(IntPtr preStubPtr)
                : base(preStubPtr)
            {
            }

            public override IntPtr PreStubWorkerAddress
            {
                get
                {
                    #region listing
                    /*  
                        ThePreStub code:
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
                    #endregion

                    var ptr = Address.ToBytePtr();
                    Debug.Assert(*(ulong*)ptr == 0x5251575653EC8B55UL);
                    ptr += 0xC;
                    return new IntPtr(Utils.JmpOrCallDest(ptr));
                }
            }
        }

        private unsafe class ThePreStubX64 : ThePreStub
        {
            public ThePreStubX64(IntPtr preStubPtr)
                : base(preStubPtr)
            {
            }

            public override IntPtr PreStubWorkerAddress
            {
                get {
                    var ptr = Address.ToBytePtr();
                    if (Env.CurrentRuntime == Runtime.CLR2) {
                        #region listing
                        /*
                           0:   48 8d 44 24 08          lea    rax,[rsp+0x8]
                           5:   41 52                   push   r10
                           7:   41 57                   push   r15
                           9:   41 56                   push   r14
                           b:   41 55                   push   r13
                           d:   41 54                   push   r12
                           f:   55                      push   rbp
                          10:   53                      push   rbx
                          11:   56                      push   rsi
                          12:   57                      push   rdi
                          13:   50                      push   rax
                          14:   48 83 ec 78             sub    rsp,0x78
                          18:   48 89 8c 24 d0 00 00    mov    QWORD PTR [rsp+0xd0],rcx
                          1f:   00
                          20:   48 89 94 24 d8 00 00    mov    QWORD PTR [rsp+0xd8],rdx
                          27:   00
                          28:   4c 89 84 24 e0 00 00    mov    QWORD PTR [rsp+0xe0],r8
                          2f:   00
                          30:   4c 89 8c 24 e8 00 00    mov    QWORD PTR [rsp+0xe8],r9
                          37:   00
                          38:   66 0f 7f 44 24 20       movdqa XMMWORD PTR [rsp+0x20],xmm0
                          3e:   66 0f 7f 4c 24 30       movdqa XMMWORD PTR [rsp+0x30],xmm1
                          44:   66 0f 7f 54 24 40       movdqa XMMWORD PTR [rsp+0x40],xmm2
                          4a:   66 0f 7f 5c 24 50       movdqa XMMWORD PTR [rsp+0x50],xmm3
                          50:   e8 6f 8f ff ff          call   0xffffffffffff8fc4 ; <-------- PrestubMethodFrame::GetMethodFrameVPtr
                          55:   48 89 44 24 68          mov    QWORD PTR [rsp+0x68],rax
                          5a:   48 8b 05 1f 8f d8 ff    mov    rax,QWORD PTR [rip+0xffffffffffd88f1f]        # 0xffffffffffd88f80
                          61:   48 89 44 24 60          mov    QWORD PTR [rsp+0x60],rax
                          66:   e8 e5 cd ff ff          call   0xffffffffffffce50 ; <-------- GetThread
                          6b:   4c 8b e0                mov    r12,rax
                          6e:   49 8b 54 24 10          mov    rdx,QWORD PTR [r12+0x10]
                          73:   48 89 54 24 70          mov    QWORD PTR [rsp+0x70],rdx
                          78:   48 8d 4c 24 68          lea    rcx,[rsp+0x68]
                          7d:   49 89 4c 24 10          mov    QWORD PTR [r12+0x10],rcx
                          82:   e8 19 71 ee ff          call   0xffffffffffee71a0 ; <------- PreStubWorker
                          87:   49 8b 4c 24 10          mov    rcx,QWORD PTR [r12+0x10]
                          8c:   48 8b 51 08             mov    rdx,QWORD PTR [rcx+0x8]
                          90:   49 89 54 24 10          mov    QWORD PTR [r12+0x10],rdx
                          95:   66 0f 6f 44 24 20       movdqa xmm0,XMMWORD PTR [rsp+0x20]
                          9b:   66 0f 6f 4c 24 30       movdqa xmm1,XMMWORD PTR [rsp+0x30]
                          a1:   66 0f 6f 54 24 40       movdqa xmm2,XMMWORD PTR [rsp+0x40]
                          a7:   66 0f 6f 5c 24 50       movdqa xmm3,XMMWORD PTR [rsp+0x50]
                          ad:   48 8b 8c 24 d0 00 00    mov    rcx,QWORD PTR [rsp+0xd0]
                          b4:   00
                          b5:   48 8b 94 24 d8 00 00    mov    rdx,QWORD PTR [rsp+0xd8]
                          bc:   00
                          bd:   4c 8b 84 24 e0 00 00    mov    r8,QWORD PTR [rsp+0xe0]
                          c4:   00
                          c5:   4c 8b 8c 24 e8 00 00    mov    r9,QWORD PTR [rsp+0xe8]
                          cc:   00
                          cd:   90                      nop
                          ce:   48 81 c4 80 00 00 00    add    rsp,0x80
                          d5:   5f                      pop    rdi
                          d6:   5e                      pop    rsi
                          d7:   5b                      pop    rbx
                          d8:   5d                      pop    rbp
                          d9:   41 5c                   pop    r12
                          db:   41 5d                   pop    r13
                          dd:   41 5e                   pop    r14
                          df:   41 5f                   pop    r15
                          e1:   41 5a                   pop    r10
                          e3:   48 ff e0                rex.W jmp rax
                        */
                        #endregion

                        Debug.Assert(*(ulong*)ptr == 0x4152410824448D48UL);
                        ptr += 0x82;
                        return new IntPtr(Utils.JmpOrCallDest(ptr));
                    }

                    if (Env.CurrentRuntime == Runtime.CLR4) {
                        throw new NotImplementedException();
                    }

                    if (Env.CurrentRuntime == Runtime.CLR46) {
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

                        Debug.Assert(*(ulong*)ptr == 0x5441554156415741UL);
                        ptr += 0x50;
                        return new IntPtr(Utils.JmpOrCallDest(ptr));
                    }

                    throw new NotSupportedException("Unsupported runtime.");
                }
            }
        }
    }
}
