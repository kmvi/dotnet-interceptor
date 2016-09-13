using System;
using System.Collections;
using System.Collections.Generic;
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

        // TODO: use CAS to write data
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
                        var bytes = BitConverter.GetBytes(tmp);
                        for (int j = 0; j < rem; ++j)
                            oldCode.Append(bytes[j]);
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
            var result = new byte[8];
            for (int i = offset; i < Math.Min(8 + offset, bytes.Count); ++i)
                result[i - offset] = bytes[i];

            return BitConverter.ToInt64(result, 0);
        }
    }
}
