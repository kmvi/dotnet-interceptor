using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETInterceptor
{
#if NET35
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
#endif
}
