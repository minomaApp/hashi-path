
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    [CreateAssetMenu(fileName = nameof(ProjectSettings), menuName = nameof(ProjectSettings), order = 0)]
    public class ProjectSettings : ScriptableObject
    {
        private static ProjectSettings s_Instance;
        private const string Path = "Settings/" + nameof(ProjectSettings);

        public static ProjectSettings Load()
        {
            if (s_Instance == null)
                s_Instance = Resources.Load<ProjectSettings>(Path);
            return s_Instance;
        }


        [SerializeField]
        private float _exampleSpeed;

        public float ExampleSpeed
        {
            get => _exampleSpeed;
            set => _exampleSpeed = value;
        }

    }
}
