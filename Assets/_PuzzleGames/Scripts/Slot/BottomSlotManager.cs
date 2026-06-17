using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.SO;
using BoxPuller.Scripts.Runtime.LevelCreation;
using UnityEngine;

public class BottomSlotManager : MonoBehaviour
{
    [SerializeField] private List<BottomSlotLane> lanes = new List<BottomSlotLane>();

    private readonly Dictionary<int, ShooterLinkGroup> linkGroups = new Dictionary<int, ShooterLinkGroup>();

    public IReadOnlyList<BottomSlotLane> Lanes => lanes;

    [Header("Visual")]
    [SerializeField] private GameColors gameColors;
    public void Setup(LevelData levelData, LevelContainer levelContainer, GamePrefabs prefabs)
    {
        Debug.Log("[BottomSlotManager] Setup called");

        if (levelData == null)
        {
            Debug.LogError("[BottomSlotManager] LevelData null.");
            return;
        }

        if (levelContainer == null)
        {
            Debug.LogError("[BottomSlotManager] LevelContainer null.");
            return;
        }

        if (prefabs == null || prefabs.shooterPrefab == null)
        {
            Debug.LogError("[BottomSlotManager] Shooter prefab missing.");
            return;
        }

        levelData.EnsureBottomLaneCount(levelData.bottomLaneCount);
        levelData.RefreshBottomShooterIndexes();

        Debug.Log($"[BottomSlotManager] Runtime LevelData bottomLaneCount: {levelData.bottomLaneCount}");
        Debug.Log($"[BottomSlotManager] Runtime LevelData bottomShooterLanes Count: {levelData.bottomShooterLanes.Count}");
        Debug.Log($"[BottomSlotManager] LevelContainer GeneratedBottomNodes: {levelContainer.GeneratedBottomNodes.Count}");
        Debug.Log($"[BottomSlotManager] LevelContainer GeneratedShooters: {levelContainer.GeneratedShooters.Count}");

        linkGroups.Clear();

        int laneCount = levelData.bottomLaneCount;

        EnsureLaneCount(laneCount);

        for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
        {
            List<BottomSlotNode> laneNodes = GetRuntimeBottomNodes(levelContainer, laneIndex);
            List<Shooter> initialShooters = GetRuntimeShooters(levelContainer, laneIndex);

            Debug.Log(
                $"[BottomSlotManager] Lane {laneIndex} " +
                $"Nodes:{laneNodes.Count}, InitialShooters:{initialShooters.Count}"
            );

            if (laneIndex >= levelData.bottomShooterLanes.Count)
            {
                Debug.LogWarning($"[BottomSlotManager] LevelData içinde lane yok: {laneIndex}");
                continue;
            }

            BottomShooterLaneData laneData = levelData.bottomShooterLanes[laneIndex];

            int alreadyVisibleCount = initialShooters.Count;

            List<ShooterSpawnData> waitingData = laneData.shooters
                .OrderBy(data => data.orderIndex)
                .Skip(alreadyVisibleCount)
                .ToList();

            Debug.Log(
    $"[BottomSlotManager Me me me WAITING CHECK] Lane:{laneIndex} " +
    $"DataShooters:{laneData.shooters.Count} " +
    $"InitialVisible:{alreadyVisibleCount} " +
    $"Waiting:{waitingData.Count}"
);
            lanes[laneIndex].SetupRuntime(
                laneIndex,
                laneNodes,
                initialShooters,
                waitingData,
                prefabs.shooterPrefab,
                this,
                gameColors
            );
        }
    }

