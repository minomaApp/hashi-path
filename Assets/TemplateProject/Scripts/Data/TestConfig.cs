using UnityEngine;

namespace TemplateProject.Scripts.Data
{
    [CreateAssetMenu(fileName = "Test Config", menuName = "ScriptableObjects/TestConfig", order = 1)]
    public class TestConfig : ScriptableObject
    {
        [Header("Test Settings")] 
        public int testLevelIndex;

    }
}