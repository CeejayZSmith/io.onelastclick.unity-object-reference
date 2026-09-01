using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace OneLastClick.UnityObjectReferencing.ILPostProcessing
{
    public class InterfaceSerializationILPostProcessor : ILPostProcessor
    {
        const string RuntimeAssemblyName = "OneLastClick.UnityObjectReferencing.Runtime";
        const string WrapperTypeFullName = "OneLastClick.UnityObjectReferencing.UnityObjectReference`1";

        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return true;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            return AssemblyModifier.ModifyAssembly(compiledAssembly, Modify);
        }

        private bool Modify(AssemblyDefinition assemblyDefinition, List<DiagnosticMessage> diagnostics, IAssemblyResolver resolver)
        {
            ModuleDefinition module = assemblyDefinition.MainModule;
                
            List<FieldDefinition> allTargetFields = new List<FieldDefinition>();
            foreach (TypeDefinition type in module.GetTypes())
            {
                allTargetFields.AddRange(InterfaceSerializeFieldDiscoverer.FindTargetFields(type, diagnostics));
            }

            if (allTargetFields.Count == 0)
            {
                // No interface fields requiring serialization support found.
                return false;
            }

            if ( TryResolveWrapperTypeDefinition(module, diagnostics, out TypeDefinition wrapperTypeDef) == false)
            {
                return false;
            }

            bool anyFailures = false;
            foreach (FieldDefinition field in allTargetFields)
            {
                if (TryRewriteFieldType(field, module, wrapperTypeDef, diagnostics) == false)
                {
                    anyFailures = true;
                }
            }

            if (anyFailures == true)
            {
                return false;
            }

            return TryRewriteAccessSites(module, allTargetFields, wrapperTypeDef, resolver, diagnostics);
        }

        private bool TryRewriteFieldType(FieldDefinition field, ModuleDefinition module, TypeDefinition wrapperTypeDef, List<DiagnosticMessage> diagnostics)
        {
            try
            {
                TypeReference originalInterfaceType = field.FieldType;
                GenericInstanceType wrapperGenericInstance = new GenericInstanceType(wrapperTypeDef);
                wrapperGenericInstance.GenericArguments.Add(originalInterfaceType);
                field.FieldType = module.ImportReference(wrapperGenericInstance);
                return true;
            }
            catch (Exception ex)
            {
                AddDiagnostic(diagnostics, DiagnosticType.Error, $"Failed to rewrite field type for '{Describe(field)}': {ex.Message}");
                return false;
            }
        }

        private bool TryRewriteAccessSites(ModuleDefinition module, List<FieldDefinition> targetFields,
            TypeDefinition wrapperTypeDef, IAssemblyResolver resolver, List<DiagnosticMessage> diagnostics)
        {
            if (targetFields.Count == 0)
            {
                return true;
            }

            foreach (TypeDefinition type in module.GetTypes())
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method.HasBody == false)
                    {
                        continue;
                    }

                    ILProcessor il = method.Body.GetILProcessor();
                    // Iterate over a copy because we'll modify the instruction list.
                    Instruction[] instructions = method.Body.Instructions.ToArray();
                    for (int i = 0; i < instructions.Length; i++)
                    {
                        Instruction instruction = instructions[i];

                        if ((instruction.Operand is FieldReference fieldRef) == false)
                        {
                            continue;
                        }

                        FieldDefinition fieldDef = ResolveField(fieldRef);
                        if (fieldDef == null)
                        {
                            continue;
                        }

                        if (targetFields.Contains(fieldDef) == false)
                        {
                            continue;
                        }
                        
                        switch (instruction.OpCode.Code)
                        {
                            // READ access
                            case Code.Ldfld:
                            {
                                AccessorReplacement.RewriteRead(
                                    module, 
                                    resolver, 
                                    il, 
                                    fieldDef, 
                                    instruction);
                                break;
                            }
                            // WRITE access
                            case Code.Stfld:
                            {
                                AccessorReplacement.RewriteWrite(
                                    module,
                                    resolver,
                                    il,
                                    method,
                                    fieldDef,
                                    instruction);
                                break;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static FieldDefinition ResolveField(FieldReference fieldReference)
        {
            try
            {
                return fieldReference.Resolve();
            }
            catch
            {
                return null;
            }
        }

        private bool TryResolveWrapperTypeDefinition(
            ModuleDefinition module,
            List<DiagnosticMessage> diagnostics,
            out TypeDefinition wrapperTypeDef)
        {
            wrapperTypeDef = null;

            try
            {
                AssemblyDefinition runtimeAssembly =
                    module.AssemblyResolver.Resolve(
                        new AssemblyNameReference(RuntimeAssemblyName, null));

                if (runtimeAssembly == null)
                {
                    AddDiagnostic(
                        diagnostics,
                        DiagnosticType.Error,
                        $"Failed to resolve assembly '{RuntimeAssemblyName}' " +
                        $"for assembly: '{module.Name}'.");

                    return false;
                }

                wrapperTypeDef = runtimeAssembly.MainModule.Types
                    .FirstOrDefault(t => t.FullName == WrapperTypeFullName);

                if (wrapperTypeDef == null)
                {
                    AddDiagnostic(
                        diagnostics,
                        DiagnosticType.Error,
                        $"Could not find type '{WrapperTypeFullName}' " +
                        $"in '{RuntimeAssemblyName}'.");
            
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AddDiagnostic(
                    diagnostics,
                    DiagnosticType.Error,
                    $"Failed to resolve wrapper type '{WrapperTypeFullName}': {ex}");

                return false;
            }
        }

        private static string Describe(FieldDefinition field) => $"{field.DeclaringType.FullName}.{field.Name}";

        private static void AddDiagnostic(List<DiagnosticMessage> diagnostics, DiagnosticType type, string message)
        {
            diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = type,
                MessageData = message,
            });
        }
    }


}