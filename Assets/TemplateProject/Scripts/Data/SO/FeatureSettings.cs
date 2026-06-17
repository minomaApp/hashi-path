using System;
using System.Collections.Generic;
using UnityEngine;

namespace TemplateProject.Scripts.Data.SO
{
    [Serializable]
    public class Feature
    {
        public int unlockLevelNumber;
        public Sprite prizeSprite;
        public Sprite prizeHiddenSprite;
    }

    [CreateAssetMenu(fileName = "FeatureSettings", menuName = "ScriptableObjects/FeatureSettings")]
    public class FeatureSettings : ScriptableObject
    {
        public List<Feature> features;
    }
}