using NUnit.Framework;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Tests
{
    public class ComponentTests
    {
        [Test]
        public void Component_Reference_Keeps_Component_Not_GameObject()
        {
            var go = new GameObject();
            var component = go.AddComponent<TestComponent>();

            UnityObjectReference<TestComponent> reference = component;

            Assert.AreSame(component, reference.Value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameObject_Reference_Does_Not_Return_Component()
        {
            var go = new GameObject();
            go.AddComponent<TestComponent>();

            UnityObjectReference<GameObject> reference = go;

            Assert.AreSame(go, reference.Value);

            Object.DestroyImmediate(go);
        }
    }
}