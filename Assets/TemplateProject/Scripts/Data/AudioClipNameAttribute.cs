using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Data
{
    public class AudioClipNameAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AudioClipNameAttribute))]
    public class AudioClipNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var audioLibrary = Resources.Load<AudioLibrary>("AudioLibrary");
            if (!audioLibrary)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var clipNames = audioLibrary.audioClips.Select(clip => clip.clipName).ToList();

            var selectedIndex = Mathf.Max(0, clipNames.IndexOf(property.stringValue));
            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, clipNames.ToArray());

            if (selectedIndex >= 0 && selectedIndex < clipNames.Count)
            {
                property.stringValue = clipNames[selectedIndex];
            }
        }
    }
#endif
}