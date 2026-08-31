using NUnit.Framework;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Tests
{
    public class BasicTests
    {
        [Test]
        public void Stores_And_Returns_GameObject()
        {
            GameObject go = new GameObject("Test");

            UnityObjectReference<GameObject> reference = go;

            Assert.AreEqual(go, reference.Value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Stores_And_Returns_Component()
        {
            var go = new GameObject();
            var component = go.AddComponent<TestComponent>();

            UnityObjectReference<TestComponent> reference = component;

            Assert.AreEqual(component, reference.Value);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Stores_And_Returns_ScriptableObject()
        {
            var asset = ScriptableObject.CreateInstance<TestScriptableObject>();

            UnityObjectReference<TestScriptableObject> reference = asset;

            Assert.AreEqual(asset, reference.Value);

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void Empty_Reference_Returns_Null()
        {
            UnityObjectReference<GameObject> reference = new();

            Assert.IsNull(reference.Value);
        }

        [Test]
        public void Empty_Reference_Returns_IsInvalid()
        {
            UnityObjectReference<GameObject> reference = new();

            Assert.False(reference.IsValid);
        }
        
        [Test]
        public void Implicit_Conversion_To_Object_Works()
        {
            var go = new GameObject();

            UnityObjectReference<GameObject> reference = go;
            GameObject result = reference;

            Assert.AreSame(go, result);

            Object.DestroyImmediate(go);
        }
    }
}