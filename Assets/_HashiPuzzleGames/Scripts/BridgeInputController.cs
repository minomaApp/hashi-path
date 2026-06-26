using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TemplateProject.Scripts.Runtime.Managers;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HashiGame.Scripts.Runtime
{
    [Serializable]
    public class HashiStringUnityEvent : UnityEvent<string>
    {
    }

    public class BridgeInputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private BridgeBoardManager boardManager;
        [SerializeField] private BridgePreviewController previewController;

        [Header("Raycast")]
        [SerializeField] private LayerMask islandLayerMask = ~0;
        [SerializeField] private float raycastDistance = 500f;

        [Header("Feedback")]
        [SerializeField] private float invalidPreviewDuration = 0.18f;
        [SerializeField] private bool useBasicDeviceVibration;
        [SerializeField] private HashiStringUnityEvent onInvalidMove;

        [Header("Cut Gesture")]
        [SerializeField] private float cutPlaneHeight = 0f;
        [SerializeField] private float minimumCutScreenDistance = 25f;
        [SerializeField] private float minimumCutWorldDistance = 0.25f;
        [SerializeField] private bool showCutPreview = true;

        [Header("Cut Trail")]
        [SerializeField] private TrailRenderer cutTrailRenderer;
        [SerializeField] private float cutTrailVerticalOffset = 0.25f;
        [SerializeField] private float cutTrailDisableDelay = 0.2f;

        [SerializeField] private float cutTrailFollowSpeed = 35f;
        private Vector3 smoothedCutTrailPosition;
        private bool hasSmoothedCutTrailPosition;

        private IslandNode dragStartIsland;
        private Coroutine invalidPreviewCoroutine;
        private bool inputEnabled;
        private bool isDragging;

        private bool isCutting;
        private Vector3 cutStartWorldPoint;
        private Vector3 lastCutWorldPoint;
        private Vector2 cutStartScreenPosition;
        private bool cutCompletedThisGesture;
        private Coroutine cutTrailDisableCoroutine;

        public void Setup(BridgeBoardManager newBoardManager)
        {
            boardManager = newBoardManager;

            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }

            if (previewController == null)
            {
                previewController = FindFirstObjectByType<BridgePreviewController>();
            }

            if (previewController != null)
            {
                previewController.Setup(
                    boardManager != null ? boardManager.VisualSettings : null);
            }

            inputEnabled = boardManager != null && boardManager.IsSetup;
            CancelDrag();
        }

        public void SetInputEnabled(bool value)
        {
            inputEnabled = value;

            if (!inputEnabled)
            {
                CancelDrag();
            }
        }

        private void Update()
        {
            if (!inputEnabled || boardManager == null || !boardManager.IsSetup)
            {
                return;
            }

            if (!TryReadPointerFrame(out PointerFrame pointerFrame))
            {
                return;
            }

            if (pointerFrame.pressedThisFrame)
            {
                HandlePointerPressed(pointerFrame);
            }

            if (pointerFrame.isPressed && isDragging)
            {
                HandlePointerDragged(pointerFrame.screenPosition);
            }

            if (pointerFrame.isPressed && isCutting)
            {
                HandleCutDragged(pointerFrame.screenPosition);
            }

            if (pointerFrame.releasedThisFrame)
            {
                if (isDragging)
                {
                    HandlePointerReleased(pointerFrame.screenPosition);
                }
                else if (isCutting)
                {
                    HandleCutReleased(pointerFrame.screenPosition);
                }
            }
        }

        private void HandlePointerPressed(PointerFrame pointerFrame)
        {
            if (IsPointerOverUi(pointerFrame.pointerId))
            {
                return;
            }

            if (!TryRaycastIsland(pointerFrame.screenPosition, out IslandNode island))
            {
                StartTimerOnFirstGameplayInput();
                StartCutGesture(pointerFrame.screenPosition);
                return;
            }

            //if (island.IsLocked)
            //{
            //    ShowInvalidFeedback(
            //        island.ConnectionPosition,
            //        island.ConnectionPosition,
            //        "This island is locked.");
            //    return;
            //}

            if (island.IsLocked)
            {
                island.PlayLockedShake();

                ShowInvalidFeedback(
                    island.ConnectionPosition,
                    island.ConnectionPosition,
                    "This island is locked.");
                return;
            }

            StartTimerOnFirstGameplayInput();

            dragStartIsland = island;
            isDragging = true;
            isCutting = false;
            cutCompletedThisGesture = false;

            if (previewController != null)
            {
                previewController.Show(
                    dragStartIsland.ConnectionPosition,
                    dragStartIsland.ConnectionPosition,
                    true);
            }
        }

        private void StartTimerOnFirstGameplayInput()
        {
            if (UIManager.instance == null)
            {
                return;
            }

            if (TimeManager.instance == null)
            {
                return;
            }

            if (TimeManager.instance.HasTimerStarted())
            {
                return;
            }

            UIManager.instance.HandleTimer();
        }

        private void HandlePointerDragged(Vector2 screenPosition)
        {
            if (dragStartIsland == null)
            {
                CancelDrag();
                return;
            }

            IslandNode hoveredIsland = null;
            Vector3 endPoint = GetPointerWorldPoint(
                screenPosition,
                dragStartIsland.ConnectionPosition.y);

            bool isValid = false;

            if (TryRaycastIsland(screenPosition, out hoveredIsland))
            {
                endPoint = hoveredIsland.ConnectionPosition;

                if (hoveredIsland != dragStartIsland)
                {
                    isValid = boardManager.CanCycleConnection(
                        dragStartIsland,
                        hoveredIsland,
                        out _);
                }
            }

            if (previewController != null)
            {
                previewController.Show(
                    dragStartIsland.ConnectionPosition,
                    endPoint,
                    isValid);
            }
        }

        private void HandlePointerReleased(Vector2 screenPosition)
        {
            IslandNode startIsland = dragStartIsland;
            isDragging = false;
            dragStartIsland = null;

            if (startIsland == null)
            {
                HidePreview();
                return;
            }

            if (!TryRaycastIsland(screenPosition, out IslandNode endIsland) ||
                endIsland == startIsland)
            {
                HidePreview();
                return;
            }

            bool success = boardManager.TryCycleConnection(
                startIsland,
                endIsland,
                out string reason);

            if (success)
            {
                HidePreview();
                return;
            }

            ShowInvalidFeedback(
                startIsland.ConnectionPosition,
                endIsland.ConnectionPosition,
                reason);
        }

        private void StartCutGesture(Vector2 screenPosition)
        {
            isCutting = true;
            isDragging = false;
            dragStartIsland = null;
            cutCompletedThisGesture = false;

            cutStartScreenPosition = screenPosition;
            cutStartWorldPoint = GetPointerWorldPoint(
                screenPosition,
                cutPlaneHeight);

            lastCutWorldPoint = cutStartWorldPoint;
            StartCutTrail(cutStartWorldPoint);

            if (showCutPreview && previewController != null)
            {
                previewController.Show(
                    cutStartWorldPoint,
                    cutStartWorldPoint,
                    false);
            }
        }

        private void HandleCutDragged(Vector2 screenPosition)
        {
            Vector3 currentWorldPoint = GetPointerWorldPoint(
                screenPosition,
                cutPlaneHeight);

            UpdateCutTrail(currentWorldPoint);

            if (showCutPreview && previewController != null)
            {
                previewController.Show(
                    cutStartWorldPoint,
                    currentWorldPoint,
                    false);
            }

            float screenDistance = Vector2.Distance(
                cutStartScreenPosition,
                screenPosition);

            Vector3 totalWorldDistance = currentWorldPoint - cutStartWorldPoint;
            totalWorldDistance.y = 0f;

            if (screenDistance < minimumCutScreenDistance ||
                totalWorldDistance.magnitude < minimumCutWorldDistance)
            {
                lastCutWorldPoint = currentWorldPoint;
                return;
            }

            bool success = boardManager.TryCutConnection(
                lastCutWorldPoint,
                currentWorldPoint,
                out _);

            lastCutWorldPoint = currentWorldPoint;

            if (!success)
            {
                return;
            }

            HidePreview();
        }

        private void HandleCutReleased(Vector2 screenPosition)
        {
            isCutting = false;
            cutCompletedThisGesture = false;
            HidePreview();
            StopCutTrail(false);
        }

        private bool TryRaycastIsland(Vector2 screenPosition, out IslandNode island)
        {
            island = null;

            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }

            if (inputCamera == null)
            {
                return false;
            }

            Ray ray = inputCamera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    raycastDistance,
                    islandLayerMask,
                    QueryTriggerInteraction.Collide))
            {
                return false;
            }

            island = hit.collider.GetComponentInParent<IslandNode>();
            return island != null;
        }

        private Vector3 GetPointerWorldPoint(Vector2 screenPosition, float planeHeight)
        {
            if (inputCamera == null)
            {
                return Vector3.zero;
            }

            Plane plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
            Ray ray = inputCamera.ScreenPointToRay(screenPosition);

            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return ray.origin + ray.direction * 10f;
        }

        private void ShowInvalidFeedback(
            Vector3 startPoint,
            Vector3 endPoint,
            string reason)
        {
            if (invalidPreviewCoroutine != null)
            {
                StopCoroutine(invalidPreviewCoroutine);
            }

            if (previewController != null)
            {
                previewController.Show(startPoint, endPoint, false);
                invalidPreviewCoroutine = StartCoroutine(HideInvalidPreviewAfterDelay());
            }

            if (useBasicDeviceVibration)
            {
                Handheld.Vibrate();
            }

            onInvalidMove?.Invoke(reason ?? string.Empty);
        }

        private IEnumerator HideInvalidPreviewAfterDelay()
        {
            yield return new WaitForSeconds(invalidPreviewDuration);
            invalidPreviewCoroutine = null;
            HidePreview();
        }

        private void HidePreview()
        {
            if (previewController != null)
            {
                previewController.Hide();
            }
        }

        private void CancelDrag()
        {
            isDragging = false;
            isCutting = false;
            cutCompletedThisGesture = false;
            dragStartIsland = null;

            if (invalidPreviewCoroutine != null)
            {
                StopCoroutine(invalidPreviewCoroutine);
                invalidPreviewCoroutine = null;
            }

            HidePreview();
            StopCutTrail(true);
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private void StartCutTrail(Vector3 worldPoint)
        {
            if (cutTrailRenderer == null)
            {
                return;
            }

            if (cutTrailDisableCoroutine != null)
            {
                StopCoroutine(cutTrailDisableCoroutine);
                cutTrailDisableCoroutine = null;
            }

            cutTrailRenderer.gameObject.SetActive(true);
            cutTrailRenderer.transform.position = GetCutTrailPosition(worldPoint);
            smoothedCutTrailPosition = GetCutTrailPosition(worldPoint);
            hasSmoothedCutTrailPosition = true;

            cutTrailRenderer.Clear();
            cutTrailRenderer.emitting = true;
        }

        private void UpdateCutTrail(Vector3 worldPoint)
        {
            if (cutTrailRenderer == null)
            {
                return;
            }

            if (!cutTrailRenderer.gameObject.activeSelf)
            {
                cutTrailRenderer.gameObject.SetActive(true);
            }

            Vector3 targetPosition = GetCutTrailPosition(worldPoint);

            if (!hasSmoothedCutTrailPosition)
            {
                smoothedCutTrailPosition = targetPosition;
                hasSmoothedCutTrailPosition = true;
            }

            float t = 1f - Mathf.Exp(-cutTrailFollowSpeed * Time.deltaTime);
            smoothedCutTrailPosition = Vector3.Lerp(
                smoothedCutTrailPosition,
                targetPosition,
                t);

            cutTrailRenderer.transform.position = smoothedCutTrailPosition;
        }

        private void StopCutTrail(bool clearImmediately)
        {
            if (cutTrailRenderer == null)
            {
                return;
            }

            cutTrailRenderer.emitting = false;
            hasSmoothedCutTrailPosition = false;
            if (cutTrailDisableCoroutine != null)
            {
                StopCoroutine(cutTrailDisableCoroutine);
                cutTrailDisableCoroutine = null;
            }

            if (clearImmediately)
            {
                cutTrailRenderer.Clear();
                cutTrailRenderer.gameObject.SetActive(false);
                return;
            }

            float delay = Mathf.Max(cutTrailDisableDelay, cutTrailRenderer.time);
            cutTrailDisableCoroutine = StartCoroutine(DisableCutTrailAfterDelay(delay));
        }

        private IEnumerator DisableCutTrailAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (cutTrailRenderer != null)
            {
                cutTrailRenderer.Clear();
                cutTrailRenderer.gameObject.SetActive(false);
            }

            cutTrailDisableCoroutine = null;
        }

        private Vector3 GetCutTrailPosition(Vector3 worldPoint)
        {
            worldPoint.y += cutTrailVerticalOffset;
            return worldPoint;
        }

        private bool TryReadPointerFrame(out PointerFrame frame)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                UnityEngine.InputSystem.Controls.TouchControl touch =
                    Touchscreen.current.primaryTouch;

                bool pressed = touch.press.isPressed;
                bool pressedThisFrame = touch.press.wasPressedThisFrame;
                bool releasedThisFrame = touch.press.wasReleasedThisFrame;

                if (pressed || pressedThisFrame || releasedThisFrame)
                {
                    frame = new PointerFrame
                    {
                        screenPosition = touch.position.ReadValue(),
                        pointerId = touch.touchId.ReadValue(),
                        isPressed = pressed,
                        pressedThisFrame = pressedThisFrame,
                        releasedThisFrame = releasedThisFrame
                    };
                    return true;
                }
            }

            if (Mouse.current != null)
            {
                frame = new PointerFrame
                {
                    screenPosition = Mouse.current.position.ReadValue(),
                    pointerId = -1,
                    isPressed = Mouse.current.leftButton.isPressed,
                    pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame,
                    releasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame
                };

                return frame.isPressed ||
                       frame.pressedThisFrame ||
                       frame.releasedThisFrame;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                frame = new PointerFrame
                {
                    screenPosition = touch.position,
                    pointerId = touch.fingerId,
                    isPressed = touch.phase == TouchPhase.Began ||
                                touch.phase == TouchPhase.Moved ||
                                touch.phase == TouchPhase.Stationary,
                    pressedThisFrame = touch.phase == TouchPhase.Began,
                    releasedThisFrame = touch.phase == TouchPhase.Ended ||
                                        touch.phase == TouchPhase.Canceled
                };
                return true;
            }

            frame = new PointerFrame
            {
                screenPosition = Input.mousePosition,
                pointerId = -1,
                isPressed = Input.GetMouseButton(0),
                pressedThisFrame = Input.GetMouseButtonDown(0),
                releasedThisFrame = Input.GetMouseButtonUp(0)
            };

            return frame.isPressed ||
                   frame.pressedThisFrame ||
                   frame.releasedThisFrame;
#endif

            frame = default;
            return false;
        }

        private struct PointerFrame
        {
            public Vector2 screenPosition;
            public int pointerId;
            public bool isPressed;
            public bool pressedThisFrame;
            public bool releasedThisFrame;
        }
    }
}