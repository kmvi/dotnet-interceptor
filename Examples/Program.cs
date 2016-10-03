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

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            StaticMethodsExample.Demo();
            StructMethodsExample.Demo();
            InstancePropertyGetterExample.Demo();
            InstancePropertySetterExample.Demo();
            ConstructorExample.Demo();
        }
    }
}
