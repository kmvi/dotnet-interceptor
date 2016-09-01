using NETInterceptor;
using SharpDisasm;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleApp1
{
    public delegate int Test(string p);
    class Program
    {
        static unsafe void Main(string[] args)
        {

            //var ptr = new Func<String>(test).Method.MethodHandle.GetFunctionPointer();
            //var ptr = typeof(Program).GetMethod("Shim", BindingFlags.Static | BindingFlags.Public).MethodHandle;
            //var ptr = typeof(Action).GetMethod("BeginInvoke").MethodHandle.GetFunctionPointer();
            // var ptr = typeof(MarshalByRefObject).GetMethod("GetLifetimeService").MethodHandle.GetFunctionPointer();
            ////Utils.Dump(ptr, 70);

            var target = typeof(DirectoryInfo).GetProperty("Exists").GetGetMethod();
            var subst = typeof(Program).GetMethod("test");

            var addr = new Method(target).GetCompiledCodeAddress();


            handle = Intercept.On(target, subst);
            var d = new DirectoryInfo("d:\\");
            var e = d.Exists;
            handle.Dispose();

        }

        private static HookHandle handle;

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
