using DG.Tweening;
using UnityEngine;

public class FlowerPetal : MonoBehaviour
{
    [SerializeField] private int petalIndex;

    [Header("Scale Settings")]
    [SerializeField] private float attachedScale = 2.5f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Jump Settings")]
    [SerializeField] private float jumpPowerToShooter = 0.1f;
    [SerializeField] private float jumpPowerToFlower = 0.5f;
    [SerializeField] private int jumpCount = 1;
    [SerializeField] private float jumpDuration = 0.25f;
    [SerializeField] private Ease jumpEase = Ease.OutQuad;

    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;
    private Vector3 defaultLocalScale;
    private Transform defaultParent;

    public Shooter CurrentShooter { get; private set; }

    public int PetalIndex => petalIndex;
    //public bool IsEmpty => CurrentShooter == null;

    private bool isReleasing;

    public bool IsReleasing => isReleasing;
    public bool IsEmpty => CurrentShooter == null && !isReleasing;

    private void Awake()
    {
        CacheDefaultTransform();
    }

    public void Setup(int index)
    {
        petalIndex = index;
        CacheDefaultTransform();
    }

    private void CacheDefaultTransform()
    {
        defaultParent = transform.parent;
        defaultLocalPosition = transform.localPosition;
        defaultLocalRotation = transform.localRotation;
        defaultLocalScale = Vector3.one * normalScale;
    }

    public void AttachToShooter(Shooter shooter, Transform shooterBottomPoint)
    {
        isReleasing = false;
        CurrentShooter = shooter;

        if (shooterBottomPoint == null)
            return;

        transform.DOKill();

        Vector3 startWorldPosition = transform.position;
        Quaternion startWorldRotation = transform.rotation;

        transform.SetParent(shooterBottomPoint, true);
        transform.position = startWorldPosition;
        transform.rotation = startWorldRotation;

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            transform
                .DOJump(shooterBottomPoint.position, jumpPowerToShooter, jumpCount, jumpDuration)
                .SetEase(jumpEase)
        );

        sequence.Join(
            transform
                .DOScale(Vector3.one * attachedScale, scaleDuration)
                .SetEase(scaleEase)
        );

        sequence.OnComplete(() =>
        {
            transform.SetParent(shooterBottomPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * attachedScale;
        });
    }

    public void Release(System.Action onComplete = null)
    {
        if (defaultParent == null)
            return;

        isReleasing = true;
        CurrentShooter = null;

        transform.DOKill();

        Vector3 startWorldPosition = transform.position;
        Quaternion startWorldRotation = transform.rotation;

        Vector3 targetWorldPosition = defaultParent.TransformPoint(defaultLocalPosition);
        Quaternion targetWorldRotation = defaultParent.rotation * defaultLocalRotation;

        transform.SetParent(defaultParent, true);
        transform.position = startWorldPosition;
        transform.rotation = startWorldRotation;

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            transform
                .DOJump(targetWorldPosition, jumpPowerToFlower, jumpCount, jumpDuration)
                .SetEase(jumpEase)
        );

        sequence.Join(
            transform
                .DOScale(Vector3.one * normalScale, scaleDuration)
                .SetEase(Ease.OutQuad)
        );

        sequence.OnComplete(() =>
        {
            transform.SetParent(defaultParent, false);
            transform.localPosition = defaultLocalPosition;
            transform.localRotation = defaultLocalRotation;
            transform.localScale = Vector3.one * normalScale;

            isReleasing = false;

            onComplete?.Invoke();
        });
    }
    public void AttachToShooterInstant(Shooter shooter, Transform shooterBottomPoint)
    {
        isReleasing = false;
        CurrentShooter = shooter;

        if (shooterBottomPoint == null)
            return;

        transform.DOKill();

        transform.SetParent(shooterBottomPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * normalScale;

        transform
            .DOScale(Vector3.one * attachedScale, scaleDuration)
            .SetEase(scaleEase);
    }
}