using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Runtime.Managers;
using DG.Tweening;
// using ElephantSDK;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Data.Config;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    [DefaultExecutionOrder(-1)]
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager instance;

        [Header("Cached References")] [SerializeField]
        private GameConfig gameConfig;

        [SerializeField] private UIManager uiManager;
        // public List<TowerScript> levelTowers;
        // public List<TowerScript> activeTowers;

        [Header("Parameters")] [AudioClipName] public string levelFailSound;
        [AudioClipName] public string levelCompleteSound;
        [AudioClipName] public string busArrivedSound;

        [Header("Game Flags")] [SerializeField]
        private bool isChangingGoal;

        [SerializeField] private bool isAudioOn;
        [SerializeField] private bool isVibrationOn;
        private bool _initialSettingsSet = true;
        private bool _isInitialBusArrived;

        [Header("Actions")] public Action onBusChangeDone;
        public Action onGameLost;
        private bool _winCalled;
        private bool _loseCountdownActive;
        private float _loseCountdownTimer;
        private const float LoseCountdownDuration = 0.5f;


#if UNITY_EDITOR

        // private void OnEnable()
        // {
        //     if (!LevelManager.instance.isTestScene) return;
        //     Application.logMessageReceived += HandleLog;
        // }
        //
        // private void OnDisable()
        // {
        //     if (!LevelManager.instance.isTestScene) return;
        //     Application.logMessageReceived -= HandleLog;
        // }
        //
        // void HandleLog(string logString, string stackTrace, LogType type)
        // {
        //     if (type == LogType.Error)
        //     {
        //         if (!LevelManager.instance.isTestScene) return;
        //         EditorApplication.isPlaying = false;
        //         EditorApplication.playModeStateChanged += GoToLevelCreator;
        //     }
        // }
#endif

        private void Awake()
        {
            InitializeSingleton();
            HandleGameConfig();
        }

        private void Update()
        {
            HandleLoseCondition();
        }

        private void HandleLoseCondition()
        {
            if (_loseCountdownActive && LevelManager.instance.isGamePlayable && !LevelManager.instance.isLevelFailed)
            {
                _loseCountdownTimer -= Time.deltaTime;
                if (_loseCountdownTimer <= 0f)
                {
                    LoseGame(isTimeLose: true);
                    // _loseCountdownActive = false;
                }
            }
        }

        private void InitializeSingleton()
        {
            if (instance) return;
            instance = this;
        }

        private void HandleGameConfig()
        {
            isAudioOn = gameConfig.isAudioOn == 1;
            isVibrationOn = gameConfig.isVibrationOn == 1;
            uiManager.HandleSwitches(isAudioOn, isVibrationOn);
            _initialSettingsSet = false;
            DOTween.SetTweensCapacity(400, 200);
        }

        public bool GetIsChangingGoal()
        {
            return isChangingGoal;
        }

        public void SetIsChangingGoal(bool flag)
        {
            isChangingGoal = flag;
        }

        public bool GetVibration()
        {
            return isVibrationOn;
        }

        public bool GetAudio()
        {
            return isAudioOn;
        }

        public void ToggleVibration()
        {
            if (_initialSettingsSet) return;
            isVibrationOn = !isVibrationOn;
            SaveConfig();
        }

        public void ToggleAudio()
        {
            if (_initialSettingsSet) return;
            isAudioOn = !isAudioOn;
            AudioListener.volume = isAudioOn ? 1 : 0;
            SaveConfig();

            if (AudioManager.instance != null)
            {
                AudioManager.instance.SetAudioEnabled(isAudioOn);
            }
        }

        private void SaveConfig()
        {
            gameConfig.Save(isAudioOn ? 1 : 0, isVibrationOn ? 1 : 0);
        }

        public void WinGame()
        {
            LevelManager.instance.isGamePlayable = false;
            uiManager.LevelCompleteEvents();

            if (TimeManager.instance != null)
            {
                TimeManager.instance.PauseTimer();
            }

            if (VibrationManager.instance)
            {
                VibrationManager.instance.Win();
            }

            if (AudioManager.instance)
            {
                AudioManager.instance.PlaySound(levelCompleteSound);
            }

            DOVirtual.DelayedCall(3f, () =>
            {
#if UNITY_EDITOR

                if (LevelManager.instance.isTestScene)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.playModeStateChanged += GoToLevelCreator;
                }
#endif
            });
        }

        public void LoseGame(bool isTimeLose)
        {
            if (!LevelManager.instance.isGamePlayable || LevelManager.instance.isLevelFailed) return;
            LevelManager.instance.isGamePlayable = false;
            LevelManager.instance.isLevelFailed = true;
            // Elephant.LevelFailed(LevelManager.instance.GetTotalLevelPlayed());
            DOVirtual.DelayedCall(0.25f, () =>
            {
                onGameLost?.Invoke();
                if (TimeManager.instance != null)
                {
                    TimeManager.instance.PauseTimer();
                }
                if (VibrationManager.instance)
                {
                    VibrationManager.instance.Fail();
                }

                if (AudioManager.instance)
                {
                    AudioManager.instance.PlaySound(levelFailSound);
                }

                uiManager.OpenLoseScreen();
            });


#if UNITY_EDITOR

            if (LevelManager.instance.isTestScene)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.playModeStateChanged += GoToLevelCreator;
                return;
            }
#endif


            
        }


#if UNITY_EDITOR

        private void GoToLevelCreator(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode)
            {
                EditorSceneManager.OpenScene("Assets/TemplateProject/Scenes/LevelCreator.unity");
                EditorApplication.playModeStateChanged -= GoToLevelCreator;
            }
        }


#endif
        // public void SetLevelTowers(List<TowerScript> towerScripts)
        // {
        //     levelTowers = towerScripts;
        // }
        //
        // public void RemoveTower(TowerScript tower)
        // {
        //     if (!levelTowers.Contains(tower)) return;
        //     levelTowers.Remove(tower);
        //     if (levelTowers.Count <= 0 && !_winCalled)
        //     {
        //         _winCalled = true;
        //         DOVirtual.DelayedCall(0.5f, WinGame);
        //     }
        // }
        //
        // public void AddActiveTowers(TowerScript tower)
        // {
        //     if (!activeTowers.Contains(tower))
        //     {
        //         activeTowers.Add(tower);
        //     }
        // }
        //
        // public void RemoveActiveTower(TowerScript tower)
        // {
        //     if (activeTowers.Contains(tower))
        //     {
        //         activeTowers.Remove(tower);
        //     }
        // }
        //
        // public List<TowerScript> GetActiveTowers()
        // {
        //     return activeTowers;
        // }

        public void NotifyLowPercent()
        {
            if (!_loseCountdownActive)
            {
                _loseCountdownActive = true;
                _loseCountdownTimer = LoseCountdownDuration;
            }
        }

        public void ResetLoseTimer()
        {
            _loseCountdownActive = false;
        }

        public bool IsLoseCountdownActive() => _loseCountdownActive;

        public float GetLoseTimer()
        {
            return _loseCountdownTimer;
        }
    }
}