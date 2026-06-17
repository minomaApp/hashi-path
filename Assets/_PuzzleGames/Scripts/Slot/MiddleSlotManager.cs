using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;
public class MiddleSlotManager : MonoBehaviour
{
    [SerializeField] private List<MiddleSlotNode> nodes = new List<MiddleSlotNode>();

    [Header("Middle Full Warning")]
    [SerializeField] private Renderer fullWarningRenderer;
    [SerializeField] private Material fullWarningMaterial;
    [SerializeField] private int fullWarningBlinkCount = 2;
    [SerializeField] private float fullWarningBlinkDuration = 0.08f;

    private Material[] fullWarningOriginalMaterials;
    private Sequence fullWarningSequence;
    public IReadOnlyList<MiddleSlotNode> Nodes => nodes;

    public void Setup()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
            {
                nodes[i].Setup(i);
            }
        }
        CacheFullWarningOriginalMaterials();
    }

    public bool HasEmptyNode()
    {
        foreach (MiddleSlotNode node in nodes)
        {
            if (node != null && node.IsEmpty)
                return true;
        }

        return false;
    }

    public bool TryPlaceShooter(Shooter shooter)
    {
        if (shooter == null)
            return false;

        foreach (MiddleSlotNode node in nodes)
        {
            if (node == null)
                continue;

            if (node.IsEmpty)
            {
                node.SetShooter(shooter, true);
                shooter.SetState(ShooterState.IdleInMiddle);

                if (IsMiddleFull())
                {
                    PlayFullWarningFeedback();
                }

                return true;
            }
        }

        PlayFullWarningFeedback();
        return false;
    }
    public void RemoveShooter(Shooter shooter)
    {
        if (shooter == null)
            return;

        int removedIndex = -1;

        for (int i = 0; i < nodes.Count; i++)
        {
            MiddleSlotNode node = nodes[i];

            if (node == null)
                continue;

            if (node.CurrentShooter == shooter)
            {
                removedIndex = i;
                node.ClearShooter();
                break;
            }
        }

        if (removedIndex < 0)
            return;

        ShiftShootersLeftFromIndex(removedIndex);
    }

    private void ShiftShootersLeftFromIndex(int startIndex)
    {
        if (startIndex < 0)
            return;

        for (int i = startIndex; i < nodes.Count - 1; i++)
        {
            MiddleSlotNode currentNode = nodes[i];
            MiddleSlotNode nextNode = nodes[i + 1];

            if (currentNode == null || nextNode == null)
                continue;

            if (!currentNode.IsEmpty)
                continue;

            if (nextNode.IsEmpty)
                continue;

            Shooter shooterToMove = nextNode.CurrentShooter;

            nextNode.ClearShooter(false);
            currentNode.SetShooter(shooterToMove, true);
            shooterToMove.SetState(ShooterState.IdleInMiddle);
        }
    }

    private bool IsMiddleFull()
    {
        if (nodes == null || nodes.Count == 0)
            return false;

        foreach (MiddleSlotNode node in nodes)
        {
            if (node == null)
                return false;

            if (node.IsEmpty)
                return false;
        }

        return true;
    }

    private void CacheFullWarningOriginalMaterials()
    {
        if (fullWarningRenderer == null)
            return;

        if (fullWarningOriginalMaterials != null && fullWarningOriginalMaterials.Length > 0)
            return;

        fullWarningOriginalMaterials = fullWarningRenderer.materials;
    }

    private void PlayFullWarningFeedback()
    {
        if (fullWarningRenderer == null || fullWarningMaterial == null)
            return;

        CacheFullWarningOriginalMaterials();

        if (fullWarningOriginalMaterials == null || fullWarningOriginalMaterials.Length == 0)
            return;

        if (fullWarningSequence != null)
        {
            fullWarningSequence.Kill();
            fullWarningSequence = null;
        }

        Material[] warningMaterials = (Material[])fullWarningOriginalMaterials.Clone();

        // Sadece Element 0 deðiþecek.
        warningMaterials[0] = fullWarningMaterial;

        fullWarningRenderer.materials = fullWarningOriginalMaterials;

        fullWarningSequence = DOTween.Sequence();

        for (int i = 0; i < fullWarningBlinkCount; i++)
        {
            fullWarningSequence.AppendCallback(() =>
            {
                fullWarningRenderer.materials = warningMaterials;
            });

            fullWarningSequence.AppendInterval(fullWarningBlinkDuration);

            fullWarningSequence.AppendCallback(() =>
            {
                fullWarningRenderer.materials = fullWarningOriginalMaterials;
            });

            fullWarningSequence.AppendInterval(fullWarningBlinkDuration);
        }

        fullWarningSequence.OnComplete(() =>
        {
            fullWarningRenderer.materials = fullWarningOriginalMaterials;
            fullWarningSequence = null;
        });
    }
    private void OnDisable()
    {
        if (fullWarningSequence != null)
        {
            fullWarningSequence.Kill();
            fullWarningSequence = null;
        }

        if (fullWarningRenderer != null &&
            fullWarningOriginalMaterials != null &&
            fullWarningOriginalMaterials.Length > 0)
        {
            fullWarningRenderer.materials = fullWarningOriginalMaterials;
        }
    }

    public IEnumerator TryPlaceShooterRoutine(
    Shooter shooter,
    float startDelay,
    Action<bool> onComplete)
    {
        if (shooter == null)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        MiddleSlotNode targetNode = null;

        foreach (MiddleSlotNode node in nodes)
        {
            if (node == null)
                continue;

            if (node.IsEmpty)
            {
                targetNode = node;
                break;
            }
        }

        if (targetNode == null)
        {
            PlayFullWarningFeedback();
            onComplete?.Invoke(false);
            yield break;
        }

        bool moveCompleted = false;

        targetNode.SetShooter(
            shooter,
            true,
            () =>
            {
                moveCompleted = true;
            }
        );

        yield return new WaitUntil(() => moveCompleted);

        if (IsMiddleFull())
        {
            PlayFullWarningFeedback();
        }

        onComplete?.Invoke(true);
    }
}