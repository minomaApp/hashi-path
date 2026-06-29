using System;
using System.Collections.Generic;
using TemplateProject.Scripts.Utilities;
using UnityEngine;

namespace TemplateProject.Scripts.Data.SO
{
    [Serializable]
    public enum StepType
    {
        Classic,
        EventTriggered,
        DoubleBridgeCreated
    }

    [Serializable]
    public struct TutorialLevel {
        public int tutorialIndex;
        public TutorialParent tutorialParent;
    }
    
    [Serializable]
    public struct TutorialStep {
        public GameObject stepPrefab;
        public StepType stepType;
    }

    [CreateAssetMenu(fileName = "TutorialSettings", menuName = "ScriptableObjects/TutorialSettings")]
    public class TutorialSettings : ScriptableObject
    {
        public List<TutorialLevel> tutorialLevels;
    }
}
