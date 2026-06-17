using System;
using DG.Tweening;
using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.12f;
    [SerializeField] private Ease moveEase = Ease.Linear;

    private BulletPool pool;
    private Action onImpact;

    public void SetupPool(BulletPool ownerPool)
    {
        pool = ownerPool;
    }

    public void Fire(
        Vector3 startPosition,
        Vector3 targetPosition,
        Action impactCallback)
    {
        transform.DOKill();

        onImpact = impactCallback;

        transform.position = startPosition;
        transform.LookAt(targetPosition);

        transform.DOMove(targetPosition, moveDuration)
            .SetEase(moveEase)
            .OnComplete(Impact);
    }

    private void Impact()
    {
        onImpact?.Invoke();
        onImpact = null;

        if (pool != null)
        {
            pool.ReturnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}