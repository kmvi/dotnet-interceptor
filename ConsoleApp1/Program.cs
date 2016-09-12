using NETInterceptor;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

            //var ptr = new Func<String>(test).Method.MethodHandle.GetFunctionPointer();
            //var ptr = typeof(Program).GetMethod("Shim", BindingFlags.Static | BindingFlags.Public).MethodHandle;
            //var ptr = typeof(Action).GetMethod("BeginInvoke").MethodHandle.GetFunctionPointer();
            // var ptr = typeof(MarshalByRefObject).GetMethod("GetLifetimeService").MethodHandle.GetFunctionPointer();
            ////Utils.Dump(ptr, 70);

            /*            var target = typeof(DirectoryInfo).GetProperty("Exists").GetGetMethod();
                        var subst = typeof(Program).GetMethod("test");

                        var addr = new Method(target).GetCompiledCodeAddress();


                        handle = Intercept.On(target, subst);
                        var d = new DirectoryInfo("d:\\");
                        var e = d.Exists;
                        handle.Dispose();*/

            var target = typeof(DateTime).GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(DateTime).MakeByRefType() }, null);
            var subst = typeof(Program).GetMethod("parse");
            handle = Intercept.On(target, subst);
            var r = DateTime.TryParse("2000-01-01", out var t);
            handle.Dispose();
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
