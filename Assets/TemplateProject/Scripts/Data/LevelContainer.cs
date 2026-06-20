using System.Collections.Generic;
using HashiGame.Scripts.Runtime;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Runtime.Models;
using Unity.Cinemachine;
using UnityEngine;

namespace BoxPuller.Scripts.Data
{
    public class LevelContainer : MonoBehaviour
    {
        [Header("Legacy Cached References")]
        [SerializeField] private GridSaveClass[] levelGridBases;
        [SerializeField] private List<ObjectSpawner> objectSpawners;
        private List<BoxContainerChain> chains;

        [Header("Parameters")]
        [SerializeField] private int gridWidth;
        [SerializeField] private int gridHeight;
        [SerializeField] private Vector3 cameraPosition;
        [SerializeField] private Vector3 cameraEuler;
        [SerializeField] private float orthoVal;

        [Header("Hashi Grid Placement")]
        [SerializeField] private float hashiHorizontalSpacing = 2f;
        [SerializeField] private float hashiVerticalSpacing = 2f;
        [SerializeField] private Vector3 hashiGridOrigin;
        [SerializeField] private float hashiBaseHeight;

        [Header("Hashi Runtime References")]
        [SerializeField] private GameObject islandParent;
        [SerializeField] private GameObject bridgeParent;
        [SerializeField] private GameObject chainParent;
        [SerializeField] private GameObject effectsParent;
        [SerializeField] private List<IslandNode> generatedIslands = new List<IslandNode>();
        [SerializeField] private List<BridgeConnection> generatedFixedBridges = new List<BridgeConnection>();
        [SerializeField] private List<ChainBarrier> generatedChains = new List<ChainBarrier>();

        [Header("Legacy Runtime References")]
        [SerializeField] private GameObject boxGridParent;
        [SerializeField] private GameObject bottomSlotParent;
        [SerializeField] private GameObject flowerParent;
        [SerializeField] private List<GameObject> generatedBoxes = new List<GameObject>();
        [SerializeField] private List<GameObject> generatedShooters = new List<GameObject>();
        [SerializeField] private List<GameObject> generatedBottomNodes = new List<GameObject>();
        [SerializeField] private List<GameObject> generatedFlowerPetals = new List<GameObject>();

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        public GameObject IslandParent => islandParent;
        public GameObject BridgeParent => bridgeParent;
        public GameObject ChainParent => chainParent;
        public GameObject EffectsParent => effectsParent;

        public Transform IslandParentTransform => islandParent != null ? islandParent.transform : transform;
        public Transform BridgeParentTransform => bridgeParent != null ? bridgeParent.transform : transform;
        public Transform ChainParentTransform => chainParent != null ? chainParent.transform : transform;
        public Transform EffectsParentTransform => effectsParent != null ? effectsParent.transform : transform;

        public IReadOnlyList<IslandNode> GeneratedIslands => generatedIslands;
        public IReadOnlyList<BridgeConnection> GeneratedFixedBridges => generatedFixedBridges;
        public IReadOnlyList<ChainBarrier> GeneratedChains => generatedChains;

        public GameObject BoxGridParent => boxGridParent;
        public GameObject BottomSlotParent => bottomSlotParent;
        public GameObject FlowerParent => flowerParent;
        public IReadOnlyList<GameObject> GeneratedBoxes => generatedBoxes;
        public IReadOnlyList<GameObject> GeneratedShooters => generatedShooters;
        public IReadOnlyList<GameObject> GeneratedBottomNodes => generatedBottomNodes;
        public IReadOnlyList<GameObject> GeneratedFlowerPetals => generatedFlowerPetals;

        public void InitHashiRuntimeReferences(
            int width,
            int height,
            float horizontalSpacing,
            float verticalSpacing,
            Vector3 gridOrigin,
            float baseHeight,
            GameObject newIslandParent,
            GameObject newBridgeParent,
            GameObject newChainParent,
            GameObject newEffectsParent,
            List<IslandNode> islands,
            List<BridgeConnection> fixedBridges,
            List<ChainBarrier> chainsList)
        {
            gridWidth = width;
            gridHeight = height;
            hashiHorizontalSpacing = horizontalSpacing;
            hashiVerticalSpacing = verticalSpacing;
            hashiGridOrigin = gridOrigin;
            hashiBaseHeight = baseHeight;

            islandParent = newIslandParent;
            bridgeParent = newBridgeParent;
            chainParent = newChainParent;
            effectsParent = newEffectsParent;

            generatedIslands = islands ?? new List<IslandNode>();
            generatedFixedBridges = fixedBridges ?? new List<BridgeConnection>();
            generatedChains = chainsList ?? new List<ChainBarrier>();
        }

