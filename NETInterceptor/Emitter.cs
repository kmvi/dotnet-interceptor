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

        public static MethodBase EmitMethod(MethodBase mb)
        {
            lock (_sync) {
                return EmitMethodInternal(mb);
            }
        }

        private static MethodBase EmitMethodInternal(MethodBase mb)
        {
            Type parent;
            var attr = TypeAttributes.Public | TypeAttributes.AnsiClass | TypeAttributes.Sealed;
            if (mb.DeclaringType.IsValueType) {
                attr |= TypeAttributes.Serializable;
                parent = typeof(ValueType);
            } else {
                attr |= TypeAttributes.Class;
                parent = typeof(object);
            }

            var tb = _builder.Value.DefineType("__" + Guid.NewGuid().ToString("N"), attr, parent);

            if (mb.DeclaringType.IsValueType) {
                var size = Utils.SizeOf(mb.DeclaringType);
                for (int i = 0; i < size; ++i) {
                    tb.DefineField("_f" + i, typeof(byte), FieldAttributes.Private);
                }
            }

            EmitCtor(tb);
            var methodName = EmitMethodInternal(mb, tb);
            var type = tb.CreateType();

            return type.GetMethod(methodName);
        }

        private static string EmitMethodInternal(MethodBase mb, TypeBuilder tb)
        {
            var attr = MethodAttributes.Public;
            if (mb.IsStatic)
                attr |= MethodAttributes.Static;

            var returnType = typeof(void);
            var info = mb as MethodInfo;
            if (info != null) {
                returnType = info.ReturnType;
            }

            var methodName = "__" + Guid.NewGuid().ToString("N");
            var mbuilder = tb.DefineMethod(methodName, attr, returnType, GetParametersTypes(mb));
            mbuilder.SetImplementationFlags(MethodImplAttributes.NoInlining | MethodImplAttributes.NoOptimization);

            var gen = mbuilder.GetILGenerator();
            for (int i = 0; i < 20; ++i)
                gen.Emit(OpCodes.Nop);

            if (returnType != typeof(void)) {
                // TODO: return ref value case
                var loc = gen.DeclareLocal(returnType);
                gen.Emit(OpCodes.Ldloc, loc);
                gen.Emit(OpCodes.Initobj, returnType);
                gen.Emit(OpCodes.Ldloc, loc);
            }

            gen.Emit(OpCodes.Ret);

            return methodName;
        }

        private static Type[] GetParametersTypes(MethodBase info)
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
