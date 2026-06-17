using System;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Data.SO;
using TemplateProject.Scripts.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TemplateProject.Scripts.Runtime.Managers
{
    [DefaultExecutionOrder(-1)]
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager instance;

        [Header("Cached References")]
        [SerializeField] private TestConfig testConfig;
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private FeatureController featureController;

        [Header("Flags")]
        public bool isGamePlayable;
        public bool isLevelFailed;
        public bool isTestScene;
        public bool isTutorialOn;

        [Header("Parameters")]
        [SerializeField] private int levelIndex;
        [SerializeField] private int totalLevelCount;
        [SerializeField] private int totalPlayedLevelCount;

        public Action onLevelLoadComplete;

        private bool saveDataInitialized;
        private bool postLevelLoadInitialized;

        private void Awake()
        {
            HandleFPS();
            MakeSingleton();
        }

        private void HandleFPS()
        {
            if (Application.targetFrameRate != 120)
            {
                Application.targetFrameRate = 120;
            }
        }

        private void MakeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Prefab seçilmeden önce çaðrýlýr.
        /// Sadece level index datasýný hazýrlar.
        /// Tutorial, feature, UI baþlatmaz.
        /// </summary>
        public void InitializeLevelDataForLoading(int levelCount)
        {
            SetTotalLevelCount(levelCount);

            if (saveDataInitialized)
                return;

            HandleSaveData();
            ClampLevelIndexIfNeeded();

            saveDataInitialized = true;
        }

        /// <summary>
        /// Sadece save datasýný okur.
        /// Tutorial baþlatmaz.
        /// Feature baþlatmaz.
        /// </summary>
        public void HandleSaveData()
        {
            if (!isTestScene)
            {
                FetchPlayerPrefs();
            }
            else
            {
                levelIndex = testConfig != null ? testConfig.testLevelIndex : 0;
                totalPlayedLevelCount = levelIndex + 1;
            }

            ClampLevelIndexIfNeeded();
        }

        private void FetchPlayerPrefs()
        {
            levelIndex = PlayerPrefs.GetInt("CurrentLevel", 0);
            totalPlayedLevelCount = PlayerPrefs.GetInt("TotalPlayedLevel", levelIndex + 1);
        }

        /// <summary>
        /// Prefab instantiate edildikten sonra çaðrýlýr.
        /// Tutorial, feature gibi sahneye baðlý sistemler burada baþlatýlýr.
        /// </summary>
        public void InitializeAfterLevelLoaded()
        {
            if (postLevelLoadInitialized)
                return;

            if (featureController != null)
            {
                featureController.InitializeController();
            }

            HandleTutorialLevelIndexes();

            if (tutorialController != null)
            {
                tutorialController.StartTutorial(levelIndex, totalPlayedLevelCount);
            }

            postLevelLoadInitialized = true;

            // Elephant.LevelStarted(totalPlayedLevelCount);
        }

        private void HandleTutorialLevelIndexes()
        {
            if (tutorialController == null)
                return;

            if (tutorialController.tutorialSettings == null)
                return;

            if (tutorialController.tutorialSettings.tutorialLevels == null)
                return;

            if (ABManager.TutorialLevels == null)
                return;

            int count = Mathf.Min(
                ABManager.TutorialLevels.Length,
                tutorialController.tutorialSettings.tutorialLevels.Count
            );

            for (var i = 0; i < count; i++)
            {
                var level = ABManager.TutorialLevels[i];
                var tutorialLevel = tutorialController.tutorialSettings.tutorialLevels[i];

                tutorialLevel.tutorialIndex = level;
                tutorialController.tutorialSettings.tutorialLevels[i] = tutorialLevel;
            }
        }

        public void LevelIncrease()
        {
            IncreaseLevelIndex();
            LoadLevel();
        }

        private void LoadLevel()
        {
            var asyncOperation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            asyncOperation.allowSceneActivation = false;

            UIManager.instance.OpenTransition(() =>
            {
                asyncOperation.allowSceneActivation = true;
            });
        }

        private void IncreaseLevelIndex()
        {
            levelIndex++;
            totalPlayedLevelCount++;

            if (totalLevelCount > 0 && levelIndex >= totalLevelCount)
            {
                levelIndex = GetSafeLoopStartLevel();
            }

            PlayerPrefs.SetInt("CurrentLevel", levelIndex);
            PlayerPrefs.SetInt("TotalPlayedLevel", totalPlayedLevelCount);
            PlayerPrefs.Save();
        }

        public void RestartLevel()
        {
            LoadLevel();
        }

        public int GetLevelIndex()
        {
            ClampLevelIndexIfNeeded();

            if (ABManager.Levels == null || ABManager.Levels.Length == 0)
            {
                Debug.LogError("[LevelManager] ABManager.Levels boþ. Level index 0 döndürüldü.");
                return 0;
            }

            return ABManager.Levels[levelIndex];
        }

        public int GetCurrentRawLevelIndex()
        {
            return levelIndex;
        }

        private void ClampLevelIndexIfNeeded()
        {
            if (totalLevelCount <= 0)
                return;

            if (levelIndex < 0)
            {
                levelIndex = 0;
            }

            if (levelIndex >= totalLevelCount)
            {
                levelIndex = GetSafeLoopStartLevel();

                PlayerPrefs.SetInt("CurrentLevel", levelIndex);
                PlayerPrefs.Save();
            }
        }

        private int GetSafeLoopStartLevel()
        {
            if (totalLevelCount <= 0)
                return 0;

            int loopStart = ABManager.LevelLoopStartLevel;

            if (loopStart < 0)
                loopStart = 0;

            if (loopStart >= totalLevelCount)
                loopStart = 0;

            return loopStart;
        }

        public void SetTotalLevelCount(int levelCount)
        {
            totalLevelCount = Mathf.Max(0, levelCount);
        }

        public void SetLevelTMP(TextMeshProUGUI levelTMP, TextMeshProUGUI startLevelTMP)
        {
            if (levelTMP != null)
            {
                levelTMP.text = "Level " + totalPlayedLevelCount;
            }

            if (startLevelTMP != null)
            {
                startLevelTMP.text = "LEVEL " + totalPlayedLevelCount;
            }
        }

        public int GetTotalLevelCount()
        {
            return totalLevelCount;
        }

        public int GetTotalLevelPlayed()
        {
            return totalPlayedLevelCount;
        }

        public void OpenCheatLevel(int index)
        {
            levelIndex = Mathf.Max(0, index);

            if (totalLevelCount > 0 && levelIndex >= totalLevelCount)
            {
                levelIndex = 0;
            }

            totalPlayedLevelCount = levelIndex + 1;

            PlayerPrefs.SetInt("CurrentLevel", levelIndex);
            PlayerPrefs.SetInt("TotalPlayedLevel", totalPlayedLevelCount);
            PlayerPrefs.Save();
        }
    }
}