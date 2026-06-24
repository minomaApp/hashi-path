using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.SO;
using UnityEngine;
using UnityEngine.Events;

namespace HashiGame.Scripts.Runtime
{
    public class BridgeBoardManager : MonoBehaviour
    {
        [Header("Optional Defaults")]
        [SerializeField] private GamePrefabs defaultPrefabs;
        [SerializeField] private HashiVisualSettings defaultVisualSettings;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> onCompletedIslandMilestoneChanged;

        [Header("Cut Settings")]
        [SerializeField] private float cutDetectionThickness = 0.18f;

        private readonly Dictionary<Vector2Int, IslandNode> islandsByCoordinate =
            new Dictionary<Vector2Int, IslandNode>();

        private readonly Dictionary<IslandPairKey, BridgeConnection> connectionsByPair =
            new Dictionary<IslandPairKey, BridgeConnection>();

        private readonly List<ChainBarrier> chainBarriers = new List<ChainBarrier>();
        private readonly HashSet<Vector2Int> everCompletedIslands = new HashSet<Vector2Int>();

        private LevelData levelData;
        private LevelContainer levelContainer;
        private GamePrefabs prefabs;
        private HashiVisualSettings visualSettings;
        private global::GameManager gameManager;
        private bool isSetup;
        private bool hasWon;

        public bool IsSetup => isSetup;
        public bool HasWon => hasWon;
        public int CompletedIslandMilestoneCount => everCompletedIslands.Count;
        public IReadOnlyCollection<IslandNode> Islands => islandsByCoordinate.Values;
        public IReadOnlyCollection<BridgeConnection> Connections => connectionsByPair.Values;
        public IReadOnlyList<ChainBarrier> ChainBarriers => chainBarriers;
        public HashiVisualSettings VisualSettings => visualSettings;

        public bool Setup(
            LevelData newLevelData,
            LevelContainer newLevelContainer,
            GamePrefabs newPrefabs,
            HashiVisualSettings newVisualSettings,
            global::GameManager newGameManager)
        {
            if (newLevelData == null || newLevelContainer == null)
            {
                Debug.LogError("[BridgeBoardManager] LevelData or LevelContainer is null.");
                return false;
            }

            levelData = newLevelData;
            levelContainer = newLevelContainer;
            prefabs = newPrefabs != null ? newPrefabs : defaultPrefabs;
            visualSettings = newVisualSettings != null ? newVisualSettings : defaultVisualSettings;
            gameManager = newGameManager;
            hasWon = false;
            isSetup = false;

            levelData.EnsureHashiData();
            RemoveOldDynamicBridgeObjects();
            ClearRuntimeCollections();

            if (!RegisterIslands())
            {
                return false;
            }

            RegisterGeneratedFixedBridges();
            CreateMissingFixedBridgesFromData();
            RegisterGeneratedChains();
            CreateMissingChainsFromData();

            isSetup = true;
            RefreshBoardState();

            Debug.Log(
                "[BridgeBoardManager] Setup complete. Islands: " + islandsByCoordinate.Count +
                ", fixed bridges: " + connectionsByPair.Count +
                ", chains: " + chainBarriers.Count);

            return true;
        }

        public IslandNode GetIsland(Vector2Int coordinate)
        {
            islandsByCoordinate.TryGetValue(coordinate, out IslandNode island);
            return island;
        }

