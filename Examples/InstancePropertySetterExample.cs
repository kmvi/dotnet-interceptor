using NETInterceptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Examples
{
    class InstancePropertySetterExample
    {
        private static HookHandle _handle;

        public static void Demo()
        {
            var target = typeof(UriBuilder).GetProperty("Host").GetSetMethod();
            var subst = typeof(InstancePropertySetterExample).GetMethod("Host_Subst");

            using (_handle = Intercept.On(target, subst)) {
                var builder = new UriBuilder();
                builder.Scheme = "http";
                builder.Host = "localhost";
                Console.WriteLine("Result: " + builder);
                Console.WriteLine();
            }
        }

        public void Host_Subst(string value)
        {
            Console.WriteLine("--- Instance property setter interception --- ");
            Console.WriteLine("--- UriBuilder.Host ---");
            Console.WriteLine("Calling original method... ");

            // note: this.GetType() == typeof(UriBuilder)
            _handle.InvokeTarget(this, value);

            Console.WriteLine("Modifying instance... ");

            // add some additional data to UriBuilder instance
            ((UriBuilder)((object)this)).Port = 8080;
        }
    }
}
