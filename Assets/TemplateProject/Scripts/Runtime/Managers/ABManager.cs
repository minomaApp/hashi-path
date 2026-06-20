using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TemplateProject.Scripts.Runtime.Managers
{
    [DefaultExecutionOrder(-2)]
    public class ABManager : MonoBehaviour
    {
        public static ABManager Instance;

        public static bool IsTutorialOn { get; private set; }
        public static bool IsHighlight { get; private set; }
        public static bool IsShowDebugMenu { get; private set; }
        public static float LoadingSceneDelayTime { get; private set; }
        public static int LevelLoopStartLevel { get; private set; }

        public static int[] Levels { get; private set; }
        private static string levelsString;

        public static int[] TutorialLevels { get; private set; }
        private static string tutorialLevelsString;

        public static int[] FeatureUnlockLevels { get; private set; }
        private static string featureUnlockLevelsString;

        public static int[] SkipAfterLoopLevels { get; private set; }
        private static string skipAfterLoopLevelsString;

        public static bool IsTimerEnabled { get; private set; }
        public static float[] LevelTimers { get; private set; }
        private static string levelTimersString;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RefreshData();
        }

        public async Task RefreshDataWithoutElephant()
        {
            var handle = Addressables.LoadResourceLocationsAsync("Level");
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                int levelCount = handle.Result.Count;
                ApplyDefaultData(levelCount);
                Debug.Log($"[ABManager] Default level data created. Level Count: {levelCount}");
            }
            else
            {
                Debug.LogError("[ABManager] Resource location load failed. Fallback default data created.");
                ApplyDefaultData(30);
            }
        }

        public static void RefreshData()
        {
            /*
            var remoteConfig = RemoteConfig.GetInstance();

            #if UNITY_EDITOR
            IsShowDebugMenu = true;
            #else
            IsShowDebugMenu = remoteConfig.GetBool("isShowDebugMenu", false);
            #endif

            IsTutorialOn = remoteConfig.GetBool("isTutorialOn", true);
            IsHighlight = remoteConfig.GetBool("isHighlight", true);
            LoadingSceneDelayTime = remoteConfig.GetFloat("loadingSceneDelayTime", 1.5f);
            LevelLoopStartLevel = remoteConfig.GetInt("levelLoopStartLevel", 1);

            levelsString = remoteConfig.Get("levels", CreateDefaultLevelsString(30));
            tutorialLevelsString = remoteConfig.Get("tutorialLevels", "0");
            skipAfterLoopLevelsString = remoteConfig.Get("skipAfterLoopLevels", "");
            featureUnlockLevelsString = remoteConfig.Get("featureUnlockLevels", "");

            Levels = GetIntArrayFromString(levelsString);
            TutorialLevels = GetIntArrayFromString(tutorialLevelsString);
            FeatureUnlockLevels = GetIntArrayFromString(featureUnlockLevelsString);
            SkipAfterLoopLevels = GetIntArrayFromString(skipAfterLoopLevelsString);

            IsTimerEnabled = remoteConfig.GetBool("isTimerEnabled", true);
            levelTimersString = remoteConfig.Get("levelTimers", CreateDefaultString("180", Levels.Length));
            LevelTimers = GetFloatArrayFromString(levelTimersString);
            */

            // Remote config kapalýyken sistem boþ kalmasýn diye güvenli default.
            if (Levels == null || Levels.Length == 0)
            {
                ApplyDefaultData(30);
            }
        }

        private static void ApplyDefaultData(int levelCount)
        {
            if (levelCount <= 0)
            {
                levelCount = 1;
            }

            IsShowDebugMenu = true;
            IsTutorialOn = true;
            IsHighlight = true;
            LoadingSceneDelayTime = 1.5f;
            LevelLoopStartLevel = 1;

            levelsString = CreateDefaultLevelsString(levelCount);
            tutorialLevelsString = "0,1";
            skipAfterLoopLevelsString = "0";
            featureUnlockLevelsString = "4,8,14";
            levelTimersString = CreateDefaultString("60", levelCount);

            Levels = GetIntArrayFromString(levelsString);
            TutorialLevels = GetIntArrayFromString(tutorialLevelsString);
            FeatureUnlockLevels = GetIntArrayFromString(featureUnlockLevelsString);
            SkipAfterLoopLevels = GetIntArrayFromString(skipAfterLoopLevelsString);

            IsTimerEnabled = false;
            IsTimerEnabled = true;
            LevelTimers = GetFloatArrayFromString(levelTimersString);
        }

        public static int GetTimerSeconds(int logicalLevelIndex)
        {
            if (LevelTimers == null || LevelTimers.Length == 0)
            {
                return 180;
            }

            if (logicalLevelIndex < 0)
            {
                logicalLevelIndex = 0;
            }

            if (logicalLevelIndex >= LevelTimers.Length)
            {
                logicalLevelIndex = LevelTimers.Length - 1;
            }

            return Mathf.RoundToInt(LevelTimers[logicalLevelIndex]);
        }

        private static int[] GetIntArrayFromString(string stringToParse)
        {
            var intArray = new List<int>();

            if (string.IsNullOrEmpty(stringToParse))
            {
                return intArray.ToArray();
            }

            var stringArray = stringToParse.Split(',');

            foreach (var str in stringArray)
            {
                if (int.TryParse(str, out var level))
                {
                    intArray.Add(level);
                }
            }

            return intArray.ToArray();
        }

        private static float[] GetFloatArrayFromString(string stringToParse)
        {
            var floatArray = new List<float>();

            if (string.IsNullOrEmpty(stringToParse))
            {
                return floatArray.ToArray();
            }

            var stringArray = stringToParse.Split(',');

            foreach (var str in stringArray)
            {
                if (float.TryParse(str, out var level))
                {
                    floatArray.Add(level);
                }
            }

            return floatArray.ToArray();
        }

        private static string CreateDefaultString(string defaultValue, int levelAmount)
        {
            var stringToReturn = "";

            for (var i = 0; i < levelAmount; i++)
            {
                if (i != levelAmount - 1)
                {
                    stringToReturn += $"{defaultValue},";
                }
                else
                {
                    stringToReturn += defaultValue;
                }
            }

            return stringToReturn;
        }

        private static string CreateDefaultLevelsString(int levelAmount)
        {
            var stringToReturn = "";

            for (var i = 0; i < levelAmount; i++)
            {
                if (i != levelAmount - 1)
                {
                    stringToReturn += $"{i},";
                }
                else
                {
                    stringToReturn += i;
                }
            }

            return stringToReturn;
        }
    }
}