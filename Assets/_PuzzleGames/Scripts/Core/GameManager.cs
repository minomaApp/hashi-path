using System.Threading.Tasks;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.SO;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum RuntimeLevelLoadMode
{
    Addressables = 0,
    Resources = 1,
    ExistingInScene = 2
}

public class GameManager : MonoBehaviour
{
    [Header("Runtime Managers")]
    [SerializeField] private BottomSlotManager bottomSlotManager;
    [SerializeField] private MiddleSlotManager middleSlotManager;
    [SerializeField] private FlowerRouletteManager flowerRouletteManager;
    [SerializeField] private BoxGridManager boxGridManager;
    [SerializeField] private ShooterTransferQueue shooterTransferQueue;
    [SerializeField] private BulletPool bulletPool;

    [Header("Prefabs")]
    [SerializeField] private GamePrefabs gamePrefabs;

    [Header("Level Loading")]
    [SerializeField] private bool loadLevelOnStart = true;
    [SerializeField] private RuntimeLevelLoadMode levelLoadMode = RuntimeLevelLoadMode.ExistingInScene;
    [SerializeField] private Transform levelParent;

    [Header("Level Index")]
    [SerializeField] private bool usePlayerPrefsLevelIndex = true;

    [Tooltip("Eðer level prefab/data Level_0 / Level0 diye baþlýyorsa 0 kullan. Level_1 / Level1 diye baþlýyorsa 1 kullan.")]
    [SerializeField] private int levelIndexOffset = 0;

    [SerializeField] private int fallbackLevelIndex = 0;

    [Header("Addressables")]
    [SerializeField] private string addressableLevelPrefix = "Level_";

    [Header("Resources")]
    [SerializeField] private string resourcesLevelPathPrefix = "Levels/Level_";

    [Header("Gameplay")]
    [SerializeField] private bool setGamePlayableAfterSetup = true;

    [Header("Existing Scene Level")]
    [SerializeField] private float waitLevelContainerTimeout = 5f;
    [SerializeField] private bool destroyDuplicateLevelContainers = true;

    private LevelData currentLevelData;
    private LevelContainer currentLevelContainer;
    private GameObject currentLevelObject;

    private bool isGameOver;

    private async void Start()
    {
        AutoAssignSceneManagers();
        ConnectSceneManagers();

        if (!loadLevelOnStart)
            return;

        await LoadAndStartCurrentLevel();
    }

    private void AutoAssignSceneManagers()
    {
        if (bottomSlotManager == null)
            bottomSlotManager = FindFirstObjectByType<BottomSlotManager>();

        if (middleSlotManager == null)
            middleSlotManager = FindFirstObjectByType<MiddleSlotManager>();

        if (flowerRouletteManager == null)
            flowerRouletteManager = FindFirstObjectByType<FlowerRouletteManager>();

        if (boxGridManager == null)
            boxGridManager = FindFirstObjectByType<BoxGridManager>();

        if (shooterTransferQueue == null)
            shooterTransferQueue = FindFirstObjectByType<ShooterTransferQueue>();

        if (bulletPool == null)
            bulletPool = FindFirstObjectByType<BulletPool>();
    }

 private void ConnectSceneManagers()
{
    if (flowerRouletteManager != null)
    {
        flowerRouletteManager.ConfigureRuntimeReferences(
            middleSlotManager,
            bottomSlotManager,
            this
        );
    }

    if (shooterTransferQueue != null)
    {
        shooterTransferQueue.ConfigureRuntimeReferences(flowerRouletteManager);
    }

    if (boxGridManager != null)
    {
        boxGridManager.ConfigureRuntimeReferences(this);
    }
}

    public async Task LoadAndStartCurrentLevel()
    {
        int levelIndex = GetRuntimeLevelIndex();
        Debug.Log($"[GameManager me me me LEVEL CHECK] Runtime loading LevelData/Level{levelIndex}");
        LevelData levelData = RuntimeLevelDataLoader.LoadLevel(levelIndex);

        if (levelData == null)
        {
            Debug.LogError($"[GameManager] LevelData yüklenemedi. Level index: {levelIndex}");
            return;
        }

        LevelContainer levelContainer = await LoadLevelContainer(levelIndex);

        if (levelContainer == null)
        {
            Debug.LogError($"[GameManager] LevelContainer bulunamadý. Level index: {levelIndex}");
            return;
        }

        ConnectSceneManagers();
        StartGame(levelData, levelContainer);
    }

    private int GetRuntimeLevelIndex()
    {
        if (!usePlayerPrefsLevelIndex)
            return fallbackLevelIndex;

        int savedLevelIndex = PlayerPrefs.GetInt("CurrentLevel", fallbackLevelIndex);
        int resolvedLevelIndex = savedLevelIndex;

        if (ABManager.Levels != null && ABManager.Levels.Length > 0)
        {
            if (savedLevelIndex >= ABManager.Levels.Length)
            {
                savedLevelIndex = ABManager.LevelLoopStartLevel;
                PlayerPrefs.SetInt("CurrentLevel", savedLevelIndex);
            }

            resolvedLevelIndex = ABManager.Levels[savedLevelIndex];
        }

        return resolvedLevelIndex + levelIndexOffset;
    }

