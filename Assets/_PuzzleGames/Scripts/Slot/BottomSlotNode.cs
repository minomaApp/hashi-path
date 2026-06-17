using DG.Tweening;
using UnityEngine;

public class BottomSlotNode : MonoBehaviour
{
    [SerializeField] private int laneIndex;
    [SerializeField] private int nodeIndex;

    [Header("Move Animation")]
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private int jumpCount = 1;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float flipAngle = 360f;

    public Shooter CurrentShooter { get; private set; }

    public int LaneIndex => laneIndex;
    public int NodeIndex => nodeIndex;
    public bool IsEmpty => CurrentShooter == null;

    public void Setup(int lane, int node)
    {
        laneIndex = lane;
        nodeIndex = node;
    }

    public void SetShooter(Shooter shooter, bool animate = false)
    {
        CurrentShooter = shooter;

        if (shooter == null)
            return;

        Debug.Log($"[BottomSlotNode] SetShooter Lane:{laneIndex} Node:{nodeIndex} Shooter:{shooter.name}");

        shooter.transform.SetParent(transform.parent, true);

        if (animate)
        {
            MoveShooterAnimated(shooter);
        }
        else
        {
            shooter.transform.position = transform.position;
            shooter.transform.rotation = transform.rotation;
        }

        shooter.SetBottomNode(this);
    }

    //private void MoveShooterAnimated(Shooter shooter)
    //{
    //    Transform shooterTransform = shooter.transform;

    //    shooterTransform.DOKill();

    //    Sequence sequence = DOTween.Sequence();

    //    sequence.Join(
    //        shooterTransform
    //            .DOJump(transform.position, jumpPower, jumpCount, moveDuration)
    //            .SetEase(Ease.OutQuad)
    //    );

    //    sequence.Join(
    //        shooterTransform
    //            .DORotate(
    //                shooterTransform.eulerAngles + new Vector3(flipAngle, 0f, 0f),
    //                moveDuration,
    //                RotateMode.FastBeyond360
    //            )
    //    );

    //    sequence.OnComplete(() =>
    //    {
    //        shooterTransform.position = transform.position;
    //        shooterTransform.rotation = transform.rotation;
    //    });
    //}

    private void MoveShooterAnimated(Shooter shooter)
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

        sequence.OnComplete(() =>
        {
            shooterTransform.position = transform.position;
            shooterTransform.rotation = transform.rotation;
            shooter.ResetArmPose();
            shooter.ForceResetScaleAnimations(true);
        });
    }
    public void ClearShooter()
    {
        if (CurrentShooter != null)
        {
            CurrentShooter.ClearPlacement();
        }

        CurrentShooter = null;
    }
}