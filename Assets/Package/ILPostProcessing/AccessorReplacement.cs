using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace OneLastClick.UnityObjectReferencing.ILPostProcessing
{
    public static class AccessorReplacement
    {
        private const string FullNameOfAccessorClass = "OneLastClick.UnityObjectReferencing.UnityObjectReferenceSafeAccessors";

        private const string SafeGetMethodName = "SafeGet";
        private const string SafeSetMethodName = "SafeSet";

        public static void RewriteRead(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            ILProcessor il,
            FieldDefinition fieldDef,
            Instruction instruction)
        {
            MethodReference safeGet = ResolveMethod(module, resolver, SafeGetMethodName);

            TypeReference interfaceType = GetInterfaceType(fieldDef);

            GenericInstanceMethod genericSafeGet = new GenericInstanceMethod(safeGet);

            genericSafeGet.GenericArguments.Add(module.ImportReference(interfaceType));

            il.InsertAfter(instruction, il.Create(OpCodes.Call, genericSafeGet));
        }

        public static void RewriteWrite(
            ModuleDefinition module,
            IAssemblyResolver resolver,
            ILProcessor il,
            MethodDefinition method,
            FieldDefinition fieldDef,
            Instruction instruction)
        {
            MethodReference safeSet = ResolveMethod(
                module,
                resolver,
                SafeSetMethodName);

            TypeReference interfaceType = GetInterfaceType(fieldDef);

            GenericInstanceMethod genericSafeSet = new GenericInstanceMethod(safeSet);

            genericSafeSet.GenericArguments.Add(module.ImportReference(interfaceType));

            VariableDefinition temp = new VariableDefinition(module.ImportReference(interfaceType));

            method.Body.Variables.Add(temp);
            method.Body.InitLocals = true;

            il.InsertBefore(instruction, il.Create(OpCodes.Stloc, temp));
            il.InsertBefore(instruction, il.Create(OpCodes.Dup));
            il.InsertBefore(instruction, il.Create(OpCodes.Ldfld, fieldDef));
            il.InsertBefore(instruction, il.Create(OpCodes.Ldloc, temp));
            il.InsertBefore(instruction, il.Create(OpCodes.Call, genericSafeSet));
        }

        private static TypeReference GetInterfaceType(FieldDefinition fieldDef)
        {
            GenericInstanceType wrapper = (GenericInstanceType)fieldDef.FieldType;
            return wrapper.GenericArguments[0];
        }

        private static MethodReference ResolveMethod(ModuleDefinition module, IAssemblyResolver resolver, string methodName)
        {
            TypeDefinition accessorType = ResolveAccessorType(module, resolver);

            MethodDefinition method = accessorType.Methods.FirstOrDefault(m =>
                m.Name == methodName &&
                m.HasGenericParameters &&
                m.GenericParameters.Count == 1);

            if (method == null)
            {
                throw new InvalidOperationException($"Could not resolve method '{FullNameOfAccessorClass}.{methodName}<T>'.");
            }

            return module.ImportReference(method);
        }

        private static TypeDefinition ResolveAccessorType(ModuleDefinition module, IAssemblyResolver resolver)
        {
            TypeDefinition accessorType = module.GetType(FullNameOfAccessorClass);

            if (accessorType != null)
            {
                return accessorType;
            }

            foreach (AssemblyNameReference reference in module.AssemblyReferences)
            {
                AssemblyDefinition assembly = resolver.Resolve(reference);

                accessorType = assembly?.MainModule.GetType(FullNameOfAccessorClass);

                if (accessorType != null)
                {
                    return accessorType;
                }
            }

            throw new InvalidOperationException($"Could not resolve type '{FullNameOfAccessorClass}'.");
        }
    }
}