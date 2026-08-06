using UnityEngine;
using UnityEngine.UI;

namespace TemplateProject.Scripts.Utilities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class RecorderFinger : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private bool startVisible = true;
        [Tooltip("Enabled: the hand is visible only while the screen/mouse is pressed. Disabled: it follows the mouse continuously and remains at the last touch position on mobile.")]
        [SerializeField] private bool showOnlyWhilePressed = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F8;

        [Header("Appearance")]
        [SerializeField] private RectTransform fingerHolder;
        [SerializeField] private RectTransform fingerImage;
        [SerializeField] private RectTransform fingerShadowImage;
        [Tooltip("Offsets the hand body so the image's fingertip stays on the real pointer position.")]
        [SerializeField] private Vector2 fingerImageOffset = new(42f, -42f);
        [SerializeField] private bool smoothMovement = true;
        [SerializeField, Min(0f)] private float followSharpness = 35f;

        [Header("Press Animation")]
        [Tooltip("How far above the shadow the hand waits before it is pressed.")]
        [SerializeField] private Vector2 raisedHandOffset = new(0f, 18f);
        [Tooltip("Fine-tunes the shadow position relative to the hand's pressed position.")]
        [SerializeField] private Vector2 shadowImageOffset = new(4f, -4f);
        [SerializeField, Min(0f)] private float pressAnimationSharpness = 22f;
        [SerializeField, Min(0.01f)] private float normalScale = 1f;
        [SerializeField, Min(0.01f)] private float pressedScale = 0.96f;
        [SerializeField, Min(0.01f)] private float raisedShadowScale = 1.06f;
        [Tooltip("Enabled: hides the shadow while pressed. Disabled: moves the shadow onto the hand while pressed, then returns it on release.")]
        [SerializeField] private bool hideShadowWhilePressed = true;

        private RectTransform _canvasRect;
        private Canvas _canvas;
        private bool _overlayEnabled;
        private bool _hasPointerPosition;
        private Vector2 _lastPointerPosition;

        private void Awake()
        {
            _canvasRect = GetComponent<RectTransform>();
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            if (fingerHolder == null && transform.childCount > 0)
                fingerHolder = transform.GetChild(0) as RectTransform;

            ResolveHandParts();

            if (fingerImage != null)
            {
                fingerImage.anchoredPosition = fingerImageOffset + raisedHandOffset;
                fingerImage.localScale = Vector3.one * normalScale;
            }

            if (fingerShadowImage != null)
            {
                fingerShadowImage.anchoredPosition = fingerImageOffset + shadowImageOffset;
                fingerShadowImage.localScale = Vector3.one * raisedShadowScale;
            }

            // This canvas is a visual overlay and must never consume gameplay/UI input.
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;

            _overlayEnabled = startVisible;
            _lastPointerPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            SetHandVisible(_overlayEnabled && !showOnlyWhilePressed);
        }

        private void Update()
        {
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                _overlayEnabled = !_overlayEnabled;
                if (!_overlayEnabled)
                    SetHandVisible(false);
            }

            if (!_overlayEnabled || fingerHolder == null)
                return;

            bool hasCurrentPointer = TryReadPointer(out Vector2 pointerPosition, out bool isPressed);
            if (hasCurrentPointer)
            {
                _lastPointerPosition = pointerPosition;
                _hasPointerPosition = true;
            }

            bool shouldShow = showOnlyWhilePressed
                ? hasCurrentPointer && isPressed
                : hasCurrentPointer || _hasPointerPosition;

            bool appearedThisFrame = shouldShow && !fingerHolder.gameObject.activeSelf;
            SetHandVisible(shouldShow);
            if (!shouldShow)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    _lastPointerPosition,
                    null,
                    out Vector2 localPosition))
                return;

            if (smoothMovement && followSharpness > 0f)
            {
                float t = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
                fingerHolder.anchoredPosition = Vector2.Lerp(fingerHolder.anchoredPosition, localPosition, t);
            }
            else
            {
                fingerHolder.anchoredPosition = localPosition;
            }

            AnimatePressState(isPressed, appearedThisFrame);
        }

        public void SetOverlayEnabled(bool isEnabled)
        {
            _overlayEnabled = isEnabled;
            if (!_overlayEnabled)
                SetHandVisible(false);
        }

        private static bool TryReadPointer(out Vector2 screenPosition, out bool isPressed)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                screenPosition = touch.position;
                isPressed = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
                return true;
            }

            if (Application.isMobilePlatform)
            {
                screenPosition = default;
                isPressed = false;
                return false;
            }

            screenPosition = Input.mousePosition;
            isPressed = Input.GetMouseButton(0);
            return true;
        }

        private void ResolveHandParts()
        {
            if (fingerHolder == null)
                return;

            for (int i = 0; i < fingerHolder.childCount; i++)
            {
                RectTransform child = fingerHolder.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                if (fingerImage == null && child.name == "FingerImage")
                    fingerImage = child;
                else if (fingerShadowImage == null && child.name == "FingerShadowImage")
                    fingerShadowImage = child;
            }
        }

        private void AnimatePressState(bool isPressed, bool appearedThisFrame)
        {
            if (appearedThisFrame && isPressed)
            {
                if (fingerImage != null)
                {
                    fingerImage.anchoredPosition = fingerImageOffset + raisedHandOffset;
                    fingerImage.localScale = Vector3.one * normalScale;
                }

                if (fingerShadowImage != null)
                    fingerShadowImage.localScale = Vector3.one * raisedShadowScale;
            }

            float animationT = pressAnimationSharpness > 0f
                ? 1f - Mathf.Exp(-pressAnimationSharpness * Time.unscaledDeltaTime)
                : 1f;

            if (fingerImage != null)
            {
                Vector2 targetPosition = fingerImageOffset + (isPressed ? Vector2.zero : raisedHandOffset);
                float targetScale = isPressed ? pressedScale : normalScale;
                fingerImage.anchoredPosition = Vector2.Lerp(fingerImage.anchoredPosition, targetPosition, animationT);
                fingerImage.localScale = Vector3.Lerp(
                    fingerImage.localScale,
                    Vector3.one * targetScale,
                    animationT);
            }

            if (fingerShadowImage != null)
            {
                bool shouldShowShadow = !hideShadowWhilePressed || !isPressed;
                if (fingerShadowImage.gameObject.activeSelf != shouldShowShadow)
                    fingerShadowImage.gameObject.SetActive(shouldShowShadow);

                if (!shouldShowShadow)
                    return;

                Vector2 targetShadowPosition = isPressed
                    ? fingerImageOffset
                    : fingerImageOffset + shadowImageOffset;
                fingerShadowImage.anchoredPosition = Vector2.Lerp(
                    fingerShadowImage.anchoredPosition,
                    targetShadowPosition,
                    animationT);

                float targetShadowScale = isPressed
                    ? pressedScale
                    : raisedShadowScale;
                fingerShadowImage.localScale = Vector3.Lerp(
                    fingerShadowImage.localScale,
                    Vector3.one * targetShadowScale,
                    animationT);
            }
        }

        private void SetHandVisible(bool isVisible)
        {
            if (fingerHolder != null && fingerHolder.gameObject.activeSelf != isVisible)
                fingerHolder.gameObject.SetActive(isVisible);
        }
    }
}
