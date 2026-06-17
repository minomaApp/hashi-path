using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

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

        [Header("Shooter Box Game")]
        [SerializeField] private global::GameManager shooterBoxGameManager;

        private GameObject loadedPrefabInstance;
        private int currentLogicalLevelIndex;

        private void Start()
        {
            LoadLocationsAndSpawn();
        }

        private void LoadLocationsAndSpawn()
        {
            Addressables.LoadResourceLocationsAsync(prefabLabel).Completed += OnLocationsLoaded;
        }

        private async void OnLocationsLoaded(AsyncOperationHandle<IList<IResourceLocation>> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[AddressablePrefabLoader] Resource location load failed.");
                return;
            }

            int levelCount = handle.Result.Count;

            if (levelCount <= 0)
            {
                Debug.LogError("[AddressablePrefabLoader] Bu label altýnda hiç level bulunamadý: " + prefabLabel);
                return;
            }

            LevelManager.instance.InitializeLevelDataForLoading(levelCount);

            int logicalIndex = LevelManager.instance.GetLevelIndex();

            if (isTest)
            {
                logicalIndex = Mathf.Clamp(testLevelIndex, 0, levelCount - 1);
            }

            currentLogicalLevelIndex = logicalIndex;

            Debug.Log($"[AddressablePrefabLoader] Loading logical level index: {currentLogicalLevelIndex}");

            string prefabAddress = await LevelCacheManager.Instance.GetPrefabAddressByIndex(currentLogicalLevelIndex);

            if (string.IsNullOrEmpty(prefabAddress))
            {
                Debug.LogError($"[AddressablePrefabLoader] Prefab address boþ geldi. Index: {currentLogicalLevelIndex}");
                return;
            }

            InstantiateFromCache(prefabAddress);
        }

        private void InstantiateFromCache(string prefabAddress)
        {
            var handle = LevelCacheManager.Instance.GetPrefabHandle(prefabAddress);

            if (!handle.IsValid() || handle.Result == null)
            {
                Debug.LogError($"[AddressablePrefabLoader] Invalid or missing prefab handle for: {prefabAddress}");
                return;
            }

            GameObject prefab = handle.Result;
            OnPrefabInstantiated(prefab);
        }

        private void OnPrefabInstantiated(GameObject prefab)
        {
            loadedPrefabInstance = Instantiate(prefab);

            if (loadedPrefabInstance.TryGetComponent(out LevelContainer levelContainer))
            {
                levelContainer.InitializeVariables(gameplayManager, gridManager, virtualCamera);

                if (shooterBoxGameManager != null)
                {
                    LevelData levelData = RuntimeLevelDataLoader.LoadLevel(currentLogicalLevelIndex);
                    shooterBoxGameManager.StartGame(levelData, levelContainer);
                }
                else
                {
                    Debug.LogWarning("[AddressablePrefabLoader] shooterBoxGameManager atanmadý.");
                }
            }
            else
            {
                Debug.LogWarning("[AddressablePrefabLoader] Yüklenen prefab üzerinde LevelContainer bulunamadý.");
            }

            HandleTransitions();

            Debug.Log($"[AddressablePrefabLoader] Instantiated prefab: {prefab.name}");
        }

        private void HandleTransitions()
        {
            LevelManager.instance.InitializeAfterLevelLoaded();

            int timerValue = ABManager.GetTimerSeconds(currentLogicalLevelIndex);
            TimeManager.instance.SetTimer(timerValue);

            TimeManager.instance.SetTimerTMP(
                UIManager.instance.GetTimerTMP(),
                UIManager.instance.GetStartLevelTimeTMP()
            );

            LevelManager.instance.SetLevelTMP(
                UIManager.instance.GetLevelTMP(),
                UIManager.instance.GetStartLevelTMP()
            );

            DOVirtual.DelayedCall(0.2f, () =>
            {
                UIManager.instance.CloseLoadingScreen();

                DOVirtual.DelayedCall(0.2f, () =>
                {
                    UIManager.instance.CloseTransition(() =>
                    {
                        LevelManager.instance.onLevelLoadComplete?.Invoke();

                        UIManager.instance.OpenTimer();
                        UIManager.instance.OpenLevelText();
                        UIManager.instance.EnableSettingsButton();
                        UIManager.instance.EnableRestartButton();
                        UIManager.instance.EnableCoinParent();
                    });
                });
            });
        }
    }
}