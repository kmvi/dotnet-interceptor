using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETInterceptor
{
    public static class Env
    {
        public static Architecture CurrentArchitecture
        {
            get { return IntPtr.Size == 8 ? Architecture.X64 : Architecture.X86; }
        }

        public static Runtime CurrentRuntime
        {
            get
            {
                if (Environment.Version.Major == 2) {
                    return Runtime.CLR2;
                }

                if (Environment.Version.Major == 4) {
                    return Is46Runtime() ? Runtime.CLR46 : Runtime.CLR4;
                }

                throw new NotSupportedException("Unsupported runtime.");
            }
        }

        private static bool Is46Runtime()
        {
            var type = Type.GetType("System.Collections.Generic.IReadOnlyCollection`1");
            if (type != null) {
                type = type.MakeGenericType(typeof(int));
                return type.IsAssignableFrom(typeof(Stack<int>));
            }

            return false;
        }
    }

    public enum Architecture
    {
        X86,
        X64,
    }

    public enum Runtime
    {
        CLR2 = 200,
        CLR4 = 400,
        CLR46 = 460,
    }
}
