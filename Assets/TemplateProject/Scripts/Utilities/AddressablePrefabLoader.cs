using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Serialization;

namespace TemplateProject.Scripts.Utilities
{
    public class AddressablePrefabLoader : MonoBehaviour
    {
        [SerializeField] private string prefabLabel = "Level";

        [Header("Cached References")]
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private CinemachineCamera virtualCamera;

        [Header("Test")]
        [SerializeField] private bool isTest;
        [SerializeField] private int testLevelIndex;

        [Header("Game Setup")]
        [FormerlySerializedAs("shooterBoxGameManager")]
        [SerializeField] private global::GameManager gameManager;
        [SerializeField] private bool useTimer;

        private GameObject loadedPrefabInstance;
        private int currentLogicalLevelIndex;

        private void Start()
        {
            LoadLocationsAndSpawn();
        }

        private void LoadLocationsAndSpawn()
        {
            Addressables.LoadResourceLocationsAsync(prefabLabel).Completed +=
                OnLocationsLoaded;
        }

        private async void OnLocationsLoaded(
            AsyncOperationHandle<IList<IResourceLocation>> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError(
                    "[AddressablePrefabLoader] Resource location load failed.");
                return;
            }

            int levelCount = handle.Result.Count;

            if (levelCount <= 0)
            {
                Debug.LogError(
                    "[AddressablePrefabLoader] No level uses label: " + prefabLabel);
                return;
            }

            if (LevelManager.instance == null)
            {
                Debug.LogError("[AddressablePrefabLoader] LevelManager is missing.");
                return;
            }

            LevelManager.instance.InitializeLevelDataForLoading(levelCount);
            int logicalIndex = LevelManager.instance.GetLevelIndex();

            if (isTest)
            {
                logicalIndex = Mathf.Clamp(testLevelIndex, 0, levelCount - 1);
            }

            currentLogicalLevelIndex = logicalIndex;

            string prefabAddress = await LevelCacheManager.Instance
                .GetPrefabAddressByIndex(currentLogicalLevelIndex);

            if (string.IsNullOrEmpty(prefabAddress))
            {
                Debug.LogError(
                    "[AddressablePrefabLoader] Prefab address is empty. Index: " +
                    currentLogicalLevelIndex);
                return;
            }

            InstantiateFromCache(prefabAddress);
        }

        private void InstantiateFromCache(string prefabAddress)
        {
            var handle =
                LevelCacheManager.Instance.GetPrefabHandle(prefabAddress);

            if (!handle.IsValid() || handle.Result == null)
            {
                Debug.LogError(
                    "[AddressablePrefabLoader] Invalid prefab handle: " +
                    prefabAddress);
                return;
            }

            OnPrefabInstantiated(handle.Result);
        }

        private void OnPrefabInstantiated(GameObject prefab)
        {
            loadedPrefabInstance = Instantiate(prefab);

            if (!loadedPrefabInstance.TryGetComponent(
                    out LevelContainer levelContainer))
            {
                levelContainer = loadedPrefabInstance
                    .GetComponentInChildren<LevelContainer>(true);
            }

            if (levelContainer == null)
            {
                Debug.LogError(
                    "[AddressablePrefabLoader] Loaded prefab has no LevelContainer.");
                return;
            }

            levelContainer.InitializeVariables(
                gameplayManager,
                gridManager,
                virtualCamera);

            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<global::GameManager>();
            }

            if (gameManager == null)
            {
                Debug.LogError(
                    "[AddressablePrefabLoader] GameManager is not assigned.");
                return;
            }

            int levelDataIndex = ResolveLevelDataIndex(
                prefab.name,
                currentLogicalLevelIndex);

            LevelData levelData = RuntimeLevelDataLoader.LoadLevel(levelDataIndex);

            if (levelData == null)
            {
                Debug.LogError(
                    "[AddressablePrefabLoader] LevelData could not be loaded. Index: " +
                    levelDataIndex);
                return;
            }

            gameManager.StartGame(levelData, levelContainer);
            //HandleTransitions(levelDataIndex);
            HandleTransitions(levelData);

            Debug.Log(
                "[AddressablePrefabLoader] Instantiated prefab: " + prefab.name);
        }

        private void HandleTransitions(LevelData levelData)
        {
            if (LevelManager.instance == null || UIManager.instance == null)
            {
                return;
            }

            LevelManager.instance.InitializeAfterLevelLoaded();

            //if (useTimer && TimeManager.instance != null)
            //{
            //    int timerValue = ABManager.GetTimerSeconds(levelDataIndex);
            //    TimeManager.instance.SetTimer(timerValue);
            //    TimeManager.instance.SetTimerTMP(
            //        UIManager.instance.GetTimerTMP(),
            //        UIManager.instance.GetStartLevelTimeTMP());
            //}

            if (useTimer && TimeManager.instance != null)
            {
                int timerValue = LevelData.DefaultLevelTimeSeconds;

                if (levelData != null)
                {
                    levelData.EnsureHashiData();
                    timerValue = Mathf.Max(1, levelData.levelTimeSeconds);
                }

                TimeManager.instance.SetTimer(timerValue);
                TimeManager.instance.SetTimerTMP(
                    UIManager.instance.GetTimerTMP(),
                    UIManager.instance.GetStartLevelTimeTMP());

                UIManager.instance.ShowTimerReady();
            }

            LevelManager.instance.SetLevelTMP(
                UIManager.instance.GetLevelTMP(),
                UIManager.instance.GetStartLevelTMP());

            DOVirtual.DelayedCall(0.2f, () =>
            {
                UIManager.instance.CloseLoadingScreen();

                DOVirtual.DelayedCall(0.2f, () =>
                {
                    UIManager.instance.CloseTransition(() =>
                    {
                        LevelManager.instance.onLevelLoadComplete?.Invoke();

                        //if (useTimer)
                        //{
                        //    UIManager.instance.OpenTimer();

                        //    if (TimeManager.instance != null)
                        //    {
                        //        TimeManager.instance.StartTimer();
                        //    }
                        //}
                        if (useTimer)
                        {
                            UIManager.instance.ShowTimerReady();
                        }

                        UIManager.instance.OpenLevelText();
                        UIManager.instance.EnableSettingsButton();
                        UIManager.instance.EnableRestartButton();
                        UIManager.instance.EnableCoinParent();
                    });
                });
            });
        }

        private static int ResolveLevelDataIndex(
            string prefabName,
            int fallbackIndex)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return fallbackIndex;
            }

            int separatorIndex = prefabName.LastIndexOf('_');
            string numberText = separatorIndex >= 0
                ? prefabName.Substring(separatorIndex + 1)
                : prefabName;

            return int.TryParse(numberText, out int parsedIndex)
                ? parsedIndex
                : fallbackIndex;
        }
    }
}
