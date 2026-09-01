using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Tests
{
    public static class ILPostProcessingTests
    {
        [Test]
        public static void SerializedInterfaceField_FieldIsWrapperType()
        {
            FieldInfo field = typeof(SerializedInterfaceReferenceContainer).GetField("_interface", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field);
            Assert.AreEqual(typeof(UnityObjectReference<ITestInterface>), field.FieldType);
        }

        [Test]
        public static void SerializedInterfaceField_GetNoValueSet_ReturnsNull()
        {
            SerializedInterfaceReferenceContainer container = new SerializedInterfaceReferenceContainer();
            Assert.IsNull(container.Interface);
        }
        
        [Test]
        public static void SerializedInterfaceField_SetWithInterfaceInstance_CorrectlySet()
        {
            SerializedInterfaceReferenceContainer container = new SerializedInterfaceReferenceContainer();
            
            GameObject go = new GameObject();
            TestComponent component = go.AddComponent<TestComponent>();

            Assert.IsNull(container.Interface);
            
            container.Interface = component;
            
            Assert.AreEqual(component, container.Interface);
            
            Object.DestroyImmediate(go);
        }
    }
}