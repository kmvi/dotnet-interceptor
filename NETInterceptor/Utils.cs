using SharpDisasm;
using SharpDisasm.Udis86;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

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
            get { return IntPtr.Size == 8 ? Architecture.X64 : Architecture.X86; }
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
                // TODO: slow
                var disasm = new Disassembler(ptr, 20, archMode);
                var instr = disasm.NextInstruction();
                if (instr.Mnemonic != ud_mnemonic_code.UD_Ijmp) {
                    break;
                }
                Debug.Assert(instr.Operands.Length == 1);
                ptr = ptr.Plus(instr.Operands[0].LvalSDWord + 5);
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
                ptr = ptr.Plus(instr.Length);
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

        public static object UnsafeInvoke(this MethodInfo @this, object value, object[] args)
        {
            if (@this.GetType() != _rmiType)
                throw new InvalidOperationException();

            var signature = _signature.GetValue(@this, null);
            var rmhType = typeof(RuntimeMethodHandle);

            var paramsCount = args != null ? args.Length : 0;
            var prms = new object[paramsCount];
            for (int i = 0; i < paramsCount; ++i)
                prms[i] = args[i];

            if (Utils.CurrentRuntime == Runtime.CLR2) {                
                var owner = new RuntimeTypeHandle();

                var cache = _rmiType.GetField("m_reflectedTypeCache", BindingFlags.Instance | BindingFlags.NonPublic);
                var isGlobal = cache.FieldType.GetProperty("IsGlobal", BindingFlags.Instance | BindingFlags.NonPublic);
                var declType = _rmiType.GetField("m_declaringType", BindingFlags.Instance | BindingFlags.NonPublic);
                if (!(bool)isGlobal.GetValue(cache.GetValue(@this), null))
                    owner = ((Type)declType.GetValue(@this)).TypeHandle;

                var methodAttr = _rmiType.GetField("m_methodAttributes", BindingFlags.Instance | BindingFlags.NonPublic);
                var invoke = rmhType.GetMethod("InvokeMethodFast", BindingFlags.Instance | BindingFlags.NonPublic);
                
                return invoke.Invoke(@this.MethodHandle, new object[] { value, paramsCount == 0 ? null : prms, signature, methodAttr.GetValue(@this), owner });
            } else if (Utils.CurrentRuntime >= Runtime.CLR4) {
                var invoke = rmhType.GetMethod("InvokeMethod", BindingFlags.Static | BindingFlags.NonPublic);
                return invoke.Invoke(null, new object[] { value, prms, signature, false });
            }

            for (int i = 0; i < paramsCount; ++i)
                args[i] = prms[i];

            throw new NotSupportedException("Unsupported runtime.");
        }

        public unsafe static IntPtr Plus(this IntPtr ptr, int offset)
        {
            return new IntPtr(ptr.ToBytePtr() + offset);
        }

        private static readonly Type _rmiType = Type.GetType("System.Reflection.RuntimeMethodInfo");
        private static readonly PropertyInfo _signature = _rmiType.GetProperty("Signature", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}
