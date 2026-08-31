using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OneLastClick.UnityObjectReferencing
{
    [Serializable]
    public abstract class UnityObjectReference : IEquatable<UnityObjectReference>
    {
        [SerializeField] UnityEngine.Object _unityObject;

        protected UnityEngine.Object UnityObject
        {
            get => _unityObject;
            set => _unityObject = value;
        }

        public abstract Type GetInterfaceType();
        
        public bool HasValue => null != _unityObject;
        
        public bool Equals(UnityObjectReference other)
        {
            if (ReferenceEquals(other, null) == true)
            {
                return false;
            }

            if (ReferenceEquals(this, other) == true)
            {
                return true;
            }

            return GetInterfaceType() == other.GetInterfaceType() && UnityObject == other.UnityObject;
        }

        public override bool Equals(object obj)
        {
            if (obj is UnityObjectReference other)
            {
                return Equals(other);
            }

            if (obj is Object unityObject)
            {
                return UnityObject == unityObject;
            }

            return false;
        } 
        public override int GetHashCode() => HashCode.Combine(GetInterfaceType(), UnityObject);

        public static bool operator ==(UnityObjectReference left, UnityObjectReference right)
        {
            if (ReferenceEquals(left, right) == true)
            {
                return true;
            }

            if (ReferenceEquals(left, null) == true || ReferenceEquals(right, null) == true)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(UnityObjectReference left, UnityObjectReference right) => !(left == right);
    }
    
    [Serializable]
    public class UnityObjectReference<T> : UnityObjectReference where T : class
    {
        public bool IsValid => HasValue == true && UnityObject is T;
        
        public T Value
        {
            get => UnityObject as T;
            set => UnityObject =  value as Object;
        }

        public static implicit operator T(UnityObjectReference<T> reference) => reference.Value;

        public static implicit operator UnityObjectReference<T>(T value) => new() { Value = value };
        
        public override Type GetInterfaceType()
        {
            return typeof(T);
        }
    }
}