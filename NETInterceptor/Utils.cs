using SharpDisasm;
using SharpDisasm.Udis86;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETInterceptor
{
    public enum Architecture
    {
        X86,
        X64,
    }

    public enum Runtime
    {
        CLR2 = 200,
        CLR4 = 400,
        CLR46 = 460, // RyuJIT
    }

    public static class Utils
    {
        public static Architecture CurrentArchitecture
        {
            get { return Environment.Is64BitProcess ? Architecture.X64 : Architecture.X86; }
        }

        public static Runtime CurrentRuntime
        {
            get {
                if (Environment.Version.Major == 2) {
                    return Runtime.CLR2;
                }
                
                if (Environment.Version.Major == 4) {
                    return Is46Runtime() ? Runtime.CLR46 : Runtime.CLR4;
                }
                
                throw new NotSupportedException("Unsupported runtime.");
            }
        }

        private static bool Is46Runtime()
        {
            var type = Type.GetType("System.Collections.Generic.IReadOnlyCollection`1");
            if (type != null) {
                type = type.MakeGenericType(typeof(int));
                return type.IsAssignableFrom(typeof(Stack<int>));
            }

            return false;
        }

        internal static unsafe void Dump(byte *ptr, int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length should be greater than zero.", "length");

            while (length-- > 0) {
                Debug.Write((*ptr++).ToString("X2"));
            }

            Debug.WriteLine(String.Empty);
        }

        internal static unsafe void Dump(IntPtr ptr, int length)
        {
            Dump((byte *)ptr.ToPointer(), length);
        }

        public static unsafe byte[] ReadBlock(byte* ptr, int length)
        {
            var result = new byte[length];
            int i = 0;
            while (i < length) {
                result[i++] = *ptr++;
            }
            return result;
        }

        public static unsafe byte[] ReadBlock(IntPtr ptr, int length)
        {
            return ReadBlock((byte*)ptr.ToPointer(), length);
        }

        internal static ArchitectureMode ToArchitectureMode(this Architecture arch)
        {
            switch (arch) {
                case Architecture.X86:
                    return SharpDisasm.ArchitectureMode.x86_32;
                case Architecture.X64:
                    return SharpDisasm.ArchitectureMode.x86_64;
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        internal unsafe static byte* ToBytePtr(this IntPtr ptr)
        {
            return (byte*)ptr.ToPointer();
        }

        public static IntPtr FollowRelJmp(IntPtr ptr)
        {
            var archMode = Utils.CurrentArchitecture.ToArchitectureMode();

            while (true) {
                var disasm = new Disassembler(ptr, 20, archMode);
                var instr = disasm.NextInstruction();
                if (instr.Mnemonic != ud_mnemonic_code.UD_Ijmp) {
                    break;
                }
                Debug.Assert(instr.Operands.Length == 1);
                ptr = IntPtr.Add(ptr, instr.Operands[0].LvalSDWord + 5);
            }

            return ptr;
        }

        public static IntPtr FindNearestOp(IntPtr ptr, int minOffset)
        {
            int len = 0;
            var archMode = Utils.CurrentArchitecture.ToArchitectureMode();
            var disasm = new Disassembler(ptr, 20, archMode);

            while (len < minOffset) {                
                var instr = disasm.NextInstruction();
                len += instr.Length;
                ptr = IntPtr.Add(ptr, instr.Length);
            }

            return ptr;
        }

        public static int UpperPowerOfTwo(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            return value;
        }

        public static T[] ConcatArray<T>(T[] a1, T[] a2)
        {
            var result = new T[a1.Length + a2.Length];
            a1.CopyTo(result, 0);
            a2.CopyTo(result, a1.Length);
            return result;
        }
    }
}
