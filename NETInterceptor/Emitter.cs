using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NETInterceptor
{
    public static class Emitter
    {
        private static readonly object _sync = new object();
        private static readonly Lazy<ModuleBuilder> _builder = new Lazy<ModuleBuilder>(CreateBuilder);

        public static Type EmitDelegate(Type[] args)
        {
            return EmitDelegate(args, typeof(void));
        }

        public static Type EmitDelegate(Type[] args, Type ret)
        {
            if (args == null)
                args = new Type[0];

            lock (_sync) {
                return Emit(args, ret);
            }
        }

        private static Type Emit(Type[] args, Type ret)
        {
            var tb = _builder.Value.DefineType("__" + Guid.NewGuid().ToString("N"),
                            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass,
                            typeof(MulticastDelegate));

            var cb = tb.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
                CallingConventions.Standard, new Type[] { typeof(object), typeof(IntPtr) });

            cb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            var mb = tb.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, ret, args);

            mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            return tb.CreateType();
        }

        public static MethodInfo EmitMethod(MethodInfo info)
        {
            lock (_sync) {
                return EmitMethodInternal(info);
            }
        }

        private static MethodInfo EmitMethodInternal(MethodInfo info)
        {
            var tb = _builder.Value.DefineType("__" + Guid.NewGuid().ToString("N"),
                            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.AutoClass);

            var cb = tb.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, new Type[] { });

            var gen = cb.GetILGenerator();
            gen.Emit(OpCodes.Ret);

            var methodName = "__" + Guid.NewGuid().ToString("N");
            var mb = tb.DefineMethod(methodName, MethodAttributes.Public, info.ReturnType, info.GetParameters().Select(x => x.ParameterType).ToArray());
            mb.SetImplementationFlags(MethodImplAttributes.NoInlining | MethodImplAttributes.NoOptimization);

            gen = mb.GetILGenerator();
            for (int i = 0; i < 20; ++i)
                gen.Emit(OpCodes.Nop);

            var loc = gen.DeclareLocal(info.ReturnType);
            gen.Emit(OpCodes.Ldloc, loc);
            gen.Emit(OpCodes.Initobj, info.ReturnType);
            gen.Emit(OpCodes.Ldloc, loc);
            gen.Emit(OpCodes.Ret);

            return tb.CreateType().GetMethod(methodName);
        }

        private static ModuleBuilder CreateBuilder()
        {
            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run);

            return ab.DefineDynamicModule(Guid.NewGuid().ToString("N"));
        }
    }
}
