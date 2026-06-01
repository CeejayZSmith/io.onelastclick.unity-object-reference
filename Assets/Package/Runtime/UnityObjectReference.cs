using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FinalClick.UnityObjectReferencing
{
    [Serializable]
    public abstract class UnityObjectReference
    {
        [SerializeField] UnityEngine.Object _unityObject;

        protected UnityEngine.Object UnityObject
        {
            get => _unityObject;
            set => _unityObject = value;
        }

        public abstract Type GetInterfaceType();
        
        public bool HasValue => null != _unityObject;
    }
    
    [Serializable]
    public class UnityObjectReference<T> : UnityObjectReference where T : class
    {
        public bool IsValid => HasValue == false || UnityObject is T;
        
        public T Value
        {
            get => UnityObject as T;
            set => UnityObject =  value as Object;
        }

        public static implicit operator T(UnityObjectReference<T> reference) => reference.Value;
        
        public override Type GetInterfaceType()
        {
            return typeof(T);
        }
    }
}