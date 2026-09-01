using NUnit.Framework;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Tests
{
    public class EqualityTests
    {
        [Test]
        public void Two_References_To_Same_Object_Are_Equal()
        {
            GameObject go = new GameObject();

            UnityObjectReference<GameObject> a = go;
            UnityObjectReference<GameObject> b = go;

            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Different_Objects_Are_Not_Equal()
        {
            GameObject aGo = new GameObject("A");
            GameObject bGo = new GameObject("B");

            UnityObjectReference<GameObject> a = aGo;
            UnityObjectReference<GameObject> b = bGo;

            Assert.AreNotEqual(a, b);
            Assert.IsTrue(a != b);

            Object.DestroyImmediate(aGo);
            Object.DestroyImmediate(bGo);
        }

        [Test]
        public void Empty_References_Are_Equal()
        {
            UnityObjectReference<GameObject> a = new();
            UnityObjectReference<GameObject> b = new();

            Assert.AreEqual(a, b);
        }

        [Test]
        public void Equals_Underlying_Unity_Object()
        {
            GameObject go = new GameObject();

            UnityObjectReference<GameObject> reference = go;

            Assert.IsTrue(reference.Equals(go));

            Object.DestroyImmediate(go);
        }
    }
}