#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities.EditorUtilities.InspectorAttributes
{
    public class IncrementDecrementButton : PropertyAttribute
    {
        public int Min { get; }
        public int Max { get; }

        public IncrementDecrementButton(int min = int.MinValue, int max = int.MaxValue)
        {
            Min = min;
            Max = max;
        }
    }

    [CustomPropertyDrawer(typeof(IncrementDecrementButton))]
    public class IncrementDecrementButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "Use IncrementDecrementButton with int.");
                return;
            }

            var attribute = (IncrementDecrementButton)this.attribute;
            var minValue = attribute.Min;
            var maxValue = attribute.Max;

            EditorGUI.BeginProperty(position, label, property);

            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            property.intValue = EditorGUILayout.IntField(property.intValue, GUILayout.Width(50));
            property.intValue = Mathf.Clamp(property.intValue, minValue, maxValue);


            if (GUILayout.Button("-", GUILayout.Width(40)))
            {
                property.intValue = Mathf.Max(minValue, property.intValue - 1);
            }

            if (GUILayout.Button("+", GUILayout.Width(40)))
            {
                property.intValue = Mathf.Min(maxValue, property.intValue + 1);
            }

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif