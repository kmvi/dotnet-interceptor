using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETInterceptor
{
    public class CodeBlock
    {
        private readonly List<byte> _code = new List<byte>();

        public CodeBlock()
        {
        }

        public CodeBlock(CodeBlock block)
        {
            _code.AddRange(block._code);
        }

        public CodeBlock(IEnumerable<byte> instr)
        {
            _code.AddRange(instr);
        }

        public CodeBlock Append(byte instr)
        {
            _code.Add(instr);
            return this;
        }

        public CodeBlock Append(IEnumerable<byte> instr)
        {
            _code.AddRange(instr);
            return this;
        }

        public CodeBlock AppendInt(int instr)
        {
            _code.AddRange(BitConverter.GetBytes(instr));
            return this;
        }

        public CodeBlock AppendLong(long instr)
        {
            _code.AddRange(BitConverter.GetBytes(instr));
            return this;
        }

        public int Length { get { return _code.Count; } }

        public unsafe CodeBlock WriteTo(IntPtr target)
        {
            if (_code.Count == 0)
                throw new InvalidOperationException();

            using (new WritableMemoryBlock(target, _code.Count)) {
                var oldCode = new CodeBlock();
                var ptr = (long*)target.ToPointer();
                for (int i = 0; i < _code.Count; i += 8, ++ptr) {
                    var code = GetInt64(_code, i);
                    var rem = _code.Count - i;
                    if (rem < 8) {
                        long mask = (1L << (rem * 8)) - 1L;
                        long tmp = *ptr;
                        *ptr = (code & mask) | (tmp & ~mask);
                        oldCode.Append(BitConverter.GetBytes(tmp).Take(rem));
                    } else {
                        oldCode.AppendLong(*ptr);
                        *ptr = code;
                    }
                }
                return oldCode;
            }
        }

        private static long GetInt64(IList<byte> bytes, int offset)
        {
            byte[] a = bytes.Skip(offset).Take(8).ToArray();

            if (a.Length < 8) {
                a = Utils.ConcatArray(a, new byte[8 - a.Length]);
            }

            return BitConverter.ToInt64(a, 0);
        }
    }
}
