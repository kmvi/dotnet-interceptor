using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NETInterceptor
{
    internal abstract class UnsafeInvoke
    {
        protected const BindingFlags NonPublicInst = BindingFlags.NonPublic | BindingFlags.Instance;
        protected static readonly Type _rmiType = Type.GetType("System.Reflection.RuntimeMethodInfo");
        protected static readonly PropertyInfo _signatureProperty = _rmiType.GetProperty("Signature", NonPublicInst);
        protected static readonly FieldInfo _cacheField = _rmiType.GetField("m_reflectedTypeCache", NonPublicInst);
        protected static readonly FieldInfo _declTypeField = _rmiType.GetField("m_declaringType", NonPublicInst);
        protected static readonly PropertyInfo _isGlobalProperty = _cacheField.FieldType.GetProperty("IsGlobal", NonPublicInst);
        protected static readonly MethodInfo _checkArgsMethod = typeof(MethodBase).GetMethod("CheckArguments", NonPublicInst);
        protected static readonly Type _rmhType = typeof(RuntimeMethodHandle);
        protected static readonly FieldInfo _methodAttrField = _rmiType.GetField("m_methodAttributes", BindingFlags.Instance | BindingFlags.NonPublic);

        protected readonly MethodInfo _method;

        protected UnsafeInvoke(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            if (method.GetType() != _rmiType)
                throw new ArgumentException();

            _method = method;
        }

        protected object Signature
        {
            get { return _signatureProperty.GetValue(_method, null); }
        }

        protected object[] CheckArguments(object[] args)
        {
            var a = new object[] { args, null, BindingFlags.Default, null, Signature };
            return (object[])_checkArgsMethod.Invoke(_method, a);
        }

        public object Invoke(object target, params object[] args)
        {
            var paramsCount = args != null ? args.Length : 0;
            var prms = new object[paramsCount];
            for (int i = 0; i < paramsCount; ++i)
                prms[i] = args[i];

            var result = InvokeImpl(target, args, paramsCount, prms);

            for (int i = 0; i < paramsCount; ++i)
                args[i] = prms[i];

            return result;
        }

        protected abstract object InvokeImpl(object target, object[] args, int paramsCount, object[] prms);

        public static UnsafeInvoke Create(MethodInfo method)
        {
            if (Env.CurrentRuntime == Runtime.CLR2) {
                return new UnsafeInvoke_CLR2(method);
            }

            if (Env.CurrentRuntime == Runtime.CLR4) {
                return new UnsafeInvoke_CLR4(method);
            }

            if (Env.CurrentRuntime >= Runtime.CLR46) {
                return new UnsafeInvoke_CLR46(method);
            }

            throw new NotSupportedException("Unsupported runtime.");
        }

        private class UnsafeInvoke_CLR2 : UnsafeInvoke
        {
            private static readonly MethodInfo _invokeMethodFast = _rmhType.GetMethod("InvokeMethodFast", BindingFlags.Instance | BindingFlags.NonPublic);

            public UnsafeInvoke_CLR2(MethodInfo method)
                : base(method)
            {
            }

            protected override object InvokeImpl(object target, object[] args, int paramsCount, object[] prms)
            {
                var owner = new RuntimeTypeHandle();

                if (!(bool)_isGlobalProperty.GetValue(_cacheField.GetValue(_method), null))
                    owner = ((Type)_declTypeField.GetValue(_method)).TypeHandle;

                object result;
                object attr = _methodAttrField.GetValue(_method);
                if (paramsCount == 0) {
                    result = _invokeMethodFast.Invoke(_method.MethodHandle,
                        new object[] { target, null, Signature, attr, owner });
                } else {
                    prms = CheckArguments(args);
                    result = _invokeMethodFast.Invoke(_method.MethodHandle,
                        new object[] { target, prms, Signature, attr, owner });
                }

                return result;
            }
        }

        private class UnsafeInvoke_CLR4 : UnsafeInvoke
        {
            private static readonly MethodInfo _invokeMethodFastStatic = _rmhType.GetMethod("InvokeMethodFast", BindingFlags.Static | BindingFlags.NonPublic);

            public UnsafeInvoke_CLR4(MethodInfo method)
                : base(method)
            {
            }

            protected override object InvokeImpl(object target, object[] args, int paramsCount, object[] prms)
            {
                // TODO: check clr4 with updates
                object owner = null;

                if (!(bool)_isGlobalProperty.GetValue(_cacheField.GetValue(_method), null))
                    owner = _declTypeField.GetValue(_method);

                object result;
                object attr = _methodAttrField.GetValue(_method);
                if (paramsCount == 0) {
                    result = _invokeMethodFastStatic.Invoke(null,
                        new object[] { _method, target, null, Signature, attr, owner });
                } else {
                    prms = CheckArguments(args);
                    result = _invokeMethodFastStatic.Invoke(null,
                        new object[] { _method, target, prms, Signature, attr, owner });
                }

                return result;
            }
        }

        private class UnsafeInvoke_CLR46 : UnsafeInvoke
        {
            private static readonly MethodInfo _invokeMethod = _rmhType.GetMethod("InvokeMethod", BindingFlags.Static | BindingFlags.NonPublic);

            public UnsafeInvoke_CLR46(MethodInfo method)
                : base(method)
            {
            }

            protected override object InvokeImpl(object target, object[] args, int paramsCount, object[] prms)
            {
                object result;
                if (paramsCount == 0)
                    result = _invokeMethod.Invoke(null, new object[] { target, null, Signature, false });
                else {
                    prms = CheckArguments(args);
                    result = _invokeMethod.Invoke(null, new object[] { target, prms, Signature, false });
                }

                return result;
            }
        }
    }
}
