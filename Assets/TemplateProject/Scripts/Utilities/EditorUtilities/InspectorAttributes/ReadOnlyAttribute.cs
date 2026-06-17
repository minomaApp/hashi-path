using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities.EditorUtilities.InspectorAttributes
{
    // This attribute will always be available, even in builds.
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    // This drawer will only be included in the editor and excluded from builds.
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var originalLabel = label.text;
            label.text = originalLabel + " [RO]";

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndDisabledGroup();
        }
    }
#endif
}