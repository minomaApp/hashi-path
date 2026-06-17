#if UNITY_EDITOR
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Data.SO;
using TemplateProject.Scripts.Runtime.LevelCreation;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Runtime.Models;
using TemplateProject.Scripts.Utilities;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

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
        #region Variables

        // GAME REFERENCES
        [Header("Game References")]
        public GameColors gameColors;
        public GamePrefabs prefabs;
        public WallGenerator wallGenerator;
        public LevelContainer currentLevelContainer;

        [SerializeField] private AddressablePrefabSaver prefabSaver;
        [SerializeField] private AddressablePrefabLoader prefabLoader;
        [SerializeField] private AddressablePrefabLoaderOld prefabLoaderOld;
        [SerializeField] private CinemachineCamera vCam;

        private GameObject _loadedLevel;

        // PREFABS0
        [Space(2)]
        [Header("Prefabs")]
        [HideInInspector] public GameObject gridBasePrefab;

        [HideInInspector] public GameObject normalObjectPrefab;
        [HideInInspector] public GameObject hiddenObjectPrefab;
        [HideInInspector] public GameObject lockedObjectPrefab;
        [HideInInspector] public GameObject spawnerObjectPrefab;
        [HideInInspector] public GameObject matchAreaPrefab;

        [FormerlySerializedAs("targetQueuePrefab")]
        [HideInInspector] public GameObject targetObjectPrefab;

        [Header("New Level References")]
        public List<Transform> boxBottomRowReferences = new List<Transform>();

        [Tooltip("Box grid satýr yönü. Kutular Y ekseninde üst üste çýkacaksa (0,3,0), Z ekseninde dizilecekse (0,0,3) veya (0,0,-3).")]
        public Vector3 boxRowOffset = new Vector3(0f, 3f, 0f);

        [Header("Bottom Lane References")]
        public List<BottomLaneReferenceData> bottomLaneReferences = new List<BottomLaneReferenceData>();

        [Header("Dynamic Bottom Slot Layout")]
        [HideInInspector] public bool useDynamicBottomSlotLayout = true;

        [Tooltip("Lane 0 / Node 0 pozisyonu. Dinamik bottom slot üretimi buradan baþlar.")]
        [HideInInspector] public Transform bottomSlotStartReference;

        [Tooltip("Lane index arttýkça eklenecek world offset. Örn: satýrlar aþaðý doðru gidiyorsa (0,0,-1.5).")]
        [HideInInspector] public Vector3 bottomLaneOffset = new Vector3(0f, 0f, -3f);

        [Tooltip("Node index arttýkça eklenecek world offset. Örn: ayný satýr içinde saða gidiyorsa (1.5,0,0).")]
        [HideInInspector] public Vector3 bottomNodeOffset = new Vector3(3f, 0f, 0f);

        [Tooltip("Node/Shooter rotasyonuna ekstra açý vermek gerekirse kullan.")]
        [HideInInspector] public Vector3 bottomSlotEulerOffset = Vector3.zero;

        public Transform flowerParentReference;
        public List<Transform> flowerPetalReferences = new List<Transform>();

        [Header("Bottom Shooter Editor Settings")]
        [HideInInspector] public int bottomLaneCount = 3;
        [HideInInspector] public int visibleShooterCountPerLane = 4;
        [HideInInspector] public int shooterBulletCount = 5;
        [HideInInspector] public int shooterLinkGroupId = -1;
        [HideInInspector] public bool shooterIsHidden;

        [Header("Box Editor Settings")]
        [HideInInspector] public int boxMoldGroupId = -1;

        // GRID SETTINGS
        [Header("Grid Settings")]
        [HideInInspector] public EnumHolder.GridType gridType;

        [HideInInspector] public GridBase[,] gridBases;
        [HideInInspector] public int levelIndex;
        [HideInInspector] public int gridWidth;
        [HideInInspector] public int gridHeight;
        [HideInInspector] public int conveyorLength;
        [HideInInspector] public float horizontalSpaceModifier = 1;
        [HideInInspector] public float verticalSpaceModifier = 1;
        [HideInInspector] public float emptyAreaSpaceModifier;

        // ENUMS
        [HideInInspector] public EnumHolder.GameColor color;
        [HideInInspector] public EnumHolder.ObjectType objectType;
        [HideInInspector] public EnumHolder.Direction direction;

        // LEVEL DATA
        private LevelData levelData;

        // LEVEL DATA SETTINGS
        [HideInInspector] public bool expandGridToLeft;
        [HideInInspector] public bool expandGridToUp;

        [HideInInspector] public bool expandConveyorToLeft;
        [HideInInspector] public bool expandConveyorToUp;

        // MANAGERS
        private GridManager _gridManager;
        private MatchTargetManager _targetManager;

        // PARENTS
        private GameObject parentObject;
        private GameObject gridParentObject;
        private GameObject conveyorParentObject;
        private GameObject boxContainerChainParentObject;

        private GameObject boxGridParentObject;
        private GameObject bottomSlotParentObject;
        private GameObject flowerParentObject;

        public LevelData GetLevelData() => levelData;

        [HideInInspector] public bool isSecret;
        [HideInInspector] public bool isFrozen;
        [HideInInspector] public int Count;
        [HideInInspector] public bool isDirection;
        [HideInInspector] public bool isHead;

        [HideInInspector] public List<BoxContainerChain> boxContainerChains;
        [HideInInspector] public List<ObjectSpawner> objectSpawners;

        private readonly List<GameObject> generatedBoxes = new List<GameObject>();
        private readonly List<GameObject> generatedShooters = new List<GameObject>();
        private readonly List<GameObject> generatedBottomNodes = new List<GameObject>();
        private readonly List<GameObject> generatedFlowerPetals = new List<GameObject>();

        #endregion


        #region Save/Load/Reset/Generate Level

        public void SaveLevel()
        {
            var cam = Camera.main;
            if (cam && currentLevelContainer != null)
            {
                var camTransform = cam.transform;
                currentLevelContainer.SetCameraSettings(
                    camTransform.position,
                    camTransform.rotation.eulerAngles,
                    cam.orthographicSize
                );
            }

            // Yeni oyunda conveyor datasý kullanýlmayacak.
            // levelData.ResizeConveyorCells(
            //     gridWidth,
            //     conveyorLength,
            //     expandConveyorToUp,
            //     false
            // );

            if (parentObject != null)
            {
                prefabSaver.SaveAndAssignPrefab(parentObject, levelIndex);
                EditorUtility.SetDirty(prefabSaver);
            }

            if (currentLevelContainer != null)
            {
                EditorUtility.SetDirty(currentLevelContainer);
            }


            if (levelData != null)
            {
                levelData.bottomLaneCount = bottomLaneCount;
                levelData.visibleShooterCountPerLane = visibleShooterCountPerLane;
                levelData.EnsureBottomLaneCount(bottomLaneCount);
                levelData.RefreshBottomShooterIndexes();

                Debug.Log(
                    $"[LevelCreator SAVE CHECK] Me me me LaneCount:{levelData.bottomLaneCount} " +
                    $"Visible:{levelData.visibleShooterCountPerLane}"
                );
                Debug.Log($"[LevelCreator SAVE CHECK] Me me me Saving LevelData/Level{levelIndex}");
                Debug.Log($"[LevelCreator SAVE CHECK] Me me me LaneCount:{levelData.bottomLaneCount} Visible:{levelData.visibleShooterCountPerLane}");

                for (int i = 0; i < levelData.bottomShooterLanes.Count; i++)
                {
                    Debug.Log(
                        $"[LevelCreator SAVE CHECK] Me me me Lane:{i} " +
                        $"Shooters:{levelData.bottomShooterLanes[i].shooters.Count}"
                    );
                }
            }
            else
            {
                Debug.Log($"[LevelCreator SAVE CHECK] Me me me Saving LevelData/Level levelData == null ");

            }

            LevelSaveSystem.SaveLevel(levelData, levelIndex);
        }

        public void LoadLevel()
        {
            levelData = LevelSaveSystem.LoadLevel(levelIndex);
            if (levelData == null) return;

            gridWidth = levelData.Width;
            gridHeight = levelData.Height;
            conveyorLength = 0;

            // Yeni oyunda conveyor datasý kullanýlmayacak.
            // levelData.ResizeConveyorCells(
            //     gridWidth,
            //     conveyorLength,
            //     expandConveyorToUp,
            //     false
            // );

            levelData.EnsureBottomLaneCount(levelData.bottomLaneCount);
            bottomLaneCount = levelData.bottomLaneCount;
            visibleShooterCountPerLane = levelData.visibleShooterCountPerLane;

            var prefabName = $"Level_{levelIndex}";

            _loadedLevel = GameObject.FindGameObjectWithTag("LevelParent");
            if (_loadedLevel)
            {
                DestroyImmediate(_loadedLevel);
            }

            var prevList = GameObject.FindGameObjectsWithTag("LevelParent");
            foreach (var oldLevel in prevList)
            {
                if (oldLevel)
                {
                    DestroyImmediate(oldLevel);
                }
            }

            if (parentObject)
            {
                DestroyImmediate(parentObject);
            }

            if (currentLevelContainer)
            {
                DestroyImmediate(currentLevelContainer.gameObject);
            }

            _loadedLevel = prefabLoaderOld.ManualPrefabLoader(
                prefabName,
                level =>
                {
                    currentLevelContainer = level.GetComponent<LevelContainer>();
                    AssignCameraSettings();
                    _loadedLevel = level;
                }
            );
        }

        private void AssignCameraSettings()
        {
            if (currentLevelContainer == null || vCam == null) return;

            vCam.transform.position = currentLevelContainer.GetCameraPos();
            vCam.transform.eulerAngles = currentLevelContainer.GetCameraEuler();
            vCam.Lens.OrthographicSize = currentLevelContainer.GetCameraOrthoSize();
        }

        public void ResetLevel()
        {
            int targetQueueCount = levelData?.TargetQueue?.Count ?? 0;

            if (gridWidth <= 0) gridWidth = 5;
            if (gridHeight <= 0) gridHeight = 10;

            conveyorLength = 0;

            levelData = new LevelData(gridWidth, gridHeight, targetQueueCount, conveyorLength);
            levelData.bottomLaneCount = bottomLaneCount;
            levelData.visibleShooterCountPerLane = visibleShooterCountPerLane;
            levelData.EnsureBottomLaneCount(bottomLaneCount);

            prefabSaver.RemovePrefabFromAddressablesAndDelete(levelIndex);
        }
        public void GenerateLevel()
        {
            if (levelData == null)
            {
                ResetLevel();
            }

            levelData.bottomLaneCount = bottomLaneCount;
            levelData.visibleShooterCountPerLane = visibleShooterCountPerLane;
            levelData.EnsureBottomLaneCount(bottomLaneCount);
            levelData.RefreshBottomShooterIndexes();

            SetParents();

            CreateGrid();
            CreateBoxes();
            CreateBottomShooters();
            CreateFlowerPetals();

            // Eski oyun akýþý iptal edildi.
            // CreateConveyors();
            // CreateChains();

            // Wall generator yeni akýþtan çýkarýldý.
            // wallGenerator.Init(levelData, this, levelIndex);
            // wallGenerator.GenerateWalls();

            currentLevelContainer.Init(gridWidth, gridHeight, gridBases, boxContainerChains, objectSpawners);

            currentLevelContainer.InitNewRuntimeReferences(
                boxGridParentObject,
                bottomSlotParentObject,
                flowerParentObject,
                generatedBoxes,
                generatedShooters,
                generatedBottomNodes,
                generatedFlowerPetals
            );

            // Eski template level parent'ý 180 derece döndürüyordu.
            // Yeni sistemde transform referanslarýný world position olarak kullandýðýmýz için
            // bu satýr child objeleri referans noktalarýndan uzaklaþtýrýr.
            // parentObject.transform.localEulerAngles = new Vector3(
            //     parentObject.transform.localEulerAngles.x,
            //     180f,
            //     parentObject.transform.localEulerAngles.z
            // );

            SaveLevel();

            EditorUtility.SetDirty(currentLevelContainer);
        }
        #endregion


        #region Initialization

        private void SetParents()
        {
            generatedBoxes.Clear();
            generatedShooters.Clear();
            generatedBottomNodes.Clear();
            generatedFlowerPetals.Clear();

            boxContainerChains ??= new List<BoxContainerChain>();
            objectSpawners ??= new List<ObjectSpawner>();

            boxContainerChains.Clear();
            objectSpawners.Clear();

            parentObject = GameObject.Find($"Level_{levelIndex}");
            if (parentObject) DestroyImmediate(parentObject);

            parentObject = new GameObject($"Level_{levelIndex}");
            parentObject.tag = "LevelParent";

            currentLevelContainer = parentObject.AddComponent<LevelContainer>();

            gridParentObject = new GameObject("Grid Parent");
            gridParentObject.transform.SetParent(parentObject.transform, false);

            boxGridParentObject = new GameObject("Box Grid Parent");
            boxGridParentObject.transform.SetParent(parentObject.transform, false);

            bottomSlotParentObject = new GameObject("Bottom Slot Parent");
            bottomSlotParentObject.transform.SetParent(parentObject.transform, false);

            flowerParentObject = new GameObject("Flower Parent");
            flowerParentObject.transform.SetParent(parentObject.transform, false);

            // Eski parentlar þimdilik dursun ama yeni akýþta kullanýlmayacak.
            conveyorParentObject = new GameObject("Conveyor Parent - Disabled");
            conveyorParentObject.transform.SetParent(parentObject.transform, false);
            conveyorParentObject.SetActive(false);

            boxContainerChainParentObject = new GameObject("BoxContainerChains Parent - Disabled");
            boxContainerChainParentObject.transform.SetParent(parentObject.transform, false);
            boxContainerChainParentObject.SetActive(false);
        }

        #endregion


        #region Create Grid

        private void CreateGrid()
        {
            Dictionary<Vector2Int, Vector3> spawnPositions = new();

            switch (gridType)
            {
                case EnumHolder.GridType.Normal:
                    spawnPositions = CalculateSpawnPositions();
                    break;

                case EnumHolder.GridType.DynamicSpaced:
                    spawnPositions = CalculateCumulativeSpawnPositions();
                    break;

                case EnumHolder.GridType.Hexagon:
                    break;
            }

            gridBases = new GridBase[gridWidth, gridHeight];

            for (var y = 0; y < levelData.Height; y++)
            {
                for (var x = 0; x < levelData.Width; x++)
                {
                    CreateGridBaseObjects(
                        x,
                        y,
                        spawnPositions[new Vector2Int(x, y)],
                        levelData.GridData[x, y]
                    );
                }
            }
        }

        private void CreateGridBaseObjects(int x, int y, Vector3 spawnPosition, GridCellData data)
        {
            var gridData = levelData.GridData[x, y];

            var gridObject = PrefabUtility.InstantiatePrefab(gridBasePrefab) as GameObject;
            if (!gridObject) return;

            gridObject.transform.SetParent(gridParentObject.transform, true);
            gridObject.transform.position = spawnPosition;
            gridObject.transform.name = gridObject.transform.name + " (" + x + "x" + y + ") ";

            var gridScript = gridObject.GetComponent<GridBase>();
            gridScript.Init(x, y);
            gridScript.SetActive(gridData.isActive);
            gridBases[x, y] = gridScript;
        }

        #endregion


        #region Create New Box Grid

        private void CreateBoxes()
        {
            if (prefabs == null || prefabs.boxPrefab == null)
            {
                Debug.LogWarning("[LevelCreator] Box prefab is missing in GamePrefabs.");
                return;
            }

            if (boxBottomRowReferences == null || boxBottomRowReferences.Count == 0)
            {
                Debug.LogWarning("[LevelCreator] Box bottom row references are missing.");
                return;
            }

            var boxCells = levelData.GetBoxCells();

            foreach (var boxCell in boxCells)
            {
                if (boxCell.x < 0 || boxCell.x >= boxBottomRowReferences.Count)
                {
                    Debug.LogWarning(
                        $"[LevelCreator] Box x index out of reference range: {boxCell.x}. " +
                        $"Grid Width: {gridWidth}, Box Bottom Row References Count: {boxBottomRowReferences.Count}"
                    );
                    continue;
                }

                Transform bottomReference = boxBottomRowReferences[boxCell.x];

                if (bottomReference == null)
                {
                    Debug.LogWarning($"[LevelCreator] Box bottom reference is null at x: {boxCell.x}");
                    continue;
                }

                GameObject boxObject = PrefabUtility.InstantiatePrefab(prefabs.boxPrefab) as GameObject;
                if (!boxObject) continue;

                boxObject.transform.SetParent(boxGridParentObject.transform, true);

                Vector3 spawnPosition = bottomReference.position + boxRowOffset * boxCell.y;

                boxObject.transform.position = spawnPosition;
                boxObject.transform.rotation = bottomReference.rotation;
                boxObject.name = $"Box ({boxCell.x},{boxCell.y}) {boxCell.color} Mold:{boxCell.moldGroupId}";

                GeneratedLevelItem generatedItem = boxObject.GetComponent<GeneratedLevelItem>();
                if (!generatedItem)
                {
                    generatedItem = boxObject.AddComponent<GeneratedLevelItem>();
                }

                generatedItem.SetupBox(
                    boxCell.x,
                    boxCell.y,
                    boxCell.color,
                    boxCell.moldGroupId
                );

                Box box = boxObject.GetComponent<Box>();
                if (box != null)
                {
                    var runtimeBoxData = new global::BoxSpawnData
                    {
                        color = boxCell.color,
                        x = boxCell.x,
                        y = boxCell.y,
                        moldGroupId = boxCell.moldGroupId
                    };

                    box.Setup(runtimeBoxData);
                }

                boxObject.SendMessage(
                    "InitFromLevelData",
                    boxCell,
                    SendMessageOptions.DontRequireReceiver
                );

                generatedBoxes.Add(boxObject);
            }
        }

        #endregion

        #region Create Bottom Shooters

        private void CreateBottomShooters()
        {
            if (levelData.bottomShooterLanes == null)
                return;

            if (prefabs == null)
            {
                Debug.LogWarning("[LevelCreator] GamePrefabs reference is missing.");
                return;
            }

            if (useDynamicBottomSlotLayout)
            {
                CreateBottomShootersDynamic();
            }
            else
            {
                CreateBottomShootersFromReferences();
            }
        }

        private void CreateBottomShootersDynamic()
        {
            if (!TryGetBottomSlotBasePose(out Vector3 basePosition, out Quaternion baseRotation))
            {
                Debug.LogWarning("[LevelCreator] Dynamic bottom slot layout için Bottom Slot Start Reference atanmalý.");
                return;
            }

            int laneCount = Mathf.Max(0, bottomLaneCount);
            int nodeCountPerLane = Mathf.Max(1, visibleShooterCountPerLane);

            for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
            {
                if (laneIndex >= levelData.bottomShooterLanes.Count)
                {
                    Debug.LogWarning($"[LevelCreator] LevelData içinde bottom lane yok: {laneIndex}");
                    continue;
                }

                BottomShooterLaneData laneData = levelData.bottomShooterLanes[laneIndex];

                if (laneData?.shooters == null)
                    continue;

                GameObject laneParent = new GameObject($"Bottom Lane {laneIndex}");
                laneParent.transform.SetParent(bottomSlotParentObject.transform, false);

                for (int nodeIndex = 0; nodeIndex < nodeCountPerLane; nodeIndex++)
                {
                    GetDynamicBottomSlotPose(
                        basePosition,
                        baseRotation,
                        laneIndex,
                        nodeIndex,
                        out Vector3 nodePosition,
                        out Quaternion nodeRotation
                    );

                    CreateBottomNode(
                        laneParent.transform,
                        laneIndex,
                        nodeIndex,
                        nodePosition,
                        nodeRotation
                    );
                }

                int visibleCount = Mathf.Min(
                    nodeCountPerLane,
                    laneData.shooters.Count
                );

                for (int orderIndex = 0; orderIndex < visibleCount; orderIndex++)
                {
                    ShooterSpawnData shooterData = laneData.shooters[orderIndex];

                    GetDynamicBottomSlotPose(
                        basePosition,
                        baseRotation,
                        laneIndex,
                        orderIndex,
                        out Vector3 shooterPosition,
                        out Quaternion shooterRotation
                    );

                    CreateBottomShooter(
                      laneParent.transform,
                      shooterData,
                      laneIndex,
                      orderIndex,
                      shooterPosition,
                      shooterRotation
                  );
                }
            }
        }

        private void CreateBottomShootersFromReferences()
        {
            for (int laneIndex = 0; laneIndex < levelData.bottomShooterLanes.Count; laneIndex++)
            {
                BottomShooterLaneData laneData = levelData.bottomShooterLanes[laneIndex];

                if (laneData?.shooters == null)
                    continue;

                if (laneIndex < 0 || laneIndex >= bottomLaneReferences.Count)
                {
                    Debug.LogWarning(
                        $"[LevelCreator] Bottom lane reference missing for lane {laneIndex}. " +
                        $"Bottom Lane References Count: {bottomLaneReferences.Count}"
                    );
                    continue;
                }

                BottomLaneReferenceData laneReferenceData = bottomLaneReferences[laneIndex];

                if (laneReferenceData == null ||
                    laneReferenceData.nodeReferences == null ||
                    laneReferenceData.nodeReferences.Count == 0)
                {
                    Debug.LogWarning($"[LevelCreator] Lane {laneIndex} has no node references.");
                    continue;
                }

                GameObject laneParent = new GameObject($"Bottom Lane {laneIndex}");
                laneParent.transform.SetParent(bottomSlotParentObject.transform, false);

                int nodeCount = Mathf.Min(
                    visibleShooterCountPerLane,
                    laneReferenceData.nodeReferences.Count
                );

                int visibleCount = Mathf.Min(
                    visibleShooterCountPerLane,
                    laneReferenceData.nodeReferences.Count,
                    laneData.shooters.Count
                );

                for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
                {
                    Transform nodeReference = laneReferenceData.nodeReferences[nodeIndex];

                    if (nodeReference == null)
                    {
                        Debug.LogWarning($"[LevelCreator] Lane {laneIndex} node reference {nodeIndex} is null.");
                        continue;
                    }

                    CreateBottomNode(
                        laneParent.transform,
                        laneIndex,
                        nodeIndex,
                        nodeReference.position,
                        nodeReference.rotation
                    );
                }

                for (int orderIndex = 0; orderIndex < visibleCount; orderIndex++)
                {
                    ShooterSpawnData shooterData = laneData.shooters[orderIndex];
                    Transform nodeReference = laneReferenceData.nodeReferences[orderIndex];

                    if (nodeReference == null)
                    {
                        Debug.LogWarning($"[LevelCreator] Lane {laneIndex} shooter reference {orderIndex} is null.");
                        continue;
                    }

                    CreateBottomShooter(
                         laneParent.transform,
                         shooterData,
                         laneIndex,
                         orderIndex,
                         nodeReference.position,
                         nodeReference.rotation
                     );
                }
            }
        }

        private bool TryGetBottomSlotBasePose(out Vector3 basePosition, out Quaternion baseRotation)
        {
            if (bottomSlotStartReference != null)
            {
                basePosition = bottomSlotStartReference.position;
                baseRotation = bottomSlotStartReference.rotation * Quaternion.Euler(bottomSlotEulerOffset);
                return true;
            }

            if (bottomLaneReferences != null &&
                bottomLaneReferences.Count > 0 &&
                bottomLaneReferences[0] != null &&
                bottomLaneReferences[0].nodeReferences != null &&
                bottomLaneReferences[0].nodeReferences.Count > 0 &&
                bottomLaneReferences[0].nodeReferences[0] != null)
            {
                Transform fallback = bottomLaneReferences[0].nodeReferences[0];

                basePosition = fallback.position;
                baseRotation = fallback.rotation * Quaternion.Euler(bottomSlotEulerOffset);

                Debug.LogWarning("[LevelCreator] Bottom Slot Start Reference boþ. Fallback olarak BottomLaneReferences[0][0] kullanýldý.");
                return true;
            }

            basePosition = Vector3.zero;
            baseRotation = Quaternion.identity;
            return false;
        }

        private void GetDynamicBottomSlotPose(
        Vector3 basePosition,
        Quaternion baseRotation,
        int laneIndex,
        int nodeIndex,
        out Vector3 position,
        out Quaternion rotation)
        {
            float centeredLaneIndex = laneIndex - ((bottomLaneCount - 1) * 0.5f);
            float centeredNodeIndex = nodeIndex - ((visibleShooterCountPerLane - 1) * 0.5f);

            position =
                basePosition +
                bottomLaneOffset * centeredLaneIndex +
                bottomNodeOffset * centeredNodeIndex;

            rotation = baseRotation;
        }

        private void CreateBottomNode(
            Transform laneParent,
            int laneIndex,
            int nodeIndex,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject nodeObject;

            if (prefabs.bottomSlotNodePrefab != null)
            {
                nodeObject = PrefabUtility.InstantiatePrefab(prefabs.bottomSlotNodePrefab) as GameObject;
            }
            else
            {
                nodeObject = new GameObject("Bottom Slot Node");
            }

            if (!nodeObject)
                return;

            nodeObject.transform.SetParent(laneParent, true);
            nodeObject.transform.position = position;
            nodeObject.transform.rotation = rotation;
            nodeObject.name = $"Bottom Node Lane:{laneIndex} Node:{nodeIndex}";

            GeneratedLevelItem generatedItem = nodeObject.GetComponent<GeneratedLevelItem>();
            if (!generatedItem)
            {
                generatedItem = nodeObject.AddComponent<GeneratedLevelItem>();
            }

            generatedItem.SetupBottomSlotNode(laneIndex, nodeIndex);

            BottomSlotNode bottomSlotNode = nodeObject.GetComponent<BottomSlotNode>();
            if (bottomSlotNode == null)
            {
                bottomSlotNode = nodeObject.AddComponent<BottomSlotNode>();
            }

            bottomSlotNode.Setup(laneIndex, nodeIndex);

            generatedBottomNodes.Add(nodeObject);
        }

        private void CreateBottomShooter(
       Transform laneParent,
       ShooterSpawnData shooterData,
       int laneIndex,
       int orderIndex,
       Vector3 position,
       Quaternion rotation)
        {
            if (prefabs.shooterPrefab == null)
            {
                Debug.LogWarning("[LevelCreator] Shooter prefab is missing in GamePrefabs.");
                return;
            }

            GameObject shooterObject = PrefabUtility.InstantiatePrefab(prefabs.shooterPrefab) as GameObject;
            if (!shooterObject)
                return;

            shooterObject.transform.SetParent(laneParent, true);
            shooterObject.transform.position = position;
            shooterObject.transform.rotation = rotation;

            // ÖNEMLÝ:
            // Data içindeki eski lane/order deðerlerine güvenmiyoruz.
            // Generate anýndaki gerçek pozisyon bilgisini zorla yazýyoruz.
            shooterData.laneIndex = laneIndex;
            shooterData.orderIndex = orderIndex;

            shooterObject.name =
             $"Shooter Lane:{laneIndex} Order:{orderIndex} " +
             $"{shooterData.color} Bullet:{shooterData.bulletCount} " +
             $"Link:{shooterData.linkGroupId} Hidden:{shooterData.isHidden}";

            GeneratedLevelItem generatedItem = shooterObject.GetComponent<GeneratedLevelItem>();
            if (!generatedItem)
            {
                generatedItem = shooterObject.AddComponent<GeneratedLevelItem>();
            }

            generatedItem.SetupShooter(
                  laneIndex,
                  orderIndex,
                  shooterData.color,
                  shooterData.bulletCount,
                  shooterData.linkGroupId,
                  shooterData.isHidden
              );

            Shooter shooter = shooterObject.GetComponent<Shooter>();
            if (shooter == null)
            {
                shooter = shooterObject.AddComponent<Shooter>();
            }

            shooter.Setup(shooterData);

            Collider shooterCollider = shooterObject.GetComponent<Collider>();
            if (shooterCollider == null)
            {
                Debug.LogWarning($"[LevelCreator] Shooter prefabýnda collider yok: {shooterObject.name}. OnMouseDown çalýþmayabilir.");
            }

            shooterObject.SendMessage(
                "InitFromLevelData",
                shooterData,
                SendMessageOptions.DontRequireReceiver
            );

            generatedShooters.Add(shooterObject);
        }

        #endregion
        #region Create Flower Petals

        private void CreateFlowerPetals()
        {
            Transform flowerRoot = flowerParentObject.transform;

            if (flowerParentReference != null)
            {
                flowerParentObject.transform.position = flowerParentReference.position;
                flowerParentObject.transform.rotation = flowerParentReference.rotation;
                flowerParentObject.transform.localScale = flowerParentReference.lossyScale;
                flowerRoot = flowerParentObject.transform;
            }

            if (flowerPetalReferences != null && flowerPetalReferences.Count > 0)
            {
                for (int i = 0; i < flowerPetalReferences.Count; i++)
                {
                    Transform reference = flowerPetalReferences[i];
                    if (reference == null) continue;

                    CreateFlowerPetal(i, reference.position, reference.rotation, flowerRoot);
                }

                return;
            }

            const int defaultPetalCount = 5;
            float radius = 1f;

            for (int i = 0; i < defaultPetalCount; i++)
            {
                float angle = i * Mathf.PI * 2f / defaultPetalCount;
                Vector3 localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector3 worldPosition = flowerRoot.position + localPosition;

                CreateFlowerPetal(i, worldPosition, Quaternion.identity, flowerRoot);
            }
        }

        private void CreateFlowerPetal(
         int petalIndex,
         Vector3 position,
         Quaternion rotation,
         Transform flowerRoot)
        {
            GameObject petalObject;

            if (prefabs != null && prefabs.flowerPetalPrefab != null)
            {
                petalObject = PrefabUtility.InstantiatePrefab(prefabs.flowerPetalPrefab) as GameObject;
            }
            else
            {
                petalObject = new GameObject("Flower Petal");
            }

            if (!petalObject)
                return;

            petalObject.transform.SetParent(flowerRoot, false);
            petalObject.transform.position = position;
            petalObject.transform.rotation = rotation;
            petalObject.name = $"Flower Petal {petalIndex}";

            GeneratedLevelItem generatedItem = petalObject.GetComponent<GeneratedLevelItem>();
            if (!generatedItem)
            {
                generatedItem = petalObject.AddComponent<GeneratedLevelItem>();
            }

            generatedItem.SetupFlowerPetal(petalIndex);

            FlowerPetal flowerPetal = petalObject.GetComponent<FlowerPetal>();
            if (flowerPetal == null)
            {
                flowerPetal = petalObject.AddComponent<FlowerPetal>();
            }

            flowerPetal.Setup(petalIndex);

            generatedFlowerPetals.Add(petalObject);
        }

        #endregion


        #region Create Chains - Legacy Disabled

        private void CreateChains()
        {
            var chains = levelData.GetChains();

            foreach (var chainData in chains)
            {
                var chainParentObject = PrefabUtility.InstantiatePrefab(prefabs.chainPrefab) as GameObject;
                if (!chainParentObject) return;

                var chainParent = chainParentObject.GetComponent<BoxContainerChain>();
                chainParent.color = chainData[0].Color;
                chainParentObject.transform.SetParent(boxContainerChainParentObject.transform);

                for (var i = 0; i < chainData.Count; i++)
                {
                    var chainDataItem = chainData[i];
                    var chainCellObject = PrefabUtility.InstantiatePrefab(prefabs.chainNodePrefab) as GameObject;
                    if (!chainCellObject) return;

                    chainCellObject.transform.SetParent(chainParentObject.transform);
                    chainCellObject.transform.position = GridSpaceToWorldSpace(chainDataItem.X, chainDataItem.Y);

                    var chainNodeScript = chainCellObject.GetComponent<BoxContainerChainNode>();
                    chainNodeScript.Init(chainParent);
                    chainNodeScript.SetGrid(gridBases[chainDataItem.X, chainDataItem.Y]);
                    chainNodeScript.transform.name += " " + i;
                    chainParent.AddCell(chainNodeScript);
                }

                var material = gameColors.chainMaterials[(int)chainData[0].Color];
                chainParent.Init(material, gridBases);
                boxContainerChains.Add(chainParent);
                CreateChainNodes(chainParent);

                if (chainData[0].isFrozen)
                {
                    chainParent.IsFrozen = chainData[0].isFrozen;
                    chainParent.IceCount = chainData[0].BlockCount;

                    foreach (var cell in chainParent.chainNodes)
                    {
                        var iceParticle = PrefabUtility.InstantiatePrefab(prefabs.iceParticle) as ParticleSystem;
                        var iceFinalParticle =
                            PrefabUtility.InstantiatePrefab(prefabs.iceFinalParticle) as ParticleSystem;
                        var iceText = PrefabUtility.InstantiatePrefab(prefabs.iceTextPrefab) as TextMeshPro;

                        iceParticle.transform.SetParent(cell.transform);
                        iceFinalParticle.transform.SetParent(cell.transform);
                        iceText.transform.SetParent(cell.transform);

                        iceText.transform.localEulerAngles = new Vector3(90f, 180f, 180f);
                        iceParticle.transform.localPosition = new Vector3(0, 0.86f, 0);
                        iceFinalParticle.transform.localPosition = new Vector3(0, 0.86f, 0);
                        iceText.transform.localPosition = new Vector3(0, 0.86f, 0f);

                        cell.iceBrokeParticle = iceFinalParticle;
                        cell.iceDecreaseParticle = iceParticle;
                        cell.iceText = iceText;
                        cell.iceText.text = chainData[0].BlockCount.ToString();
                    }

                    chainParent.AllNodes.ForEach(node =>
                    {
                        var materials = new Material[node.visual.sharedMaterials.Length];

                        for (var index = 0; index < materials.Length; index++)
                        {
                            materials[index] = gameColors.iceMaterial;
                        }

                        node.visual.sharedMaterials = materials;

                        foreach (var subRenderer in node.subRenderers)
                        {
                            subRenderer.sharedMaterial = gameColors.iceMaterial;
                        }

                        if (node.insideBoxRenderer)
                        {
                            node.insideBoxRenderer.sharedMaterial = gameColors.iceMaterial;
                        }
                    });
                }
            }
        }

        private void CreateChainNodes(BoxContainerChain chain)
        {
            chain.ResetNodes();

            var material = gameColors.chainMaterials[(int)chain.color];
            var insideMat = gameColors.chainInsideMaterials[(int)chain.color];

            var nodes = new List<Node>();
            var midNodes = new List<Node>();

            for (var i = 0; i < chain.TotalNodeCount; i++)
            {
                var nodeObject = PrefabUtility.InstantiatePrefab(prefabs.nodePrefab) as GameObject;
                if (!nodeObject) return;

                nodeObject.transform.SetParent(chain.transform);

                var nodeScript = nodeObject.GetComponent<Node>();

                chain.chainNodes[i].BoxContainer = nodeScript.boxContainer;
                chain.chainNodes[i].BoxContainer.ContainerColorType = chain.color;

                var placeParticle =
                    PrefabUtility.InstantiatePrefab(gameColors.boxParticles[(int)chain.color]) as GameObject;

                placeParticle.transform.SetParent(nodeScript.transform);
                placeParticle.transform.localPosition = Vector3.zero + new Vector3(0f, 0.3f, 0f);
                placeParticle.SetActive(false);

                chain.chainNodes[i].BoxContainer.placeParticle = placeParticle;

                var sharedMaterials = nodeScript.visual.sharedMaterials;
                sharedMaterials[0] = material;
                nodeScript.insideBoxRenderer.sharedMaterial = insideMat;
                nodeScript.visual.sharedMaterials = sharedMaterials;

                chain.AddNode(nodeScript);
                nodes.Add(nodeScript);

                if (i != 0)
                {
                    GameObject midNodeObject = PrefabUtility.InstantiatePrefab(prefabs.midNodePrefab) as GameObject;
                    if (!midNodeObject) return;

                    midNodeObject.transform.SetParent(nodeObject.transform);
                    midNodeObject.transform.localPosition = Vector3.zero;

                    nodeScript = midNodeObject.GetComponent<Node>();
                    chain.AddMidNode(nodeScript);
                    midNodes.Add(nodeScript);
                }
            }

            var positions = chain.CalculateChainNodePositions(gridBases);

            for (int i = 0; i < chain.TotalNodeCount; i++)
            {
                nodes[i].transform.position = positions[i];
                nodes[i].ResetForward();
                nodes[i].transform.localRotation = Quaternion.Euler(0, 0, 0);

                if (i != 0)
                {
                    midNodes[i - 1].transform.localPosition = Vector3.zero;

                    var directionToNode = (nodes[i - 1].transform.position - nodes[i].transform.position).normalized;
                    var lookEuler = Quaternion.LookRotation(directionToNode).eulerAngles;

                    lookEuler.x = 0;
                    lookEuler.z = 90;

                    midNodes[i - 1].transform.eulerAngles = lookEuler;
                }
            }
        }

        private void CreateConveyors()
        {
            objectSpawners.Clear();

            for (var i = 0; i < gridWidth; i++)
            {
                var spawner = PrefabUtility.InstantiatePrefab(prefabs.objectSpawnerPrefab) as GameObject;
                if (!spawner) return;

                spawner.transform.SetParent(conveyorParentObject.transform);
                spawner.transform.localPosition =
                    gridBases[i, gridHeight - 1].transform.position + new Vector3(0f, 0.45f, 4.29f);
                spawner.transform.localRotation = Quaternion.Euler(0, 180f, 0);

                var spawnerScript = spawner.GetComponent<ObjectSpawner>();
                var matchingObjects = new List<MatchingObject>();

                var conveyorData = levelData.ConveyorData;
                for (var j = 0; j < conveyorData.GetLength(1); j++)
                {
                    var cell = conveyorData[i, j].BasePlaceable as ConveyorItemData;
                    if (cell == null) continue;
                    if (cell.Color == EnumHolder.GameColor.None) continue;

                    var matchingObject = PrefabUtility.InstantiatePrefab(prefabs.matchingObjectPrefab) as GameObject;
                    if (!matchingObject) continue;

                    matchingObject.transform.SetParent(spawner.transform);
                    matchingObject.transform.localPosition = spawnerScript.objectSpawnTransform.localPosition;

                    var matchingObjectScript = matchingObject.GetComponent<MatchingObject>();
                    matchingObjectScript.Color = cell.Color;
                    matchingObjectScript.Init(spawnerScript, cell.isSecret);

                    matchingObjects.Add(matchingObjectScript);
                }

                spawnerScript.Init(i, matchingObjects);

                if (!objectSpawners.Contains(spawnerScript))
                {
                    objectSpawners.Add(spawnerScript);
                }
            }

            var platform = PrefabUtility.InstantiatePrefab(prefabs.objectSpawnerPlatform) as GameObject;
            platform.transform.SetParent(conveyorParentObject.transform);
            platform.transform.localPosition =
                new Vector3(-0.05f, 0.55f, gridBases[0, gridHeight - 1].transform.position.z + 2.95f);
            platform.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            platform.transform.localScale = new Vector3(gridWidth * 1 + 0.5f, 4.5f, 1f);
        }

        #endregion


        #region Utility

        private Dictionary<Vector2Int, Vector3> CalculateSpawnPositions()
        {
            var spawnPositions = new Dictionary<Vector2Int, Vector3>();

            for (var y = gridHeight - 1; y >= 0; y--)
            {
                for (var x = 0; x < gridWidth; x++)
                {
                    var spawnPosition = GridSpaceToWorldSpace(x, y);
                    spawnPositions.Add(new Vector2Int(x, y), spawnPosition);
                }
            }

            return spawnPositions;
        }

        private Dictionary<Vector2Int, Vector3> CalculateCumulativeSpawnPositions()
        {
            var spawnPositions = new Dictionary<Vector2Int, Vector3>();

            var cumulativeHorizontalEmptyAreaSpaceAmount =
                new float[levelData.Height > levelData.Width ? levelData.Height : levelData.Width];

            for (var y = levelData.Height - 1; y >= 0; y--)
            {
                var cumulativeVerticalEmptyAreaSpaceAmount = 0f;

                for (var x = 0; x < levelData.Width; x++)
                {
                    var spawnPosition = GridSpaceToWorldSpace(x, y);

                    cumulativeVerticalEmptyAreaSpaceAmount +=
                        levelData.VerticalEmptyAreaData[x, y] * emptyAreaSpaceModifier;

                    cumulativeHorizontalEmptyAreaSpaceAmount[x] +=
                        levelData.HorizontalEmptyAreaData[x, y] * emptyAreaSpaceModifier;

                    spawnPosition.x += cumulativeVerticalEmptyAreaSpaceAmount;
                    spawnPosition.z -= cumulativeHorizontalEmptyAreaSpaceAmount[x];

                    spawnPositions.Add(new Vector2Int(x, y), spawnPosition);
                }
            }

            return spawnPositions;
        }

        public Vector3 GridSpaceToWorldSpace(float x, float y)
        {
            var offsetX = gridWidth / 2f * horizontalSpaceModifier;
            var offsetY = gridHeight / 2f * verticalSpaceModifier;

            return new Vector3(
                (x * horizontalSpaceModifier) - offsetX + horizontalSpaceModifier / 2f,
                0,
                (y * verticalSpaceModifier) - offsetY + verticalSpaceModifier / 2f
            );
        }

        private Vector2Int GetSpawnerVector2Int(Vector2Int coordinate, EnumHolder.Direction direction)
        {
            var vector = DirectionToVector(direction);
            return new Vector2Int(coordinate.x + vector.x, coordinate.y + vector.y);
        }

        private Vector2Int DirectionToVector(EnumHolder.Direction direction)
        {
            switch (direction)
            {
                case EnumHolder.Direction.Up:
                    return new Vector2Int(0, 1);

                case EnumHolder.Direction.Down:
                    return new Vector2Int(0, -1);

                case EnumHolder.Direction.Left:
                    return new Vector2Int(-1, 0);

                case EnumHolder.Direction.Right:
                    return new Vector2Int(1, 0);

                default:
                    return Vector2Int.zero;
            }
        }

        private Vector3 DirectionToEuler(EnumHolder.Direction direction)
        {
            switch (direction)
            {
                case EnumHolder.Direction.Up:
                    return new Vector3(0, 0, 0);

                case EnumHolder.Direction.Down:
                    return new Vector3(0, 180, 0);

                case EnumHolder.Direction.Left:
                    return new Vector3(0, -90, 0);

                case EnumHolder.Direction.Right:
                    return new Vector3(0, 90, 0);

                default:
                    return Vector3.zero;
            }
        }

        #endregion
    }
}

#endif