using System;
using System.Collections.Generic;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Runtime.Models;
using Unity.Cinemachine;
using UnityEngine;

namespace BoxPuller.Scripts.Data
{
    public class LevelContainer : MonoBehaviour
    {
        [Header("Cached References")]
        [SerializeField]
        private GridSaveClass[] levelGridBases;

        [SerializeField] private List<ObjectSpawner> objectSpawners;

        private List<BoxContainerChain> chains;

        [Header("Parameters")]
        [SerializeField]
        private int gridWidth;

        [SerializeField] private int gridHeight;
        [SerializeField] private Vector3 cameraPosition;
        [SerializeField] private Vector3 cameraEuler;
        [SerializeField] private float orthoVal;

        [Header("New Runtime References")]
        [SerializeField] private GameObject boxGridParent;
        [SerializeField] private GameObject bottomSlotParent;
        [SerializeField] private GameObject flowerParent;

        [SerializeField] private List<GameObject> generatedBoxes = new List<GameObject>();
        [SerializeField] private List<GameObject> generatedShooters = new List<GameObject>();
        [SerializeField] private List<GameObject> generatedBottomNodes = new List<GameObject>();
        [SerializeField] private List<GameObject> generatedFlowerPetals = new List<GameObject>();

        public GameObject BoxGridParent => boxGridParent;
        public GameObject BottomSlotParent => bottomSlotParent;
        public GameObject FlowerParent => flowerParent;

        public IReadOnlyList<GameObject> GeneratedBoxes => generatedBoxes;
        public IReadOnlyList<GameObject> GeneratedShooters => generatedShooters;
        public IReadOnlyList<GameObject> GeneratedBottomNodes => generatedBottomNodes;
        public IReadOnlyList<GameObject> GeneratedFlowerPetals => generatedFlowerPetals;

        public void Init(int width, int height, GridBase[,] gridBases, List<BoxContainerChain> boxContainerChains,
            List<ObjectSpawner> spawners)
        {
            CopyGridArray(gridBases);
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

        public void SetCameraSettings(Vector3 pos, Vector3 euler, float val)
        {
            cameraPosition = pos;
            cameraEuler = euler;
            orthoVal = val;
        }

        private void CopyGridArray(GridBase[,] gridBases)
        {
            levelGridBases = new GridSaveClass[gridBases.GetLength(0)];
            for (var x = 0; x < gridBases.GetLength(0); x++)
            {
                levelGridBases[x] = new GridSaveClass
                {
                    gridCells = new GridBase[gridBases.GetLength(1)]
                };
                for (var y = 0; y < gridBases.GetLength(1); y++)
                {
                    levelGridBases[x].gridCells[y] = gridBases[x, y];
                }
            }
        }

        public void InitializeVariables(GameplayManager gameplayManager,
            GridManager gridManager, CinemachineCamera virtualCamera)
        {
            // InitializeInteractionManager(interactionManager);
            InitializeGameplayManager(gameplayManager);
            InitializeGridManager(gridManager);
            InitializeCamera(virtualCamera);
        }

        private void InitializeCamera(CinemachineCamera virtualCamera)
        {
            virtualCamera.transform.position = cameraPosition;
            virtualCamera.transform.eulerAngles = cameraEuler;
            virtualCamera.Lens.OrthographicSize = orthoVal;
        }

        private void InitializeInteractionManager(InputManager inputManager)
        {
            // inputManager.SetLevelContainer(this);
            // interactionManager.InitializeInteractionManager();
        }

        private void InitializeGameplayManager(GameplayManager gameplayManager)
        {
            //Initialize GameManager if needed   
        }

        private void InitializeGridManager(GridManager gridManager)
        {
            if (levelGridBases == null || levelGridBases.Length == 0)
            {
                return;
            }

            var gridBasesArray = MorphTo2DArray(levelGridBases);
            gridManager.Init(gridBasesArray, this, chains, objectSpawners);
        }

        public GridBase[,] GetGridBases()
        {
            var gridBasesArray = MorphTo2DArray(levelGridBases);
            return gridBasesArray;
        }

        public void HandleGridBasesPathfinding(GridBase[,] gridBasesArray)
        {
            for (var i = 0; i < gridBasesArray.GetLength(0); i++)
            {
                for (var j = 0; j < gridBasesArray.GetLength(1); j++)
                {
                    gridBasesArray[i, j].HandlePath();
                }
            }
        }

        private GridBase[,] MorphTo2DArray(GridSaveClass[] gridBases)
        {
            var newGridBases = new GridBase[gridWidth, gridHeight];
            for (var x = 0; x < gridBases.GetLength(0); x++)
            {
                for (var y = 0; y < gridBases[x].gridCells.Length; y++)
                {
                    newGridBases[x, y] = gridBases[x].gridCells[y];
                }
            }

            return newGridBases;
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
    }

    [System.Serializable]
    public class GridSaveClass
    {
        public GridBase[] gridCells;
    }
}