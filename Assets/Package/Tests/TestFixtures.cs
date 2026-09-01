using System;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Tests
{
    public interface ITestInterface
    {
        int Value { get; }
    }

    public class TestComponent : MonoBehaviour, ITestInterface
    {
        public int Value => 42;
    }

    public class OtherComponent : MonoBehaviour
    {
    }

    public class TestScriptableObject : ScriptableObject
    {
        public string Message;
    }

    [System.Serializable]
    public class TestContainer
    {
        public UnityObjectReference<GameObject> GameObject;
        public UnityObjectReference<TestComponent> Component;
        public UnityObjectReference<ITestInterface> Interface;
        public UnityObjectReference<TestScriptableObject> Scriptable;
    }

    [Serializable]
    public struct SerializedInterfaceReferenceContainer
    {
        [SerializeField]
        private ITestInterface _interface;
    }
}