using NUnit.Framework;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Tests
{
    public class InterfaceTests
    {
        [Test]
        public void Interface_Reference_Resolves_Component()
        {
            var go = new GameObject();
            var component = go.AddComponent<TestComponent>();

            UnityObjectReference<ITestInterface> reference = component;

            Assert.NotNull(reference.Value);
            Assert.AreEqual(42, reference.Value.Value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Interface_Returns_Same_Component_Instance()
        {
            var go = new GameObject();
            var component = go.AddComponent<TestComponent>();

            UnityObjectReference<ITestInterface> reference = component;

            Assert.AreSame(component, reference.Value);

            Object.DestroyImmediate(go);
        }
    }
}