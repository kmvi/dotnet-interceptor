using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NETInterceptor
{
    abstract class CodeInject : IDisposable
    {
        protected readonly IntPtr _target;
        protected readonly IntPtr _subst;

        protected long[] _oldCode;
        protected int _size;
        protected bool _injected;
        protected bool _disposed;
        protected GlobalMemoryBlock _relocated;

        protected CodeInject(IntPtr pCompiledTarget, IntPtr pCompiledSubst)
        {
            _target = pCompiledTarget;
            _subst = pCompiledSubst;
        }

        protected abstract int GetRelativeJumpDistance(IntPtr target, IntPtr subst);
        protected abstract bool CanMakeRelativeJump(IntPtr target, IntPtr subst);

        public bool IsInjected
        {
            get {
                EnsureNotDisposed();
                return _injected;
            }
        }

        public void Inject()
        {
            EnsureNotDisposed();

            if (_injected)
                throw new InvalidOperationException();

            if (CanMakeRelativeJump(_target, _subst)) {
                int jmpDistance = GetRelativeJumpDistance(_target, _subst);
                var relJmpBytes = BitConverter.GetBytes(jmpDistance);

                var ptr = Utils.FindNearestOp(_target, 5);
                _size = (int)(ptr.ToInt64() - _target.ToInt64());
                Debug.Assert(_size <= 16);

                var jmp = new byte[8] {
                    0xE9, relJmpBytes[0], relJmpBytes[1], relJmpBytes[2], relJmpBytes[3],
                    0, 0, 0,
                };
                var code = BitConverter.ToInt64(jmp, 0);

                _relocated = GlobalMemoryBlock.Allocate(_size);

                if (_size <= 8) {                    
                    _oldCode = WriteCode(new[] { code }, _size);
                } else {
                    _oldCode = WriteCode(new[] { code, 0L }, _size);
                }
            } else {
                // TODO: absolute jmp to subst code
                throw new NotImplementedException();
            }

            _injected = true;
        }

        public void Restore()
        {
            EnsureNotDisposed();

            if (!_injected)
                throw new InvalidOperationException();

            WriteCode(_oldCode, _size);

            _injected = false;
        }

        private unsafe long[] WriteCode(long[] code, int size)
        {
            using (var block = new WritableMemoryBlock(_target, size)) {
                var ptr = (long*)_target.ToPointer();
                var oldCode = new long[code.Length];
                for (int i = 0; i < code.Length; ++i, ++ptr, size -= 8) {
                    int sz = size < 8 ? size : 8;
                    if (sz < 8) {
                        long mask = (1L << (sz * 8)) - 1L;
                        oldCode[i] = *ptr;
                        *ptr = (code[i] & mask) | (oldCode[i] & ~mask);
                    } else {
                        oldCode[i] = *ptr;
                        *ptr = code[i];
                    }
                }
                return oldCode;
            }
        }

        public void Dispose()
        {
            if (!_disposed) {
                if (_injected)
                    Restore();
                _disposed = true;
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("CodeInject");
        }

        public static CodeInject Create(IntPtr pCompiledTarget, IntPtr pCompiledDest)
        {
            switch (Utils.CurrentArchitecture) {
                case Architecture.X86:
                    return new CodeInjectX86(pCompiledTarget, pCompiledDest);
                case Architecture.X64:
                    return new CodeInjectX64(pCompiledTarget, pCompiledDest);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        private class CodeInjectX86 : CodeInject
        {
            public CodeInjectX86(IntPtr pCompiledTarget, IntPtr pCompiledDest)
                : base(pCompiledTarget, pCompiledDest)
            {
            }

            protected override bool CanMakeRelativeJump(IntPtr target, IntPtr subst)
            {
                return true;
            }

            protected override int GetRelativeJumpDistance(IntPtr target, IntPtr subst)
            {
                return subst.ToInt32() - target.ToInt32() - 5;
            }
        }

        private class CodeInjectX64 : CodeInject
        {
            public CodeInjectX64(IntPtr pCompiledTarget, IntPtr pCompiledDest)
                : base(pCompiledTarget, pCompiledDest)
            {
            }

            protected override int GetRelativeJumpDistance(IntPtr target, IntPtr subst)
            {
                return (int)(subst.ToInt64() - target.ToInt64() - 5);
            }

            protected override bool CanMakeRelativeJump(IntPtr target, IntPtr subst)
            {
                var dist = subst.ToInt64() - target.ToInt64() - 5;
                return dist >= Int32.MinValue && dist <= Int32.MaxValue;
            }
        }
    }
}
