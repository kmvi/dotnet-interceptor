using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public static class Utils
    {
        internal static unsafe void Dump(byte *ptr, int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length should be greater than zero.", "length");

            var sb = new StringBuilder(length);
            while (length-- > 0) {
                sb.Append((*ptr++).ToString("X2"));
            }

            Debug.WriteLine(sb.ToString());
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

        internal unsafe static byte* ToBytePtr(this IntPtr ptr)
        {
            return (byte*)ptr.ToPointer();
        }

        public static IntPtr FollowRelJmp(IntPtr ptr)
        {
            while (true) {
                var dec = new InstructionDecoder(ptr);
                var e = dec.GetEnumerator();
                e.MoveNext();
                if (e.Current.Opcode != 0xE9) {
                    break;
                }
                Debug.Assert(e.Current.Data != 0);
                ptr = ptr.Plus((int)e.Current.Data + 5);
            }

            return ptr;
        }

        public static IntPtr FindNearestOp(IntPtr ptr, int minOffset)
        {
            int len = 0;
            var dec = new InstructionDecoder(ptr);
            var e = dec.GetEnumerator();

            while (len < minOffset) {
                e.MoveNext();
                len += e.Current.Length;
                ptr = ptr.Plus(e.Current.Length);
            }

            return ptr;
        }

        internal unsafe static byte* JmpOrCallDest(byte *code)
        {
            Debug.Assert(*code == 0xE9 || *code == 0xE8);
            var offset = *(int*)(code + 1);
            return code + offset + 5;
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

            var signature = _signatureProperty.GetValue(@this, null);

            var paramsCount = args != null ? args.Length : 0;
            var prms = new object[paramsCount];
            for (int i = 0; i < paramsCount; ++i)
                prms[i] = args[i];

            object result;
            
            if (Env.CurrentRuntime == Runtime.CLR2) {
                var owner = new RuntimeTypeHandle();

                if (!(bool)_isGlobalProperty.GetValue(_cacheField.GetValue(@this), null))
                    owner = ((Type)_declTypeField.GetValue(@this)).TypeHandle;

                object attr = _methodAttrField.GetValue(@this);
                if (paramsCount == 0) {
                    result = _invokeMethodFast.Invoke(@this.MethodHandle, new object[] { value, null, signature, attr, owner });
                } else {
                    var a = new object[] { args, null, BindingFlags.Default, null, signature };
                    prms = (object[])_checkArgsMethod.Invoke(@this, a);
                    result = _invokeMethodFast.Invoke(@this.MethodHandle, new object[] { value, prms, signature, attr, owner });
                }
            } else if (Env.CurrentRuntime == Runtime.CLR4) {
                // TODO: check clr4 with updates
                object owner = null;

                if (!(bool)_isGlobalProperty.GetValue(_cacheField.GetValue(@this), null))
                    owner = _declTypeField.GetValue(@this);

                object attr = _methodAttrField.GetValue(@this);
                if (paramsCount == 0) {
                    result = _invokeMethodFastStatic.Invoke(null, new object[] { @this, value, null, signature, attr, owner });
                } else {
                    var a = new object[] { args, null, BindingFlags.Default, null, signature };
                    prms = (object[])_checkArgsMethod.Invoke(@this, a);
                    result = _invokeMethodFastStatic.Invoke(null, new object[] { @this, value, prms, signature, attr, owner });
                }
            } else if (Env.CurrentRuntime >= Runtime.CLR46) {
                if (paramsCount == 0)
                    result = _invokeMethod.Invoke(null, new object[] { value, null, signature, false });
                else {
                    var a = new object[] { args, null, BindingFlags.Default, null, signature };
                    prms = (object[])_checkArgsMethod.Invoke(@this, a);
                    result = _invokeMethod.Invoke(null, new object[] { value, prms, signature, false });
                }
            } else {
                throw new NotSupportedException("Unsupported runtime.");
            }

            for (int i = 0; i < paramsCount; ++i)
                args[i] = prms[i];

            return result;
        }

        public unsafe static IntPtr Plus(this IntPtr ptr, int offset)
        {
            return new IntPtr(ptr.ToBytePtr() + offset);

        }

        public static int SizeOf(Type t)
        {
            if (t == null)
                throw new ArgumentNullException("t");

            if (!t.IsValueType)
                throw new ArgumentException();

            var wrapper = typeof(SizeOfWrapper<>).MakeGenericType(t);
            var inst = Activator.CreateInstance(wrapper);

            return Marshal.SizeOf(inst);
        }

        private struct SizeOfWrapper<T>
        {
            #pragma warning disable CS0169
            private T _field;
            #pragma warning restore CS0169
        }

        private const BindingFlags NonPublicInst = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Type _rmiType = Type.GetType("System.Reflection.RuntimeMethodInfo");
        private static readonly PropertyInfo _signatureProperty = _rmiType.GetProperty("Signature", NonPublicInst);
        private static readonly FieldInfo _cacheField = _rmiType.GetField("m_reflectedTypeCache", NonPublicInst);
        private static readonly FieldInfo _declTypeField = _rmiType.GetField("m_declaringType", NonPublicInst);
        private static readonly PropertyInfo _isGlobalProperty = _cacheField.FieldType.GetProperty("IsGlobal", NonPublicInst);
        private static readonly MethodInfo _checkArgsMethod = typeof(MethodBase).GetMethod("CheckArguments", NonPublicInst);
        private static readonly Type _rmhType = typeof(RuntimeMethodHandle);
        private static readonly FieldInfo _methodAttrField = _rmiType.GetField("m_methodAttributes", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _invokeMethodFast = _rmhType.GetMethod("InvokeMethodFast", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _invokeMethodFastStatic = _rmhType.GetMethod("InvokeMethodFast", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo _invokeMethod = _rmhType.GetMethod("InvokeMethod", BindingFlags.Static | BindingFlags.NonPublic);
    }
}
