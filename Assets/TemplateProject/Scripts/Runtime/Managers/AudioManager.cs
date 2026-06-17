using System.Collections.Generic;
using System.Linq;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;

        [Header("Cached References")] [SerializeField]
        private AudioLibrary audioLibrary;

        [SerializeField]
        private Dictionary<string, AudioClip> audioClipDictionary = new Dictionary<string, AudioClip>();

        [SerializeField] private List<AudioSource> audioSources;

        [Header("Background Music")]
        [SerializeField] private AudioSource musicSource;
        [AudioClipName] public string backgroundMusicClipName;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.35f;
        [SerializeField] private bool playBackgroundMusicOnStart = true;

        private void Awake()
        {
            InitializeSingleton();
            DontDestroyOnLoad(gameObject);
            InitializeAudioLibrary();
        }

        private void InitializeSingleton()
        {
            if (!instance)
            {
                instance = this;
            }
        }
        private void Start()
        {
            if (playBackgroundMusicOnStart)
            {
                PlayBackgroundMusic(backgroundMusicClipName);
            }
        }

        private void InitializeAudioLibrary()
        {
            foreach (var audioData in audioLibrary.audioClips)
            {
                audioClipDictionary.TryAdd(audioData.clipName, audioData.clip);
            }
        }

        private AudioSource GetOrCreateAudioSource()
        {
            foreach (var source in audioSources.Where(source => !source.isPlaying))
            {
                return source;
            }

            var newSource = gameObject.AddComponent<AudioSource>();
            audioSources.Add(newSource);
            return newSource;
        }

        public void PlaySound(string clipName, bool oneShot = true, bool loop = false, float volume = 1f)
        {
            if (!GameplayManager.instance.GetAudio()) return;
            if (audioClipDictionary.TryGetValue(clipName, out var clip))
            {
                var source = GetOrCreateAudioSource();
                source.volume = volume;
                if (oneShot)
                {
                    source.PlayOneShot(clip);
                    return;
                }

                source.loop = loop;
                source.clip = clip;
                source.Play();
            }
            else
            {
                Debug.LogWarning($"AudioClip '{clipName}' not found in AudioLibrary.");
            }
        }
        private void PrepareBackgroundMusic(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
                return;

            if (!audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
            {
                Debug.LogWarning($"Background music clip '{clipName}' not found in AudioLibrary.");
                return;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            if (GameplayManager.instance != null && GameplayManager.instance.GetAudio())
            {
                musicSource.volume = backgroundMusicVolume;
            }
            else
            {
                musicSource.volume = 0f;
            }
        }
        public void PlayBackgroundMusic(string clipName)
        {
            PrepareBackgroundMusic(clipName);

            if (musicSource == null)
                return;

            if (GameplayManager.instance == null)
                return;

            if (!GameplayManager.instance.GetAudio())
            {
                musicSource.Pause();
                musicSource.volume = 0f;
                return;
            }

            musicSource.volume = backgroundMusicVolume;

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void StopBackgroundMusic()
        {
            if (musicSource == null)
                return;

            musicSource.Stop();
        }

        public void PauseBackgroundMusic()
        {
            if (musicSource == null)
                return;

            musicSource.Pause();
        }

        public void ResumeBackgroundMusic()
        {
            if (musicSource == null)
                return;

            if (GameplayManager.instance.GetAudio())
            {
                musicSource.UnPause();
            }
        }

        public void SetBackgroundMusicVolume(float volume)
        {
            backgroundMusicVolume = Mathf.Clamp01(volume);

            if (musicSource != null)
            {
                musicSource.volume = backgroundMusicVolume;
            }
        }
        public void SetAudioEnabled(bool enabled)
        {
            AudioListener.volume = enabled ? 1f : 0f;

            if (musicSource == null)
            {
                PrepareBackgroundMusic(backgroundMusicClipName);
            }

            if (musicSource == null)
                return;

            if (enabled)
            {
                musicSource.volume = backgroundMusicVolume;

                if (!musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }
            else
            {
                musicSource.Pause();
                musicSource.volume = 0f;
            }
        }
    }
}