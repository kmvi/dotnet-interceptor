using NETInterceptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Examples
{
    struct StructMethodsExample
    {
        private static HookHandle _handle;

        #pragma warning disable 169
        // struct size should be equal to sizeof(DateTime)
        private ulong _f;
        #pragma warning restore 169

        public static void Demo()
        {
            var target = typeof(DateTime).GetMethod("ToShortDateString");
            var subst = typeof(StructMethodsExample).GetMethod("ToShortDateString_Subst");

            using (_handle = Intercept.On(target, subst)) {
                var date = DateTime.Now;
                var str = date.ToShortDateString();
                Console.WriteLine("Replaced result: " + str);
                Console.WriteLine();
            }
        }

        public string ToShortDateString_Subst()
        {
            Console.WriteLine("--- Struct methods interception --- ");
            Console.WriteLine("--- DateTime.ToShortDateString ---");
            Console.Write("Calling original method... ");
            var result = (string)_handle.InvokeTarget(this, null);
            Console.WriteLine("result: " + result);
            return "Current date is: " + result;
        }
    }
}
