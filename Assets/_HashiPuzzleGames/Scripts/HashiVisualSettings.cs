using UnityEngine;

namespace BoxPuller.Scripts.Data.SO
{
    [CreateAssetMenu(fileName = "HashiVisualSettings", menuName = "ScriptableObjects/Hashi Visual Settings")]
    public class HashiVisualSettings : ScriptableObject
    {
        [Header("Island Materials")]
        public Material normalIslandMaterial;
        public Material completedIslandMaterial;
        public Material lockedIslandMaterial;
        public Material failedIslandMaterial;

        [Header("Bridge Materials")]
        public Material normalBridgeMaterial;
        public Material fixedBridgeMaterial;

        [Header("Chain Material")]
        public Material chainMaterial;

        [Header("Preview Materials")]
        public Material validPreviewMaterial;
        public Material invalidPreviewMaterial;

        [Header("Preview Colors")]
        public Color validPreviewColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        public Color invalidPreviewColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        [Header("Optional Effects")]
        public GameObject islandUnlockEffectPrefab;
        public GameObject chainBreakEffectPrefab;
    }
}
