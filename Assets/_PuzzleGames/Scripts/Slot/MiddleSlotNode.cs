using DG.Tweening;
using UnityEngine;
using System;
public class MiddleSlotNode : MonoBehaviour
{
    [SerializeField] private int nodeIndex;

    [Header("Move Animation")]
    [SerializeField] private float jumpPower = 3f;
    [SerializeField] private int jumpCount = 1;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float flipAngle = 360f;
    [SerializeField] private Ease rotateEase = Ease.OutQuad;
    public Shooter CurrentShooter { get; private set; }

    public int NodeIndex => nodeIndex;
    public bool IsEmpty => CurrentShooter == null;

    public void Setup(int index)
    {
        nodeIndex = index;
    }

    public void SetShooter(Shooter shooter, bool animate = true, Action onComplete = null)
    {
        CurrentShooter = shooter;

        if (shooter == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (animate)
        {
            shooter.LockTransfer();
            MoveShooterAnimated(shooter, onComplete);
        }
        else
        {
            shooter.transform.position = transform.position;
            shooter.transform.rotation = transform.rotation;
            shooter.SetMiddleNode(this);
            onComplete?.Invoke();
        }
    }

    private void MoveShooterAnimated(Shooter shooter, Action onComplete = null)
    {
        Transform shooterTransform = shooter.transform;

        shooter.ForceResetScaleAnimations(false);

        shooterTransform.DOKill(false);
        shooter.PlayJumpArmAnimation(moveDuration);

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            shooterTransform
                .DOJump(transform.position, jumpPower, jumpCount, moveDuration)
                .SetEase(Ease.OutQuad)
        );

        sequence.Join(
            shooterTransform
                .DORotateQuaternion(transform.rotation, moveDuration)
                .SetEase(rotateEase)
        );

        sequence.OnComplete(() =>
        {
            shooterTransform.position = transform.position;
            shooterTransform.rotation = transform.rotation;

            shooter.ResetArmPose();
            shooter.ForceResetScaleAnimations(true);

            shooter.SetMiddleNode(this);

            onComplete?.Invoke();
        });
    }

    public void ClearShooter(bool clearPlacement = true)
    {
        if (CurrentShooter != null && clearPlacement)
        {
            CurrentShooter.ClearPlacement();
        }

        CurrentShooter = null;
    }
}