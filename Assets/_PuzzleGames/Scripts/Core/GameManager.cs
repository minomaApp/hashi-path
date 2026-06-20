using System.Threading.Tasks;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.SO;
using HashiGame.Scripts.Runtime;
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
    [Header("Game Mode")]
    [SerializeField] private bool useHashiGameplay = true;

    [Header("Hashi Runtime Managers")]
    [SerializeField] private BridgeBoardManager bridgeBoardManager;
    [SerializeField] private BridgeInputController bridgeInputController;
    [SerializeField] private BridgePreviewController bridgePreviewController;
    [SerializeField] private HashiVisualSettings hashiVisualSettings;

    [Header("Legacy Shooter Box Managers")]
    [SerializeField] private BottomSlotManager bottomSlotManager;
    [SerializeField] private MiddleSlotManager middleSlotManager;
    [SerializeField] private FlowerRouletteManager flowerRouletteManager;
    [SerializeField] private BoxGridManager boxGridManager;
    [SerializeField] private ShooterTransferQueue shooterTransferQueue;
    [SerializeField] private BulletPool bulletPool;

    [Header("Prefabs")]
    [SerializeField] private GamePrefabs gamePrefabs;

    [Header("Level Loading")]
    [Tooltip("Enable this when AddressablePrefabLoader creates the level prefab.")]
    [SerializeField] private bool externalLoaderOwnsLevel = true;
    [SerializeField] private bool loadLevelOnStart;
    [SerializeField]
    private RuntimeLevelLoadMode levelLoadMode =
        RuntimeLevelLoadMode.ExistingInScene;
    [SerializeField] private Transform levelParent;

    [Header("Level Index")]
    [SerializeField] private bool usePlayerPrefsLevelIndex = true;
    [SerializeField] private int levelIndexOffset;
    [SerializeField] private int fallbackLevelIndex;

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
        ConnectLegacySceneManagers();

        if (externalLoaderOwnsLevel || !loadLevelOnStart)
        {
            return;
        }

        await LoadAndStartCurrentLevel();
    }

    public async Task LoadAndStartCurrentLevel()
    {
        int levelIndex = GetRuntimeLevelIndex();
        LevelData loadedLevelData = RuntimeLevelDataLoader.LoadLevel(levelIndex);

        if (loadedLevelData == null)
        {
            Debug.LogError(
                "[GameManager] LevelData could not be loaded. Index: " + levelIndex);
            return;
        }

        LevelContainer loadedLevelContainer = await LoadLevelContainer(levelIndex);

        if (loadedLevelContainer == null)
        {
            Debug.LogError(
                "[GameManager] LevelContainer could not be loaded. Index: " + levelIndex);
            return;
        }

        StartGame(loadedLevelData, loadedLevelContainer);
    }

    public void StartGame(LevelData levelData, LevelContainer levelContainer)
    {
        currentLevelData = levelData;
        currentLevelContainer = levelContainer;
        isGameOver = false;

        if (currentLevelData == null || currentLevelContainer == null)
        {
            Debug.LogError("[GameManager] LevelData or LevelContainer is null.");
            return;
        }

        AutoAssignSceneManagers();

        bool setupSucceeded = useHashiGameplay
            ? StartHashiGame()
            : StartLegacyShooterBoxGame();

        if (!setupSucceeded)
        {
            return;
        }

        if (LevelManager.instance != null)
        {
            LevelManager.instance.isLevelFailed = false;
            LevelManager.instance.isGamePlayable = setGamePlayableAfterSetup;

            if (!externalLoaderOwnsLevel)
            {
                LevelManager.instance.onLevelLoadComplete?.Invoke();
            }
        }

        Debug.Log("[GameManager] Runtime setup completed.");
    }

    public void GameLose()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;

        if (bridgeInputController != null)
        {
            bridgeInputController.SetInputEnabled(false);
        }

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
        {
            return;
        }

        isGameOver = true;

        if (bridgeInputController != null)
        {
            bridgeInputController.SetInputEnabled(false);
        }

        if (GameplayManager.instance != null)
        {
            GameplayManager.instance.WinGame();
            return;
        }

        Debug.Log("GAME WIN");
    }

    private bool StartHashiGame()
    {
        if (bridgeBoardManager == null)
        {
            bridgeBoardManager = FindFirstObjectByType<BridgeBoardManager>();
        }

        if (bridgeInputController == null)
        {
            bridgeInputController = FindFirstObjectByType<BridgeInputController>();
        }

        if (bridgePreviewController == null)
        {
            bridgePreviewController = FindFirstObjectByType<BridgePreviewController>();
        }

        if (bridgeBoardManager == null)
        {
            Debug.LogError("[GameManager] BridgeBoardManager was not found.");
            return false;
        }

        bool boardReady = bridgeBoardManager.Setup(
            currentLevelData,
            currentLevelContainer,
            gamePrefabs,
            hashiVisualSettings,
            this);

        if (!boardReady)
        {
            return false;
        }

        if (bridgeInputController == null)
        {
            Debug.LogError("[GameManager] BridgeInputController was not found.");
            return false;
        }

        bridgeInputController.Setup(bridgeBoardManager);
        bridgeInputController.SetInputEnabled(!bridgeBoardManager.HasWon);
        return true;
    }

    private bool StartLegacyShooterBoxGame()
    {
        ConnectLegacySceneManagers();

        if (middleSlotManager != null)
        {
            middleSlotManager.Setup();
        }

        if (boxGridManager != null)
        {
            boxGridManager.Setup(currentLevelContainer);
        }

        if (bottomSlotManager != null)
        {
            bottomSlotManager.Setup(
                currentLevelData,
                currentLevelContainer,
                gamePrefabs);
        }

        if (flowerRouletteManager != null)
        {
            flowerRouletteManager.Setup(currentLevelContainer);
        }

        return true;
    }

    private void AutoAssignSceneManagers()
    {
        if (bridgeBoardManager == null)
        {
            bridgeBoardManager = FindFirstObjectByType<BridgeBoardManager>();
        }

        if (bridgeInputController == null)
        {
            bridgeInputController = FindFirstObjectByType<BridgeInputController>();
        }

        if (bridgePreviewController == null)
        {
            bridgePreviewController = FindFirstObjectByType<BridgePreviewController>();
        }

        if (bottomSlotManager == null)
        {
            bottomSlotManager = FindFirstObjectByType<BottomSlotManager>();
        }

        if (middleSlotManager == null)
        {
            middleSlotManager = FindFirstObjectByType<MiddleSlotManager>();
        }

        if (flowerRouletteManager == null)
        {
            flowerRouletteManager = FindFirstObjectByType<FlowerRouletteManager>();
        }

        if (boxGridManager == null)
        {
            boxGridManager = FindFirstObjectByType<BoxGridManager>();
        }

        if (shooterTransferQueue == null)
        {
            shooterTransferQueue = FindFirstObjectByType<ShooterTransferQueue>();
        }

        if (bulletPool == null)
        {
            bulletPool = FindFirstObjectByType<BulletPool>();
        }
    }

    private void ConnectLegacySceneManagers()
    {
        if (flowerRouletteManager != null)
        {
            flowerRouletteManager.ConfigureRuntimeReferences(
                middleSlotManager,
                bottomSlotManager,
                this);
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

    private int GetRuntimeLevelIndex()
    {
        if (!usePlayerPrefsLevelIndex)
        {
            return fallbackLevelIndex;
        }

        int savedLevelIndex = PlayerPrefs.GetInt(
            "CurrentLevel",
            fallbackLevelIndex);
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
        {
            return null;
        }

        currentLevelObject = loadedLevelObject;
        currentLevelContainer = currentLevelObject.GetComponent<LevelContainer>();

        if (currentLevelContainer == null)
        {
            currentLevelContainer =
                currentLevelObject.GetComponentInChildren<LevelContainer>(true);
        }

        return currentLevelContainer;
    }

    private async Task<GameObject> InstantiateAddressableLevel(int levelIndex)
    {
        string addressKey = addressableLevelPrefix + levelIndex;

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(
            addressKey,
            levelParent);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError(
                "[GameManager] Addressable level could not be loaded. Key: " +
                addressKey);
            return null;
        }

        return handle.Result;
    }

    private GameObject InstantiateResourcesLevel(int levelIndex)
    {
        string path = resourcesLevelPathPrefix + levelIndex;
        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogError(
                "[GameManager] Resources level prefab was not found: " + path);
            return null;
        }

        return Instantiate(prefab, levelParent);
    }

    private async Task<LevelContainer> WaitForExistingLevelContainer()
    {
        float timer = 0f;

        while (timer < waitLevelContainerTimeout)
        {
            LevelContainer[] containers = FindObjectsByType<LevelContainer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            if (containers != null && containers.Length > 0)
            {
                return containers[containers.Length - 1];
            }

            timer += Time.deltaTime;
            await Task.Yield();
        }

        Debug.LogError(
            "[GameManager] No LevelContainer was found in ExistingInScene mode.");
        return null;
    }

    private void DestroyDuplicateLevelContainers(LevelContainer keepContainer)
    {
        LevelContainer[] containers = FindObjectsByType<LevelContainer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < containers.Length; i++)
        {
            LevelContainer container = containers[i];

            if (container == null || container == keepContainer)
            {
                continue;
            }

            Destroy(container.gameObject);
        }
    }
}
