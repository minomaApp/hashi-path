using System.Collections.Generic;
using System.Threading.Tasks;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TemplateProject.Scripts.Utilities
{
    public class LevelCacheManager : MonoBehaviour
    {
        public static LevelCacheManager Instance { get; private set; }

        private Dictionary<string, AsyncOperationHandle<GameObject>> prefabHandles = new();
        private List<string> sortedAddresses = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Loads and caches levels starting from current logical levelIndex using ABManager.Levels mapping.
        /// </summary>
        public async Task PreloadInitialLevelsWindow(int startLogicalIndex, int count, LoadingUI loadingUI = null)
        {
            Debug.Log("StartLevelChange");

            int maxIndex = Mathf.Min(startLogicalIndex + count, ABManager.Levels.Length);
            int totalToLoad = maxIndex - startLogicalIndex;
            int completed = 0;

            var tasks = new List<Task>();

            for (int i = startLogicalIndex; i < maxIndex; i++)
            {
                int actualIndex = ABManager.Levels[i];
                string key = $"Level_{actualIndex}";

                if (prefabHandles.ContainsKey(key))
                {
                    completed++;
                    continue;
                }

                var handle = Addressables.LoadAssetAsync<GameObject>(key);
                prefabHandles[key] = handle;

                tasks.Add(handle.Task.ContinueWith(_ =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (!sortedAddresses.Contains(key))
                            sortedAddresses.Add(key);
                    }
                    else
                    {
                        Debug.LogError($"[LevelCacheManager] Failed to load: {key}");
                    }

                    completed++;
                }));
            }

            while (completed < totalToLoad)
            {
                if (loadingUI != null)
                    loadingUI.SetProgress((float)completed / totalToLoad);

                await Task.Yield();
            }

            await Task.WhenAll(tasks);

            if (loadingUI != null)
                loadingUI.SetProgress(1f);

            Debug.Log($"[LevelCacheManager] Parallel preload complete: {completed}/{totalToLoad} levels.");
        }

        /// <summary>
        /// Loads one level by logical index (mapped to actual via ABManager).
        /// </summary>
        public async Task LoadSingleLevelByLogicalIndex(int logicalIndex, bool log = false)
        {
            if (logicalIndex < 0 || logicalIndex >= ABManager.Levels.Length)
                return;

            int actualIndex = ABManager.Levels[logicalIndex];
            string key = $"Level_{actualIndex}";

            if (prefabHandles.ContainsKey(key))
            {
                if (log) Debug.Log($"[LevelCacheManager] Already loaded: {key}");
                return;
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(key);
            prefabHandles[key] = handle;

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (log) Debug.Log($"[LevelCacheManager] Loaded level: {key}");
                if (!sortedAddresses.Contains(key)) sortedAddresses.Add(key);
            }
            else
            {
                Debug.LogError($"[LevelCacheManager] Failed to load: {key}");
            }
        }

        /// <summary>
        /// Gets a loaded prefab handle by address (e.g., "Level_12").
        /// </summary>
        public AsyncOperationHandle<GameObject> GetPrefabHandle(string address)
        {
            if (prefabHandles.TryGetValue(address, out var handle))
                return handle;

            Debug.LogError($"[LevelCacheManager] Prefab handle not found for: {address}");
            return default;
        }

        /// <summary>
        /// Instantiate using a cached handle.
        /// </summary>
        public AsyncOperationHandle<GameObject> InstantiateCached(string address)
        {
            if (!prefabHandles.ContainsKey(address))
            {
                Debug.LogError($"[LevelCacheManager] Can't instantiate. No cached prefab for: {address}");
                return default;
            }

            return Addressables.InstantiateAsync(prefabHandles[address]);
        }

        /// <summary>
        /// Gets a prefab address by mapped index (e.g., "Level_12").
        /// </summary>
        public async Task<string> GetPrefabAddressByIndex(int logicalIndex)
        {
            if (logicalIndex < 0 || logicalIndex >= ABManager.Levels.Length)
            {
                Debug.LogError($"[LevelCacheManager] Invalid logical index: {logicalIndex}");
                return null;
            }
        
            int actualIndex = ABManager.Levels[logicalIndex];
            string key = $"Level_{actualIndex}";
        
            if (!prefabHandles.ContainsKey(key))
            {
                Debug.LogWarning($"[LevelCacheManager] Address not cached yet: {key}");
                await LoadSingleLevelByLogicalIndex(logicalIndex, true);
                actualIndex = ABManager.Levels[logicalIndex];
                key = $"Level_{actualIndex}";
            }
        
            return key;
        }

        public void ReleaseAll()
        {
            foreach (var handle in prefabHandles.Values)
            {
                Addressables.Release(handle);
            }

            prefabHandles.Clear();
            sortedAddresses.Clear();

            Debug.Log("[LevelCacheManager] All handles released.");
        }
    }
}
