using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NETInterceptor
{
    public static class Emitter
    {
        private static readonly object _sync = new object();
        private static readonly Lazy<ModuleBuilder> _builder = new Lazy<ModuleBuilder>(CreateBuilder);

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

            var attr = MethodAttributes.Public;
            if (info.IsStatic)
                attr |= MethodAttributes.Static;

            var prms = info.GetParameters();
            var prmTypes = new Type[prms.Length];
            for (int i = 0; i < prms.Length; ++i)
                prmTypes[i] = prms[i].ParameterType;

            var methodName = "__" + Guid.NewGuid().ToString("N");
            var mb = tb.DefineMethod(methodName, attr, info.ReturnType, prmTypes);
            mb.SetImplementationFlags(MethodImplAttributes.NoInlining | MethodImplAttributes.NoOptimization);

            gen = mb.GetILGenerator();
            for (int i = 0; i < 20; ++i)
                gen.Emit(OpCodes.Nop);

            if (info.ReturnType != typeof(void)) {
                // TODO: return ref value case
                var loc = gen.DeclareLocal(info.ReturnType);
                gen.Emit(OpCodes.Ldloc, loc);
                gen.Emit(OpCodes.Initobj, info.ReturnType);
                gen.Emit(OpCodes.Ldloc, loc);
            }
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
