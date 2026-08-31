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

            return TryRewriteAccessSites(module, allTargetFields, wrapperTypeDef, diagnostics);
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

        private bool TryRewriteAccessSites(ModuleDefinition module, List<FieldDefinition> targetFields, TypeDefinition wrapperTypeDef, List<DiagnosticMessage> diagnostics)
        {
            if (targetFields.Count == 0)
            {
                return true;
            }

            if (TryCreateValueAccessorsForUnityObjectReferenceValueProperty(
                    module, 
                    targetFields, 
                    wrapperTypeDef,
                    diagnostics, 
                    out Dictionary<FieldDefinition, (MethodReference Getter, MethodReference Setter)> valueAccessors) == false)
            {
                return false;
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
                    Collection<Instruction> instructions = method.Body.Instructions;

                    // Iterate over a copy because we'll modify the instruction list.
                    for (int i = 0; i < instructions.Count; i++)
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

                        if (valueAccessors.TryGetValue(fieldDef, out (MethodReference Getter, MethodReference Setter) accessors) == false)
                        {
                            continue;
                        }

                        switch (instruction.OpCode.Code)
                        {
                            // READ access
                            case Code.Ldfld:
                            case Code.Ldsfld:
                            {
                                // Replace READ access to field with UnityObjectReference<T>.Value property getter.
                                Instruction call = il.Create(
                                    instruction.OpCode.Code == Code.Ldfld ? OpCodes.Callvirt : OpCodes.Call,
                                    accessors.Getter);
                                il.InsertAfter(instruction, call);
                            }
                                break;
                            // WRITE access
                            case Code.Stfld:
                            case Code.Stsfld:
                            {
                                AddDiagnostic(
                                    diagnostics,
                                    DiagnosticType.Error,
                                    $"Write access overriding is currently not supported. field: {Describe(fieldDef)}");
                                break;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static bool TryCreateValueAccessorsForUnityObjectReferenceValueProperty(
            ModuleDefinition module, 
            List<FieldDefinition> targetFields, 
            TypeDefinition wrapperTypeDef, 
            List<DiagnosticMessage> diagnostics, 
            out Dictionary<FieldDefinition, (MethodReference Getter, MethodReference Setter)> generatedValueAccessorsPerType)
        {
            bool allSuccess = true;
            
            generatedValueAccessorsPerType = new Dictionary<FieldDefinition, (MethodReference Getter, MethodReference Setter)>();

            foreach (FieldDefinition field in targetFields)
            {
                try
                {
                    GenericInstanceType wrapperInstance = (GenericInstanceType)field.FieldType;

                    PropertyDefinition valueProperty = wrapperTypeDef.Properties.First(p => p.Name == "Value");

                    MethodReference getter = module.ImportReference(valueProperty.GetMethod);
                    MethodReference setter = module.ImportReference(valueProperty.SetMethod);

                    getter = MakeGenericMethod(getter, wrapperInstance);
                    setter = MakeGenericMethod(setter, wrapperInstance);

                    generatedValueAccessorsPerType[field] = (getter, setter);
                }
                catch (Exception ex)
                {
                    AddDiagnostic(diagnostics, DiagnosticType.Error, $"Failed to resolve Value property for '{Describe(field)}': {ex.Message}");
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        private static MethodReference MakeGenericMethod(MethodReference method, GenericInstanceType declaringType)
        {
            MethodReference reference = new MethodReference(method.Name, declaringType.Module.ImportReference(method.ReturnType), declaringType)
            {
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention
            };

            foreach (ParameterDefinition parameter in method.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(declaringType.Module.ImportReference(parameter.ParameterType)));
            }

            return reference;
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