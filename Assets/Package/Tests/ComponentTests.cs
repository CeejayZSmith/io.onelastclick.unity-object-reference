using NUnit.Framework;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Tests
{
    public class ComponentTests
    {
        [Test]
        public void Component_Reference_Keeps_Component_Not_GameObject()
        {
            GameObject go = new GameObject();
            TestComponent component = go.AddComponent<TestComponent>();

            UnityObjectReference<TestComponent> reference = component;

            Assert.AreSame(component, reference.Value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameObject_Reference_Does_Not_Return_Component()
        {
            GameObject go = new GameObject();
            go.AddComponent<TestComponent>();

            UnityObjectReference<GameObject> reference = go;

            Assert.AreSame(go, reference.Value);

            Object.DestroyImmediate(go);
        }
    }
}