    private List<BottomSlotNode> GetRuntimeBottomNodes(LevelContainer levelContainer, int laneIndex)
    {
        List<BottomSlotNode> result = new List<BottomSlotNode>();

        foreach (GameObject nodeObject in levelContainer.GeneratedBottomNodes)
        {
            if (nodeObject == null)
                continue;

            GeneratedLevelItem generatedItem = nodeObject.GetComponent<GeneratedLevelItem>();

            if (generatedItem == null)
                continue;

            if (generatedItem.itemType != GeneratedLevelItemType.BottomSlotNode)
                continue;

            if (generatedItem.laneIndex != laneIndex)
                continue;

            BottomSlotNode node = nodeObject.GetComponent<BottomSlotNode>();

            if (node == null)
            {
                node = nodeObject.AddComponent<BottomSlotNode>();
            }

            node.Setup(generatedItem.laneIndex, generatedItem.orderIndex);

            result.Add(node);
        }

        return result
            .OrderBy(node => node.NodeIndex)
            .ToList();
    }

    private List<Shooter> GetRuntimeShooters(LevelContainer levelContainer, int laneIndex)
    {
        List<Shooter> result = new List<Shooter>();

        foreach (GameObject shooterObject in levelContainer.GeneratedShooters)
        {
            if (shooterObject == null)
                continue;

            GeneratedLevelItem generatedItem = shooterObject.GetComponent<GeneratedLevelItem>();

            if (generatedItem == null)
                continue;

            if (generatedItem.itemType != GeneratedLevelItemType.Shooter)
                continue;

            if (generatedItem.laneIndex != laneIndex)
                continue;

            Shooter shooter = shooterObject.GetComponent<Shooter>();

            if (shooter == null)
            {
                shooter = shooterObject.AddComponent<Shooter>();
            }

            ShooterSpawnData shooterData = new ShooterSpawnData
            {
                color = generatedItem.color,
                bulletCount = generatedItem.bulletCount,
                laneIndex = generatedItem.laneIndex,
                orderIndex = generatedItem.orderIndex,
                linkGroupId = generatedItem.linkGroupId,
                isHidden = generatedItem.isHidden
            };

            shooter.Setup(shooterData, gameColors);

            Debug.Log(
                  $"[Runtime Shooter Setup] Obj:{shooterObject.name} " +
                  $"Lane:{generatedItem.laneIndex} Order:{generatedItem.orderIndex} " +
                  $"Color:{generatedItem.color} Bullet:{generatedItem.bulletCount} " +
                  $"Hidden:{generatedItem.isHidden}"
              );
            result.Add(shooter);
        }

        return result
            .OrderBy(shooter => shooter.OrderIndex)
            .ToList();
    }

    private void EnsureLaneCount(int count)
    {
        while (lanes.Count < count)
        {
            GameObject laneObject = new GameObject($"Runtime Bottom Lane {lanes.Count}");
            laneObject.transform.SetParent(transform);
            BottomSlotLane lane = laneObject.AddComponent<BottomSlotLane>();
            lanes.Add(lane);
        }

        while (lanes.Count > count)
        {
            BottomSlotLane lastLane = lanes[lanes.Count - 1];

            if (lastLane != null)
            {
                Destroy(lastLane.gameObject);
            }

            lanes.RemoveAt(lanes.Count - 1);
        }
    }

    public void RegisterShooterToLinkGroup(Shooter shooter)
    {
        if (shooter == null)
            return;

        if (shooter.LinkGroupId < 0)
            return;

        if (!linkGroups.ContainsKey(shooter.LinkGroupId))
        {
            linkGroups.Add(shooter.LinkGroupId, new ShooterLinkGroup(shooter.LinkGroupId));
        }

        linkGroups[shooter.LinkGroupId].AddShooter(shooter);
    }

    public void NotifyShooterRemoved(Shooter shooter)
    {
        if (shooter == null)
            return;

        int laneIndex = shooter.CurrentBottomNode != null
            ? shooter.CurrentBottomNode.LaneIndex
            : shooter.LaneIndex;

        Debug.Log(
            $"[BottomSlotManager] NotifyShooterRemoved Shooter:{shooter.name} Lane:{laneIndex}"
        );

        if (laneIndex < 0 || laneIndex >= lanes.Count)
        {
            Debug.LogError(
                $"[BottomSlotManager] Invalid laneIndex:{laneIndex}. Lanes Count:{lanes.Count}"
            );
            return;
        }

        lanes[laneIndex].RemoveShooter(shooter);
    }
}