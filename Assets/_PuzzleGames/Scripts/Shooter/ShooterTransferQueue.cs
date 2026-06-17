using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShooterTransferQueue : MonoBehaviour
{
    public static ShooterTransferQueue Instance { get; private set; }

    [SerializeField] private FlowerRouletteManager flowerRouletteManager;
    [SerializeField] private float delayBetweenTransfers = 0.3f;
    [SerializeField] private float delayBetweenLinkedShooters = 0.5f;

    private readonly Queue<ShooterTransferRequest> queue = new Queue<ShooterTransferRequest>();
    private readonly HashSet<Shooter> queuedShooters = new HashSet<Shooter>();

    private bool isProcessing;

    public float DelayBetweenLinkedShooters => delayBetweenLinkedShooters;

    private void Awake()
    {
        Instance = this;
    }

    public void ConfigureRuntimeReferences(FlowerRouletteManager runtimeFlowerRouletteManager)
    {
        flowerRouletteManager = runtimeFlowerRouletteManager;
    }

    public void Enqueue(ShooterTransferRequest request)
    {
        if (request == null)
            return;

        if (request.Shooters == null || request.Shooters.Count == 0)
            return;

        if (HasAnyQueuedShooter(request))
        {
            Debug.Log("[ShooterTransferQueue] Request rejected. Shooter already queued.");

            UnlockRequestShooters(request);
            return;
        }

        RegisterQueuedShooters(request);

        queue.Enqueue(request);

        if (!isProcessing)
        {
            StartCoroutine(ProcessQueueRoutine());
        }
    }

    private bool HasAnyQueuedShooter(ShooterTransferRequest request)
    {
        foreach (Shooter shooter in request.Shooters)
        {
            if (shooter == null)
                continue;

            if (queuedShooters.Contains(shooter))
                return true;
        }

        return false;
    }

    private void RegisterQueuedShooters(ShooterTransferRequest request)
    {
        foreach (Shooter shooter in request.Shooters)
        {
            if (shooter == null)
                continue;

            queuedShooters.Add(shooter);
        }
    }

    private void UnregisterQueuedShooters(ShooterTransferRequest request)
    {
        if (request == null || request.Shooters == null)
            return;

        foreach (Shooter shooter in request.Shooters)
        {
            if (shooter == null)
                continue;

            queuedShooters.Remove(shooter);
        }
    }

    private void UnlockRequestShooters(ShooterTransferRequest request)
    {
        if (request == null || request.Shooters == null)
            return;

        foreach (Shooter shooter in request.Shooters)
        {
            if (shooter == null)
                continue;

            shooter.UnlockTransfer();
        }
    }

    private IEnumerator ProcessQueueRoutine()
    {
        isProcessing = true;

        while (queue.Count > 0)
        {
            ShooterTransferRequest request = queue.Dequeue();

            if (flowerRouletteManager != null)
            {
                yield return flowerRouletteManager.SendShootersToFlowerRoutine(request);
            }
            else
            {
                UnlockRequestShooters(request);
            }

            UnregisterQueuedShooters(request);

            yield return new WaitForSeconds(delayBetweenTransfers);
        }

        isProcessing = false;
    }
}