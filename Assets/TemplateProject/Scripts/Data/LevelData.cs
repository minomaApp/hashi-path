using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data.Enums;
using UnityEngine;

namespace BoxPuller.Scripts.Data
{
    [Serializable]
    public class GridCellData
    {
        public BasePlaceableData BasePlaceable { get; set; }
        public Vector2Int coordinates;
        public bool isActive = true;
        public int blockCount;
        public bool IsEmpty => BasePlaceable == null;
    }


    [Serializable]
    public class BasePlaceableData
    {
    }


    public class StackedObjectData : BasePlaceableData
    {
        public List<SingleObjectData> Stack = new();
    }


    public class SingleObjectData : BasePlaceableData
    {
        public EnumHolder.GameColor Color;
        public bool isFrozen;
        public int BlockCount;
        public int X, Y;
    }


    public class ChainData : SingleObjectData
    {
        public bool isHead;
        public EnumHolder.Direction direction;
    }


    public class FoodData : SingleObjectData
    {
    }

    public class KeyFoodData : FoodData
    {
        public int id;
    }

    public class ConveyorItemData : StackedObjectData
    {
        public EnumHolder.GameColor Color;

        public int X, Y;
        public bool isSecret;
    }

    public class LockFoodData : FoodData
    {
        public int id;
    }

    public class LockedObjectData : SingleObjectData
    {
        public LockedObjectData(int lockCount)
        {
            this.LockCount = lockCount;
        }

        public int LockCount;
    }


    public class HiddenObjectData : SingleObjectData
    {
    }


    public class SpawnerObjectData : StackedObjectData
    {
        public EnumHolder.Direction Direction;
    }


    [Serializable]
    public class ConnectionData
    {
        public List<Vector2Int> connectedGridPositions = new List<Vector2Int>();
    }

    [Serializable]
    public class ShooterSpawnData
    {
        public EnumHolder.GameColor color = EnumHolder.GameColor.None;
        public int bulletCount = 5;

        public int laneIndex;
        public int orderIndex;

        // -1 = baglanti yok
        public int linkGroupId = -1;

        public bool isHidden;
    }

    [Serializable]
    public class BottomShooterLaneData
    {
        public List<ShooterSpawnData> shooters = new List<ShooterSpawnData>();
    }

    [Serializable]
    public class BoxCellData : BasePlaceableData
    {
        public EnumHolder.GameColor color = EnumHolder.GameColor.None;

        public int x;
        public int y;

        // -1 = kalip yok
        public int moldGroupId = -1;

        public bool isFilled = true;
    }

    // Eski gecis kodlarinda BoxSpawnData adi kullanilmissa compile kirilmasin diye birakiyorum.
    // Yeni sistem GridData[x,y].BasePlaceable icindeki BoxCellData'yi kullanacak.
    [Serializable]
    public class BoxSpawnData
    {
        public EnumHolder.GameColor color = EnumHolder.GameColor.None;
        public int x;
        public int y;
        public int moldGroupId = -1;
        public bool isFilled = true;
    }


    [Serializable]
    public class IslandCellData : BasePlaceableData
    {
        public int x;
        public int y;
        public int requiredBridgeCount = 1;
        public EnumHolder.IslandBridgeMode bridgeMode = EnumHolder.IslandBridgeMode.SingleOnly;
        public bool startsLocked;
        public int unlockAfterCompletedIslandCount;

        public Vector2Int Coordinate => new Vector2Int(x, y);
    }

    [Serializable]
    public class FixedBridgeDefinitionData
    {
        public int id;
        public Vector2Int startCoordinate;
        public Vector2Int endCoordinate;
        public int bridgeCount = 1;
    }

    [Serializable]
    public class TutorialBridgeDefinitionData
    {
        public int id;
        public Vector2Int startCoordinate;
        public Vector2Int endCoordinate;
        public int bridgeCount = 1;
    }

    [Serializable]
    public class ChainBarrierData
    {
        public int id;
        public Vector2Int startCoordinate;
        public Vector2Int endCoordinate;
        public int unlockAfterCompletedIslandCount;
    }

    [Serializable]
    public class HashiLevelRulesData
    {
        public bool blockBridgeThroughIsland = true;
        public bool blockBridgeCrossing = true;
        public bool requireAllIslandsConnected;
        public float islandBlockingRadius = 0.45f;
    }

    [Serializable]
    public class LevelData
    {
        #region Variables

        public int Width => GridData.GetLength(0);
        public int Height => GridData.GetLength(1);

        public readonly EnumHolder.LevelDataDefaultObjectType levelDataDefaultObjectType =
            EnumHolder.LevelDataDefaultObjectType.Single;

        // Don't make Data's setters private, JSON converter can't deserialize if setters private and loads empty data
        public GridCellData[,] GridData { get; set; }

        public int[,] VerticalEmptyAreaData { get; set; }
        public int[,] HorizontalEmptyAreaData { get; set; }

        public List<BasePlaceableData> TargetQueue = new();

        public List<ConnectionData> connections;

