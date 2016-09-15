using NETInterceptor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static unsafe void Main(string[] args)
        {
            StaticMethodsExample.Demo();
            InstancePropertyGetterExample.Demo();
            InstancePropertySetterExample.Demo();
        }

        private static HookHandle handle;

        public static bool parse(string s, out DateTime d)
        {
            Console.WriteLine(s);
            DateTime tmp = default(DateTime);
            var args = new object[] { s, tmp };
            var r = handle.InvokeTarget(null, args);
            Console.WriteLine(args[1]);
            d = new DateTime(2010, 2, 3);
            return false;
        }

        public bool test()
        {
            var r = (bool)handle.InvokeTarget(this);
            Console.WriteLine("tset");
            return r;
        }

        public static void m()
        {
            Console.WriteLine("hello");
        }
    }


}
