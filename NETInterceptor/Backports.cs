using System;
using System.Collections.Generic;
using System.Text;

#if NET20

namespace System
{
    public delegate T Func<T>();
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
}

#endif

#if NET35 || NET20

namespace NETInterceptor
{
    // TODO: we need something better
    public class Lazy<T>
    {
        private readonly object _sync = new object();
        private readonly Func<T> _valueFactory;
        private volatile Boxed _value;

        public Lazy(Func<T> valueFactory)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");

            _valueFactory = valueFactory;
        }

        public T Value
        {
            get
            {
                if (_value == null) {
                    lock (_sync) {
                        if (_value == null) {
                            var boxed = new Boxed(_valueFactory());
                            _value = boxed;
                        }
                    }
                }
                return _value.value;
            }
        }

        private class Boxed
        {
            public Boxed(T v)
            {
                value = v;
            }

            public T value;
        }
    }
}
#endif
