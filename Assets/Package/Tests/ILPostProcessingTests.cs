using System.Reflection;
using NUnit.Framework;

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
    }
}