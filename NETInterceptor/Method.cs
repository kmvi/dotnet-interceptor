using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace NETInterceptor
{
    public class Method
    {
        private static readonly Type _dynamicMethod = typeof(DynamicMethod);
        private static readonly Type _rtDynamicMethod = _dynamicMethod.GetNestedType("RTDynamicMethod", BindingFlags.NonPublic);
        private static readonly FieldInfo _ownerField = _rtDynamicMethod.GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _methodField = _dynamicMethod.GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo _getMethodDescriptorMethod = _dynamicMethod.GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly MethodBase _method;
        private Precode _precode;

        public Method(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            _method = method;
        }

        public IntPtr Address
        {
            get {
                // TODO: create classes
                if (_method.GetType() == _dynamicMethod) {
                    return GetDynamicMethodRuntimeHandle(_method).GetFunctionPointer();
                }
                if (_method.GetType() == _rtDynamicMethod) {
                    var owner = (DynamicMethod)_ownerField.GetValue(_method);
                    return GetDynamicMethodRuntimeHandle(owner).GetFunctionPointer();
                }
                return _method.MethodHandle.GetFunctionPointer();
            }
        }

        public bool HasPrecode
        {
            get { return Precode.HasPrecode(Address); }
        }

        public bool IsJitted()
        {
            return HasPrecode ? GetCompiledCodeAddressWithPrecode() != IntPtr.Zero : true;
        }

        public IntPtr GetCompiledCodeAddress()
        {
            // Trying PrepareMethod first
            RuntimeHelpers.PrepareMethod(_method.MethodHandle);

            if (!HasPrecode)
                return Address;

            var compiledAddr = GetCompiledCodeAddressWithPrecode();
            if (compiledAddr == IntPtr.Zero) {
                // TODO: call PreStubWorker manually
                throw new NotImplementedException();
            }

            return compiledAddr;
        }

        private IntPtr GetCompiledCodeAddressWithPrecode()
        {
            var precode = GetPrecode();
            var result = GoThroughPrecode(precode);

            if (result != IntPtr.Zero) {
                Debug.Assert(result != ThePreStub.Address);
                Debug.Assert(result != FixupPrecode.PrecodeFixupThunk);
                Debug.Assert(!Precode.HasPrecode(result));
            }

            return result;
        }

        private static IntPtr GoThroughPrecode(Precode precode)
        {
            if (precode is StubPrecode || precode is RemotingPrecode) {
                if (precode.TargetPtr == ThePreStub.Address)
                    return IntPtr.Zero;

                var addr = Utils.FollowRelJmp(precode.JmpToTargetPtr);
                if (addr == ThePreStub.Address) {
                    return IntPtr.Zero;
                } else if (FixupPrecode.IsFixupPrecode(addr)) {
                    return GoThroughFixupPrecode(FixupPrecode.Create(addr));
                }
                
                return addr;
            }

            if (precode is FixupPrecode) {
                return GoThroughFixupPrecode((FixupPrecode)precode);
            }

            throw new NotSupportedException("Unsupported precode type.");
        }

        private static IntPtr GoThroughFixupPrecode(FixupPrecode precode)
        {
            if (precode.TargetPtr == ThePreStub.Address)
                return IntPtr.Zero; // method is not jitted

            return Utils.FollowRelJmp(precode.JmpToTargetPtr);
        }

        public Precode GetPrecode()
        {
            if (!HasPrecode)
                throw new InvalidOperationException("Method does not have a precode.");

            if (_precode == null)
                _precode = Precode.Create(Address);

            return _precode;
        }

        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            if (Env.CurrentRuntime == Runtime.CLR2) {
                return ((RuntimeMethodHandle)_methodField.GetValue(method));
            } else if (Env.CurrentRuntime >= Runtime.CLR4) {
                return (RuntimeMethodHandle)_getMethodDescriptorMethod.Invoke(method, null);
            }

            throw new NotSupportedException("Unsupported runtime.");
        } 
    }
}
