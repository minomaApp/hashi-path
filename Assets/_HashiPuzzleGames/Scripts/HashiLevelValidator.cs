using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using UnityEngine;

namespace HashiGame.Scripts.Runtime
{
    public enum HashiValidationSeverity
    {
        Warning,
        Error
    }

    [Serializable]
    public class HashiValidationIssue
    {
        public HashiValidationSeverity severity;
        public string message;

        public HashiValidationIssue(HashiValidationSeverity issueSeverity, string issueMessage)
        {
            severity = issueSeverity;
            message = issueMessage;
        }
    }

    public class HashiValidationResult
    {
        public readonly List<HashiValidationIssue> issues = new List<HashiValidationIssue>();

        public bool HasErrors => issues.Exists(
            issue => issue.severity == HashiValidationSeverity.Error);

        public void AddError(string message)
        {
            issues.Add(new HashiValidationIssue(HashiValidationSeverity.Error, message));
        }

        public void AddWarning(string message)
        {
            issues.Add(new HashiValidationIssue(HashiValidationSeverity.Warning, message));
        }
    }

    public static class HashiLevelValidator
    {
        public static HashiValidationResult Validate(
            LevelData levelData,
            Func<Vector2Int, Vector3> coordinateToWorld = null)
        {
            HashiValidationResult result = new HashiValidationResult();

            if (levelData == null)
            {
                result.AddError("LevelData is null.");
                return result;
            }

            levelData.EnsureHashiData();
            List<IslandCellData> islands = levelData.GetIslandCells();

            if (islands.Count == 0)
            {
                result.AddError("The level must contain at least one island.");
                return result;
            }

            Dictionary<Vector2Int, IslandCellData> islandsByCoordinate =
                new Dictionary<Vector2Int, IslandCellData>();

            for (int i = 0; i < islands.Count; i++)
            {
                IslandCellData island = islands[i];
                Vector2Int coordinate = island.Coordinate;

                if (islandsByCoordinate.ContainsKey(coordinate))
                {
                    result.AddError("Duplicate island coordinate: " + coordinate);
                    continue;
                }

                islandsByCoordinate.Add(coordinate, island);

                if (island.requiredBridgeCount <= 0)
                {
                    result.AddError(
                        "Island " + coordinate + " must have a positive target value.");
                }

                if (island.startsLocked &&
                    island.unlockAfterCompletedIslandCount > islands.Count)
                {
                    result.AddError(
                        "Island " + coordinate +
                        " requires more completed islands than the level contains.");
                }
            }

            ValidateFixedBridges(
                levelData,
                islands,
                islandsByCoordinate,
                coordinateToWorld,
                result);

            ValidateChains(
                levelData,
                islandsByCoordinate,
                coordinateToWorld,
                result);

            return result;
        }

