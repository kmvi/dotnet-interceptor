using System;
using System.Collections.Generic;
using System.Text;

namespace NETInterceptor
{
    public struct Instruction
    {
        public readonly int Opcode;
        public readonly int Length;
        public readonly ulong Memory;
        public readonly ulong Data;

        public Instruction(int op, int len, ulong mem, ulong data)
        {
            Opcode = op;
            Length = len;
            Memory = mem;
            Data = data;
        }
    }

    public class InstructionDecoder : IEnumerable<Instruction>
    {
        private readonly IEnumerable<byte> _code;

        public InstructionDecoder(IntPtr address)
        {
            if (address == IntPtr.Zero)
                throw new ArgumentException("Pointer should not be equal to zero.", "address");

            _code = new IntPtrEnumerable(address);
        }

        public InstructionDecoder(IEnumerable<byte> code)
        {
            if (code == null)
                throw new ArgumentNullException("code");

            _code = code;
        }

        public IEnumerator<Instruction> GetEnumerator()
        {
            switch (Env.CurrentArchitecture) {
                case Architecture.X64:
                    return new InstructionLengthEnumerator64(new ByteEnumerator(_code.GetEnumerator()));
                case Architecture.X86:
                    return new InstructionLengthEnumerator86(new ByteEnumerator(_code.GetEnumerator()));
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class ByteEnumerator : IDisposable
        {
            private readonly IEnumerator<byte> _ptr;
            private int _pos;
            private bool _disposed;

            public ByteEnumerator(IEnumerator<byte> ptr)
            {
                _ptr = ptr;
            }

            public byte NextByte()
            {
                EnsureNotDisposed();
                var result = _ptr.Current;
                if (!_ptr.MoveNext())
                    throw new InvalidOperationException();
                _pos++;
                return result;
            }

            public void Add(int offset)
            {
                EnsureNotDisposed();
                while (offset-- > 0) {
                    NextByte();
                }
            }

            public void Dispose()
            {
                if (!_disposed) {
                    _ptr.Dispose();
                    _disposed = true;
                }
            }

            public byte Current
            {
                get {
                    EnsureNotDisposed();
                    return _ptr.Current;
                }
            }

            public int Position
            {
                get {
                    EnsureNotDisposed();
                    return _pos;
                }
            }

            private void EnsureNotDisposed()
            {
                if (_disposed)
                    throw new ObjectDisposedException("ByteEnumerator");
            }
        }

        private class IntPtrEnumerable : IEnumerable<byte>
        {
            private readonly IntPtr _ptr;

            public IntPtrEnumerable(IntPtr ptr)
            {
                _ptr = ptr;
            }

            public IEnumerator<byte> GetEnumerator()
            {
                return new IntPtrEnumerator(_ptr);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private unsafe class IntPtrEnumerator : IEnumerator<byte>
            {
                private byte* _ptr;

                public IntPtrEnumerator(IntPtr ptr)
                {
                    _ptr = (byte*)ptr.ToPointer();
                }

                public byte Current
                {
                    get { return *_ptr; }
                }

                public void Dispose()
                {
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    _ptr++;
                    return true;
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }
        }

        private unsafe class InstructionLengthEnumerator86 : IEnumerator<Instruction>
        {
            private ByteEnumerator _ptr;
            private Instruction _current;

            public InstructionLengthEnumerator86(ByteEnumerator ptr)
            {
                _ptr = ptr;
                _current = new Instruction(0, -1, 0, 0);
            }

            public Instruction Current
            {
                get {
                    if (_current.Length < 0)
                        throw new InvalidOperationException("Call MoveNext first.");
                    return _current;
                }
            }

            public void Dispose()
            {
                _ptr.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                _current = GetLength();
                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            private static bool IsPrefix(byte op)
            {
                switch (op) {
                    case 0x26: case 0x2E: case 0x36: case 0x3E:
                    case 0x64: case 0x65:
                    case 0xF0:
                    case 0xF2: case 0xF3:
                    case 0x66:
                    case 0x67:
                        return true;
                    default:
                        return false;
                }
            }

            // http://z0mbie.daemonlab.org/disasme.txt
            private Instruction GetLength()
            {
                byte op;
                bool hasModRM = false;
                int dataSize = 0, memSize = 0;
                int defaultDataSize = 4, defaultMemSize = 4;
                int start = _ptr.Position;
                int result;

                while (IsPrefix(op = _ptr.NextByte())) {
                    if (op == 0x66) defaultDataSize = 2;
                    else if (op == 0x67) defaultMemSize = 2;
                }

                if (op != 0x0F) {
                    // one-byte instruction
                    switch (op) {
                        case 0x00: case 0x01: case 0x02: case 0x03:
                        case 0x08: case 0x09: case 0x0A: case 0x0B:
                        case 0x10: case 0x11: case 0x12: case 0x13:
                        case 0x18: case 0x19: case 0x1A: case 0x1B:
                        case 0x20: case 0x21: case 0x22: case 0x23:
                        case 0x28: case 0x29: case 0x2A: case 0x2B:
                        case 0x30: case 0x31: case 0x32: case 0x33:
                        case 0x38: case 0x39: case 0x3A: case 0x3B:
                        case 0x62: case 0x63:
                        case 0x84: case 0x85: case 0x86: case 0x87:
                        case 0x88: case 0x89: case 0x8A: case 0x8B:
                        case 0x8C: case 0x8D: case 0x8E: case 0x8F:
                        case 0xC4: case 0xC5:
                        case 0xD0: case 0xD1: case 0xD2: case 0xD3:
                        case 0xD8: case 0xD9: case 0xDA: case 0xDB:
                        case 0xDC: case 0xDD: case 0xDE: case 0xDF:
                        case 0xFE: case 0xFF:
                            hasModRM = true;
                            break;
                        case 0xF6:
                        case 0xF7:
                            hasModRM = true;
                            if ((_ptr.Current & 0x38) == 0)
                                dataSize += (((op & 1) == 0) ? 1 : defaultDataSize);
                            break;
                        case 0x04: case 0x05: case 0x0C: case 0x0D:
                        case 0x14: case 0x15: case 0x1C: case 0x1D:
                        case 0x24: case 0x25: case 0x2C: case 0x2D:
                        case 0x34: case 0x35: case 0x3C: case 0x3D:
                            dataSize += (((op & 1) == 0) ? 1 : defaultDataSize);
                            break;
                        case 0x6A:
                        case 0xA8:
                        case 0xB0: case 0xB1: case 0xB2: case 0xB3:
                        case 0xB4: case 0xB5: case 0xB6: case 0xB7:
                        case 0xD4: case 0xD5:
                        case 0xE4: case 0xE5: case 0xE6: case 0xE7:
                        case 0x70: case 0x71: case 0x72: case 0x73:
                        case 0x74: case 0x75: case 0x76: case 0x77:
                        case 0x78: case 0x79: case 0x7A: case 0x7B:
                        case 0x7C: case 0x7D: case 0x7E: case 0x7F:
                        case 0xEB:
                        case 0xE0: case 0xE1: case 0xE2: case 0xE3:
                        case 0xCD:
                            dataSize++;
                            break;                    
                        case 0x6B:
                        case 0x80:
                        case 0x82:
                        case 0x83:
                        case 0xC0:
                        case 0xC1:
                        case 0xC6:
                            dataSize++;
                            hasModRM = true;
                            break;
                        case 0x69:
                        case 0x81:
                        case 0xC7:
                            dataSize += defaultDataSize;
                            hasModRM = true;
                            break;
                        case 0x9A:
                        case 0xEA:
                            dataSize += 2 + defaultDataSize;
                            break;
                        case 0xA0:
                        case 0xA1:
                        case 0xA2:
                        case 0xA3:
                            memSize += defaultMemSize;
                            break;
                        case 0x68:
                        case 0xA9:
                        case 0xB8: case 0xB9: case 0xBA: case 0xBB:
                        case 0xBC: case 0xBD: case 0xBE: case 0xBF:
                        case 0xE8:
                        case 0xE9:
                            dataSize += defaultDataSize;
                            break;
                        case 0xC2:
                        case 0xCA:
                            dataSize += 2;
                            break;
                        case 0xC8:
                            dataSize += 3;
                            break;
                        case 0xF1:
                            throw new InvalidOperationException("Invalid opcode 0xF1.");
                    }
                    result = op;
                } else {
                    // two-byte instruction
                    op = _ptr.NextByte();
                    switch (op)
                    {
                        case 0x00: case 0x01: case 0x02: case 0x03:
                        case 0x90: case 0x91: case 0x92: case 0x93:
                        case 0x94: case 0x95: case 0x96: case 0x97:
                        case 0x98: case 0x99: case 0x9A: case 0x9B:
                        case 0x9C: case 0x9D: case 0x9E: case 0x9F:
                        case 0xA3:
                        case 0xA5:
                        case 0xAB:
                        case 0xAD:
                        case 0xAF:
                        case 0xB0: case 0xB1: case 0xB2: case 0xB3:
                        case 0xB4: case 0xB5: case 0xB6: case 0xB7:
                        case 0xBB:
                        case 0xBC: case 0xBD: case 0xBE: case 0xBF:
                        case 0xC0:
                        case 0xC1:
                            hasModRM = true;
                            break;
                        case 0x06:
                        case 0x08: case 0x09: case 0x0A: case 0x0B:
                        case 0xA0: case 0xA1: case 0xA2: case 0xA8:
                        case 0xA9:
                        case 0xAA:
                        case 0xC8: case 0xC9: case 0xCA: case 0xCB:
                        case 0xCC: case 0xCD: case 0xCE: case 0xCF:
                            break;
                        case 0x80: case 0x81: case 0x82: case 0x83:
                        case 0x84: case 0x85: case 0x86: case 0x87:
                        case 0x88: case 0x89: case 0x8A: case 0x8B:
                        case 0x8C: case 0x8D: case 0x8E: case 0x8F:
                            dataSize += defaultDataSize;
                            break;
                        case 0xA4:
                        case 0xAC:
                        case 0xBA:
                            dataSize++;
                            hasModRM = true;
                            break;
                        default:
                            throw new InvalidOperationException("Invalid opcode 0F " + op.ToString("X2"));
                    }
                    result = 0x0F00 | op;
                }

                if (hasModRM) {
                    byte modrm = _ptr.NextByte();
                    byte mod = (byte)(modrm & 0xC0);
                    byte rm = (byte)(modrm & 0x07);
                    if (mod != 0xC0) {
                        if (mod == 0x40) memSize++;
                        if (mod == 0x80) memSize += defaultMemSize;
                        if (defaultMemSize == 2) { // modrm16
                            if (mod == 0x00 && rm == 0x06) defaultMemSize += 2;
                        } else { // modrm32
                            if (rm == 0x04) rm = (byte)(_ptr.NextByte() & 0x07);
                            if (rm == 0x05 && mod == 0x00) memSize += 4;
                        }
                    }
                }

                var amem = new byte[8];
                for (int i = 0; i < memSize; ++i)
                    amem[i] = _ptr.NextByte();
                var mem = BitConverter.ToUInt64(amem, 0);

                var adata = new byte[8];
                for (int i = 0; i < dataSize; ++i)
                    adata[i] = _ptr.NextByte();
                var data = BitConverter.ToUInt64(adata, 0);

                return new Instruction(result, _ptr.Position - start, mem, data);
            }
        }

        private unsafe class InstructionLengthEnumerator64 : IEnumerator<Instruction>
        {
            private ByteEnumerator _ptr;
            private Instruction _current;

            public InstructionLengthEnumerator64(ByteEnumerator ptr)
            {
                _ptr = ptr;
                _current = new Instruction(0, -1, 0, 0);
            }

            public Instruction Current
            {
                get {
                    if (_current.Length < 0)
                        throw new InvalidOperationException("Call MoveNext first.");
                    return _current;
                }
            }

            public void Dispose()
            {
                _ptr.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                _current = GetLength();
                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            private static bool IsPrefix(byte op)
            {
                switch (op) {
                    case 0x26: case 0x2E: case 0x36: case 0x3E:
                    case 0x40: case 0x41: case 0x42: case 0x43: 
                    case 0x44: case 0x45: case 0x46: case 0x47:
                    case 0x48: case 0x49: case 0x4A: case 0x4B:
                    case 0x4C: case 0x4D: case 0x4E: case 0x4F:
                    case 0x64: case 0x65: case 0x66: case 0x67:
                    case 0xF0:
                    case 0xF2: case 0xF3:
                        return true;
                    default:
                        return false;
                }
            }

            private Instruction GetLength()
            {
                byte op;
                bool hasModRM = false;
                int dataSize = 0, memSize = 0;
                int defaultDataSize = 4, defaultMemSize = 4;
                int start = _ptr.Position;
                int result;

                while (IsPrefix(op = _ptr.NextByte())) {
                    if (op == 0x66) defaultDataSize = 2;
                    else if (op == 0x67) defaultMemSize = 4;
                }

                if (op != 0x0F) {
                    // one-byte instruction
                    switch (op) {
                        case 0x00: case 0x01: case 0x02: case 0x03:
                        case 0x08: case 0x09: case 0x0A: case 0x0B:
                        case 0x10: case 0x11: case 0x12: case 0x13:
                        case 0x18: case 0x19: case 0x1A: case 0x1B:
                        case 0x20: case 0x21: case 0x22: case 0x23:
                        case 0x28: case 0x29: case 0x2A: case 0x2B:
                        case 0x30: case 0x31: case 0x32: case 0x33:
                        case 0x38: case 0x39: case 0x3A: case 0x3B:
                        case 0x62: case 0x63:
                        case 0x84: case 0x85: case 0x86: case 0x87:
                        case 0x88: case 0x89: case 0x8A: case 0x8B:
                        case 0x8C: case 0x8D: case 0x8E: case 0x8F:
                        case 0xC4: case 0xC5:
                        case 0xD0: case 0xD1: case 0xD2: case 0xD3:
                        case 0xD8: case 0xD9: case 0xDA: case 0xDB:
                        case 0xDC: case 0xDD: case 0xDE: case 0xDF:
                        case 0xFE: case 0xFF:
                            hasModRM = true;
                            break;
                        case 0xF6:
                        case 0xF7:
                            hasModRM = true;
                            if ((_ptr.Current & 0x38) == 0)
                                dataSize += (((op & 1) == 0) ? 1 : defaultDataSize);
                            break;
                        case 0x04: case 0x05: case 0x0C: case 0x0D:
                        case 0x14: case 0x15: case 0x1C: case 0x1D:
                        case 0x24: case 0x25: case 0x2C: case 0x2D:
                        case 0x34: case 0x35: case 0x3C: case 0x3D:
                            dataSize += (((op & 1) == 0) ? 1 : defaultDataSize);
                            break;
                        case 0x6A:
                        case 0xA8:
                        case 0xB0: case 0xB1: case 0xB2: case 0xB3:
                        case 0xB4: case 0xB5: case 0xB6: case 0xB7:
                        case 0xD4: case 0xD5:
                        case 0xE4: case 0xE5: case 0xE6: case 0xE7:
                        case 0x70: case 0x71: case 0x72: case 0x73:
                        case 0x74: case 0x75: case 0x76: case 0x77:
                        case 0x78: case 0x79: case 0x7A: case 0x7B:
                        case 0x7C: case 0x7D: case 0x7E: case 0x7F:
                        case 0xEB:
                        case 0xE0: case 0xE1: case 0xE2: case 0xE3:
                        case 0xCD:
                            dataSize++;
                            break;                    
                        case 0x6B:
                        case 0x80:
                        case 0x82:
                        case 0x83:
                        case 0xC0:
                        case 0xC1:
                        case 0xC6:
                            dataSize++;
                            hasModRM = true;
                            break;
                        case 0x69:
                        case 0x81:
                        case 0xC7:
                            dataSize += defaultDataSize;
                            hasModRM = true;
                            break;
                        case 0x9A:
                        case 0xEA:
                            dataSize += 2 + defaultDataSize;
                            break;
                        case 0xA0:
                        case 0xA1:
                        case 0xA2:
                        case 0xA3:
                            memSize += defaultMemSize;
                            break;
                        case 0x68:
                        case 0xA9:
                        case 0xB8: case 0xB9: case 0xBA: case 0xBB:
                        case 0xBC: case 0xBD: case 0xBE: case 0xBF:
                        case 0xE8:
                        case 0xE9:
                            dataSize += defaultDataSize;
                            break;
                        case 0xC2:
                        case 0xCA:
                            dataSize += 2;
                            break;
                        case 0xC8:
                            dataSize += 3;
                            break;
                        case 0xF1:
                            throw new InvalidOperationException("Invalid opcode 0xF1.");
                    }
                    result = op;
                } else {
                    // two-byte instruction
                    op = _ptr.NextByte();
                    switch (op)
                    {
                        case 0x00: case 0x01: case 0x02: case 0x03:
                        case 0x90: case 0x91: case 0x92: case 0x93:
                        case 0x94: case 0x95: case 0x96: case 0x97:
                        case 0x98: case 0x99: case 0x9A: case 0x9B:
                        case 0x9C: case 0x9D: case 0x9E: case 0x9F:
                        case 0xA3:
                        case 0xA5:
                        case 0xAB:
                        case 0xAD:
                        case 0xAF:
                        case 0xB0: case 0xB1: case 0xB2: case 0xB3:
                        case 0xB4: case 0xB5: case 0xB6: case 0xB7:
                        case 0xBB:
                        case 0xBC: case 0xBD: case 0xBE: case 0xBF:
                        case 0xC0:
                        case 0xC1:
                        case 0x7F:
                            hasModRM = true;
                            break;
                        case 0x06:
                        case 0x08: case 0x09: case 0x0A: case 0x0B:
                        case 0xA0: case 0xA1: case 0xA2: case 0xA8:
                        case 0xA9:
                        case 0xAA:
                        case 0xC8: case 0xC9: case 0xCA: case 0xCB:
                        case 0xCC: case 0xCD: case 0xCE: case 0xCF:
                            break;
                        case 0x80: case 0x81: case 0x82: case 0x83:
                        case 0x84: case 0x85: case 0x86: case 0x87:
                        case 0x88: case 0x89: case 0x8A: case 0x8B:
                        case 0x8C: case 0x8D: case 0x8E: case 0x8F:
                            dataSize += defaultDataSize;
                            break;
                        case 0xA4:
                        case 0xAC:
                        case 0xBA:
                            dataSize++;
                            hasModRM = true;
                            break;
                        default:
                            throw new InvalidOperationException("Invalid opcode 0F " + op.ToString("X2"));
                    }
                    result = 0x0F | op;
                }

                if (hasModRM) {
                    byte modrm = _ptr.NextByte();
                    byte mod = (byte)(modrm & 0xC0);
                    byte rm = (byte)(modrm & 0x07);
                    if (mod != 0xC0) { // 11b, register-indirect addressing
                        if (mod == 0x40) memSize++; // 01b [reg]+disp8
                        if (mod == 0x80) memSize += defaultMemSize; // 10b [reg]+disp32
                        if (rm == 0x04) rm = (byte)(_ptr.NextByte() & 0x07); // next sib byte
                        if (rm == 0x05 && mod == 0x00) memSize += 4; // disp32
                    }
                }

                var amem = new byte[8];
                for (int i = 0; i < memSize; ++i)
                    amem[i] = _ptr.NextByte();
                var mem = BitConverter.ToUInt64(amem, 0);

                var adata = new byte[8];
                for (int i = 0; i < dataSize; ++i)
                    adata[i] = _ptr.NextByte();
                var data = BitConverter.ToUInt64(adata, 0);

                return new Instruction(result, _ptr.Position - start, mem, data);
            }
        }
    }
}
