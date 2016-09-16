using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
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
            Type parent;
            var attr = TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.Sealed;
            if (info.DeclaringType.IsValueType) {
                attr |= TypeAttributes.Serializable;
                parent = typeof(ValueType);
            } else {
                attr |= TypeAttributes.Class;
                parent = typeof(object);
            }

            var tb = _builder.Value.DefineType("__" + Guid.NewGuid().ToString("N"), attr, parent);

            if (info.DeclaringType.IsValueType) {
                var size = Utils.SizeOf(info.DeclaringType);
                for (int i = 0; i < size; ++i) {
                    tb.DefineField("_f" + i, typeof(byte), FieldAttributes.Private);
                }
            }

            EmitCtor(tb);
            var methodName = EmitMethod(info, tb);
            var type = tb.CreateType();

            return type.GetMethod(methodName);
        }

        private static string EmitMethod(MethodInfo info, TypeBuilder tb)
        {
            var attr = MethodAttributes.Public;
            if (info.IsStatic)
                attr |= MethodAttributes.Static;

            var methodName = "__" + Guid.NewGuid().ToString("N");
            var mb = tb.DefineMethod(methodName, attr, info.ReturnType, GetParametersTypes(info));
            mb.SetImplementationFlags(MethodImplAttributes.NoInlining | MethodImplAttributes.NoOptimization);

            var gen = mb.GetILGenerator();
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
            return methodName;
        }

        private static Type[] GetParametersTypes(MethodInfo info)
        {
            var prms = info.GetParameters();
            var prmTypes = new Type[prms.Length];
            for (int i = 0; i < prms.Length; ++i)
                prmTypes[i] = prms[i].ParameterType;
            return prmTypes;
        }

        private static void EmitCtor(TypeBuilder tb)
        {
            var cb = tb.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard, new Type[] { });
            cb.SetImplementationFlags(MethodImplAttributes.NoInlining | MethodImplAttributes.NoOptimization);

            var gen = cb.GetILGenerator();
            for (int i = 0; i < 20; ++i)
                gen.Emit(OpCodes.Nop);

            gen.Emit(OpCodes.Ret);
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