        public bool CanCycleConnection(
            IslandNode firstIsland,
            IslandNode secondIsland,
            out string reason)
        {
            reason = string.Empty;

            if (!isSetup)
            {
                reason = "Board is not ready.";
                return false;
            }

            if (firstIsland == null || secondIsland == null)
            {
                reason = "Both endpoints must be islands.";
                return false;
            }

            if (firstIsland == secondIsland)
            {
                reason = "An island cannot connect to itself.";
                return false;
            }

            if (firstIsland.IsLocked || secondIsland.IsLocked)
            {
                reason = "A locked island cannot be used.";
                return false;
            }

            IslandPairKey key = new IslandPairKey(
                firstIsland.Coordinate,
                secondIsland.Coordinate);

            if (connectionsByPair.TryGetValue(key, out BridgeConnection existingConnection))
            {
                if (existingConnection == null)
                {
                    reason = "Bridge data is invalid.";
                    return false;
                }

                if (existingConnection.IsFixed)
                {
                    reason = "A fixed bridge cannot be changed.";
                    return false;
                }

                int maximumCount = firstIsland.GetMaximumBridgeCountWith(secondIsland);

                if (existingConnection.BridgeCount >= maximumCount)
                {
                    reason = "Bridge is already at maximum. Cut it to remove it.";
                    return false;
                }

                return true;
            }

            return CanCreateNewConnection(firstIsland, secondIsland, out reason);
        }

        public bool TryCycleConnection(
            IslandNode firstIsland,
            IslandNode secondIsland,
            out string reason)
        {
            if (!CanCycleConnection(firstIsland, secondIsland, out reason))
            {
                return false;
            }

            IslandPairKey key = new IslandPairKey(
                firstIsland.Coordinate,
                secondIsland.Coordinate);

            if (!connectionsByPair.TryGetValue(key, out BridgeConnection connection))
            {
                connection = CreateBridgeConnection(
                    firstIsland,
                    secondIsland,
                    1,
                    false);

                if (connection == null)
                {
                    reason = "Bridge object could not be created.";
                    return false;
                }
            }
            else
            {
                int maximumCount = firstIsland.GetMaximumBridgeCountWith(secondIsland);
                int nextCount = Mathf.Min(connection.BridgeCount + 1, maximumCount);
                connection.SetBridgeCount(nextCount, visualSettings);
            }

            RefreshBoardState();
            return true;
        }

        public bool TryCutConnection(
            Vector3 cutStartPoint,
            Vector3 cutEndPoint,
            out string reason)
        {
            reason = string.Empty;

            if (!isSetup)
            {
                reason = "Board is not ready.";
                return false;
            }

            Vector3 flatCut = cutEndPoint - cutStartPoint;
            flatCut.y = 0f;

            if (flatCut.magnitude <= 0.001f)
            {
                reason = "Cut gesture is too short.";
                return false;
            }

            BridgeConnection bestConnection = null;
            float bestDistance = float.MaxValue;
            Vector3 cutMidPoint = (cutStartPoint + cutEndPoint) * 0.5f;

            foreach (BridgeConnection connection in connectionsByPair.Values)
            {
                if (connection == null)
                {
                    continue;
                }

                if (connection.IsFixed)
                {
                    continue;
                }

                if (connection.BridgeCount <= 0)
                {
                    continue;
                }

                float segmentDistance = BridgeGeometryUtility.DistanceSegmentToSegment(
                    cutStartPoint,
                    cutEndPoint,
                    connection.StartWorldPosition,
                    connection.EndWorldPosition);

                if (segmentDistance > cutDetectionThickness)
                {
                    continue;
                }

                float midDistance = BridgeGeometryUtility.DistancePointToSegment(
                    cutMidPoint,
                    connection.StartWorldPosition,
                    connection.EndWorldPosition);

                if (midDistance < bestDistance)
                {
                    bestDistance = midDistance;
                    bestConnection = connection;
                }
            }

            if (bestConnection == null)
            {
                reason = "No bridge was cut.";
                return false;
            }

            CutBridgeConnection(bestConnection);
            RefreshBoardState();
            return true;
        }

        public bool TryCutConnectionAtPoint(
            Vector3 cutPoint,
            out string reason)
        {
            reason = string.Empty;

            if (!isSetup)
            {
                reason = "Board is not ready.";
                return false;
            }

            BridgeConnection bestConnection = null;
            float bestDistance = float.MaxValue;

            foreach (BridgeConnection connection in connectionsByPair.Values)
            {
                if (connection == null)
                {
                    continue;
                }

                if (connection.IsFixed)
                {
                    continue;
                }

                if (connection.BridgeCount <= 0)
                {
                    continue;
                }

                float distance = BridgeGeometryUtility.DistancePointToSegment(
                    cutPoint,
                    connection.StartWorldPosition,
                    connection.EndWorldPosition);

                if (distance > cutDetectionThickness)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestConnection = connection;
                }
            }

