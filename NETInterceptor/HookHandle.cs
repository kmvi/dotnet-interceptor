using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public class HookHandle : IDisposable
    {
        private bool _disposed;
        private readonly MethodBase _target;
        private readonly CodeInject _inject;
        private readonly MethodBase _reloc;

        internal HookHandle(MethodBase target, MethodBase reloc, CodeInject inject)
        {
            _target = target;
            _inject = inject;
            _reloc = reloc;
        }

        public object InvokeTarget(object value, params object[] args)
        {
            if (_disposed)
                throw new ObjectDisposedException("HookHandle");

            var result = UnsafeInvoke.Create((MethodInfo)_reloc).Invoke(value, args);

            return result;
        }

        public void Dispose()
        {
            if (!_disposed) {
                if (_inject.IsInjected)
                    _inject.Restore();
                Intercept.HookedMethods.Remove(_target);
                _disposed = true;
            }
        }
    }
}
