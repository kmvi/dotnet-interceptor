using NETInterceptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    class InstanceMethodsExample
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
    }
}
