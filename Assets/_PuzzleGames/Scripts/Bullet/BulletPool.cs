using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private BulletProjectile bulletPrefab;
    [SerializeField] private int initialPoolSize = 20;

    private readonly Queue<BulletProjectile> pool = new Queue<BulletProjectile>();

    private void Awake()
    {
        Instance = this;
        CreateInitialPool();
    }

    private void CreateInitialPool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("[BulletPool] Bullet prefab missing.");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            BulletProjectile bullet = CreateBullet();
            ReturnToPool(bullet);
        }
    }

    private BulletProjectile CreateBullet()
    {
        BulletProjectile bullet = Instantiate(bulletPrefab, transform);
        bullet.SetupPool(this);
        return bullet;
    }

    public BulletProjectile GetBullet()
    {
        BulletProjectile bullet = pool.Count > 0 ? pool.Dequeue() : CreateBullet();

        bullet.gameObject.SetActive(true);
        return bullet;
    }

    public void ReturnToPool(BulletProjectile bullet)
    {
        if (bullet == null)
            return;

        bullet.gameObject.SetActive(false);
        bullet.transform.SetParent(transform);
        pool.Enqueue(bullet);
    }
}