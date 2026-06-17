#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities.EditorUtilities.InspectorAttributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class PrefabPreview : PropertyAttribute
    {
        public float Width { get; }
        public float Height { get; }
        public Type[] ComponentTypes { get; }

        public PrefabPreview(float width = 60, float height = 60, params Type[] componentTypes)
        {
            Width = width;
            Height = height;
            ComponentTypes = componentTypes.Length > 0 ? componentTypes : null;
        }
    }

    [CustomPropertyDrawer(typeof(PrefabPreview))]
    public class PrefabPreviewDrawer : PropertyDrawer
    {
        private Texture2D previewTexture;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PrefabPreview attribute = (PrefabPreview)base.attribute;



            EditorGUI.BeginProperty(position, label, property);
            EditorGUILayout.Separator();
            // Prefab alanı ve popup menü butonu için alanları belirleyin
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect objectFieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth - attribute.Width - 5, position.height);
            Rect buttonRect = new Rect(position.x + position.width - attribute.Width, position.y, attribute.Width, attribute.Height);

            // Prefab seçimi için ObjectField
            EditorGUI.ObjectField(objectFieldRect, property, typeof(GameObject), GUIContent.none);


            // Prefab'ın adını göstermek için bir Label ekleyin
            GameObject prefab = property.objectReferenceValue as GameObject;
            EditorGUI.LabelField(labelRect, label);

            DrawGameObjectPreview(prefab, buttonRect, attribute.ComponentTypes, attribute.Width, attribute.Height);

            EditorGUI.EndProperty();

        }
        private void DrawGameObjectPreview(GameObject prefab, Rect previewRect, Type[] componentTypes, float width, float height)
        {
            if (prefab != null)
            {
                GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));

                previewTexture = AssetPreview.GetAssetPreview(prefab);

                EditorGUILayout.Separator();

                // Önizleme alanına tıklanabilir bir düğme ekleyin
                if (GUI.Button(previewRect, GUIContent.none))
                {
                    if (componentTypes != null && componentTypes.Length > 0)
                    {
                        ShowProperties(prefab, componentTypes);
                    }
                }
                if (previewTexture != null)
                {
                    GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);
                }
                else
                {
                    // Placeholder texture area
                    EditorGUI.DrawRect(previewRect, Color.gray);
                    GUI.Label(previewRect, "Preview not available", new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }, wordWrap = true });
                }
            }
        }

        private void ShowProperties(GameObject prefab, Type[] componentTypes)
        {

            // Prefab'ın bileşenlerini göstermek için yeni bir pencere oluşturun
            var propertiesWindow = EditorWindow.GetWindow<PropertiesWindow>("Prefab Properties");
            propertiesWindow.Init(prefab, componentTypes);
        }
    }


    public class PropertiesWindow : EditorWindow
    {
        private GameObject prefab;
        private SerializedObject serializedPrefab;
        private Vector2 scrollPosition;
        private Texture2D previewTexture;
        private Type[] componentTypes;

        private Dictionary<Component, bool> componentFoldoutStates = new Dictionary<Component, bool>();

        public void Init(GameObject prefab, Type[] componentTypes)
        {
            this.prefab = prefab;
            this.componentTypes = componentTypes;
            serializedPrefab = new SerializedObject(prefab);
            previewTexture = AssetPreview.GetAssetPreview(prefab);
            Repaint();
        }

        private void OnGUI()
        {
            if (prefab == null)
            {
                EditorGUILayout.LabelField("No prefab selected.");
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawGameObjectProperties();

            DrawComponents(prefab);

            DrawGameObjectPreview();

            EditorGUILayout.EndScrollView();
        }

        private void DrawGameObjectProperties()
        {
            EditorGUILayout.LabelField("GameObject Properties", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedPrefab.FindProperty("m_Name"), new GUIContent("Name"));
            EditorGUILayout.PropertyField(serializedPrefab.FindProperty("m_TagString"), new GUIContent("Tag"));
            EditorGUILayout.PropertyField(serializedPrefab.FindProperty("m_Layer"), new GUIContent("Layer"));

            serializedPrefab.ApplyModifiedProperties();
        }

        private void DrawComponents(GameObject obj)
        {
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null) continue;

                // Sadece belirtilen türlerdeki bileşenleri çiz
                if (componentTypes != null && Array.Exists(componentTypes, type => type == component.GetType()))
                {
                    if (!componentFoldoutStates.ContainsKey(component))
                    {
                        componentFoldoutStates[component] = true;
                    }

                    componentFoldoutStates[component] = EditorGUILayout.Foldout(componentFoldoutStates[component], component.GetType().Name, true);

                    if (componentFoldoutStates[component])
                    {
                        var serializedComponent = new SerializedObject(component);
                        var property = serializedComponent.GetIterator();

                        property.NextVisible(true); // İlk görünen özelliğe git

                        while (property.NextVisible(false))
                        {
                            EditorGUILayout.PropertyField(property, true);
                        }

                        serializedComponent.ApplyModifiedProperties();
                    }
                }
            }

            // Child'ları da aynı şekilde işleme tabi tut
            foreach (Transform child in obj.transform)
            {
                DrawComponents(child.gameObject);
            }
        }

        private void DrawGameObjectPreview()
        {
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(300), GUILayout.MaxHeight(300));

            if (prefab != null)
            {
                if (previewTexture == null)
                {
                    previewTexture = AssetPreview.GetAssetPreview(prefab);
                    if (previewTexture == null)
                    {
                        EditorGUI.DrawRect(previewRect, Color.gray);
                        GUIStyle labelStyle = new GUIStyle
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.white },
                            wordWrap = true
                        };
                        string text = "Preview not available";
                        int fontSize = 20; // Başlangıç font boyutu
                        labelStyle.fontSize = fontSize;

                        // Metin yüksekliğini hesapla ve dikdörtgenin yüksekliğine uyana kadar azalt
                        while (labelStyle.CalcHeight(new GUIContent(text), previewRect.width) > previewRect.height && fontSize > 1)
                        {
                            fontSize--;
                            labelStyle.fontSize = fontSize;
                        }

                        GUI.Label(previewRect, text, labelStyle);
                        return;
                    }
                }
                EditorGUI.DrawPreviewTexture(previewRect, previewTexture);
            }
        }
    }
}
#endif