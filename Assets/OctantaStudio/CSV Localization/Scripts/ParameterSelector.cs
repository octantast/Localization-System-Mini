using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LocalizationSystemMini
{
    /// <summary>
    /// Serializable class that allows selecting a specific field or property from a component on a GameObject.
    /// Used for dynamic parameter references in the tutorial system.
    /// </summary>
    [System.Serializable]
    public class ParameterSelector
    {
        public GameObject targetObject;
        public string componentTypeName;  // Name of the component type
        public string fieldName;

        /// <summary>
        /// Gets the current value of the selected field/property.
        /// </summary>
        public object GetValue()
        {
            return GetSelectedFieldValue(this);
        }

        /// <summary>
        /// Retrieves the value of the field/property specified in the selector using reflection.
        /// </summary>
        /// <param name="selector">The parameter selector containing target object, component, and field information</param>
        /// <returns>The value of the selected field/property, or null if not found</returns>
        public static object GetSelectedFieldValue(ParameterSelector selector)
        {
            if (selector == null || selector.targetObject == null ||
                string.IsNullOrEmpty(selector.componentTypeName) ||
                string.IsNullOrEmpty(selector.fieldName))
                return null;

            // Get component by type name
            var components = selector.targetObject.GetComponents<Component>();
            Component targetComponent = null;

            foreach (var comp in components)
            {
                if (comp.GetType().Name == selector.componentTypeName)
                {
                    targetComponent = comp;
                    break;
                }
            }

            if (targetComponent == null)
            {
                Debug.LogWarning($"Component '{selector.componentTypeName}' not found on {selector.targetObject.name}");
                return null;
            }

            var type = targetComponent.GetType();

            // Try to find as field
            var field = type.GetField(selector.fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (field != null)
                return field.GetValue(targetComponent);

            // Try to find as property
            var property = type.GetProperty(selector.fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (property != null && property.CanRead)
                return property.GetValue(targetComponent);

            Debug.LogWarning($"Parameter '{selector.fieldName}' not found in {type.Name}");
            return null;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom property drawer for ParameterSelector that provides a user-friendly interface
    /// with dropdowns for selecting GameObject, Component, and Field/Property.
    /// </summary>
    [CustomPropertyDrawer(typeof(ParameterSelector))]
    public class ParameterSelectorDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18f;
        private const float SPACING = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (LINE_HEIGHT + SPACING) * 3 + 4f; // Three lines total
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Define rectangles for each row
            Rect objectRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
            Rect componentRect = new Rect(position.x, position.y + LINE_HEIGHT + SPACING, position.width, LINE_HEIGHT);
            Rect fieldRect = new Rect(position.x, position.y + (LINE_HEIGHT + SPACING) * 2, position.width, LINE_HEIGHT);

            SerializedProperty objectProp = property.FindPropertyRelative("targetObject");
            SerializedProperty componentTypeProp = property.FindPropertyRelative("componentTypeName");
            SerializedProperty fieldNameProp = property.FindPropertyRelative("fieldName");

            // 1. Draw GameObject field
            EditorGUI.PropertyField(objectRect, objectProp, GUIContent.none);

            GameObject targetGO = objectProp.objectReferenceValue as GameObject;

            // 2. Show component dropdown if GameObject is selected
            if (targetGO != null)
            {
                var components = targetGO.GetComponents<Component>()
                    .Where(c => IsCustomComponent(c))
                    .ToList();
                var componentNames = components.Select(c => c.GetType().Name).ToList();

                if (componentNames.Count > 0)
                {
                    int currentComponentIndex = componentNames.IndexOf(componentTypeProp.stringValue);
                    if (currentComponentIndex == -1) currentComponentIndex = 0;

                    int newComponentIndex = EditorGUI.Popup(componentRect, currentComponentIndex, componentNames.ToArray());

                    if (newComponentIndex != currentComponentIndex)
                    {
                        componentTypeProp.stringValue = componentNames[newComponentIndex];
                        fieldNameProp.stringValue = ""; // Reset field when component changes
                    }

                    // 3. Show field/property dropdown if component is selected
                    if (!string.IsNullOrEmpty(componentTypeProp.stringValue))
                    {
                        var selectedComponent = components[newComponentIndex];
                        var availableMembers = GetAvailableMembers(selectedComponent);

                        if (availableMembers.Count > 0)
                        {
                            int currentFieldIndex = availableMembers.IndexOf(fieldNameProp.stringValue);
                            if (currentFieldIndex == -1)
                            {
                                availableMembers.Insert(0, fieldNameProp.stringValue);
                                currentFieldIndex = 0;
                            }

                            int newFieldIndex = EditorGUI.Popup(fieldRect, currentFieldIndex, availableMembers.ToArray());

                            if (newFieldIndex != currentFieldIndex)
                            {
                                fieldNameProp.stringValue = availableMembers[newFieldIndex];
                            }

                            // Display current value as a preview
                            var selector = new ParameterSelector
                            {
                                targetObject = targetGO,
                                componentTypeName = componentTypeProp.stringValue,
                                fieldName = fieldNameProp.stringValue
                            };
                            var value = ParameterSelector.GetSelectedFieldValue(selector);

                            if (value != null)
                            {
                                string valueStr = value.ToString();
                                if (valueStr.Length > 20) valueStr = valueStr.Substring(0, 17) + "...";

                                GUIStyle miniLabel = new GUIStyle(EditorStyles.miniLabel);
                                miniLabel.alignment = TextAnchor.MiddleRight;

                                GUI.Label(new Rect(fieldRect.x, fieldRect.y, fieldRect.width - 5, LINE_HEIGHT),
                                    $"= {valueStr}", miniLabel);
                            }
                        }
                        else
                        {
                            EditorGUI.LabelField(fieldRect, "No parameters found");
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(fieldRect, "Select component first");
                    }
                }
                else
                {
                    EditorGUI.LabelField(componentRect, "No components found");
                }
            }
            else
            {
                EditorGUI.LabelField(componentRect, "Select GameObject first");
                EditorGUI.LabelField(fieldRect, "---");
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Gets all accessible fields and properties from a component that are suitable for display.
        /// Filters out backing fields, Unity built-in properties, and complex types.
        /// </summary>
        private List<string> GetAvailableMembers(Component component)
        {
            if (component == null) return new List<string>();

            var type = component.GetType();
            var members = new List<string>();

            // Get all fields (public and private with SerializeField attribute)
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .Where(f => !f.Name.StartsWith("<")) // Exclude auto-property backing fields
                .Where(f => IsSimpleType(f.FieldType)) // Only simple types
                .Select(f => f.Name);

            members.AddRange(fields);

            // Get public properties with getters (only declared in this class)
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(p => p.CanRead)
                .Where(p => p.GetIndexParameters().Length == 0) // Exclude indexers
                .Where(p => IsSimpleType(p.PropertyType)) // Only simple types
                .Where(p => !IsUnityBuiltInProperty(p.Name)) // Exclude Unity built-in properties
                .Select(p => p.Name);

            members.AddRange(properties);

            return members.Distinct().OrderBy(m => m).ToList();
        }

        /// <summary>
        /// Checks if a property name is a Unity built-in property that should be excluded.
        /// </summary>
        private bool IsUnityBuiltInProperty(string propertyName)
        {
            var excludedNames = new HashSet<string>
            {
                "enabled", "isActiveAndEnabled", "tag", "name", "hideFlags",
                "transform", "gameObject", "didAwake", "didStart", "runInEditMode",
                "useGUILayout", "isActiveAndEnabled"
            };

            return excludedNames.Contains(propertyName);
        }

        /// <summary>
        /// Determines if a component is custom (user-created) and not a Unity built-in component.
        /// </summary>
        private bool IsCustomComponent(Component component)
        {
            if (component == null) return false;

            var type = component.GetType();

            // Exclude all Unity built-in components by namespace
            string namespaceName = type.Namespace ?? "";

            if (namespaceName.StartsWith("UnityEngine") ||
                namespaceName.StartsWith("UnityEditor") ||
                namespaceName.StartsWith("Unity."))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a type is considered "simple" for display purposes.
        /// Includes primitives, strings, Unity math types, and enums.
        /// </summary>
        private bool IsSimpleType(Type type)
        {
            // Primitive types: int, float, double, bool, byte, etc.
            if (type.IsPrimitive) return true;

            // String
            if (type == typeof(string)) return true;

            // Decimal
            if (type == typeof(decimal)) return true;

            // Unity vectors
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4)) return true;
            if (type == typeof(Vector2Int) || type == typeof(Vector3Int)) return true;

            // Unity color types
            if (type == typeof(Color) || type == typeof(Color32)) return true;

            // Quaternion
            if (type == typeof(Quaternion)) return true;

            // Rect types
            if (type == typeof(Rect) || type == typeof(RectInt)) return true;

            // Enums
            if (type.IsEnum) return true;

            return false;
        }
    }
#endif
}