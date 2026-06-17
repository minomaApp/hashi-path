using System.Collections.Generic;
using UnityEngine;

namespace TemplateProject.Scripts.Data
{
    [CreateAssetMenu(fileName = "Audio Library", menuName = "ScriptableObjects/AudioLibrary", order = 1)]
    public class AudioLibrary : ScriptableObject
    {
        public List<AudioClipData> audioClips = new List<AudioClipData>();
        
        public AudioClip GetClip(string clipName)
        {
            return audioClips.Find(audio => audio.clipName == clipName)?.clip;
        }
    }

    [System.Serializable]
    public class AudioClipData
    {
        public string clipName;
        public AudioClip clip;
    }
}