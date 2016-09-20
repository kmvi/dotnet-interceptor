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
                if (IsRelJmp(e.Current.Opcode)) {
                    throw new NotImplementedException();
                }
            }

            return ptr;
        }

        internal unsafe static byte* JmpOrCallDest(byte *code)
        {
            Debug.Assert(*code == 0xE9 || *code == 0xE8);
            var offset = *(int*)(code + 1);
            return code + offset + 5;
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

        private static bool IsRelJmp(int opcode)
        {
            return (opcode >= 0x70 && opcode <= 0x7F) || opcode == 0xE3 ||
                (opcode >= 0xE8 && opcode <= 0xEB) ||
                (opcode >= 0x0F80 && opcode <= 0x0F8F);
        }

        private struct SizeOfWrapper<T>
        {
            #pragma warning disable 169
            private T _field;
            #pragma warning restore 169
        }
    }
}
