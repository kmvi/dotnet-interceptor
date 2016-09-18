using NETInterceptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Examples
{
    class InstancePropertyGetterExample
    {
        private static HookHandle _handle;

        public static void Demo()
        {
            var target = typeof(DirectoryInfo).GetProperty("Exists").GetGetMethod();
            var subst = typeof(InstancePropertyGetterExample).GetMethod("Exists_Subst");

            using (_handle = Intercept.On(target, subst)) {
                var di = new DirectoryInfo("c:\\");
                // Exists getter call will be replaced with Exists_Subst call
                var r = di.Exists; // r == false
            }
        }

        public bool Exists_Subst()
        {
            Console.WriteLine("--- Instance property getter interception --- ");
            Console.WriteLine("--- DirectoryInfo.Exists ---");
            Console.Write("Calling original method... ");

            // note: this.GetType() == typeof(DirectoryInfo)
            bool result = (bool)_handle.InvokeTarget(this, null);
            Console.WriteLine("result: " + result);
            Console.WriteLine();

            // replace return value
            return false;
        }
    }
}
