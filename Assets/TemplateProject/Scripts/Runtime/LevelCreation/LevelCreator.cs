#if UNITY_EDITOR
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Data.SO;
using HashiGame.Scripts.Runtime;
using TemplateProject.Scripts.Runtime.LevelCreation;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Runtime.Models;
using TemplateProject.Scripts.Utilities;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace BoxPuller.Scripts.Runtime.LevelCreation
{
    [System.Serializable]
    public class BottomLaneReferenceData
    {
        public string laneName;
        public List<Transform> nodeReferences = new List<Transform>();
    }

    public class LevelCreator : MonoBehaviour
    {
        [Header("Game References")]
        public GameColors gameColors;
        public GamePrefabs prefabs;
        public HashiVisualSettings visualSettings;
        public LevelContainer currentLevelContainer;
        public WallGenerator wallGenerator;

        [Header("Legacy Prefab Compatibility")]
        [HideInInspector] public GameObject gridBasePrefab;
        [HideInInspector] public GameObject normalObjectPrefab;
        [HideInInspector] public GameObject hiddenObjectPrefab;
        [HideInInspector] public GameObject lockedObjectPrefab;
        [HideInInspector] public GameObject spawnerObjectPrefab;
        [HideInInspector] public GameObject matchAreaPrefab;
        [HideInInspector] public GameObject targetObjectPrefab;

        [Header("Legacy Grid Compatibility")]
        [HideInInspector] public EnumHolder.GridType gridType = EnumHolder.GridType.Normal;
        [HideInInspector] public GridBase[,] gridBases;
        [HideInInspector] public float emptyAreaSpaceModifier;
        [HideInInspector] public int conveyorLength;
        [HideInInspector] public bool expandConveyorToLeft;
        [HideInInspector] public bool expandConveyorToUp;
        [HideInInspector] public EnumHolder.GameColor color;
        [HideInInspector] public EnumHolder.ObjectType objectType;
        [HideInInspector] public EnumHolder.Direction direction;
        [HideInInspector] public bool isSecret;
        [HideInInspector] public bool isFrozen;
        [HideInInspector] public int Count;
        [HideInInspector] public bool isDirection;
        [HideInInspector] public bool isHead;
        [HideInInspector] public int bottomLaneCount = 3;
        [HideInInspector] public int visibleShooterCountPerLane = 4;
        [HideInInspector] public int shooterBulletCount = 5;
        [HideInInspector] public int shooterLinkGroupId = -1;
        [HideInInspector] public bool shooterIsHidden;
        [HideInInspector] public int boxMoldGroupId = -1;
        [HideInInspector]
        public List<BottomLaneReferenceData> bottomLaneReferences =
            new List<BottomLaneReferenceData>();
        [HideInInspector]
        public List<BoxContainerChain> boxContainerChains =
            new List<BoxContainerChain>();
        [HideInInspector]
        public List<ObjectSpawner> objectSpawners =
            new List<ObjectSpawner>();


        [SerializeField] private AddressablePrefabSaver prefabSaver;
        [SerializeField] private AddressablePrefabLoader prefabLoader;
        [SerializeField] private AddressablePrefabLoaderOld prefabLoaderOld;
        [SerializeField] private CinemachineCamera vCam;

        [Header("Level")]
        public int levelIndex;
        public int gridWidth = 13;
        public int gridHeight = 20;
        public bool expandGridToLeft;
        public bool expandGridToUp;

        [Header("Level Time")]
        public int levelTimeMinutes = 2;
        [Range(0, 59)] public int levelTimeSecondsPart;

        [Header("Grid Placement")]
        public float horizontalSpaceModifier = 2f;
        public float verticalSpaceModifier = 2f;
        public Vector3 gridOriginOffset;
        public float islandBaseHeight;
        public Vector3 islandEulerAngles;

        [Header("Island Paint Settings")]
        public int islandRequiredBridgeCount = 1;
        public EnumHolder.IslandBridgeMode islandBridgeMode =
            EnumHolder.IslandBridgeMode.SingleOnly;
        public bool islandStartsLocked;
        public int islandUnlockRequirement;

        [Header("Fixed Bridge Settings")]
        [Range(1, 2)] public int fixedBridgeCount = 1;

        [Header("Chain Settings")]
        public int chainUnlockRequirement;

        [Header("Level Rules")]
        public bool blockBridgeThroughIsland = true;
        public bool blockBridgeCrossing = true;
        public bool requireAllIslandsConnected;
        public float islandBlockingRadius = 0.45f;

        [Header("Tutorial Bridge Settings")]
        [Range(1, 2)] public int tutorialBridgeCount = 1;


        private LevelData levelData;
        private int activeDataLevelIndex = int.MinValue;
        private GameObject parentObject;
        private GameObject islandParentObject;
        private GameObject bridgeParentObject;
        private GameObject chainParentObject;
        private GameObject effectsParentObject;

        private readonly List<IslandNode> generatedIslands = new List<IslandNode>();
        private readonly List<BridgeConnection> generatedFixedBridges =
            new List<BridgeConnection>();
        private readonly List<ChainBarrier> generatedChains = new List<ChainBarrier>();

        private readonly List<BridgeConnection> generatedTutorialBridges =
    new List<BridgeConnection>();

        public LevelData GetLevelData()
        {
            return levelData;
        }
        public int GetEditorLevelTimeTotalSeconds()
        {
            int totalSeconds = levelTimeMinutes * 60 + levelTimeSecondsPart;
            return Mathf.Max(1, totalSeconds);
        }

        private void SetEditorLevelTimeFromTotalSeconds(int totalSeconds)
        {
            totalSeconds = Mathf.Max(1, totalSeconds);

            levelTimeMinutes = totalSeconds / 60;
            levelTimeSecondsPart = totalSeconds % 60;
        }

        public void EnsureLevelData()
        {
            if (levelData != null && activeDataLevelIndex == levelIndex)
            {
                levelData.EnsureHashiData();
                return;
            }

            levelData = null;

            if (LevelSaveSystem.IsLevelExists(levelIndex))
            {
                levelData = LevelSaveSystem.LoadLevel(levelIndex);
            }

            if (levelData == null)
            {
                levelData = new LevelData(
                    Mathf.Max(1, gridWidth),
                    Mathf.Max(1, gridHeight),
                    0,
                    0);

                levelData.levelTimeSeconds = LevelData.DefaultLevelTimeSeconds;
            }

            activeDataLevelIndex = levelIndex;
            ApplyLoadedLevelDataToEditor();
        }

        public void ResizeGrid()
        {
            EnsureLevelData();

            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);

            if (levelData.Width == gridWidth && levelData.Height == gridHeight)
            {
                return;
            }

            levelData.ResizeGridCells(
                gridWidth,
                gridHeight,
                expandGridToUp,
                expandGridToLeft);

            EditorUtility.SetDirty(this);
        }

        public void SetIslandAt(Vector2Int coordinate)
        {
            EnsureLevelData();

            levelData.SetIslandCell(
                coordinate.x,
                coordinate.y,
                Mathf.Max(1, islandRequiredBridgeCount),
                islandBridgeMode,
                islandStartsLocked,
                Mathf.Max(0, islandUnlockRequirement));

            EditorUtility.SetDirty(this);
        }

        public void RemoveIslandAt(Vector2Int coordinate)
        {
            EnsureLevelData();
            levelData.RemoveIslandCell(coordinate.x, coordinate.y);
            EditorUtility.SetDirty(this);
        }

        public bool TryAddFixedBridge(
            Vector2Int firstCoordinate,
            Vector2Int secondCoordinate,
            out string error)
        {
            EnsureLevelData();
            error = string.Empty;

            if (!levelData.TryGetIslandCell(firstCoordinate, out IslandCellData firstIsland) ||
                !levelData.TryGetIslandCell(secondCoordinate, out IslandCellData secondIsland))
            {
                error = "Fixed bridge endpoints must both contain islands.";
                return false;
            }

            int count = Mathf.Clamp(fixedBridgeCount, 1, 2);

            if (count == 2 &&
                (firstIsland.bridgeMode != EnumHolder.IslandBridgeMode.DoubleAllowed ||
                 secondIsland.bridgeMode != EnumHolder.IslandBridgeMode.DoubleAllowed))
            {
                error = "A double fixed bridge requires two Double Allowed islands.";
                return false;
            }

            if (!levelData.AddFixedBridgeDefinition(
                    firstCoordinate,
                    secondCoordinate,
                    count))
            {
                error = "This fixed bridge definition is invalid or already exists.";
                return false;
            }

            EditorUtility.SetDirty(this);
            return true;
        }

        public bool TryAddTutorialBridge(
    Vector2Int firstCoordinate,
    Vector2Int secondCoordinate,
    out string error)
        {
            EnsureLevelData();
            error = string.Empty;

            if (!levelData.TryGetIslandCell(firstCoordinate, out IslandCellData firstIsland) ||
                !levelData.TryGetIslandCell(secondCoordinate, out IslandCellData secondIsland))
            {
                error = "Tutorial bridge endpoints must both contain islands.";
                return false;
            }

            int count = Mathf.Clamp(tutorialBridgeCount, 1, 2);

            if (count == 2 &&
                (firstIsland.bridgeMode != EnumHolder.IslandBridgeMode.DoubleAllowed ||
                 secondIsland.bridgeMode != EnumHolder.IslandBridgeMode.DoubleAllowed))
            {
                error = "A double tutorial bridge requires two Double Allowed islands.";
                return false;
            }

            if (!levelData.AddTutorialBridgeDefinition(
                    firstCoordinate,
                    secondCoordinate,
                    count))
            {
                error = "This tutorial bridge definition is invalid or already exists.";
                return false;
            }

            EditorUtility.SetDirty(this);
            return true;
        }

        public bool TryAddChain(
            Vector2Int firstCoordinate,
            Vector2Int secondCoordinate,
            out string error)
        {
            EnsureLevelData();
            error = string.Empty;

            if (!levelData.AddChainBarrier(
                    firstCoordinate,
                    secondCoordinate,
                    Mathf.Max(0, chainUnlockRequirement)))
            {
                error = "This chain definition is invalid or already exists.";
                return false;
            }

            EditorUtility.SetDirty(this);
            return true;
        }

        public void RemoveFixedBridge(int id)
        {
            EnsureLevelData();
            levelData.RemoveFixedBridgeDefinition(id);
            EditorUtility.SetDirty(this);
        }

        public void RemoveTutorialBridge(int id)
        {
            EnsureLevelData();
            levelData.RemoveTutorialBridgeDefinition(id);
            EditorUtility.SetDirty(this);
        }

        public void RemoveChain(int id)
        {
            EnsureLevelData();
            levelData.RemoveChainBarrier(id);
            EditorUtility.SetDirty(this);
        }

        public Vector3 GridSpaceToWorldSpace(float x, float y)
        {
            float centeredX = x - (gridWidth - 1) * 0.5f;
            float centeredY = y - (gridHeight - 1) * 0.5f;

            return gridOriginOffset + new Vector3(
                centeredX * horizontalSpaceModifier,
                islandBaseHeight,
                centeredY * verticalSpaceModifier);
        }

        public Vector3 GridCoordinateToWorld(Vector2Int coordinate)
        {
            return GridSpaceToWorldSpace(coordinate.x, coordinate.y);
        }

        public HashiValidationResult GetValidationResult()
        {
            EnsureLevelData();
            SynchronizeRulesToLevelData();
            return HashiLevelValidator.Validate(levelData, GridCoordinateToWorld);
        }

        public void GenerateLevel()
        {
            EnsureLevelData();
            ResizeGrid();
            SynchronizeRulesToLevelData();

            if (!ValidateRequiredPrefabs(out string prefabError))
            {
                EditorUtility.DisplayDialog("Hashi Level Error", prefabError, "OK");
                return;
            }

            HashiValidationResult validation = GetValidationResult();
            if (validation.HasErrors)
            {
                EditorUtility.DisplayDialog(
                    "Hashi Level Error",
                    BuildValidationMessage(validation),
                    "OK");
                return;
            }

            SetParents();
            Dictionary<Vector2Int, IslandNode> islandMap = CreateIslands();
            CreateFixedBridges(islandMap);
            CreateTutorialBridges(islandMap);
            CreateChains();

            currentLevelContainer.InitHashiRuntimeReferences(
      gridWidth,
      gridHeight,
      horizontalSpaceModifier,
      verticalSpaceModifier,
      gridOriginOffset,
      islandBaseHeight,
      islandParentObject,
      bridgeParentObject,
      chainParentObject,
      effectsParentObject,
      generatedIslands,
      generatedFixedBridges,
      generatedTutorialBridges,
      generatedChains);

            SaveLevel();
            EditorUtility.SetDirty(currentLevelContainer);
            Selection.activeGameObject = parentObject;
        }

        public void SaveLevel()
        {
            EnsureLevelData();
            SynchronizeRulesToLevelData();

            Camera mainCamera = Camera.main;
            if (mainCamera != null && currentLevelContainer != null)
            {
                currentLevelContainer.SetCameraSettings(
                    mainCamera.transform.position,
                    mainCamera.transform.rotation.eulerAngles,
                    mainCamera.orthographicSize);
            }

            LevelSaveSystem.SaveLevel(levelData, levelIndex);

            GameObject levelRoot = parentObject != null
                ? parentObject
                : currentLevelContainer != null
                    ? currentLevelContainer.gameObject
                    : null;

            if (levelRoot != null && prefabSaver != null)
            {
                prefabSaver.SaveAndAssignPrefab(levelRoot, levelIndex);
                EditorUtility.SetDirty(prefabSaver);
            }

            if (currentLevelContainer != null)
            {
                EditorUtility.SetDirty(currentLevelContainer);
            }

            AssetDatabase.SaveAssets();
        }

        public void LoadLevel()
        {
            if (!LevelSaveSystem.IsLevelExists(levelIndex))
            {
                Debug.LogWarning("[LevelCreator] Level data does not exist: " + levelIndex);
                return;
            }

            levelData = LevelSaveSystem.LoadLevel(levelIndex);
            activeDataLevelIndex = levelIndex;
            if (levelData == null)
            {
                return;
            }

            levelData.EnsureHashiData();
            ApplyLoadedLevelDataToEditor();
            DestroyExistingLevelObjects();

            string prefabAddress = "Level_" + levelIndex;

            if (prefabLoaderOld != null)
            {
                prefabLoaderOld.ManualPrefabLoader(
                    prefabAddress,
                    loadedObject =>
                    {
                        parentObject = loadedObject;
                        currentLevelContainer = loadedObject != null
                            ? loadedObject.GetComponent<LevelContainer>()
                            : null;
                        AssignCameraSettings();
                    });
            }
            else
            {
                LoadPrefabDirectlyFromAssetDatabase(prefabAddress);
            }

            EditorUtility.SetDirty(this);
        }

        public void ResetLevel()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            SetEditorLevelTimeFromTotalSeconds(LevelData.DefaultLevelTimeSeconds);

            levelData = new LevelData(gridWidth, gridHeight, 0, 0);
            activeDataLevelIndex = levelIndex;
            SynchronizeRulesToLevelData();
            DestroyExistingLevelObjects();

            if (prefabSaver != null)
            {
                prefabSaver.RemovePrefabFromAddressablesAndDelete(levelIndex);
            }

            EditorUtility.SetDirty(this);
        }

        private void SetParents()
        {
            DestroyExistingLevelObjects();

            generatedIslands.Clear();
            generatedFixedBridges.Clear();
            generatedTutorialBridges.Clear();
            generatedChains.Clear();

            parentObject = new GameObject("Level_" + levelIndex);
            parentObject.tag = "LevelParent";
            currentLevelContainer = parentObject.AddComponent<LevelContainer>();

            islandParentObject = CreateChildParent("Island Parent");
            bridgeParentObject = CreateChildParent("Bridge Parent");
            chainParentObject = CreateChildParent("Chain Parent");
            effectsParentObject = CreateChildParent("Effects Parent");
        }

        private GameObject CreateChildParent(string objectName)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parentObject.transform, false);
            return child;
        }

        private Dictionary<Vector2Int, IslandNode> CreateIslands()
        {
            Dictionary<Vector2Int, IslandNode> result =
                new Dictionary<Vector2Int, IslandNode>();

            List<IslandCellData> islandCells = levelData.GetIslandCells();

            for (int i = 0; i < islandCells.Count; i++)
            {
                IslandCellData islandData = islandCells[i];

                GameObject selectedIslandPrefab =
                    prefabs.GetIslandPrefab(islandData.bridgeMode);

                GameObject islandObject = PrefabUtility.InstantiatePrefab(
                    selectedIslandPrefab) as GameObject;

                if (islandObject == null)
                {
                    continue;
                }

                islandObject.transform.SetParent(islandParentObject.transform, true);
                islandObject.transform.position = GridCoordinateToWorld(islandData.Coordinate);
                islandObject.transform.eulerAngles = islandEulerAngles;

                string islandTypeName =
                    islandData.bridgeMode == EnumHolder.IslandBridgeMode.DoubleAllowed
                        ? "TwoBridge"
                        : "OneBridge";

                islandObject.name =
                    "Island_" + islandData.x + "_" + islandData.y +
                    "_" + islandTypeName +
                    "_Target_" + islandData.requiredBridgeCount;

                IslandNode islandNode = islandObject.GetComponent<IslandNode>();
                if (islandNode == null)
                {
                    islandNode = islandObject.AddComponent<IslandNode>();
                }

                islandNode.ConfigureLevelData(islandData, visualSettings);
                generatedIslands.Add(islandNode);
                result.Add(islandData.Coordinate, islandNode);
            }

            return result;
        }

        private void CreateFixedBridges(
            Dictionary<Vector2Int, IslandNode> islandMap)
        {
            for (int i = 0; i < levelData.fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData bridgeData = levelData.fixedBridges[i];

                if (!islandMap.TryGetValue(
                        bridgeData.startCoordinate,
                        out IslandNode firstIsland) ||
                    !islandMap.TryGetValue(
                        bridgeData.endCoordinate,
                        out IslandNode secondIsland))
                {
                    continue;
                }

                GameObject bridgeObject = PrefabUtility.InstantiatePrefab(
                    prefabs.bridgePrefab) as GameObject;

                if (bridgeObject == null)
                {
                    continue;
                }

                bridgeObject.transform.SetParent(bridgeParentObject.transform, true);
                bridgeObject.name = "FixedBridge_" + bridgeData.id;

                BridgeVisual bridgeVisual = bridgeObject.GetComponent<BridgeVisual>();
                if (bridgeVisual == null)
                {
                    bridgeVisual = bridgeObject.AddComponent<BridgeVisual>();
                }

                BridgeConnection connection = bridgeObject.GetComponent<BridgeConnection>();
                if (connection == null)
                {
                    connection = bridgeObject.AddComponent<BridgeConnection>();
                }

                connection.ConfigureLevelData(
                    firstIsland,
                    secondIsland,
                    bridgeData.bridgeCount,
                    true,
                    visualSettings);

                generatedFixedBridges.Add(connection);
            }
        }

        private void CreateTutorialBridges(
    Dictionary<Vector2Int, IslandNode> islandMap)
        {
            for (int i = 0; i < levelData.tutorialBridges.Count; i++)
            {
                TutorialBridgeDefinitionData bridgeData = levelData.tutorialBridges[i];

                if (!islandMap.TryGetValue(
                        bridgeData.startCoordinate,
                        out IslandNode firstIsland) ||
                    !islandMap.TryGetValue(
                        bridgeData.endCoordinate,
                        out IslandNode secondIsland))
                {
                    continue;
                }

                GameObject bridgeObject = PrefabUtility.InstantiatePrefab(
                    prefabs.bridgePrefab) as GameObject;

                if (bridgeObject == null)
                {
                    continue;
                }

                bridgeObject.transform.SetParent(bridgeParentObject.transform, true);
                bridgeObject.name = "TutorialBridge_" + bridgeData.id;

                BridgeVisual bridgeVisual = bridgeObject.GetComponent<BridgeVisual>();
                if (bridgeVisual == null)
                {
                    bridgeVisual = bridgeObject.AddComponent<BridgeVisual>();
                }

                BridgeConnection connection = bridgeObject.GetComponent<BridgeConnection>();
                if (connection == null)
                {
                    connection = bridgeObject.AddComponent<BridgeConnection>();
                }

                connection.ConfigureLevelData(
                    firstIsland,
                    secondIsland,
                    bridgeData.bridgeCount,
                    false,
                    visualSettings);

                generatedTutorialBridges.Add(connection);
            }
        }
        private void CreateChains()
        {
            for (int i = 0; i < levelData.chainBarriers.Count; i++)
            {
                ChainBarrierData chainData = levelData.chainBarriers[i];
                GameObject chainObject = PrefabUtility.InstantiatePrefab(
                    prefabs.chainBarrierPrefab) as GameObject;

                if (chainObject == null)
                {
                    continue;
                }

                chainObject.transform.SetParent(chainParentObject.transform, true);
                chainObject.name = "Chain_" + chainData.id;

                ChainBarrier chain = chainObject.GetComponent<ChainBarrier>();
                if (chain == null)
                {
                    chain = chainObject.AddComponent<ChainBarrier>();
                }

                chain.ConfigureLevelData(
                    chainData,
                    GridCoordinateToWorld(chainData.startCoordinate),
                    GridCoordinateToWorld(chainData.endCoordinate),
                    visualSettings);

                generatedChains.Add(chain);
            }
        }

        private void SynchronizeRulesToLevelData()
        {
            if (levelData == null)
            {
                return;
            }

            levelData.EnsureHashiData();
            levelData.hashiRules.blockBridgeThroughIsland = blockBridgeThroughIsland;
            levelData.hashiRules.blockBridgeCrossing = blockBridgeCrossing;
            levelData.hashiRules.requireAllIslandsConnected = requireAllIslandsConnected;
            levelData.hashiRules.islandBlockingRadius =
                Mathf.Max(0.01f, islandBlockingRadius);

            levelData.levelTimeSeconds = GetEditorLevelTimeTotalSeconds();

        }

        private void ApplyLoadedLevelDataToEditor()
        {
            if (levelData == null)
            {
                return;
            }

            levelData.EnsureHashiData();
            gridWidth = levelData.Width;
            gridHeight = levelData.Height;
            SetEditorLevelTimeFromTotalSeconds(levelData.levelTimeSeconds);

            blockBridgeThroughIsland = levelData.hashiRules.blockBridgeThroughIsland;
            blockBridgeCrossing = levelData.hashiRules.blockBridgeCrossing;
            requireAllIslandsConnected = levelData.hashiRules.requireAllIslandsConnected;
            islandBlockingRadius = levelData.hashiRules.islandBlockingRadius;
        }
        private bool ValidateRequiredPrefabs(out string error)
        {
            if (prefabs == null)
            {
                error = "GamePrefabs is not assigned.";
                return false;
            }

            if (prefabs.oneBridgeIslandPrefab == null)
            {
                error = "GamePrefabs.oneBridgeIslandPrefab is not assigned.";
                return false;
            }

            if (prefabs.twoBridgeIslandPrefab == null)
            {
                error = "GamePrefabs.twoBridgeIslandPrefab is not assigned.";
                return false;
            }

            if (prefabs.bridgePrefab == null)
            {
                error = "GamePrefabs.bridgePrefab is not assigned.";
                return false;
            }

            if (levelData.chainBarriers.Count > 0 &&
                prefabs.chainBarrierPrefab == null)
            {
                error = "GamePrefabs.chainBarrierPrefab is not assigned.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static string BuildValidationMessage(HashiValidationResult validation)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < validation.issues.Count; i++)
            {
                HashiValidationIssue issue = validation.issues[i];
                builder.Append(issue.severity);
                builder.Append(": ");
                builder.AppendLine(issue.message);
            }

            return builder.ToString();
        }

        private void AssignCameraSettings()
        {
            if (currentLevelContainer == null || vCam == null)
            {
                return;
            }

            vCam.transform.position = currentLevelContainer.GetCameraPos();
            vCam.transform.eulerAngles = currentLevelContainer.GetCameraEuler();
            vCam.Lens.OrthographicSize = currentLevelContainer.GetCameraOrthoSize();
        }

        private void LoadPrefabDirectlyFromAssetDatabase(string prefabAddress)
        {
            if (prefabSaver == null)
            {
                Debug.LogError(
                    "[LevelCreator] prefabSaver is required for direct prefab loading.");
                return;
            }

            string prefabPath = prefabSaver.prefabBasePath + prefabAddress + ".prefab";
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null)
            {
                Debug.LogError("[LevelCreator] Level prefab not found: " + prefabPath);
                return;
            }

            parentObject = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            currentLevelContainer = parentObject != null
                ? parentObject.GetComponent<LevelContainer>()
                : null;
            AssignCameraSettings();
        }

        private void DestroyExistingLevelObjects()
        {
            LevelContainer[] containers = FindObjectsByType<LevelContainer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null)
                {
                    DestroyImmediate(containers[i].gameObject);
                }
            }

            parentObject = null;
            currentLevelContainer = null;
            islandParentObject = null;
            bridgeParentObject = null;
            chainParentObject = null;
            effectsParentObject = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            horizontalSpaceModifier = Mathf.Max(0.01f, horizontalSpaceModifier);
            verticalSpaceModifier = Mathf.Max(0.01f, verticalSpaceModifier);
            islandRequiredBridgeCount = Mathf.Max(1, islandRequiredBridgeCount);
            islandUnlockRequirement = Mathf.Max(0, islandUnlockRequirement);
            chainUnlockRequirement = Mathf.Max(0, chainUnlockRequirement);
            fixedBridgeCount = Mathf.Clamp(fixedBridgeCount, 1, 2);
            islandBlockingRadius = Mathf.Max(0.01f, islandBlockingRadius);

            levelTimeMinutes = Mathf.Max(0, levelTimeMinutes);
            levelTimeSecondsPart = Mathf.Clamp(levelTimeSecondsPart, 0, 59);

            if (levelTimeMinutes == 0 && levelTimeSecondsPart == 0)
            {
                levelTimeMinutes = 0;
                levelTimeSecondsPart = 1;
            }
        }
#endif
    }
}
#endif
