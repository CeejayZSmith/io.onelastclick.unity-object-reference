using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace OneLastClick.UnityObjectReferencing.ILPostProcessing
{
    public static class InterfaceSerializeFieldDiscoverer
    { 
        const string SerializeFieldAttributeFullName = "UnityEngine.SerializeField";
        const string SerializeReferenceAttributeFullName = "UnityEngine.SerializeReference";
        
        public static List<FieldDefinition> FindTargetFields(TypeDefinition type, List<DiagnosticMessage> diagnostics)
        {
            List<FieldDefinition> result = new List<FieldDefinition>();

            foreach (FieldDefinition field in type.Fields)
            {
                if (HasAttribute(field, SerializeFieldAttributeFullName) == false)
                {
                    continue;
                }

                // Out-of-scope shape: array of interface, e.g. IFoo[]
                if (field.FieldType is ArrayType arrayType)
                {
                    if (IsInterfaceType(arrayType.ElementType) == true)
                    {
                        AddDiagnostic(diagnostics, DiagnosticType.Error, 
                            $"Field '{Describe(field)}' is an array of an " +
                            $"interface type ([SerializeField] on IFoo[]). " +
                            $"This is currently not supported.");
                    }
                    continue;
                }
                
                // [SerializeReference] is Unity's own polymorphic mechanism.
                if (HasAttribute(field, SerializeReferenceAttributeFullName) == true)
                {
                    AddDiagnostic(diagnostics, DiagnosticType.Error, $"Field '{Describe(field)}' has SerializeField AND SerializeReference attribute. This is not supported.");
                    continue;
                }

                if (IsInterfaceType(field.FieldType) == false)
                {
                    continue;
                }

                if (field.IsPrivate == false)
                {
                    AddDiagnostic(diagnostics, DiagnosticType.Warning,
                        $"Field '{Describe(field)}' is an interface-typed [SerializeField] " +
                        "field with accessibility above 'private'. This is currently not supported for serialization and will be skipped. " +
                        "Make the field private and creating a property with the desired accessibility instead.");
                    continue;
                }

                result.Add(field);
            }

            return result;
        }
        
        private static bool IsInterfaceType(TypeReference typeRef)
        {
            TypeDefinition resolved = typeRef.Resolve();
            return resolved != null && resolved.IsInterface;
        }

        private static bool HasAttribute(FieldDefinition field, string attributeFullName)
        {
            return field.HasCustomAttributes && field.CustomAttributes.Any(a => a.AttributeType.FullName == attributeFullName);
        }
        
        private static void AddDiagnostic(List<DiagnosticMessage> diagnostics, DiagnosticType type, string message)
        {
            diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = type,
                MessageData = message,
            });
        }
        
        private static string Describe(FieldDefinition field) => $"{field.DeclaringType.FullName}.{field.Name}";
    }
}