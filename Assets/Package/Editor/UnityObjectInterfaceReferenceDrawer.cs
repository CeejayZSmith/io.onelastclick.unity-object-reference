using System;
using UnityEditor;
using UnityEngine;

namespace OneLastClick.UnityObjectReferencing.Editor
{
    [CustomPropertyDrawer(typeof(UnityObjectReference), true)]
    public class UnityObjectReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var objProp = property.FindPropertyRelative("_unityObject");

            var referenceInstance = GetManagedReference(property);
            Type interfaceType = referenceInstance?.GetInterfaceType();

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            Rect objectFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect typeLabelRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

            UnityEngine.Object newObj = EditorGUI.ObjectField(
                objectFieldRect,
                label,
                objProp.objectReferenceValue,
                typeof(UnityEngine.Object),
                true
            );

            if (EditorGUI.EndChangeCheck())
            {
                if (newObj == null)
                {
                    objProp.objectReferenceValue = null;
                }
                else if (IsValidForInterface(newObj, interfaceType))
                {
                    objProp.objectReferenceValue = newObj;
                }
                else
                {
                    if(TryResolveToInterface(newObj, interfaceType, out var newReference) == true)
                    {
                        objProp.objectReferenceValue = newReference;
                    }
                    else
                    {
                        Debug.LogWarning($"{newObj.name} does not implement {interfaceType?.Name}");
                    }
                }
            }

            // --- TYPE DISPLAY ---
            DrawObjectTypeLabel(typeLabelRect, objProp.objectReferenceValue);

            EditorGUI.EndProperty();
        }

        private void DrawObjectTypeLabel(Rect rect, UnityEngine.Object obj)
        {
            if (obj == null)
            {
                EditorGUI.LabelField(rect, "Type: None");
                return;
            }

            string typeInfo;

            if (obj is GameObject)
                typeInfo = "GameObject";
            else if (obj is MonoBehaviour)
                typeInfo = $"MonoBehaviour ({obj.GetType().Name})";
            else if (obj is ScriptableObject)
                typeInfo = $"ScriptableObject ({obj.GetType().Name})";
            else if (obj is Component)
                typeInfo = $"Component ({obj.GetType().Name})";
            else
                typeInfo = obj.GetType().Name;

            EditorGUI.LabelField(rect, $"Type: {typeInfo}");
        }

        private bool TryResolveToInterface(UnityEngine.Object obj, Type interfaceType, out UnityEngine.Object newReference)
        {
            newReference = null;
            if (obj is not GameObject objAsGameObject)
            {
                return false;
            }
        
            var component = objAsGameObject.GetComponent(interfaceType);
            if (component != null)
            {
                newReference = component;
                return true;
            }

            return false;
        }
    
        private bool IsValidForInterface(UnityEngine.Object obj, Type interfaceType)
        {
            if (interfaceType == null) return true;

            // Direct interface on MonoBehaviour / ScriptableObject
            if (interfaceType.IsAssignableFrom(obj.GetType()))
                return true;

            if (obj is GameObject)
            {
                return false;
            }

            return false;
        }

        private UnityObjectReference GetManagedReference(SerializedProperty property)
        {
            // Unity does not expose this directly, so we reconstruct via reflection
            var targetObject = property.serializedObject.targetObject;
            var path = property.propertyPath;

            var field = targetObject.GetType()
                .GetField(path, System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.Instance);

            return field?.GetValue(targetObject) as UnityObjectReference;
        }
    
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = 2; // object field + type label
            return (EditorGUIUtility.singleLineHeight * lines) + (Spacing * (lines - 1));
        }
    }
}