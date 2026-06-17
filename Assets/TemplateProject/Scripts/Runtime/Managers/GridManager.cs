using System;
using System.Collections.Generic;
using System.Threading;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TemplateProject.Scripts.Interfaces;
using TemplateProject.Scripts.Mechanic;
using TemplateProject.Scripts.Runtime.Models;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class GridManager : MonoSingleton<GridManager>, IGridProvider<GridBase>
    {
        public static GridManager instance;

        public int width;
        public int height;

        [Header("Cached References")] [SerializeField]
        private GridBase[,] gridBaseArray;

        public List<BoxContainerChain> chains = new();
        public List<ObjectSpawner> objectSpawners = new List<ObjectSpawner>();
        [SerializeField] private LevelContainer currentLevel;
        private AStarPathfinding pathfinder;
        private UniTask _coloringTask = UniTask.CompletedTask;
        [SerializeField] private int completeChainsCount;
        public Action OnChainComplete;
        private Dictionary<int, SemaphoreSlim> _columnLocks = new();


        private readonly Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        protected override void Awake()
        {
            MakeSingleton();
        }

        private void InitializePathfinder()
        {
            pathfinder = new AStarPathfinding(gridBaseArray);
        }

        private void MakeSingleton()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(this);
        }

        public void Init(GridBase[,] gridBases, LevelContainer level, List<BoxContainerChain> levelChains,
            List<ObjectSpawner> spawners)
        {
            gridBaseArray = gridBases;
            currentLevel = level;
            width = gridBaseArray.GetLength(0);
            height = gridBaseArray.GetLength(1);
            InitializePathfinder();
            chains = levelChains;
            objectSpawners = spawners;
        }

        public void RecalculatePaths()
        {
            currentLevel.HandleGridBasesPathfinding(gridBaseArray);
        }

        public AStarPathfinding GetPathfinder()
        {
            return pathfinder;
        }

        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            var best = new Vector2Int(0, 0);
            var bestDist = float.MaxValue;

            var width = gridBaseArray.GetLength(0);
            var height = gridBaseArray.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var cellPos = gridBaseArray[x, y].transform.position;
                    var d = (worldPosition.x - cellPos.x) * (worldPosition.x - cellPos.x)
                            + (worldPosition.z - cellPos.z) * (worldPosition.z - cellPos.z);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = new Vector2Int(x, y);
                    }
                }
            }

            return best;
        }

        public Vector3 GetCellCenter(Vector2Int cell)
        {
            var width = gridBaseArray.GetLength(0);
            var height = gridBaseArray.GetLength(1);

            if (cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height)
            {
                return gridBaseArray[cell.x, cell.y].transform.position;
            }

            return Vector3.zero;
        }

        public GridBase GetGridBase(Vector2Int candidateCell)
        {
            return gridBaseArray[candidateCell.x, candidateCell.y];
        }

        public bool IsNeighbor(GridBase a, GridBase b)
        {
            if (a == null || b == null) return false;
            var dx = Mathf.Abs(a.GetXAxis() - b.GetXAxis());
            var dy = Mathf.Abs(a.GetYAxis() - b.GetYAxis());
            return dx + dy == 1;
        }

        public List<Vector2Int> GetEmptyNeighbors(Vector2Int position)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                Vector2Int neighbor = position + dir;
                if (IsCellValid(neighbor) && IsCellEmpty(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }


        public GridBase GetEmptyNeighbor(Vector2Int position)
        {
            var neighbors = GetEmptyNeighbors(position);
            if (neighbors.Count == 0)
            {
                return null;
            }

            return GetCell(neighbors[0]);
        }

        public float GetCellWidth()
        {
            return 1f;
        }

        public int Width { get; }
        public int Height { get; }

        public GridBase GetCell(Vector2Int position)
        {
            return gridBaseArray[position.x, position.y];
        }

        public bool IsCellValid(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        public void AddChain(BoxContainerChain chain)
        {
            chains.Add(chain);
        }

        public bool IsCellEmpty(Vector2Int position)
        {
            return GetCell(position).IsEmpty();
        }

        public bool IsCellAvailableToMove(Vector2Int position, EnumHolder.GameColor color)
        {
            var cell = GetCell(position);
            if (color == EnumHolder.GameColor.None) return false;
            if (cell.IsEmpty()) return true;

            return false;
        }

        public Vector3 GetCellPosition(Vector2Int position)
        {
            return gridBaseArray[position.x, position.y].transform.position;
        }

        public GridBase[,] GetGridBases()
        {
            return gridBaseArray;
        }

        public List<ObjectSpawner> GetObjectSpawners()
        {
            return objectSpawners;
        }

        public async UniTask HandleColumnColors(int belongedSpawnerX, MatchingObject matchingObject)
        {
            if (!_columnLocks.TryGetValue(belongedSpawnerX, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _columnLocks[belongedSpawnerX] = semaphore;
            }

            await semaphore.WaitAsync();
            try
            {
                await SetColumnColor(belongedSpawnerX, matchingObject);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async UniTask SetColumnColor(int belongedSpawnerX, MatchingObject matchingObject)
        {
            var colorToApply = matchingObject
                ? matchingObject.Color
                : EnumHolder.GameColor.None;

            for (var y = gridBaseArray.GetLength(1) - 1; y >= 0; y--)
            {
                await UniTask.SwitchToMainThread();
                gridBaseArray[belongedSpawnerX, y].SetColor(colorToApply);

                await UniTask.WaitForSeconds(0.01f);
            }
        }

        public void IncreaseCompleteChainCount()
        {
            completeChainsCount++;
            OnChainComplete?.Invoke();
            if (completeChainsCount >= chains.Count)
            {
                DOVirtual.DelayedCall(0.5f, () => { GameplayManager.instance.WinGame(); });
            }
        }
    }
}