        public GridCellData[,] ConveyorData { get; set; }

        //****
        [Header("Bottom Slot Shooter Lanes")]
        public List<BottomShooterLaneData> bottomShooterLanes = new List<BottomShooterLaneData>();

        // Eski sistemden gecis icin simdilik kalabilir.
        // Yeni sistem bunu kullanmayacak.
        [Header("Legacy Bottom Shooters")]
        public List<ShooterSpawnData> bottomShooters = new List<ShooterSpawnData>();

        // Middle slot her level'da ayni olacagi icin level datasinda tutulmayacak.
        // public List<ShooterSpawnData> middleShooters = new List<ShooterSpawnData>();

        // Yeni sistem box datasini GridData hucrelerinde BoxCellData olarak tutacak.
        // Bu liste sadece eski gecis kodlari icin kalabilir.
        [Header("Legacy Boxes")]
        public List<BoxSpawnData> boxes = new List<BoxSpawnData>();

        [Header("Grid Size")]
        public int boxGridWidth = 5;
        public int boxGridHeight = 10;

        [Header("Bottom Slot Settings")]
        public int bottomLaneCount = 3;
        public int visibleShooterCountPerLane = 4;
        //****

        [Header("Level Time")]
        public const int DefaultLevelTimeSeconds = 120;
        public int levelTimeSeconds = DefaultLevelTimeSeconds;

        [Header("Hashi Level Data")]
        public List<FixedBridgeDefinitionData> fixedBridges = new List<FixedBridgeDefinitionData>();
        public List<TutorialBridgeDefinitionData> tutorialBridges = new List<TutorialBridgeDefinitionData>();
        public List<ChainBarrierData> chainBarriers = new List<ChainBarrierData>();
        public HashiLevelRulesData hashiRules = new HashiLevelRulesData();

        public LevelData(int width, int height, int targetQueueLength, int conveyorLength = 0)
        {
            GridData = new GridCellData[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    GridData[x, y] = new GridCellData
                    {
                        coordinates = new Vector2Int(x, y)
                    };
                }
            }

            VerticalEmptyAreaData = new int[width, height];
            HorizontalEmptyAreaData = new int[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    VerticalEmptyAreaData[x, y] = 0;
                    HorizontalEmptyAreaData[x, y] = 0;
                }
            }

            switch (levelDataDefaultObjectType)
            {
                case EnumHolder.LevelDataDefaultObjectType.Single:
                    {
                        for (var i = 0; i < targetQueueLength; i++)
                        {
                            TargetQueue.Add(new SingleObjectData());
                        }

                        break;
                    }

                case EnumHolder.LevelDataDefaultObjectType.Stacked:
                    {
                        for (var i = 0; i < targetQueueLength; i++)
                        {
                            TargetQueue.Add(new StackedObjectData());
                        }

                        break;
                    }
            }

            connections = new List<ConnectionData>();