            if (bestConnection == null)
            {
                reason = "No bridge was cut.";
                return false;
            }

            CutBridgeConnection(bestConnection);
            RefreshBoardState();
            return true;
        }

        public bool HasConnection(IslandNode firstIsland, IslandNode secondIsland)
        {
            if (firstIsland == null || secondIsland == null)
            {
                return false;
            }

            IslandPairKey key = new IslandPairKey(
                firstIsland.Coordinate,
                secondIsland.Coordinate);

            return connectionsByPair.ContainsKey(key);
        }

        private void CutBridgeConnection(BridgeConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            if (connection.IsFixed)
            {
                return;
            }

            if (connection.BridgeCount > 1)
            {
                connection.SetBridgeCount(
                    connection.BridgeCount - 1,
                    visualSettings);
            }
            else
            {
                RemoveConnection(connection);
            }
        }

        private bool RegisterIslands()
        {
            IReadOnlyList<IslandNode> generatedIslands = levelContainer.GeneratedIslands;

            if (generatedIslands != null)
            {
                for (int i = 0; i < generatedIslands.Count; i++)
                {
                    IslandNode island = generatedIslands[i];
                    RegisterIsland(island);
                }
            }

            if (islandsByCoordinate.Count == 0)
            {
                IslandNode[] foundIslands = levelContainer.GetComponentsInChildren<IslandNode>(true);
                for (int i = 0; i < foundIslands.Length; i++)
                {
                    RegisterIsland(foundIslands[i]);
                }
            }

            if (islandsByCoordinate.Count == 0)
            {
                Debug.LogError("[BridgeBoardManager] No IslandNode was found in the level prefab.");
                return false;
            }

            return true;
        }

        private void RegisterIsland(IslandNode island)
        {
            if (island == null)
            {
                return;
            }

            if (islandsByCoordinate.ContainsKey(island.Coordinate))
            {
                Debug.LogError(
                    "[BridgeBoardManager] Duplicate island coordinate: " + island.Coordinate);
                return;
            }

            island.PrepareRuntime(visualSettings);
            islandsByCoordinate.Add(island.Coordinate, island);
        }

        private void RegisterGeneratedFixedBridges()
        {
            IReadOnlyList<BridgeConnection> generatedBridges =
                levelContainer.GeneratedFixedBridges;

            if (generatedBridges == null)
            {
                return;
            }

            for (int i = 0; i < generatedBridges.Count; i++)
            {
                BridgeConnection connection = generatedBridges[i];

                if (connection == null)
                {
                    continue;
                }

                IslandNode firstIsland = GetIsland(connection.StartCoordinate);
                IslandNode secondIsland = GetIsland(connection.EndCoordinate);

                if (firstIsland == null || secondIsland == null)
                {
                    Debug.LogError(
                        "[BridgeBoardManager] Fixed bridge endpoint island is missing.");
                    continue;
                }

                if (!connection.BindRuntime(firstIsland, secondIsland, visualSettings))
                {
                    continue;
                }

                RegisterConnectionInternal(connection);
            }
        }

        private void CreateMissingFixedBridgesFromData()
        {
            if (levelData.fixedBridges == null)
            {
                return;
            }

            for (int i = 0; i < levelData.fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData data = levelData.fixedBridges[i];
                IslandPairKey key = new IslandPairKey(
                    data.startCoordinate,
                    data.endCoordinate);

                if (connectionsByPair.ContainsKey(key))
                {
                    continue;
                }

                IslandNode firstIsland = GetIsland(data.startCoordinate);
                IslandNode secondIsland = GetIsland(data.endCoordinate);

                if (firstIsland == null || secondIsland == null)
                {
                    continue;
                }

                CreateBridgeConnection(
                    firstIsland,
                    secondIsland,
                    data.bridgeCount,
                    true);
            }
        }

