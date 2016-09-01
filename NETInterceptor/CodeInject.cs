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

        protected CodeBlock _oldCode;
        protected int _size;
        protected bool _injected;
        protected bool _disposed;
        protected IntPtr _relocated;

        protected CodeInject(IntPtr pCompiledTarget, IntPtr pCompiledSubst, IntPtr pReloc)
        {
            _target = pCompiledTarget;
            _subst = pCompiledSubst;
            _relocated = pReloc;
        }

        protected abstract int GetRelativeJumpDistance(IntPtr target, IntPtr subst);
        protected abstract bool CanMakeRelativeJump(IntPtr target, IntPtr subst);

        public IntPtr RelocatedAddress
        {
            get
            {
                EnsureNotDisposed();
                return _relocated;
            }
        }

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
                const int jmpSize = 5;

                int jmpDistance = GetRelativeJumpDistance(_target, _subst);                
                var ptr = Utils.FindNearestOp(_target, jmpSize);
                _size = (int)(ptr.ToInt64() - _target.ToInt64());
                Debug.Assert(_size <= 16);

                var jmp = new CodeBlock();
                jmp.Append(0xE9);
                jmp.AppendInt(jmpDistance);
                jmp.Append(Enumerable.Repeat<byte>(0x90, _size - jmpSize));
                Debug.Assert(jmp.Length == _size);

                _oldCode = jmp.WriteTo(_target);

                var reJmp = new CodeBlock(_oldCode);
                reJmp.Append(0xE9);
                reJmp.AppendInt(GetRelativeJumpDistance(_relocated, _target));

                reJmp.WriteTo(_relocated);
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

            _oldCode.WriteTo(_target);

           // _relocated.Dispose();

            _injected = false;
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

        public static CodeInject Create(IntPtr pCompiledTarget, IntPtr pCompiledDest, IntPtr pRelocated)
        {
            switch (Utils.CurrentArchitecture) {
                case Architecture.X86:
                    return new CodeInjectX86(pCompiledTarget, pCompiledDest, pRelocated);
                case Architecture.X64:
                    return new CodeInjectX64(pCompiledTarget, pCompiledDest, pRelocated);
                default:
                    throw new NotSupportedException("Unsupported architecture.");
            }
        }

        private class CodeInjectX86 : CodeInject
        {
            public CodeInjectX86(IntPtr pCompiledTarget, IntPtr pCompiledDest, IntPtr pRelocated)
                : base(pCompiledTarget, pCompiledDest, pRelocated)
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
            public CodeInjectX64(IntPtr pCompiledTarget, IntPtr pCompiledDest, IntPtr pRelocated)
                : base(pCompiledTarget, pCompiledDest, pRelocated)
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
