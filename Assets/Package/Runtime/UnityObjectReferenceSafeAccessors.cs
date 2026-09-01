namespace OneLastClick.UnityObjectReferencing
{
    public static class UnityObjectReferenceSafeAccessors
    {
        public static T SafeGet<T>(UnityObjectReference<T> reference) where T : class
        {
            return reference?.Value;
        }
        
        public static UnityObjectReference<T> SafeSet<T>(UnityObjectReference<T> reference, T value) where T : class
        {
            reference ??= new UnityObjectReference<T>();
            reference.UnityObject = value as UnityEngine.Object;
            return reference;
        }
    }
}