            ConveyorData = new GridCellData[width, conveyorLength];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < conveyorLength; y++)
                    ConveyorData[x, y] = new GridCellData
                    {
                        coordinates = new Vector2Int(x, y),
                        isActive = true
                    };

            EnsureBottomLaneCount(bottomLaneCount);
            EnsureHashiData();
        }

        public void SetConveyorCellStack(int x, int y, EnumHolder.GameColor color, bool secret)
        {
            var cell = ConveyorData[x, y];
            cell.blockCount = 0;

            var data = new ConveyorItemData
            {
                Color = color,
                X = x,
                Y = y,
                isSecret = secret
            };

            cell.BasePlaceable = data;
            ConveyorData[x, y] = cell;
        }

        public void RemoveConveyorCellStack(int x, int y)
        {
            var cell = ConveyorData[x, y];
            cell.blockCount = 0;
            cell.BasePlaceable = null;
        }

        #endregion


        public SingleObjectData HasSameColoredNeighbor(int x, int y)
        {
            var cell = GridData[x, y];
            var neighbors = GetNeighbors(x, y);

            foreach (var neighbor in neighbors)
            {
                if (neighbor.BasePlaceable is SingleObjectData singleObject)
                {
                    if (singleObject.Color == (cell.BasePlaceable as SingleObjectData)?.Color)
                    {
                        return singleObject;
                    }
                }
            }

            return null;
        }


        public List<GridCellData> GetNeighbors(int x, int y)
        {
            List<GridCellData> neighbors = new List<GridCellData>();

            if (x > 0)
                neighbors.Add(GridData[x - 1, y]);

            if (x < Width - 1)
                neighbors.Add(GridData[x + 1, y]);

            if (y > 0)
                neighbors.Add(GridData[x, y - 1]);

            if (y < Height - 1)
                neighbors.Add(GridData[x, y + 1]);

            return neighbors;
        }


        #region Grid

        public void SetGridCellStack(int x, int y, EnumHolder.GameColor color, EnumHolder.ObjectType type,
            EnumHolder.Direction direction, bool isHead, bool isFrozen = false, int count = 0)
        {
            var cell = GridData[x, y];
            cell.blockCount = 0;
            if (TryActivateCell(x, y)) return;

            if (type is EnumHolder.ObjectType.ChainCell)
            {
                ChainData chainData = new ChainData();
                chainData.Color = color;
                chainData.direction = direction;

                chainData.isFrozen = isFrozen;

                chainData.X = x;
                chainData.Y = y;
                chainData.isHead = isHead;
                cell.BasePlaceable = chainData;
                if (isFrozen)
                {
                    var chains = GetChainGroup(chainData);
                    chains.ForEach(chain =>
                    {
                        chain.isFrozen = true;
                        chain.BlockCount = count;
                    });
                }
            }
            else if (type is EnumHolder.ObjectType.Food)
            {
                FoodData foodData = new FoodData();
                foodData.Color = color;
                foodData.isFrozen = isFrozen;
                foodData.BlockCount = count;
                foodData.X = x;
                foodData.Y = y;
                cell.BasePlaceable = foodData;
            }
            else if (type is EnumHolder.ObjectType.GridLock)
            {
                cell.blockCount = count;
            }
            else if (type is EnumHolder.ObjectType.Key)
            {
                KeyFoodData foodData = new KeyFoodData();
                foodData.Color = color;
                foodData.isFrozen = isFrozen;
                foodData.BlockCount = count;
                foodData.X = x;
                foodData.Y = y;
                foodData.id = count;
                cell.BasePlaceable = foodData;
            }
            else if (type is EnumHolder.ObjectType.Lock)
            {
                LockFoodData foodData = new LockFoodData();
                foodData.Color = color;
                foodData.isFrozen = isFrozen;
                foodData.BlockCount = count;
                foodData.X = x;
                foodData.Y = y;
                foodData.id = count;
                cell.BasePlaceable = foodData;
            }
            else
            {
                cell.BasePlaceable = new SingleObjectData();
                ((cell.BasePlaceable as SingleObjectData)!).Color = color;
            }

            GridData[x, y] = cell;
        }


        public void RemoveGridCellStack(int x, int y)
        {
            var cell = GridData[x, y];
            var basePlaceable = cell.BasePlaceable;
            cell.blockCount = 0;

            if (basePlaceable is BoxCellData)
            {
                cell.BasePlaceable = null;
                GridData[x, y] = cell;
                return;
            }

            if (basePlaceable == null)
            {
                cell.isActive = false;
            }

            else if (basePlaceable is SingleObjectData)
            {
                cell.BasePlaceable = null;
            }
            else if (basePlaceable is SpawnerObjectData)
            {
                cell.BasePlaceable = null;
            }
            else if (basePlaceable is StackedObjectData)
            {
                var placeable = basePlaceable as StackedObjectData;

                switch (placeable.Stack.Count)
                {
                    case 1:
                        cell.BasePlaceable = null;
                        break;

                    default:
                        placeable.Stack.RemoveAt(placeable.Stack.Count - 1);
                        break;
                }
            }
            else
            {
                cell.BasePlaceable = null;
            }
        }


        public List<ChainData> GetChainGroup(ChainData targetChain)
        {
            foreach (var chainGroup in GetChains())
            {
                if (chainGroup.Contains(targetChain))
                {
                    return chainGroup;
                }
            }

            return new List<ChainData>();
        }


        public List<List<ChainData>> GetChains()
        {
            List<List<ChainData>> chains = new List<List<ChainData>>();
            bool[,] visited = new bool[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (visited[x, y]) continue;

                    var cell = GridData[x, y];
                    if (cell.BasePlaceable is not ChainData chain) continue;

                    int unvisitedSameColorCount =
                        GetUnvisitedSameColoredChainCount(new Vector2Int(x, y), chain.Color, visited);

                    if (unvisitedSameColorCount == 1 || chain.isHead)
                    {
                        List<ChainData> chainGroup = new List<ChainData>();
                        FloodFillChain(x, y, chain.Color, visited, chainGroup);

                        if (chainGroup.Count > 0)
                        {
                            chains.Add(chainGroup);
                        }
                    }
                }
            }

            return chains;
        }


        private void FloodFillChain(int x, int y, EnumHolder.GameColor targetColor, bool[,] visited,
            List<ChainData> group)
        {
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(x, y));

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                int px = pos.x;
                int py = pos.y;

                if (px < 0 || px >= Width || py < 0 || py >= Height) continue;
                if (visited[px, py]) continue;

                var cell = GridData[px, py];
                if (cell.BasePlaceable is not ChainData chain) continue;
                if (chain.Color != targetColor) continue;

                visited[px, py] = true;
                group.Add(chain);
                var direction = chain.direction;
                List<Vector2Int> directions = GetOrderedDirections(direction);

                foreach (var dir in directions)
                {
                    int nx = px + dir.x;
                    int ny = py + dir.y;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && !visited[nx, ny])
                    {
                        var neighbor = GridData[nx, ny];
                        if (neighbor.BasePlaceable is ChainData neighborChain && neighborChain.Color == targetColor)
                        {
                            stack.Push(new Vector2Int(nx, ny));
                        }
                    }
                }
            }
        }


        private List<Vector2Int> GetOrderedDirections(EnumHolder.Direction priority)
        {
            List<Vector2Int> ordered = new();

            Vector2Int toDir(EnumHolder.Direction d) =>
                d switch
                {
                    EnumHolder.Direction.Up => new Vector2Int(0, 1),
                    EnumHolder.Direction.Down => new Vector2Int(0, -1),
                    EnumHolder.Direction.Left => new Vector2Int(-1, 0),
                    EnumHolder.Direction.Right => new Vector2Int(1, 0),
                    _ => Vector2Int.zero
                };


            foreach (EnumHolder.Direction d in Enum.GetValues(typeof(EnumHolder.Direction)))
            {
                if (d == priority) continue;
                ordered.Add(toDir(d));
            }

            ordered.Add(toDir(priority));

            return ordered;
        }


        private int GetUnvisitedSameColoredChainCount(Vector2Int pos, EnumHolder.GameColor color, bool[,] visited)
        {
            int count = 0;

            var neighbors = GetNeighbors(pos.x, pos.y);
            foreach (var neighbor in neighbors)
            {
                if (neighbor.BasePlaceable is ChainData chain &&
                    chain.Color == color &&
                    !visited[neighbor.coordinates.x, neighbor.coordinates.y])
                {
                    count++;
                }
            }

            return count;
        }

        #endregion


        #region Queue

        public void SetQueueCellStack(int x, EnumHolder.GameColor color, List<BasePlaceableData> list)
        {
            var placeable = list[x];

            if (placeable is SingleObjectData)
            {
                var newTarget = new SingleObjectData
                {
                    Color = color
                };
                TargetQueue[x] = newTarget;
            }
            else if (placeable is StackedObjectData)
            {
                var stackedPlacable = placeable as StackedObjectData;
                var defaultObject = new SingleObjectData
                {
                    Color = color
                };
                stackedPlacable.Stack.Add(defaultObject);
                TargetQueue[x] = stackedPlacable;
            }
        }


        public void RemoveQueueCellStack(int x, List<BasePlaceableData> list)
        {
            var queueCell = list[x];

            if (queueCell is SingleObjectData)
            {
                var singleObject = queueCell as SingleObjectData;
                if (singleObject.Color == EnumHolder.GameColor.None)
                {
                    list.RemoveAt(x);
                }
                else
                {
                    singleObject.Color = EnumHolder.GameColor.None;
                }
            }
            else if (queueCell is StackedObjectData)
            {
                var stackedPlacable = queueCell as StackedObjectData;

                if (stackedPlacable.Stack.Count == 0)
                {
                    list.RemoveAt(x);
                }
                else
                {
                    stackedPlacable.Stack.RemoveAt(stackedPlacable.Stack.Count - 1);
                }
            }
        }

        #endregion


        #region Spawner

        public void AddSpawner(int x, int y, EnumHolder.Direction direction)
        {
            var cell = GridData[x, y];
            cell.BasePlaceable = new SpawnerObjectData();
            var spawnerObject = cell.BasePlaceable as SpawnerObjectData;
            spawnerObject!.Direction = direction;
            spawnerObject.Stack.Add(new SingleObjectData
            {
                Color = EnumHolder.GameColor.None
            });
        }


        public void RemoveSpawner(int x, int y)
        {
            var cell = GridData[x, y];
            cell.BasePlaceable = new BasePlaceableData();
        }


        public void SetSpawnerCellStack(int x, int y, int orderInList, EnumHolder.GameColor color,
            EnumHolder.Direction direction)
        {
            var cell = GridData[x, y];
            var spawnerObject = cell.BasePlaceable as SpawnerObjectData;
            spawnerObject ??= new SpawnerObjectData();
            spawnerObject.Direction = direction;
            spawnerObject.Stack[orderInList] = new SingleObjectData
            {
                Color = color
            };
        }


        public void RemoveSpawnerCellStack(int x, int y, int orderInList)
        {
            var cell = GridData[x, y];
            var spawnerObject = cell.BasePlaceable as SpawnerObjectData;
            spawnerObject?.Stack.RemoveAt(orderInList);
        }

        #endregion


        #region Utility

        public bool TryActivateCell(int x, int y)
        {
            if (GridData[x, y].isActive) return false;
            GridData[x, y].isActive = true;
            return true;
        }


        public void ResizeGridCells(int newWidth, int newHeight, bool expandDown = true, bool expandLeft = true)
        {
            int originalWidth = GridData.GetLength(0);
            int originalHeight = GridData.GetLength(1);

            // Determine offsets based on the moveDown and moveLeft parameters
            int widthOffset = expandLeft ? newWidth - originalWidth : 0;
            int heightOffset = expandDown ? newHeight - originalHeight : 0;

            GridCellData[,] resized = new GridCellData[newWidth, newHeight];
            int[,] resizedVertical = new int[newWidth, newHeight];
            int[,] resizedHorizontal = new int[newWidth, newHeight];

            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    // Calculate the corresponding original indices
                    int originalX = x - widthOffset;
                    int originalY = y - heightOffset;

                    if (originalX >= 0 && originalX < originalWidth &&
                        originalY >= 0 && originalY < originalHeight)
                    {
                        // Copy data from the original grid
                        resized[x, y] = GridData[originalX, originalY];
                        resizedVertical[x, y] = VerticalEmptyAreaData[originalX, originalY];
                        resizedHorizontal[x, y] = HorizontalEmptyAreaData[originalX, originalY];

                        // Update the coordinates in the copied cell
                        if (resized[x, y] != null)
                        {
                            resized[x, y].coordinates = new Vector2Int(x, y);

                            if (resized[x, y].BasePlaceable is IslandCellData islandCell)
                            {
                                islandCell.x = x;
                                islandCell.y = y;
                            }
                        }
                    }
                    else
                    {
                        // Initialize new cells for the extended area
                        resized[x, y] = new GridCellData
                        {
                            coordinates = new Vector2Int(x, y)
                        };
                        resizedVertical[x, y] = 0;
                        resizedHorizontal[x, y] = 0;
                    }
                }
            }

            // Update the grid data references
            GridData = resized;
            VerticalEmptyAreaData = resizedVertical;
            HorizontalEmptyAreaData = resizedHorizontal;

            ShiftHashiDefinitions(widthOffset, heightOffset, newWidth, newHeight);
        }

        public void ResizeConveyorCells(int conveyorWidth, int newLength, bool expandUp, bool expandLeft)
        {
            var old = ConveyorData;
            var next = new GridCellData[conveyorWidth, newLength];

            for (int x = 0; x < conveyorWidth; x++)
            {
                for (int y = 0; y < newLength; y++)
                {
                    // Eski diziden hangi indeksi alacagiz?
                    int oldY = expandUp
                        ? y - (newLength - (old?.GetLength(1) ?? 0))
                        : y;
                    int oldX = expandLeft
                        ? x - (conveyorWidth - (old?.GetLength(0) ?? 0))
                        : x;

                    if (old != null &&
                        oldX >= 0 && oldX < old.GetLength(0) &&
                        oldY >= 0 && oldY < old.GetLength(1))
                    {
                        next[x, y] = old[oldX, oldY];
                    }
                    else
                    {
                        next[x, y] = new GridCellData
                        {
                            coordinates = new Vector2Int(x, y),
                            isActive = true
                        };
                    }
                }
            }

            ConveyorData = next;
        }


        public void ResizeList<T>(List<T> list, int newWidth, EnumHolder.LevelDataDefaultObjectType typeOfListObjects)
            where T : BasePlaceableData
        {
            var originalWidth = list.Count;
            if (newWidth < list.Count)
            {
                for (var i = originalWidth; i > newWidth; i--)
                {
                    list.RemoveAt(i - 1);
                }
            }
            else
            {
                T placeable;
                switch (typeOfListObjects)
                {
                    case EnumHolder.LevelDataDefaultObjectType.Single:
                        placeable = new SingleObjectData() as T;
                        break;

                    case EnumHolder.LevelDataDefaultObjectType.Stacked:
                        placeable = new StackedObjectData() as T;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                for (var i = originalWidth; i < newWidth; i++)
                {
                    list.Add(placeable);
                }
            }
        }


        private BasePlaceableData CreateNewPlaceableInstance(Type placeableType)
        {
            return (BasePlaceableData)Activator.CreateInstance(placeableType);
        }


        public void TryToLockGridCellsInArea(int x, int y, Vector2Int blockedAreaSize, int blockAmount)
        {
            if (TryActivateCell(x, y)) return;

            List<GridCellData> cellsToLock = GetCellsInArea(new Vector2Int(x, y), blockedAreaSize);
            if (cellsToLock == null) return;

            var cell = GridData[x, y];

            // Creating a locked object with the same locked area indexes
            /* cell.BasePlaceable = new LockedArea(new List<PlaceableElementData>(), blockedAreaSize)
             {
                 lockedAreaIndexes = cellsToLock.ConvertAll(c => c.coordinates)
             };*/

            foreach (var lockedCell in cellsToLock)
            {
                lockedCell.isActive = false;
            }
        }


        /* private void RemoveCellLockedPlacable(int x, int y)
         {
             var cell = GridData[x, y];
             var lockedObject = (LockedArea)cell.BasePlaceable;

             foreach (var lockedCellCoordinates in lockedObject.lockedAreaIndexes)
             {
                 GridData[lockedCellCoordinates.x, lockedCellCoordinates.y].isActive = true;
             }

             lockedObject.lockedAreaIndexes.Clear();
             cell.BasePlaceable = new BasePlaceable(new List<PlaceableElementData>());
         }*/


        private List<GridCellData> GetCellsInArea(Vector2Int startCellCoordinates, Vector2Int blockedAreaSize)
        {
            List<GridCellData> cellsInArea = new List<GridCellData>();

            for (int i = 0; i < blockedAreaSize.x; i++)
            {
                for (int j = 0; j < blockedAreaSize.y; j++)
                {
                    int cellX = startCellCoordinates.x + i;
                    int cellY = startCellCoordinates.y + j;

                    if (cellX >= Width || cellY >= Height) return null;

                    cellsInArea.Add(GridData[cellX, cellY]);
                }
            }

            return cellsInArea;
        }


        public void SetEmptyAreaNumber(int x, int y, bool isVertical)
        {
            if (isVertical) VerticalEmptyAreaData[x, y]++;
            else HorizontalEmptyAreaData[x, y]++;
        }


        public void RemoveEmptyAreaNumber(int x, int y, bool isVertical)
        {
            var emptyAreaData = isVertical ? VerticalEmptyAreaData : HorizontalEmptyAreaData;
            emptyAreaData[x, y]--;
        }

        #endregion

        #region New Box Grid Data

        public void SetBoxCell(int x, int y, EnumHolder.GameColor color, int moldGroupId)
        {
            if (!IsInsideGrid(x, y)) return;

            var cell = GridData[x, y];

            if (TryActivateCell(x, y))
            {
                cell = GridData[x, y];
            }

            cell.blockCount = 0;
            cell.BasePlaceable = new BoxCellData
            {
                color = color,
                x = x,
                y = y,
                moldGroupId = moldGroupId,
                isFilled = color != EnumHolder.GameColor.None
            };

            GridData[x, y] = cell;
        }

        public void RemoveBoxCell(int x, int y)
        {
            if (!IsInsideGrid(x, y)) return;

            var cell = GridData[x, y];
            cell.blockCount = 0;
            cell.BasePlaceable = null;
            GridData[x, y] = cell;
        }

        public List<BoxCellData> GetBoxCells()
        {
            List<BoxCellData> result = new List<BoxCellData>();

            if (GridData == null) return result;

            for (int x = 0; x < GridData.GetLength(0); x++)
            {
                for (int y = 0; y < GridData.GetLength(1); y++)
                {
                    if (GridData[x, y].BasePlaceable is BoxCellData boxCell && boxCell.isFilled)
                    {
                        boxCell.x = x;
                        boxCell.y = y;
                        result.Add(boxCell);
                    }
                }
            }

            return result;
        }

        private bool IsInsideGrid(int x, int y)
        {
            return GridData != null &&
                   x >= 0 &&
                   y >= 0 &&
                   x < GridData.GetLength(0) &&
                   y < GridData.GetLength(1);
        }

        #endregion



        #region Hashi Level Data

        public void EnsureHashiData()
        {
            fixedBridges ??= new List<FixedBridgeDefinitionData>();
            tutorialBridges ??= new List<TutorialBridgeDefinitionData>();
            chainBarriers ??= new List<ChainBarrierData>();
            hashiRules ??= new HashiLevelRulesData();

            if (hashiRules.islandBlockingRadius <= 0f)
            {
                hashiRules.islandBlockingRadius = 0.45f;
            }

            if (levelTimeSeconds <= 0)
            {
                levelTimeSeconds = DefaultLevelTimeSeconds;
            }
        }

        public void SetIslandCell(
            int x,
            int y,
            int requiredBridgeCount,
            EnumHolder.IslandBridgeMode bridgeMode,
            bool startsLocked,
            int unlockAfterCompletedIslandCount)
        {
            if (!IsInsideGrid(x, y))
            {
                return;
            }

            EnsureHashiData();

            GridCellData cell = GridData[x, y];

            if (TryActivateCell(x, y))
            {
                cell = GridData[x, y];
            }

            cell.blockCount = 0;
            cell.BasePlaceable = new IslandCellData
            {
                x = x,
                y = y,
                requiredBridgeCount = Mathf.Max(1, requiredBridgeCount),
                bridgeMode = bridgeMode,
                startsLocked = startsLocked,
                unlockAfterCompletedIslandCount = Mathf.Max(0, unlockAfterCompletedIslandCount)
            };

            GridData[x, y] = cell;
        }

        public void RemoveIslandCell(int x, int y)
        {
            if (!IsInsideGrid(x, y))
            {
                return;
            }

            EnsureHashiData();

            GridCellData cell = GridData[x, y];
            cell.blockCount = 0;
            cell.BasePlaceable = null;
            GridData[x, y] = cell;

            //Vector2Int coordinate = new Vector2Int(x, y);
            //fixedBridges.RemoveAll(bridge =>
            //    bridge.startCoordinate == coordinate ||
            //    bridge.endCoordinate == coordinate);

            Vector2Int coordinate = new Vector2Int(x, y);

            fixedBridges.RemoveAll(bridge =>
                bridge.startCoordinate == coordinate ||
                bridge.endCoordinate == coordinate);

            tutorialBridges.RemoveAll(bridge =>
                bridge.startCoordinate == coordinate ||
                bridge.endCoordinate == coordinate);
        }

        public bool TryGetIslandCell(Vector2Int coordinate, out IslandCellData islandCell)
        {
            islandCell = null;

            if (!IsInsideGrid(coordinate.x, coordinate.y))
            {
                return false;
            }

            islandCell = GridData[coordinate.x, coordinate.y].BasePlaceable as IslandCellData;
            return islandCell != null;
        }

        public List<IslandCellData> GetIslandCells()
        {
            List<IslandCellData> result = new List<IslandCellData>();

            if (GridData == null)
            {
                return result;
            }

            for (int x = 0; x < GridData.GetLength(0); x++)
            {
                for (int y = 0; y < GridData.GetLength(1); y++)
                {
                    if (GridData[x, y].BasePlaceable is not IslandCellData islandCell)
                    {
                        continue;
                    }

                    islandCell.x = x;
                    islandCell.y = y;
                    result.Add(islandCell);
                }
            }

            return result;
        }

        public bool AddFixedBridgeDefinition(
            Vector2Int startCoordinate,
            Vector2Int endCoordinate,
            int bridgeCount)
        {
            EnsureHashiData();

            if (startCoordinate == endCoordinate)
            {
                return false;
            }

            if (!TryGetIslandCell(startCoordinate, out _) ||
                !TryGetIslandCell(endCoordinate, out _))
            {
                return false;
            }

            bool duplicate = fixedBridges.Exists(bridge =>
                (bridge.startCoordinate == startCoordinate &&
                 bridge.endCoordinate == endCoordinate) ||
                (bridge.startCoordinate == endCoordinate &&
                 bridge.endCoordinate == startCoordinate));

            if (duplicate)
            {
                return false;
            }

            fixedBridges.Add(new FixedBridgeDefinitionData
            {
                id = GetNextFixedBridgeId(),
                startCoordinate = startCoordinate,
                endCoordinate = endCoordinate,
                bridgeCount = Mathf.Clamp(bridgeCount, 1, 2)
            });

            return true;
        }
        public bool AddTutorialBridgeDefinition(
    Vector2Int startCoordinate,
    Vector2Int endCoordinate,
    int bridgeCount)
        {
            EnsureHashiData();

            if (startCoordinate == endCoordinate)
            {
                return false;
            }

            if (!TryGetIslandCell(startCoordinate, out _) ||
                !TryGetIslandCell(endCoordinate, out _))
            {
                return false;
            }

            bool duplicateFixed = fixedBridges.Exists(bridge =>
                (bridge.startCoordinate == startCoordinate &&
                 bridge.endCoordinate == endCoordinate) ||
                (bridge.startCoordinate == endCoordinate &&
                 bridge.endCoordinate == startCoordinate));

            if (duplicateFixed)
            {
                return false;
            }

            bool duplicateTutorial = tutorialBridges.Exists(bridge =>
                (bridge.startCoordinate == startCoordinate &&
                 bridge.endCoordinate == endCoordinate) ||
                (bridge.startCoordinate == endCoordinate &&
                 bridge.endCoordinate == startCoordinate));

            if (duplicateTutorial)
            {
                return false;
            }

            tutorialBridges.Add(new TutorialBridgeDefinitionData
            {
                id = GetNextTutorialBridgeId(),
                startCoordinate = startCoordinate,
                endCoordinate = endCoordinate,
                bridgeCount = Mathf.Clamp(bridgeCount, 1, 2)
            });

            return true;
        }

        public bool AddChainBarrier(
            Vector2Int startCoordinate,
            Vector2Int endCoordinate,
            int unlockAfterCompletedIslandCount)
        {
            EnsureHashiData();

            if (!IsInsideGrid(startCoordinate.x, startCoordinate.y) ||
                !IsInsideGrid(endCoordinate.x, endCoordinate.y) ||
                startCoordinate == endCoordinate)
            {
                return false;
            }

            bool duplicate = chainBarriers.Exists(chain =>
                (chain.startCoordinate == startCoordinate &&
                 chain.endCoordinate == endCoordinate) ||
                (chain.startCoordinate == endCoordinate &&
                 chain.endCoordinate == startCoordinate));

            if (duplicate)
            {
                return false;
            }

            chainBarriers.Add(new ChainBarrierData
            {
                id = GetNextChainBarrierId(),
                startCoordinate = startCoordinate,
                endCoordinate = endCoordinate,
                unlockAfterCompletedIslandCount = Mathf.Max(0, unlockAfterCompletedIslandCount)
            });

            return true;
        }

        public void RemoveFixedBridgeDefinition(int id)
        {
            EnsureHashiData();
            fixedBridges.RemoveAll(bridge => bridge.id == id);
        }

        public void RemoveTutorialBridgeDefinition(int id)
        {
            EnsureHashiData();
            tutorialBridges.RemoveAll(bridge => bridge.id == id);
        }

        public void RemoveChainBarrier(int id)
        {
            EnsureHashiData();
            chainBarriers.RemoveAll(chain => chain.id == id);
        }

        public int GetNextFixedBridgeId()
        {
            EnsureHashiData();

            int nextId = 1;
            while (fixedBridges.Exists(bridge => bridge.id == nextId))
            {
                nextId++;
            }

            return nextId;
        }
        public int GetNextTutorialBridgeId()
        {
            EnsureHashiData();

            int nextId = 1;
            while (tutorialBridges.Exists(bridge => bridge.id == nextId))
            {
                nextId++;
            }

            return nextId;
        }

        public int GetNextChainBarrierId()
        {
            EnsureHashiData();

            int nextId = 1;
            while (chainBarriers.Exists(chain => chain.id == nextId))
            {
                nextId++;
            }

            return nextId;
        }

        private void ShiftHashiDefinitions(
            int widthOffset,
            int heightOffset,
            int newWidth,
            int newHeight)
        {
            EnsureHashiData();

            Vector2Int offset = new Vector2Int(widthOffset, heightOffset);

            for (int i = 0; i < fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData bridge = fixedBridges[i];
                bridge.startCoordinate += offset;
                bridge.endCoordinate += offset;
            }

            for (int i = 0; i < chainBarriers.Count; i++)
            {
                ChainBarrierData chain = chainBarriers[i];
                chain.startCoordinate += offset;
                chain.endCoordinate += offset;
            }
            for (int i = 0; i < tutorialBridges.Count; i++)
            {
                TutorialBridgeDefinitionData bridge = tutorialBridges[i];
                bridge.startCoordinate += offset;
                bridge.endCoordinate += offset;
            }

            fixedBridges.RemoveAll(bridge =>
                !IsCoordinateInside(bridge.startCoordinate, newWidth, newHeight) ||
                !IsCoordinateInside(bridge.endCoordinate, newWidth, newHeight));

            chainBarriers.RemoveAll(chain =>
                !IsCoordinateInside(chain.startCoordinate, newWidth, newHeight) ||
                !IsCoordinateInside(chain.endCoordinate, newWidth, newHeight));

            tutorialBridges.RemoveAll(bridge =>
    !IsCoordinateInside(bridge.startCoordinate, newWidth, newHeight) ||
    !IsCoordinateInside(bridge.endCoordinate, newWidth, newHeight));
        }

        private static bool IsCoordinateInside(
            Vector2Int coordinate,
            int width,
            int height)
        {
            return coordinate.x >= 0 &&
                   coordinate.y >= 0 &&
                   coordinate.x < width &&
                   coordinate.y < height;
        }

        #endregion

        #region Bottom Shooter Lane Data

        public void EnsureBottomLaneCount(int laneCount)
        {
            if (laneCount < 0) laneCount = 0;

            bottomLaneCount = laneCount;

            bottomShooterLanes ??= new List<BottomShooterLaneData>();

            while (bottomShooterLanes.Count < laneCount)
            {
                bottomShooterLanes.Add(new BottomShooterLaneData());
            }

            while (bottomShooterLanes.Count > laneCount)
            {
                bottomShooterLanes.RemoveAt(bottomShooterLanes.Count - 1);
            }

            RefreshBottomShooterIndexes();
        }

        public void RefreshBottomShooterIndexes()
        {
            if (bottomShooterLanes == null) return;

            for (int laneIndex = 0; laneIndex < bottomShooterLanes.Count; laneIndex++)
            {
                var lane = bottomShooterLanes[laneIndex];
                lane.shooters ??= new List<ShooterSpawnData>();

                for (int orderIndex = 0; orderIndex < lane.shooters.Count; orderIndex++)
                {
                    var shooter = lane.shooters[orderIndex];
                    shooter.laneIndex = laneIndex;
                    shooter.orderIndex = orderIndex;
                }
            }
        }

        public void AddShooterToLane(
              int laneIndex,
              EnumHolder.GameColor color,
              int bulletCount,
              int linkGroupId,
              bool isHidden)
        {
            EnsureBottomLaneCount(bottomLaneCount);

            if (laneIndex < 0 || laneIndex >= bottomShooterLanes.Count)
                return;

            bottomShooterLanes[laneIndex].shooters.Add(new ShooterSpawnData
            {
                color = color,
                bulletCount = bulletCount,
                linkGroupId = linkGroupId,
                isHidden = isHidden,
                laneIndex = laneIndex,
                orderIndex = bottomShooterLanes[laneIndex].shooters.Count
            });

            RefreshBottomShooterIndexes();
        }

        public void RemoveShooterFromLane(int laneIndex, int orderIndex)
        {
            if (bottomShooterLanes == null) return;
            if (laneIndex < 0 || laneIndex >= bottomShooterLanes.Count) return;

            var lane = bottomShooterLanes[laneIndex];
            if (lane.shooters == null) return;
            if (orderIndex < 0 || orderIndex >= lane.shooters.Count) return;

            lane.shooters.RemoveAt(orderIndex);
            RefreshBottomShooterIndexes();
        }

        #endregion
    }
}