        private void RegisterGeneratedChains()
        {
            IReadOnlyList<ChainBarrier> generatedChains =
                levelContainer.GeneratedChains;

            if (generatedChains == null)
            {
                return;
            }

            for (int i = 0; i < generatedChains.Count; i++)
            {
                ChainBarrier chain = generatedChains[i];

                if (chain == null)
                {
                    continue;
                }

                chain.PrepareRuntime(visualSettings);
                chainBarriers.Add(chain);
            }
        }

        private void CreateMissingChainsFromData()
        {
            if (levelData.chainBarriers == null)
            {
                return;
            }

            for (int i = 0; i < levelData.chainBarriers.Count; i++)
            {
                ChainBarrierData data = levelData.chainBarriers[i];

                bool alreadyExists = chainBarriers.Exists(
                    chain => chain != null && chain.BarrierId == data.id);

                if (alreadyExists)
                {
                    continue;
                }

                CreateChainBarrier(data);
            }
        }

        private BridgeConnection CreateBridgeConnection(
            IslandNode firstIsland,
            IslandNode secondIsland,
            int bridgeCount,
            bool fixedBridge)
        {
            if (firstIsland == null || secondIsland == null)
            {
                return null;
            }

            GameObject bridgeObject;

            if (prefabs != null && prefabs.bridgePrefab != null)
            {
                bridgeObject = Instantiate(
                    prefabs.bridgePrefab,
                    levelContainer.BridgeParentTransform);
            }
            else
            {
                bridgeObject = new GameObject("Bridge Runtime Fallback");
                bridgeObject.transform.SetParent(
                    levelContainer.BridgeParentTransform,
                    false);
                bridgeObject.AddComponent<BridgeVisual>();

                Debug.LogWarning(
                    "[BridgeBoardManager] bridgePrefab is missing. A visual fallback object was created.");
            }

            BridgeVisual visual = bridgeObject.GetComponent<BridgeVisual>();
            if (visual == null)
            {
                visual = bridgeObject.AddComponent<BridgeVisual>();
            }

            BridgeConnection connection = bridgeObject.GetComponent<BridgeConnection>();
            if (connection == null)
            {
                connection = bridgeObject.AddComponent<BridgeConnection>();
            }

            bridgeObject.name = fixedBridge
                ? "FixedBridge_" + firstIsland.Coordinate + "_" + secondIsland.Coordinate
                : "Bridge_" + firstIsland.Coordinate + "_" + secondIsland.Coordinate;

            connection.ConfigureLevelData(
                firstIsland,
                secondIsland,
                bridgeCount,
                fixedBridge,
                visualSettings);

            if (!RegisterConnectionInternal(connection))
            {
                DestroyObject(bridgeObject);
                return null;
            }

            return connection;
        }

        private bool RegisterConnectionInternal(BridgeConnection connection)
        {
            if (connection == null ||
                connection.StartIsland == null ||
                connection.EndIsland == null)
            {
                return false;
            }

            IslandPairKey key = new IslandPairKey(
                connection.StartCoordinate,
                connection.EndCoordinate);

            if (connectionsByPair.ContainsKey(key))
            {
                Debug.LogError(
                    "[BridgeBoardManager] Duplicate bridge pair: " +
                    connection.StartCoordinate + " - " + connection.EndCoordinate);
                return false;
            }

            connectionsByPair.Add(key, connection);
            connection.StartIsland.RegisterConnection(connection);
            connection.EndIsland.RegisterConnection(connection);
            return true;
        }

        private void RemoveConnection(BridgeConnection connection)
        {
            if (connection == null || connection.IsFixed)
            {
                return;
            }

            IslandPairKey key = new IslandPairKey(
                connection.StartCoordinate,
                connection.EndCoordinate);

            connectionsByPair.Remove(key);

            if (connection.StartIsland != null)
            {
                connection.StartIsland.UnregisterConnection(connection);
            }

            if (connection.EndIsland != null)
            {
                connection.EndIsland.UnregisterConnection(connection);
            }

            DestroyObject(connection.gameObject);
        }

