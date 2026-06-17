using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShooterLinkGroup
{
    private readonly List<Shooter> shooters = new List<Shooter>();

    public int GroupId { get; private set; }

    public IReadOnlyList<Shooter> Shooters => shooters;

    public ShooterLinkGroup(int groupId)
    {
        GroupId = groupId;
    }

    public void AddShooter(Shooter shooter)
    {
        if (shooter == null)
            return;

        if (shooter.LinkGroupId < 0)
            return;

        if (shooters.Contains(shooter))
            return;

        if (HasShooterInSameLane(shooter))
        {
            Debug.LogWarning(
                $"[ShooterLinkGroup] Ayný lane içinde ayný linkGroupId bulundu. " +
                $"Group:{GroupId}, Lane:{shooter.LaneIndex}. Bu shooter gruba baðlanmadý."
            );

            shooter.SetLinkGroup(null);
            shooter.ClearLinkVisual();
            return;
        }

        shooters.Add(shooter);
        shooter.SetLinkGroup(this);

        RebuildLinks();
    }

    public void RemoveShooter(Shooter shooter)
    {
        if (shooter == null)
            return;

        if (!shooters.Contains(shooter))
            return;

        shooter.ClearLinkVisual();
        shooter.SetLinkGroup(null);

        shooters.Remove(shooter);

        RebuildLinks();
    }

    public void NotifyShooterDestroyed(Shooter shooter)
    {
        RemoveShooter(shooter);
    }

    public bool CanSendGroupToFlower()
    {
        List<Shooter> activeShooters = GetActiveShootersOrdered();

        if (activeShooters.Count == 0)
            return false;

        foreach (Shooter shooter in activeShooters)
        {
            if (shooter == null)
                return false;

            if (!shooter.IsSelectable)
                return false;

            if (!IsShooterAtSelectableBottomOrMiddle(shooter))
                return false;
        }

        return true;
    }

    //public void TrySendGroupToFlower(Shooter clickedShooter)
    //{
    //    if (!CanSendGroupToFlower())
    //        return;

    //    List<Shooter> activeShooters = GetActiveShootersOrdered();

    //    ShooterTransferRequest request = ShooterTransferRequest.CreateGroup(activeShooters);
    //    ShooterTransferQueue.Instance.Enqueue(request);
    //}

    public void TrySendGroupToFlower(Shooter clickedShooter)
    {
        if (!CanSendGroupToFlower())
            return;

        List<Shooter> activeShooters = GetActiveShootersOrdered();

        if (ShooterTransferQueue.Instance == null)
        {
            Debug.LogError("[ShooterLinkGroup] ShooterTransferQueue.Instance bulunamadi.");
            return;
        }

        foreach (Shooter shooter in activeShooters)
        {
            if (shooter != null)
            {
                shooter.LockTransfer();
            }
        }

        ShooterTransferRequest request = ShooterTransferRequest.CreateGroup(activeShooters);
        ShooterTransferQueue.Instance.Enqueue(request);
    }

    private bool HasShooterInSameLane(Shooter newShooter)
    {
        foreach (Shooter shooter in shooters)
        {
            if (shooter == null)
                continue;

            if (shooter.LaneIndex == newShooter.LaneIndex)
                return true;
        }

        return false;
    }

    private List<Shooter> GetActiveShootersOrdered()
    {
        shooters.RemoveAll(shooter => shooter == null || shooter.IsDestroyed);

        return shooters
            .Where(shooter => shooter != null && !shooter.IsDestroyed)
            .OrderBy(shooter => shooter.LaneIndex)
            .ToList();
    }

    private void RebuildLinks()
    {
        List<Shooter> orderedShooters = GetActiveShootersOrdered();

        foreach (Shooter shooter in orderedShooters)
        {
            if (shooter == null)
                continue;

            shooter.ClearLinkVisual();
        }

        if (orderedShooters.Count <= 1)
            return;

        for (int i = 0; i < orderedShooters.Count; i++)
        {
            Shooter current = orderedShooters[i];

            if (current == null)
                continue;

            if (i >= orderedShooters.Count - 1)
            {
                current.ClearLinkVisual();
                continue;
            }

            Shooter next = orderedShooters[i + 1];

            current.SetLinkVisualTarget(next);
        }
    }

    private bool IsShooterAtSelectableBottomOrMiddle(Shooter shooter)
    {
        if (shooter.CurrentMiddleNode != null)
            return true;

        if (shooter.CurrentBottomNode != null)
        {
            return shooter.CurrentBottomNode.NodeIndex == 0;
        }

        return false;
    }
}