        public Vector3 GridCoordinateToWorld(Vector2Int coordinate)
        {
            float centeredX = coordinate.x - (gridWidth - 1) * 0.5f;
            float centeredY = coordinate.y - (gridHeight - 1) * 0.5f;

            return hashiGridOrigin + new Vector3(
                centeredX * hashiHorizontalSpacing,
                hashiBaseHeight,
                centeredY * hashiVerticalSpacing);
        }

        public void SetCameraSettings(Vector3 position, Vector3 euler, float orthographicSize)
        {
            cameraPosition = position;
            cameraEuler = euler;
            orthoVal = orthographicSize;
        }

        public Vector3 GetCameraPos()
        {
            return cameraPosition;
        }

        public Vector3 GetCameraEuler()
        {
            return cameraEuler;
        }

        public float GetCameraOrthoSize()
        {
            return orthoVal;
        }

        public void InitializeVariables(
            GameplayManager gameplayManager,
            GridManager gridManager,
            CinemachineCamera virtualCamera)
        {
            InitializeGameplayManager(gameplayManager);

            if (gridManager != null)
            {
                InitializeGridManager(gridManager);
            }

            if (virtualCamera != null)
            {
                InitializeCamera(virtualCamera);
            }
        }

        private void InitializeCamera(CinemachineCamera virtualCamera)
        {
            virtualCamera.transform.position = cameraPosition;
            virtualCamera.transform.eulerAngles = cameraEuler;
            virtualCamera.Lens.OrthographicSize = orthoVal;
        }

        private void InitializeGameplayManager(GameplayManager gameplayManager)
        {
        }

        public void Init(
            int width,
            int height,
            GridBase[,] gridBases,
            List<BoxContainerChain> boxContainerChains,
            List<ObjectSpawner> spawners)
        {
            if (gridBases != null)
            {
                CopyGridArray(gridBases);
            }

            gridWidth = width;
            gridHeight = height;
            chains = boxContainerChains;
            objectSpawners = spawners;
        }

        public void InitNewRuntimeReferences(
            GameObject newBoxGridParent,
            GameObject newBottomSlotParent,
            GameObject newFlowerParent,
            List<GameObject> boxes,
            List<GameObject> shooters,
            List<GameObject> bottomNodes,
            List<GameObject> flowerPetals)
        {
            boxGridParent = newBoxGridParent;
            bottomSlotParent = newBottomSlotParent;
            flowerParent = newFlowerParent;
            generatedBoxes = boxes ?? new List<GameObject>();
            generatedShooters = shooters ?? new List<GameObject>();
            generatedBottomNodes = bottomNodes ?? new List<GameObject>();
            generatedFlowerPetals = flowerPetals ?? new List<GameObject>();
        }

        private void CopyGridArray(GridBase[,] gridBases)
        {
            levelGridBases = new GridSaveClass[gridBases.GetLength(0)];

            for (int x = 0; x < gridBases.GetLength(0); x++)
            {
                levelGridBases[x] = new GridSaveClass
                {
                    gridCells = new GridBase[gridBases.GetLength(1)]
                };

                for (int y = 0; y < gridBases.GetLength(1); y++)
                {
                    levelGridBases[x].gridCells[y] = gridBases[x, y];
                }
            }
        }

        private void InitializeGridManager(GridManager gridManager)
        {
            if (levelGridBases == null || levelGridBases.Length == 0)
            {
                return;
            }

            GridBase[,] gridBasesArray = MorphTo2DArray(levelGridBases);
            gridManager.Init(gridBasesArray, this, chains, objectSpawners);
        }

        public GridBase[,] GetGridBases()
        {
            if (levelGridBases == null || levelGridBases.Length == 0)
            {
                return new GridBase[0, 0];
            }

            return MorphTo2DArray(levelGridBases);
        }

        public void HandleGridBasesPathfinding(GridBase[,] gridBasesArray)
        {
            if (gridBasesArray == null)
            {
                return;
            }

            for (int x = 0; x < gridBasesArray.GetLength(0); x++)
            {
                for (int y = 0; y < gridBasesArray.GetLength(1); y++)
                {
                    if (gridBasesArray[x, y] != null)
                    {
                        gridBasesArray[x, y].HandlePath();
                    }
                }
            }
        }

        private GridBase[,] MorphTo2DArray(GridSaveClass[] gridBases)
        {
            GridBase[,] result = new GridBase[gridWidth, gridHeight];

            for (int x = 0; x < gridBases.Length && x < gridWidth; x++)
            {
                if (gridBases[x] == null || gridBases[x].gridCells == null)
                {
                    continue;
                }

                for (int y = 0;
                     y < gridBases[x].gridCells.Length && y < gridHeight;
                     y++)
                {
                    result[x, y] = gridBases[x].gridCells[y];
                }
            }

            return result;
        }
    }

    [System.Serializable]
    public class GridSaveClass
    {
        public GridBase[] gridCells;
    }
}