        private void CreateChainBarrier(ChainBarrierData data)
        {
            if (data == null)
            {
                return;
            }

            Vector3 firstWorldPosition =
                levelContainer.GridCoordinateToWorld(data.startCoordinate);
            Vector3 secondWorldPosition =
                levelContainer.GridCoordinateToWorld(data.endCoordinate);

            GameObject chainObject;

            if (prefabs != null && prefabs.chainBarrierPrefab != null)
            {
                chainObject = Instantiate(
                    prefabs.chainBarrierPrefab,
                    levelContainer.ChainParentTransform);
            }
            else
            {
                chainObject = new GameObject("Chain Runtime Fallback");
                chainObject.transform.SetParent(
                    levelContainer.ChainParentTransform,
                    false);

                Debug.LogWarning(
                    "[BridgeBoardManager] chainBarrierPrefab is missing. A fallback object was created.");
            }

            ChainBarrier chain = chainObject.GetComponent<ChainBarrier>();
            if (chain == null)
            {
                chain = chainObject.AddComponent<ChainBarrier>();
            }

            chainObject.name = "Chain_" + data.id;
            chain.ConfigureLevelData(
                data,
                firstWorldPosition,
                secondWorldPosition,
                visualSettings);

            chainBarriers.Add(chain);
        }

        private bool CanCreateNewConnection(
            IslandNode firstIsland,
            IslandNode secondIsland,
            out string reason)
        {
            reason = string.Empty;

            Vector3 startPoint = firstIsland.ConnectionPosition;
            Vector3 endPoint = secondIsland.ConnectionPosition;

            if (levelData.hashiRules.blockBridgeThroughIsland)
            {
                foreach (IslandNode island in islandsByCoordinate.Values)
                {
                    if (island == firstIsland || island == secondIsland)
                    {
                        continue;
                    }

                    float blockRadius = Mathf.Max(
                        levelData.hashiRules.islandBlockingRadius,
                        island.ConnectionBlockRadius);

                    float distance = BridgeGeometryUtility.DistancePointToSegment(
                        island.ConnectionPosition,
                        startPoint,
                        endPoint);

                    if (distance <= blockRadius)
                    {
                        reason = "Another island is between the selected islands.";
                        return false;
                    }
                }
            }

            if (levelData.hashiRules.blockBridgeCrossing)
            {
                foreach (BridgeConnection connection in connectionsByPair.Values)
                {
                    if (connection == null)
                    {
                        continue;
                    }

                    bool sharesEndpoint =
                        connection.StartIsland == firstIsland ||
                        connection.StartIsland == secondIsland ||
                        connection.EndIsland == firstIsland ||
                        connection.EndIsland == secondIsland;

                    if (sharesEndpoint)
                    {
                        continue;
                    }

                    if (BridgeGeometryUtility.SegmentsIntersect(
                            startPoint,
                            endPoint,
                            connection.StartWorldPosition,
                            connection.EndWorldPosition))
                    {
                        reason = "The bridge would cross another bridge.";
                        return false;
                    }
                }
            }

            for (int i = 0; i < chainBarriers.Count; i++)
            {
                ChainBarrier chain = chainBarriers[i];

                if (chain == null || !chain.IsBlocking)
                {
                    continue;
                }

                float distance = BridgeGeometryUtility.DistanceSegmentToSegment(
                    startPoint,
                    endPoint,
                    chain.StartWorldPosition,
                    chain.EndWorldPosition);

                if (distance <= chain.BlockingThickness)
                {
                    reason = "An active chain blocks this bridge.";
                    return false;
                }
            }

            return true;
        }

