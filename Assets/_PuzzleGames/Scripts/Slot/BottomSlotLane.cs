using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data;
using UnityEngine;

public class BottomSlotLane : MonoBehaviour
{
    [Header("Lane Info")]
    [SerializeField] private int laneIndex;

    [Header("Nodes")]
    [SerializeField] private List<BottomSlotNode> nodes = new List<BottomSlotNode>();

    private readonly Queue<ShooterSpawnData> waitingShooterData = new Queue<ShooterSpawnData>();

    private GameObject shooterPrefab;
    private BottomSlotManager manager;

    public int LaneIndex => laneIndex;
    public IReadOnlyList<BottomSlotNode> Nodes => nodes;

    private GameColors gameColors;
    public void SetupRuntime(
        int index,
        List<BottomSlotNode> runtimeNodes,
        List<Shooter> initialShooters,
        IEnumerable<ShooterSpawnData> waitingData,
        GameObject runtimeShooterPrefab,
        BottomSlotManager owner,
        GameColors runtimeGameColors)
    {
        Debug.Log($"[MeMe BottomSlotLane CHECK] Lane:{laneIndex} Nodes:{nodes.Count} InitialShooters:{initialShooters.Count}");
        laneIndex = index;
        manager = owner;
        shooterPrefab = runtimeShooterPrefab;
        gameColors = runtimeGameColors;

        nodes = runtimeNodes
            .Where(node => node != null)
            .OrderBy(node => node.NodeIndex)
            .ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Setup(laneIndex, i);
        }

        waitingShooterData.Clear();

        if (waitingData != null)
        {
            foreach (ShooterSpawnData data in waitingData)
            {
                waitingShooterData.Enqueue(data);
            }
        }

        List<Shooter> sortedInitialShooters = initialShooters
            .Where(shooter => shooter != null)
            .OrderBy(shooter => shooter.OrderIndex)
            .ToList();

        int count = Mathf.Min(nodes.Count, sortedInitialShooters.Count);

        for (int i = 0; i < count; i++)
        {
            nodes[i].SetShooter(sortedInitialShooters[i]);
            manager.RegisterShooterToLinkGroup(sortedInitialShooters[i]);
        }

        RefreshSelectableStates();
    }

    public void RemoveShooter(Shooter shooter)
    {
        if (shooter == null)
            return;

        foreach (BottomSlotNode node in nodes)
        {
            if (node.CurrentShooter == shooter)
            {
                node.ClearShooter();
                OnShooterRemovedFromLane();
                return;
            }
        }
    }

    private void OnShooterRemovedFromLane()
    {
        ShiftShootersForward();
        FillBackNodeIfPossible();
        RefreshSelectableStates();
    }

    private void ShiftShootersForward()
    {
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            BottomSlotNode currentNode = nodes[i];
            BottomSlotNode nextNode = nodes[i + 1];

            if (currentNode.IsEmpty && !nextNode.IsEmpty)
            {
                Shooter shooter = nextNode.CurrentShooter;

                nextNode.ClearShooter();
                currentNode.SetShooter(shooter, true);
            }
        }
    }

    private void FillBackNodeIfPossible()
    {
        Debug.Log($"[BottomSlotLane] FillBack Lane:{laneIndex} Waiting:{waitingShooterData.Count}");

        if (nodes.Count == 0)
            return;

        BottomSlotNode backNode = nodes[nodes.Count - 1];

        Debug.Log($"[BottomSlotLane] BackNode Empty:{backNode.IsEmpty}");

        if (!backNode.IsEmpty)
            return;

        if (waitingShooterData.Count <= 0)
            return;

        SpawnNextShooterToNode(backNode);
    }

    private void SpawnNextShooterToNode(BottomSlotNode node)
    {
        if (waitingShooterData.Count <= 0)
            return;

        if (shooterPrefab == null)
        {
            Debug.LogError($"[BottomSlotLane] Shooter prefab missing in lane {laneIndex}");
            return;
        }

        ShooterSpawnData data = waitingShooterData.Dequeue();
        data.laneIndex = laneIndex;
        GameObject shooterObject = Instantiate(
            shooterPrefab,
            node.transform.position,
            node.transform.rotation,
            node.transform.parent
        );

        Shooter shooter = shooterObject.GetComponent<Shooter>();

        if (shooter == null)
        {
            Debug.LogError("[BottomSlotLane] Spawn edilen shooter prefabýnda Shooter component yok.");
            Destroy(shooterObject);
            return;
        }

        shooter.Setup(data, gameColors);
        //node.SetShooter(shooter);
        node.SetShooter(shooter, true);
        manager.RegisterShooterToLinkGroup(shooter);
    }

    private void RefreshSelectableStates()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Shooter shooter = nodes[i].CurrentShooter;

            if (shooter == null)
                continue;

            if (i == 0)
            {
                shooter.SetState(ShooterState.IdleInBottom);
            }
            else
            {
                shooter.SetState(ShooterState.Locked);
            }
        }
    }
}