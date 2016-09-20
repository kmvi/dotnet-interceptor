using NETInterceptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Examples
{
    class StaticMethodsExample
    {
        private static HookHandle _handle;

        public static void Demo()
        {
            var target = typeof(Environment).GetMethod("GetEnvironmentVariable",
                BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string) }, null);
            var subst = typeof(StaticMethodsExample).GetMethod("GetEnvironmentVariable_Subst");

            using (_handle = Intercept.On(target, subst)) {
                var r = Environment.GetEnvironmentVariable("test");
                Console.WriteLine("Replaced return value: " + r);
                Console.WriteLine();
            }
        }

        public static string GetEnvironmentVariable_Subst(string name)
        {
            Console.WriteLine("--- Static method interception --- ");
            Console.WriteLine("--- Environment.GetEnvironmentVariable ---");
            Console.Write("Calling original method... ");

            var result = (string)_handle.InvokeTarget(null, name);
            Console.WriteLine("result: " + result);

            // replace return value
            return "replaced value";
        }
    }
}
