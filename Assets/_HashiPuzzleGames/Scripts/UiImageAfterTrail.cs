using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UiImageAfterTrail : MonoBehaviour
{
    [Header("Trail")]
    [SerializeField] private float spawnInterval = 0.03f;
    [SerializeField] private float ghostLifeTime = 0.25f;
    [SerializeField] private float startAlpha = 0.45f;
    [SerializeField] private float endScale = 0.85f;
    [SerializeField] private float minMoveDistance = 8f;

    [Header("References")]
    [SerializeField] private Image sourceImage;
    [SerializeField] private RectTransform trailParent;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera canvasCamera;

    private Vector2 lastScreenPosition;
    private float timer;
    private bool hasLastPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (sourceImage == null)
        {
            sourceImage = GetComponent<Image>();
        }

        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null &&
            parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = parentCanvas.worldCamera;
        }

        if (trailParent == null && parentCanvas != null)
        {
            trailParent = parentCanvas.transform as RectTransform;
        }
    }

    private void OnEnable()
    {
        timer = 0f;
        hasLastPosition = false;
    }

    private void LateUpdate()
    {
        if (sourceImage == null ||
            rectTransform == null ||
            trailParent == null)
        {
            return;
        }

        Vector2 currentScreenPosition = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            rectTransform.position);

        if (!hasLastPosition)
        {
            lastScreenPosition = currentScreenPosition;
            hasLastPosition = true;
            return;
        }

        timer += Time.deltaTime;

        if (timer < spawnInterval)
        {
            return;
        }

        float distance = Vector2.Distance(
            lastScreenPosition,
            currentScreenPosition);

        if (distance < minMoveDistance)
        {
            return;
        }

        timer = 0f;
        lastScreenPosition = currentScreenPosition;

        CreateGhost(currentScreenPosition);
    }

    private void CreateGhost(Vector2 screenPosition)
    {
        GameObject ghostObject = new GameObject("Ui Trail Ghost");

        RectTransform ghostRect = ghostObject.AddComponent<RectTransform>();
        Image ghostImage = ghostObject.AddComponent<Image>();

        ghostRect.SetParent(trailParent, false);
        ghostRect.SetAsLastSibling();

        ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRect.pivot = rectTransform.pivot;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            trailParent,
            screenPosition,
            canvasCamera,
            out localPoint);

        ghostRect.anchoredPosition = localPoint;
        ghostRect.sizeDelta = GetSourceSizeInTrailParent();
        ghostRect.rotation = rectTransform.rotation;
        ghostRect.localScale = Vector3.one;

        ghostImage.sprite = sourceImage.sprite;
        ghostImage.material = sourceImage.material;
        ghostImage.type = sourceImage.type;
        ghostImage.preserveAspect = sourceImage.preserveAspect;
        ghostImage.raycastTarget = false;

        Color color = sourceImage.color;
        color.a = startAlpha;
        ghostImage.color = color;

        StartCoroutine(FadeAndDestroy(
            ghostObject,
            ghostRect,
            ghostImage,
            Vector3.one));
    }

    private Vector2 GetSourceSizeInTrailParent()
    {
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        Vector2 bottomLeft = WorldCornerToTrailLocal(worldCorners[0]);
        Vector2 topLeft = WorldCornerToTrailLocal(worldCorners[1]);
        Vector2 topRight = WorldCornerToTrailLocal(worldCorners[2]);
        Vector2 bottomRight = WorldCornerToTrailLocal(worldCorners[3]);

        float width = Vector2.Distance(bottomLeft, bottomRight);
        float height = Vector2.Distance(bottomLeft, topLeft);

        if (width <= 0.01f)
        {
            width = rectTransform.rect.width;
        }

        if (height <= 0.01f)
        {
            height = rectTransform.rect.height;
        }

        return new Vector2(width, height);
    }

    private Vector2 WorldCornerToTrailLocal(Vector3 worldPoint)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            worldPoint);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            trailParent,
            screenPoint,
            canvasCamera,
            out localPoint);

        return localPoint;
    }

    private IEnumerator FadeAndDestroy(
        GameObject ghostObject,
        RectTransform ghostRect,
        Image ghostImage,
        Vector3 startScale)
    {
        float elapsed = 0f;
        Vector3 targetScale = startScale * endScale;

        while (elapsed < ghostLifeTime)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / ghostLifeTime);

            if (ghostImage != null)
            {
                Color color = ghostImage.color;
                color.a = Mathf.Lerp(startAlpha, 0f, t);
                ghostImage.color = color;
            }

            if (ghostRect != null)
            {
                ghostRect.localScale = Vector3.Lerp(startScale, targetScale, t);
            }

            yield return null;
        }

        if (ghostObject != null)
        {
            Destroy(ghostObject);
        }
    }
}