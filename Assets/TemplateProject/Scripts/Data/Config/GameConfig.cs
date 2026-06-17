using UnityEngine;

namespace TemplateProject.Scripts.Data.Config
{
    [CreateAssetMenu(fileName = "Game Config", menuName = "ScriptableObjects/GameConfig", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("Audio Settings")] public int isAudioOn;

        [Header("Vibration Settings")] public int isVibrationOn;

        public void Save(int isAudio, int isVibration)
        {
            isAudioOn = isAudio;
            isVibrationOn = isVibration;
        }
    }
}