        private void RefreshBoardState()
        {
            if (!isSetup)
            {
                return;
            }

            int previousMilestoneCount = everCompletedIslands.Count;

            foreach (IslandNode island in islandsByCoordinate.Values)
            {
                int bridgeCount = island.CalculateConnectedBridgeCount();
                island.SetCurrentBridgeCount(bridgeCount);

                if (island.IsCompleted)
                {
                    everCompletedIslands.Add(island.Coordinate);
                }
            }

            int milestoneCount = everCompletedIslands.Count;

            foreach (IslandNode island in islandsByCoordinate.Values)
            {
                island.TryUnlock(milestoneCount);
            }

            for (int i = 0; i < chainBarriers.Count; i++)
            {
                if (chainBarriers[i] != null)
                {
                    chainBarriers[i].RefreshRequirementText(milestoneCount);
                    chainBarriers[i].TryUnlock(milestoneCount);
                }
            }

            if (milestoneCount != previousMilestoneCount)
            {
                onCompletedIslandMilestoneChanged?.Invoke(milestoneCount);
            }

            if (!hasWon && AreAllWinConditionsMet())
            {
                hasWon = true;
                gameManager?.GameWin();
            }
        }

        private bool AreAllWinConditionsMet()
        {
            if (islandsByCoordinate.Count == 0)
            {
                return false;
            }

            foreach (IslandNode island in islandsByCoordinate.Values)
            {
                if (!island.IsCompleted)
                {
                    return false;
                }
            }

            if (levelData.hashiRules.requireAllIslandsConnected)
            {
                return AreAllIslandsConnected();
            }

            return true;
        }

        private bool AreAllIslandsConnected()
        {
            IslandNode firstIsland = null;

            foreach (IslandNode island in islandsByCoordinate.Values)
            {
                firstIsland = island;
                break;
            }

            if (firstIsland == null)
            {
                return false;
            }

            HashSet<IslandNode> visited = new HashSet<IslandNode>();
            Queue<IslandNode> queue = new Queue<IslandNode>();

            visited.Add(firstIsland);
            queue.Enqueue(firstIsland);

            while (queue.Count > 0)
            {
                IslandNode current = queue.Dequeue();
                IReadOnlyList<BridgeConnection> connections = current.Connections;

                for (int i = 0; i < connections.Count; i++)
                {
                    BridgeConnection connection = connections[i];

                    if (connection == null || connection.BridgeCount <= 0)
                    {
                        continue;
                    }

                    IslandNode next = connection.StartIsland == current
                        ? connection.EndIsland
                        : connection.StartIsland;

                    if (next != null && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited.Count == islandsByCoordinate.Count;
        }

        private void RemoveOldDynamicBridgeObjects()
        {
            if (levelContainer == null ||
                levelContainer.BridgeParentTransform == null)
            {
                return;
            }

            BridgeConnection[] existingConnections =
                levelContainer.BridgeParentTransform
                    .GetComponentsInChildren<BridgeConnection>(true);

            for (int i = 0; i < existingConnections.Length; i++)
            {
                BridgeConnection connection = existingConnections[i];

                if (connection != null && !connection.IsFixed)
                {
                    DestroyObject(connection.gameObject);
                }
            }
        }

        private void ClearRuntimeCollections()
        {
            islandsByCoordinate.Clear();
            connectionsByPair.Clear();
            chainBarriers.Clear();
            everCompletedIslands.Clear();
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private readonly struct IslandPairKey : IEquatable<IslandPairKey>
        {
            private readonly Vector2Int first;
            private readonly Vector2Int second;

            public IslandPairKey(Vector2Int a, Vector2Int b)
            {
                if (ComesBefore(a, b))
                {
                    first = a;
                    second = b;
                }
                else
                {
                    first = b;
                    second = a;
                }
            }

            public bool Equals(IslandPairKey other)
            {
                return first == other.first && second == other.second;
            }

            public override bool Equals(object obj)
            {
                return obj is IslandPairKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (first.GetHashCode() * 397) ^ second.GetHashCode();
                }
            }

            private static bool ComesBefore(Vector2Int a, Vector2Int b)
            {
                return a.x < b.x || (a.x == b.x && a.y <= b.y);
            }
        }
    }
}