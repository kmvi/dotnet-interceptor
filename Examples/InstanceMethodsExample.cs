using NETInterceptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Examples
{
    struct InstanceMethodsExample
    {
        private static HookHandle _handle;

        private ulong _f;

        public static void Demo()
        {
            var target = typeof(DateTime).GetMethod("ToShortDateString");
            var subst = typeof(InstanceMethodsExample).GetMethod("ToShortDateString_Subst");

            using (_handle = Intercept.On(target, subst)) {
                var date = DateTime.Now;
                var str = date.ToShortDateString();
            }
        }

        public string ToShortDateString_Subst()
        {
            var result = (string)_handle.InvokeTarget(this, null);
            return "";
        }
    }
}
