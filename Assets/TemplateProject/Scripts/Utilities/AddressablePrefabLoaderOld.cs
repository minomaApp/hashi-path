using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using Unity.Cinemachine;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TemplateProject.Scripts.Utilities
{
    public class AddressablePrefabLoaderOld : MonoBehaviour
    {
        [Header("Cached References")] [SerializeField]
        private GameplayManager gameplayManager;

        [SerializeField] private GridManager gridManager;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private CinemachineCamera virtualCamera;

        [Header("Variables")] public string label = "Level";
        public int levelIndex;

        private GameObject loadedPrefabInstance;
        private static bool isAddressablesInitialized = false;
#if UNITY_EDITOR
        [Header("Editor")] public Action<GameObject> callbackAction;
        public AsyncOperationHandle<GameObject>? currentHandle;
#endif

        private async void Start()
        {
            float startTime = Time.realtimeSinceStartup;
            Debug.Log($"[Start] Prefab loading started at {startTime:F2}");

            if (!isAddressablesInitialized)
            {
                await Addressables.InitializeAsync().Task;
                isAddressablesInitialized = true;
            }

            AssignLevelCount(label);

            Debug.Log($"[Start] Init + AssignLevelCount took {(Time.realtimeSinceStartup - startTime):F2} seconds");
        }


        private void AssignLevelCount(string label)
        {
            Addressables.LoadResourceLocationsAsync(label).Completed += OnLocationsLoaded;
        }

        private async void OnLocationsLoaded(
            AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                LevelManager.instance.SetTotalLevelCount(handle.Result.Count);
                await ABManager.Instance.RefreshDataWithoutElephant();

                // var prefabAddress = $"Level_{LevelManager.instance.GetLevelIndex()}";
                var prefabAddress = $"Level_{levelIndex}";
                LoadAndInstantiatePrefab(prefabAddress);
            }
            else
            {
                Debug.LogError("Addressables resource location load failed.");
            }
        }

        private async void LoadAndInstantiatePrefab(string prefabAddress)
        {
            float loadStart = Time.realtimeSinceStartup;

            // Preload dependencies
            var downloadHandle = Addressables.DownloadDependenciesAsync(prefabAddress);
            await downloadHandle.Task;

            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to download dependencies for {prefabAddress}");
                return;
            }

            // Instantiate prefab
            var instantiateHandle = Addressables.InstantiateAsync(prefabAddress);
            instantiateHandle.Completed += OnPrefabInstantiated;

            Debug.Log(
                $"[LoadAndInstantiatePrefab] Dependencies downloaded. Took {(Time.realtimeSinceStartup - loadStart):F2} seconds");
        }

        private void OnPrefabInstantiated(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedPrefabInstance = handle.Result;

                if (loadedPrefabInstance.TryGetComponent(out LevelContainer levelContainer))
                {
                    levelContainer.InitializeVariables(gameplayManager, gridManager,
                        virtualCamera);
                }

                TimeManager.instance.SetTimer((int)ABManager.LevelTimers[levelIndex]);
                TimeManager.instance.SetTimerTMP(UIManager.instance.GetTimerTMP(),
                    UIManager.instance.GetStartLevelTimeTMP());
                LevelManager.instance.SetLevelTMP(UIManager.instance.GetLevelTMP(),
                    UIManager.instance.GetStartLevelTMP());
                HandleTransitions();

                Debug.Log($"[OnPrefabInstantiated] Successfully loaded and instantiated: {handle.Result.name}");
            }
            else
            {
                Debug.LogError($"[OnPrefabInstantiated] Failed to load prefab: {handle.DebugName}");
            }
        }

        private void HandleTransitions()
        {
            // TimeManager.instance.SetTimerTMP(UIManager.instance.GetTimerTMP(),
            //     UIManager.instance.GetStartLevelTimeTMP());
            
            LevelManager.instance.HandleSaveData();
            LevelManager.instance.SetLevelTMP(UIManager.instance.GetLevelTMP(),
                UIManager.instance.GetStartLevelTMP());
        
            UIManager.instance.CloseLoadingScreen();
            DOVirtual.DelayedCall(2f, () =>
            {
                UIManager.instance.CloseTransition(() =>
                {
                    
                    LevelManager.instance.onLevelLoadComplete?.Invoke();
                    UIManager.instance.OpenLevelText();
                    UIManager.instance.EnableSettingsButton();
                    UIManager.instance.EnableRestartButton();
                    UIManager.instance.EnableCoinParent();
                });
            });
        }

        private void OnDestroy()
        {
            if (loadedPrefabInstance)
            {
                Addressables.ReleaseInstance(loadedPrefabInstance);
            }
        }

#if UNITY_EDITOR
        public GameObject ManualPrefabLoader(string prefabAddress, Action<GameObject> callback)
        {
            callbackAction = callback;
            LoadPrefabEditor(prefabAddress);
            return loadedPrefabInstance;
        }

        private void LoadPrefabEditor(string prefabAddress)
        {
            if (currentHandle.HasValue && currentHandle.Value.IsValid())
            {
                currentHandle.Value.Completed -= OnPrefabLoadedEditor;
                Addressables.Release(currentHandle.Value);
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(prefabAddress);
            handle.Completed += OnPrefabLoadedEditor;
            currentHandle = handle;
        }

        private void OnPrefabLoadedEditor(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(handle.Result);
                callbackAction?.Invoke(loadedPrefabInstance);
                Debug.Log($"[Editor] Loaded and instantiated prefab: {handle.Result.name}");
                handle.Release();
            }
            else
            {
                Debug.LogError($"[Editor] Failed to load prefab.");
            }
        }

        private void OnDisable()
        {
            if (loadedPrefabInstance)
            {
                Addressables.ReleaseInstance(loadedPrefabInstance);
            }
        }
#endif
    }
}