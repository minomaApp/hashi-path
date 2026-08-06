using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HashiGame.Scripts.Runtime
{
    [DisallowMultipleComponent]
    public class IslandProgressBar : MonoBehaviour
    {
        [SerializeField] private BridgeBoardManager boardManager;
        [SerializeField] private Image fillImage;
        [SerializeField, Min(0f)] private float fillDuration = 0.25f;

        private Coroutine fillRoutine;

        private void Awake()
        {
            if (fillImage == null)
            {
                fillImage = GetComponent<Image>();
            }

            ConfigureFillImage();
        }

        private void OnEnable()
        {
            if (boardManager == null)
            {
                boardManager = FindFirstObjectByType<BridgeBoardManager>();
            }

            if (boardManager == null)
            {
                Debug.LogWarning("[IslandProgressBar] BridgeBoardManager could not be found.", this);
                SetFillAmount(0f);
                return;
            }

            boardManager.IslandProgressChanged += HandleIslandProgressChanged;
            HandleIslandProgressChanged(
                boardManager.CurrentCompletedIslandCount,
                boardManager.TotalIslandCount);
        }

        private void OnDisable()
        {
            if (boardManager != null)
            {
                boardManager.IslandProgressChanged -= HandleIslandProgressChanged;
            }

            if (fillRoutine != null)
            {
                StopCoroutine(fillRoutine);
                fillRoutine = null;
            }
        }

        private void ConfigureFillImage()
        {
            if (fillImage == null)
            {
                return;
            }

            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillClockwise = true;
        }

        private void HandleIslandProgressChanged(int completedIslandCount, int totalIslandCount)
        {
            float targetFillAmount = totalIslandCount > 0
                ? Mathf.Clamp01(completedIslandCount / (float)totalIslandCount)
                : 0f;

            if (!isActiveAndEnabled || fillDuration <= 0f)
            {
                SetFillAmount(targetFillAmount);
                return;
            }

            if (fillRoutine != null)
            {
                StopCoroutine(fillRoutine);
            }

            fillRoutine = StartCoroutine(AnimateFill(targetFillAmount));
        }

        private IEnumerator AnimateFill(float targetFillAmount)
        {
            if (fillImage == null)
            {
                fillRoutine = null;
                yield break;
            }

            float startFillAmount = fillImage.fillAmount;
            float elapsedTime = 0f;

            while (elapsedTime < fillDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / fillDuration);
                fillImage.fillAmount = Mathf.Lerp(
                    startFillAmount,
                    targetFillAmount,
                    Mathf.SmoothStep(0f, 1f, normalizedTime));
                yield return null;
            }

            fillImage.fillAmount = targetFillAmount;
            fillRoutine = null;
        }

        private void SetFillAmount(float fillAmount)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = fillAmount;
            }
        }
    }
}
