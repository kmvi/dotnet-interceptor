using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NETInterceptor
{
    public class Method
    {
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
            get { return _method.MethodHandle.GetFunctionPointer(); }
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
                Debug.Assert(result != ThePreStub.Instance.ThePreStubPtr);
                Debug.Assert(result != FixupPrecode.PrecodeFixupThunk);
                Debug.Assert(!Precode.HasPrecode(result));
            }

            return result;
        }

        private static IntPtr GoThroughPrecode(Precode precode)
        {
            if (precode is StubPrecode || precode is RemotingPrecode) {
                if (precode.TargetPtr == ThePreStub.Instance.ThePreStubPtr)
                    return IntPtr.Zero;

                var addr = Utils.FollowRelJmp(precode.JmpToTargetPtr);
                if (addr == ThePreStub.Instance.ThePreStubPtr) {
                    return IntPtr.Zero;
                } else if (FixupPrecode.HasPrecode(addr)) {
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
            if (precode.TargetPtr == ThePreStub.Instance.ThePreStubPtr)
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
    }
}