        private static void ValidateFixedBridges(
            LevelData levelData,
            List<IslandCellData> islands,
            Dictionary<Vector2Int, IslandCellData> islandsByCoordinate,
            Func<Vector2Int, Vector3> coordinateToWorld,
            HashiValidationResult result)
        {
            Dictionary<Vector2Int, int> fixedBridgeTotals =
                new Dictionary<Vector2Int, int>();

            HashSet<string> pairKeys = new HashSet<string>();

            for (int i = 0; i < levelData.fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData bridge = levelData.fixedBridges[i];

                if (bridge.startCoordinate == bridge.endCoordinate)
                {
                    result.AddError("Fixed bridge " + bridge.id + " has identical endpoints.");
                    continue;
                }

                if (!islandsByCoordinate.TryGetValue(
                        bridge.startCoordinate,
                        out IslandCellData firstIsland) ||
                    !islandsByCoordinate.TryGetValue(
                        bridge.endCoordinate,
                        out IslandCellData secondIsland))
                {
                    result.AddError(
                        "Fixed bridge " + bridge.id +
                        " must connect two existing islands.");
                    continue;
                }

                if (bridge.bridgeCount < 1 || bridge.bridgeCount > 2)
                {
                    result.AddError(
                        "Fixed bridge " + bridge.id + " count must be 1 or 2.");
                }

                if (bridge.bridgeCount == 2 &&
                    (firstIsland.bridgeMode != EnumHolder.IslandBridgeMode.DoubleAllowed ||
                     secondIsland.bridgeMode != EnumHolder.IslandBridgeMode.DoubleAllowed))
                {
                    result.AddError(
                        "Fixed bridge " + bridge.id +
                        " is double, but both islands do not allow double bridges.");
                }

                string pairKey = GetPairKey(
                    bridge.startCoordinate,
                    bridge.endCoordinate);

                if (!pairKeys.Add(pairKey))
                {
                    result.AddError(
                        "More than one fixed bridge definition uses the same island pair: " +
                        pairKey);
                }

                AddBridgeTotal(
                    fixedBridgeTotals,
                    bridge.startCoordinate,
                    bridge.bridgeCount);
                AddBridgeTotal(
                    fixedBridgeTotals,
                    bridge.endCoordinate,
                    bridge.bridgeCount);

                Vector3 startWorld = GetWorldPosition(
                    bridge.startCoordinate,
                    coordinateToWorld);
                Vector3 endWorld = GetWorldPosition(
                    bridge.endCoordinate,
                    coordinateToWorld);

                for (int islandIndex = 0; islandIndex < islands.Count; islandIndex++)
                {
                    IslandCellData otherIsland = islands[islandIndex];
                    Vector2Int otherCoordinate = otherIsland.Coordinate;

                    if (otherCoordinate == bridge.startCoordinate ||
                        otherCoordinate == bridge.endCoordinate)
                    {
                        continue;
                    }

                    float distance = BridgeGeometryUtility.DistancePointToSegment(
                        GetWorldPosition(otherCoordinate, coordinateToWorld),
                        startWorld,
                        endWorld);

                    if (distance <= levelData.hashiRules.islandBlockingRadius)
                    {
                        result.AddError(
                            "Fixed bridge " + bridge.id +
                            " passes through island " + otherCoordinate + ".");
                    }
                }
            }

            foreach (KeyValuePair<Vector2Int, int> pair in fixedBridgeTotals)
            {
                if (!islandsByCoordinate.TryGetValue(pair.Key, out IslandCellData island))
                {
                    continue;
                }

                if (pair.Value > island.requiredBridgeCount)
                {
                    result.AddError(
                        "Fixed bridges already exceed the target of island " + pair.Key + ".");
                }
            }

            for (int firstIndex = 0;
                 firstIndex < levelData.fixedBridges.Count;
                 firstIndex++)
            {
                FixedBridgeDefinitionData first = levelData.fixedBridges[firstIndex];

                for (int secondIndex = firstIndex + 1;
                     secondIndex < levelData.fixedBridges.Count;
                     secondIndex++)
                {
                    FixedBridgeDefinitionData second = levelData.fixedBridges[secondIndex];

                    if (DefinitionsShareEndpoint(first, second))
                    {
                        continue;
                    }

                    if (BridgeGeometryUtility.SegmentsIntersect(
                            GetWorldPosition(first.startCoordinate, coordinateToWorld),
                            GetWorldPosition(first.endCoordinate, coordinateToWorld),
                            GetWorldPosition(second.startCoordinate, coordinateToWorld),
                            GetWorldPosition(second.endCoordinate, coordinateToWorld)))
                    {
                        result.AddError(
                            "Fixed bridges " + first.id + " and " + second.id +
                            " cross each other.");
                    }
                }
            }
        }