    private async Task<LevelContainer> LoadLevelContainer(int levelIndex)
    {
        if (currentLevelObject != null)
        {
            Destroy(currentLevelObject);
            currentLevelObject = null;
            currentLevelContainer = null;
        }

        if (levelLoadMode == RuntimeLevelLoadMode.ExistingInScene)
        {
            currentLevelContainer = await WaitForExistingLevelContainer();

            if (currentLevelContainer != null)
            {
                currentLevelObject = currentLevelContainer.gameObject;

                if (destroyDuplicateLevelContainers)
                {
                    DestroyDuplicateLevelContainers(currentLevelContainer);
                }
            }

            return currentLevelContainer;
        }

        GameObject loadedLevelObject = null;

        if (levelLoadMode == RuntimeLevelLoadMode.Addressables)
        {
            loadedLevelObject = await InstantiateAddressableLevel(levelIndex);
        }
        else if (levelLoadMode == RuntimeLevelLoadMode.Resources)
        {
            loadedLevelObject = InstantiateResourcesLevel(levelIndex);
        }

        if (loadedLevelObject == null)
            return null;

        currentLevelObject = loadedLevelObject;

        currentLevelContainer = currentLevelObject.GetComponent<LevelContainer>();

        if (currentLevelContainer == null)
        {
            currentLevelContainer = currentLevelObject.GetComponentInChildren<LevelContainer>(true);
        }

        return currentLevelContainer;
    }

    private async Task<GameObject> InstantiateAddressableLevel(int levelIndex)
    {
        string addressKey = $"{addressableLevelPrefix}{levelIndex}";

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(
            addressKey,
            levelParent
        );

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[GameManager] Addressable level yüklenemedi. Key: {addressKey}");
            return null;
        }

        Debug.Log($"[GameManager] Addressable level yüklendi: {addressKey}");
        return handle.Result;
    }

    private GameObject InstantiateResourcesLevel(int levelIndex)
    {
        string path = $"{resourcesLevelPathPrefix}{levelIndex}";

        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogError($"[GameManager] Resources level prefab bulunamadý. Path: Resources/{path}");
            return null;
        }

        GameObject instance = Instantiate(prefab, levelParent);
        Debug.Log($"[GameManager] Resources level yüklendi: {path}");

        return instance;
    }

    public void StartGame(LevelData levelData, LevelContainer levelContainer)
    {
        currentLevelData = levelData;
        currentLevelContainer = levelContainer;
        isGameOver = false;

        if (currentLevelData == null)
        {
            Debug.LogError("[GameManager] LevelData null.");
            return;
        }

        if (currentLevelContainer == null)
        {
            Debug.LogError("[GameManager] LevelContainer null.");
            return;
        }

        ConnectSceneManagers();

        if (middleSlotManager != null)
        {
            middleSlotManager.Setup();
        }
        else
        {
            Debug.LogError("[GameManager] MiddleSlotManager bulunamadý.");
        }

        if (boxGridManager != null)
        {
            boxGridManager.Setup(currentLevelContainer);
        }
        else
        {
            Debug.LogError("[GameManager] BoxGridManager bulunamadý.");
        }

        if (bottomSlotManager != null)
        {
            bottomSlotManager.Setup(currentLevelData, currentLevelContainer, gamePrefabs);
        }
        else
        {
            Debug.LogError("[GameManager] BottomSlotManager bulunamadý.");
        }

        if (flowerRouletteManager != null)
        {
            flowerRouletteManager.Setup(currentLevelContainer);
        }
        else
        {
            Debug.LogError("[GameManager] FlowerRouletteManager bulunamadý.");
        }

        if (LevelManager.instance != null)
        {
            LevelManager.instance.isLevelFailed = false;
            LevelManager.instance.isGamePlayable = setGamePlayableAfterSetup;
            LevelManager.instance.onLevelLoadComplete?.Invoke();
        }

        Debug.Log("[GameManager] LevelContainer yüklendi ve tüm runtime manager setup tamamlandý.");
    }


    private async Task<LevelContainer> WaitForExistingLevelContainer()
    {
        float timer = 0f;

        while (timer < waitLevelContainerTimeout)
        {
            LevelContainer[] containers = FindObjectsByType<LevelContainer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            if (containers != null && containers.Length > 0)
            {
                Debug.Log($"[GameManager] Existing LevelContainer bulundu. Count:{containers.Length}");

                // En son oluþan genelde runtime clone olur.
                // Ama birden fazla varsa ilkini kullanýp diðerlerini silebiliriz.
                return containers[containers.Length - 1];
            }

            timer += Time.deltaTime;
            await Task.Yield();
        }

        Debug.LogError("[GameManager] ExistingInScene modunda LevelContainer bulunamadý.");
        return null;
    }

    private void DestroyDuplicateLevelContainers(LevelContainer keepContainer)
    {
        LevelContainer[] containers = FindObjectsByType<LevelContainer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (LevelContainer container in containers)
        {
            if (container == null)
                continue;

            if (container == keepContainer)
                continue;

            Debug.LogWarning($"[GameManager] Duplicate LevelContainer silindi: {container.name}");
            Destroy(container.gameObject);
        }
    }

    public void GameLose()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        if (GameplayManager.instance != null)
        {
            GameplayManager.instance.LoseGame(false);
            return;
        }

        Debug.Log("GAME LOSE");
    }

    public void GameWin()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        if (GameplayManager.instance != null)
        {
            GameplayManager.instance.WinGame();
            return;
        }

        Debug.Log("GAME WIN");
    }
}