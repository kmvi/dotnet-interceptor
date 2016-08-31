using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NETInterceptor
{
    public static class Intercept
    {
        internal static readonly ConcurrentDictionary<MethodBase, object> HookedMethods =
            new ConcurrentDictionary<MethodBase, object>();

        public static HookHandle On(MethodBase target, MethodBase substitute)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            if (substitute == null)
                throw new ArgumentNullException("subst");

            CheckSignatures(target, substitute);

            if (!HookedMethods.TryAdd(target, null))
                throw new InvalidOperationException();

            var targetMethod = new Method(target);
            var destMethod = new Method(substitute);

            var inject = CodeInject.Create(
                targetMethod.GetCompiledCodeAddress(), destMethod.GetCompiledCodeAddress());

            inject.Inject();

            return new HookHandle(target, inject);
        }

        private static void CheckSignatures(MethodBase target, MethodBase substitute)
        {
            if (target is ConstructorInfo)
                throw new NotSupportedException();

            if (target.GetType() != substitute.GetType())
                throw new ArgumentException("Target and its substitute should have same types.");

            if (target.IsStatic != substitute.IsStatic)
                throw new ArgumentException();

            var targetParams = target.GetParameters();
            var substParams = substitute.GetParameters();

            if (targetParams.Length != substParams.Length)
                throw new ArgumentException();

            for (int i = 0; i < targetParams.Length; ++i) {
                Debug.Assert(targetParams[i].Position == substParams[i].Position);
                if (targetParams[i].ParameterType != substParams[i].ParameterType)
                    throw new ArgumentException();
            }

            var targetInfo = target as MethodInfo;
            var substInfo = target as MethodInfo;
            if (targetInfo != null && substInfo != null) {
                if (targetInfo.ReturnType != substInfo.ReturnType)
                    throw new ArgumentException();
            }
        }
    }
}
