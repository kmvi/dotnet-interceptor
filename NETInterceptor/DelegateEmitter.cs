using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NETInterceptor
{
    public static class DelegateEmitter
    {
        private static readonly object _sync = new object();
        private static readonly Lazy<ModuleBuilder> _builder = new Lazy<ModuleBuilder>(CreateBuilder);

        public static Type EmitDelegate(MethodBase method)
        {
            var info = method as MethodInfo;
            if (info == null)
                throw new NotSupportedException();

            var args = info.GetParameters().Select(x => x.ParameterType).ToList();
            /*if (!info.IsStatic)
                args.Insert(0, info.DeclaringType);*/

            return EmitDelegate(args.ToArray(), info.ReturnType);
        }

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

        public static MethodInfo CreateMethod(MethodInfo info)
        {
            var tb = _builder.Value.DefineType("__" + Guid.NewGuid().ToString("N"),
                            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass);

            var cb = tb.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, new Type[] {  });

            var gen = cb.GetILGenerator();
            gen.Emit(OpCodes.Ret);

            var mname = "__" + Guid.NewGuid().ToString("N");
            var mb = tb.DefineMethod(mname, MethodAttributes.Public, info.ReturnType, info.GetParameters().Select(x=>x.ParameterType).ToArray());

            gen = mb.GetILGenerator();
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            gen.Emit(OpCodes.Nop);
            var loc = gen.DeclareLocal(info.ReturnType);
            gen.Emit(OpCodes.Ldloc, loc);
            gen.Emit(OpCodes.Initobj, info.ReturnType);
            gen.Emit(OpCodes.Ldloc, loc);
            gen.Emit(OpCodes.Ret);

            var t = tb.CreateType();
            return t.GetMethod(mname);
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