        private static void ValidateChains(
            LevelData levelData,
            Dictionary<Vector2Int, IslandCellData> islandsByCoordinate,
            Func<Vector2Int, Vector3> coordinateToWorld,
            HashiValidationResult result)
        {
            HashSet<string> chainPairs = new HashSet<string>();

            for (int i = 0; i < levelData.chainBarriers.Count; i++)
            {
                ChainBarrierData chain = levelData.chainBarriers[i];

                if (!IsInsideGrid(chain.startCoordinate, levelData.Width, levelData.Height) ||
                    !IsInsideGrid(chain.endCoordinate, levelData.Width, levelData.Height))
                {
                    result.AddError(
                        "Chain " + chain.id + " has an endpoint outside the grid.");
                    continue;
                }

                if (chain.startCoordinate == chain.endCoordinate)
                {
                    result.AddError("Chain " + chain.id + " has identical endpoints.");
                }

                if (chain.unlockAfterCompletedIslandCount > islandsByCoordinate.Count)
                {
                    result.AddError(
                        "Chain " + chain.id +
                        " requires more completed islands than the level contains.");
                }

                string pairKey = GetPairKey(
                    chain.startCoordinate,
                    chain.endCoordinate);

                if (!chainPairs.Add(pairKey))
                {
                    result.AddError(
                        "More than one chain uses the same two grid points: " + pairKey);
                }

                if (chain.unlockAfterCompletedIslandCount <= 0)
                {
                    result.AddWarning(
                        "Chain " + chain.id +
                        " unlocks immediately because its requirement is zero.");
                }

                if (chain.unlockAfterCompletedIslandCount <= 0)
                {
                    continue;
                }

                Vector3 chainStart = GetWorldPosition(
                    chain.startCoordinate,
                    coordinateToWorld);
                Vector3 chainEnd = GetWorldPosition(
                    chain.endCoordinate,
                    coordinateToWorld);

                for (int bridgeIndex = 0;
                     bridgeIndex < levelData.fixedBridges.Count;
                     bridgeIndex++)
                {
                    FixedBridgeDefinitionData bridge = levelData.fixedBridges[bridgeIndex];

                    if (BridgeGeometryUtility.SegmentsIntersect(
                            chainStart,
                            chainEnd,
                            GetWorldPosition(bridge.startCoordinate, coordinateToWorld),
                            GetWorldPosition(bridge.endCoordinate, coordinateToWorld)))
                    {
                        result.AddError(
                            "Active chain " + chain.id +
                            " intersects fixed bridge " + bridge.id + ".");
                    }
                }
            }
        }

        private static bool DefinitionsShareEndpoint(
            FixedBridgeDefinitionData first,
            FixedBridgeDefinitionData second)
        {
            return first.startCoordinate == second.startCoordinate ||
                   first.startCoordinate == second.endCoordinate ||
                   first.endCoordinate == second.startCoordinate ||
                   first.endCoordinate == second.endCoordinate;
        }

        private static void AddBridgeTotal(
            Dictionary<Vector2Int, int> totals,
            Vector2Int coordinate,
            int amount)
        {
            if (!totals.ContainsKey(coordinate))
            {
                totals.Add(coordinate, 0);
            }

            totals[coordinate] += amount;
        }

        private static Vector3 GetWorldPosition(
            Vector2Int coordinate,
            Func<Vector2Int, Vector3> coordinateToWorld)
        {
            if (coordinateToWorld != null)
            {
                return coordinateToWorld(coordinate);
            }

            return new Vector3(coordinate.x, 0f, coordinate.y);
        }

        private static string GetPairKey(Vector2Int first, Vector2Int second)
        {
            bool firstComesBefore =
                first.x < second.x ||
                (first.x == second.x && first.y <= second.y);

            Vector2Int a = firstComesBefore ? first : second;
            Vector2Int b = firstComesBefore ? second : first;
            return a + " - " + b;
        }

        private static bool IsInsideGrid(
            Vector2Int coordinate,
            int width,
            int height)
        {
            return coordinate.x >= 0 &&
                   coordinate.y >= 0 &&
                   coordinate.x < width &&
                   coordinate.y < height;
        }
    }
}
