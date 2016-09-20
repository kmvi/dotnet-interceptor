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
            var target = typeof(Math).GetMethod("Max",
                BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(int), typeof(int) }, null);
            var subst = typeof(StaticMethodsExample).GetMethod("Max_Subst");

            using (_handle = Intercept.On(target, subst)) {
                // replace Max with Min
                var r = Math.Max(1, 2);
                Console.WriteLine("Replaced return value: " + r);
                Console.WriteLine();
            }
        }

        public static int Max_Subst(int i1, int i2)
        {
            Console.WriteLine("--- Static method interception --- ");
            Console.WriteLine("--- Math.Max ---");
            Console.Write("Calling original method... ");

            int result = (int)_handle.InvokeTarget(null, i1, i2);
            Console.WriteLine("result: " + result);

            // replace return value
            return Math.Min(i1, i2);
        }
    }
}
