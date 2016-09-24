using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public class MethodDesc
    {
        private static Lazy<IntPtr> _doPreStubPtr = new Lazy<IntPtr>(FindDoPreStub);

        private readonly IntPtr _md;

        public MethodDesc(IntPtr methodDesc)
        {
            _md = methodDesc;
        }

        public IntPtr DoPreStub()
        {
            using (var mem = new WritableMemoryBlock(GlobalMemoryBlock.Allocate(50))) {
                WriteCode(mem, _md);

                var doPreStub = (__DoPreSub)Marshal.GetDelegateForFunctionPointer(
                    mem.Address, typeof(__DoPreSub));

                return new IntPtr(doPreStub(_md.ToInt64(), 0L));
            }
        }

        private static void WriteCode(MemoryBlock mem, IntPtr md)
        {
            // DoPreStub uses __fastcall
            // so we need to put it's first argument (methodDesc) in rcx/ecx
            // and the second (methodTable, should be NULL) in rdx/edx

            var code = new CodeBlock();

            // mov rax, DoPreStubAdderss
            code.Append(0x48).Append(0xB8).AppendLong(_doPreStubPtr.Value.ToInt64());
            // mov rcx, methodDesc
            code.Append(0x48).Append(0xB9).AppendLong(md.ToInt64());
            // xor rdx, rdx
            code.Append(0x48).Append(0x31).Append(0xD2);
            // jmp rax
            code.Append(0xFF).Append(0xE0);

            code.WriteTo(mem.Address);
        }

        private static readonly byte[][] _patterns = new byte[][] {
            new byte[83] {
                0x55, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x8D, 0xAC, 0x24, 0x30,
                0xFB, 0xFF, 0xFF, 0x48, 0x81, 0xEC, 0xD0, 0x05, 0x00, 0x00, 0x48, 0xC7, 0x85, 0xB8, 0x00, 0x00,
                0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0x48, 0x89, 0x9C, 0x24, 0x20, 0x06, 0x00, 0x00, 0x48, 0x8B, 0x05,
                0xAA, 0xAA, 0xAA, 0xAA, 0x48, 0x33, 0xC4, 0x48, 0x89, 0x85, 0xC0, 0x04, 0x00, 0x00, 0x48, 0x89,
                0x55, 0xB8, 0x4C, 0x8B, 0xF9, 0x45, 0x33, 0xED, 0x4C, 0x89, 0x6C, 0x24, 0x60, 0xE8, 0xAA, 0xAA,
                0xAA, 0xAA, 0x4C
            },
            new byte[43] {
                0x48, 0x89, 0x54, 0x24, 0x10, 0x55, 0x53, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41,
                0x57, 0x48, 0x8D, 0xAC, 0x24, 0xAA, 0xFF, 0xFF, 0xFF, 0x48, 0x81, 0xEC, 0xAA, 0x01, 0x00, 0x00,
                0x48, 0xC7, 0x45, 0xAA, 0xFE, 0xFF, 0xFF, 0xFF, 0x48, 0x8B, 0xAA
            },
            new byte[48] {
                0x55, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x8D, 0xAC, 0x24, 0x30,
                0xFA, 0xFF, 0xFF, 0x48, 0x81, 0xEC, 0xD0, 0x06, 0x00, 0x00, 0x48, 0xC7, 0x85, 0x18, 0x01, 0x00,
                0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0x48, 0x89, 0x9C, 0x24, 0x20, 0x07, 0x00, 0x00, 0x48, 0x8B, 0x05
            },
            new byte[49] {
                0x48, 0x89, 0x54, 0x24, 0x10, 0x55, 0x53, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41,
                0x57, 0x48, 0x8D, 0x6C, 0x24, 0xE1, 0x48, 0x81, 0xEC, 0xD8, 0x00, 0x00, 0x00, 0x48, 0xC7, 0x45,
                0xFF, 0xFE, 0xFF, 0xFF, 0xFF, 0x48, 0x8B, 0xF2, 0x48, 0x8B, 0xF9, 0x45, 0x33, 0xE4, 0x45, 0x8B,
                0xF4
            },
            new byte[45] {
                0x55, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x8D, 0xAC, 0x24, 0x80,
                0xFC, 0xFF, 0xFF, 0x48, 0x81, 0xEC, 0x80, 0x04, 0x00, 0x00, 0x48, 0xC7, 0x85, 0x18, 0x01, 0x00,
                0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0x48, 0x89, 0x9C, 0x24, 0xD0, 0x04, 0x00, 0x00
            },
            new byte[45] {
                0x55, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41,  0x56, 0x41, 0x57, 0x48, 0x8D, 0xAC, 0x24, 0x60,
                0xFC, 0xFF, 0xFF, 0x48, 0x81, 0xEC, 0xA0, 0x04,  0x00, 0x00, 0x48, 0xC7, 0x85, 0xAA, 0xAA, 0x00,
                0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0x48, 0x89, 0x9C,  0x24, 0xF0, 0x04, 0x00, 0x00
            },
            new byte[45] {
                0x55, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41,  0x56, 0x41, 0x57, 0x48, 0x8D, 0xAC, 0x24, 0x20,
                0xFB, 0xFF, 0xFF, 0x48, 0x81, 0xEC, 0xE0, 0x05,  0x00, 0x00, 0x48, 0xC7, 0x85, 0xC0, 0x00, 0x00,
                0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0x48, 0x89, 0x9C,  0x24, 0x30, 0x06, 0x00, 0x00
            },
        };

        private static IntPtr FindDoPreStub()
        {
            if (Env.CurrentRuntime == Runtime.CLR2) {
                throw new NotImplementedException();
            }

            if (Env.CurrentRuntime >= Runtime.CLR4) {
                ProcessModule clrModule = FindClrModule("clr.dll");
                CheckModuleVersion(clrModule.FileVersionInfo);

                ulong clrOffset = (ulong)clrModule.BaseAddress.ToInt64();
                ulong clrSize = (ulong)clrModule.ModuleMemorySize;

                IntPtr doPreStubAddr = IntPtr.Zero;
                foreach (byte[] pattern in _patterns) {
                    doPreStubAddr = FindFunction(clrOffset, clrOffset + clrSize, _patterns[1]);
                    if (doPreStubAddr != IntPtr.Zero)
                        break;
                }

                if (doPreStubAddr == IntPtr.Zero)
                    throw new InvalidOperationException();

                return doPreStubAddr;
            }

            throw new NotSupportedException("Unsupported runtime.");
        }

        private delegate long __DoPreSub(long md, long mt);

        private static unsafe IntPtr FindFunction(ulong lo, ulong hi, byte[] pattern)
        {
            ulong patLen = (ulong)pattern.LongLength;
            fixed (byte* ptr = pattern) {
                if (lo > 0 && hi > 0 && hi >= lo) {
                    while (lo < hi - patLen + 1) {
                        for (ulong i = 0; ; ++i) {
                            if (i >= patLen)
                                return new IntPtr((long)lo);
                            byte b = *(ptr + i);
                            byte b1 = *(byte*)((long)(lo + i));
                            if (b != 0xAA && b != b1)
                                break;
                        }
                        ++lo;
                    }
                }
            }

            return IntPtr.Zero;
        }

        private static ProcessModule FindClrModule(string moduleName)
        {
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules) {
                if (module.ModuleName.Equals(moduleName, StringComparison.InvariantCultureIgnoreCase)) {
                    return module;
                }
            }

            throw new InvalidOperationException("Module clr.dll not found in process memory.");
        }

        private static void CheckModuleVersion(FileVersionInfo version)
        {
            var minVersion = new Version(4, 0, 30319, 1);
            var curVersion = new Version(version.FileMajorPart, version.FileMinorPart,
                version.FileBuildPart, version.FilePrivatePart);

            if (curVersion < minVersion)
                throw new NotSupportedException("Runtime version " + curVersion + " not supported.");
        }
    }
}
