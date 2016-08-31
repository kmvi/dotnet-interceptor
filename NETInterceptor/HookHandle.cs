using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace NETInterceptor
{
    public class HookHandle : IDisposable
    {
        private static readonly DelegateEmitter _emitter = new DelegateEmitter();

        private bool _disposed;
        private readonly MethodBase _target;
        private readonly CodeInject _inject;

        internal HookHandle(MethodBase target, CodeInject inject)
        {
            _target = target;
            _inject = inject;
        }

        public object InvokeTarget(object value, params object[] args)
        {
            if (_disposed)
                throw new ObjectDisposedException("HookHandle");

            if (args == null)
                args = new object[0];

            // TODO: use delegate, remove lock
            object result;
            lock (_target) {
                _inject.Restore();
                result = _target.Invoke(value, args);
                _inject.Inject();
            }

            return result;
        }

        public void Dispose()
        {
            if (!_disposed) {
                if (_inject.IsInjected)
                    _inject.Restore();

                object tmp;
                var result = Intercept.HookedMethods.TryRemove(_target, out tmp);
                Debug.Assert(result);

                _disposed = true;
            }
        }
    }
}
