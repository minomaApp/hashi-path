using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using DG.Tweening;
using UnityEngine;

public class Box : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EnumHolder.GameColor color;
    [SerializeField] private int gridX;
    [SerializeField] private int gridY;
    [SerializeField] private int moldGroupId = -1;

    [Header("Runtime")]
    [SerializeField] private bool isHit;
    [SerializeField] private bool isReserved;
    [SerializeField] private bool isValidMoldLinked;
    [SerializeField] private BoxGridManager gridManager;

    [Header("Visual")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Collider boxCollider;

    [Tooltip("Boþ býrakýlýrsa önce Cube Mesh Renderer kullanýlýr. O da boþsa Visual Root altýndaki rendererlar bulunur.")]
    [SerializeField] private Renderer[] colorRenderers;

    [Header("Mold Visuals")]
    [SerializeField] private GameObject moldObject;
    [SerializeField] private GameObject rightConnectorObject;

    [Header("Cube Visual")]
    [SerializeField] private MeshRenderer cubeMeshRenderer;

    [Header("Hit Effect")]
    [SerializeField] private Transform hitParticlePoint;

    [Header("Impact Shake")]
    [SerializeField] private Transform shakeRoot;
    [SerializeField] private float shakeReturnDurationMultiplier = 0.65f;

    private Vector3 shakeRootDefaultLocalPosition;
    public EnumHolder.GameColor Color => color;
    public int GridX => gridX;
    public int GridY => gridY;
    public int MoldGroupId => moldGroupId;

    public bool IsHit => isHit;
    public bool IsReserved => isReserved;

    public bool HasRawMoldId => moldGroupId >= 0;

    // Sadece id var diye mold saymýyoruz.
    // Ayný id'li kutular gerçekten yan yana ise true oluyor.
    public bool HasMold => isValidMoldLinked;

    private void Awake()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<Collider>();
        }

        if (visualRoot == null)
        {
            visualRoot = gameObject;
        }

        if (cubeMeshRenderer == null)
        {
            cubeMeshRenderer = GetComponentInChildren<MeshRenderer>(true);
        }

        ResolveShakeRoot();
        CacheShakeDefaultPosition();
        CacheRenderersIfNeeded();
    }

    public void Setup(BoxSpawnData data)
    {
        color = data.color;
        gridX = data.x;
        gridY = data.y;
        moldGroupId = data.moldGroupId;
    }

    public void SetupRuntime(
        EnumHolder.GameColor runtimeColor,
        int runtimeGridX,
        int runtimeGridY,
        int runtimeMoldGroupId,
        BoxGridManager runtimeGridManager,
        GameColors gameColors)
    {

        color = runtimeColor;
        gridX = runtimeGridX;
        gridY = runtimeGridY;
        moldGroupId = runtimeMoldGroupId;
        gridManager = runtimeGridManager;

        isHit = false;
        isReserved = false;
        isValidMoldLinked = false;
        Debug.Log($"[MeMe Box SetupRuntime] {name} X:{gridX} Y:{gridY} MoldId:{moldGroupId} HasMold:{HasMold} MoldObject:{(moldObject != null ? moldObject.name : "NULL")}");
        SetVisualActive(true);
        SetCubeVisible(true);
        SetMoldVisual(false, false);
        ResetImpactShake();
        ApplyMaterial(gameColors);

        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
    }

    public void SetValidMoldLinked(bool value)
    {
        isValidMoldLinked = value;
    }

    public void SetMoldVisual(bool showMold, bool showRightConnector)
    {
        if (moldObject != null)
        {
            moldObject.SetActive(showMold);
        }

        if (rightConnectorObject != null)
        {
            rightConnectorObject.SetActive(showRightConnector);
        }
    }

    public void ApplyMaterial(GameColors gameColors)
    {
        if (gameColors == null)
        {
            Debug.LogWarning($"[Box] GameColors null. Material atanmadý. Box: {name}");
            return;
        }

        if (gameColors.boxMaterials == null)
        {
            Debug.LogWarning("[Box] GameColors.boxMaterials null.");
            return;
        }

        int colorIndex = (int)color;

        if (colorIndex < 0 || colorIndex >= gameColors.boxMaterials.Length)
        {
            Debug.LogWarning($"[Box] Box material index out of range: {colorIndex}. Color: {color}");
            return;
        }

        Material targetMaterial = gameColors.boxMaterials[colorIndex];

        if (targetMaterial == null)
        {
            Debug.LogWarning($"[Box] Box material null. Color: {color}, Index: {colorIndex}");
            return;
        }

        CacheRenderersIfNeeded();

        foreach (Renderer renderer in colorRenderers)
        {
            if (renderer == null)
                continue;

            renderer.sharedMaterial = targetMaterial;
        }
    }

    private void CacheRenderersIfNeeded()
    {
        if (colorRenderers != null && colorRenderers.Length > 0)
            return;

        if (cubeMeshRenderer != null)
        {
            colorRenderers = new Renderer[] { cubeMeshRenderer };
            return;
        }

        if (visualRoot != null)
        {
            colorRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            colorRenderers = GetComponentsInChildren<Renderer>(true);
        }
    }

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }

    public bool ReserveHit()
    {
        if (isHit || isReserved)
            return false;

        isReserved = true;
        return true;
    }

    public void ClearReservation()
    {
        isReserved = false;
    }

    public void MarkHit()
    {
        if (isHit)
            return;

        isHit = true;
        isReserved = false;
    }

    public void HideCubeButKeepMold()
    {
        MarkHit();
        SetCubeVisible(false);

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }

    public void RemoveCompletely()
    {
        MarkHit();

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        Destroy(gameObject);
    }

    public Vector3 GetBulletTargetPosition()
    {
        if (boxCollider != null)
        {
            return boxCollider.bounds.center;
        }

        if (cubeMeshRenderer != null)
        {
            return cubeMeshRenderer.bounds.center;
        }

        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            return renderer.bounds.center;
        }

        return transform.position;
    }

    private void SetCubeVisible(bool value)
    {
        if (cubeMeshRenderer != null)
        {
            cubeMeshRenderer.enabled = value;
            return;
        }

        if (visualRoot == null)
            return;

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.enabled = value;
        }
    }

    private void SetVisualActive(bool value)
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(value);
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = value;
        }
    }

    public Vector3 GetHitParticlePosition(Vector3 fallbackPosition)
    {
        if (hitParticlePoint != null)
        {
            return hitParticlePoint.position;
        }

        if (fallbackPosition != Vector3.zero)
        {
            return fallbackPosition;
        }

        return GetBulletTargetPosition();
    }

    private void ResolveShakeRoot()
    {
        if (shakeRoot != null)
            return;

        if (cubeMeshRenderer != null)
        {
            shakeRoot = cubeMeshRenderer.transform;
            return;
        }

        if (visualRoot != null)
        {
            shakeRoot = visualRoot.transform;
            return;
        }

        shakeRoot = transform;
    }

    private void CacheShakeDefaultPosition()
    {
        if (shakeRoot == null)
            return;

        shakeRootDefaultLocalPosition = shakeRoot.localPosition;
    }

    private void ResetImpactShake()
    {
        if (shakeRoot == null)
        {
            ResolveShakeRoot();
        }

        if (shakeRoot == null)
            return;

        shakeRoot.DOKill();
        shakeRoot.localPosition = shakeRootDefaultLocalPosition;
    }

    public void PlayNeighborImpactShake(
        Vector3 impactWorldPosition,
        float pushDistance,
        float duration,
        Ease ease)
    {
        if (shakeRoot == null)
        {
            ResolveShakeRoot();
            CacheShakeDefaultPosition();
        }

        if (shakeRoot == null)
            return;

        if (duration <= 0f)
            return;

        Vector3 direction = transform.position - impactWorldPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.right;
        }

        direction.Normalize();

        Vector3 worldOffset = direction * pushDistance;

        Vector3 localOffset = shakeRoot.parent != null
            ? shakeRoot.parent.InverseTransformVector(worldOffset)
            : worldOffset;

        shakeRoot.DOKill();
        shakeRoot.localPosition = shakeRootDefaultLocalPosition;

        float pushDuration = duration * (1f - shakeReturnDurationMultiplier);
        float returnDuration = duration * shakeReturnDurationMultiplier;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            shakeRoot
                .DOLocalMove(shakeRootDefaultLocalPosition + localOffset, pushDuration)
                .SetEase(ease)
        );

        sequence.Append(
            shakeRoot
                .DOLocalMove(shakeRootDefaultLocalPosition, returnDuration)
                .SetEase(Ease.OutBack)
        );

        sequence.OnComplete(() =>
        {
            if (shakeRoot != null)
            {
                shakeRoot.localPosition = shakeRootDefaultLocalPosition;
            }
        });
    }
}