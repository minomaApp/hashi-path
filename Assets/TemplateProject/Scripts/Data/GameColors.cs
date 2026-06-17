using UnityEngine;

namespace BoxPuller.Scripts.Data
{
    [CreateAssetMenu(fileName = "Game Color", menuName = "Game Color")]
    public class GameColors : ScriptableObject
    {
        public Color[] activeColors;
        public Material[] activeMaterials;

        [Header("Box Materials")]
        public Material[] boxMaterials;

        [Header("Shooter Materials")]
        public Material[] shooterMaterials;
        public Material[] shooterDarkMaterials;

        public Color[] editorColors;
        public Material[] chainMaterials;
        public Material[] chainInsideMaterials;
        public Material[] gridMatchColors;
        public GameObject[] boxParticles;

        [Space(10)]
        public Material iceMaterial;
        public Material secretColor;
        public Material ballMaterial;
        public Material connectorMaterial;
    }
}