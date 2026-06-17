using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TemplateProject.Scripts.Utilities
{
    public class LoadingUI : MonoBehaviour
    {
        [SerializeField] private Image progressBar;
        [SerializeField] private GameObject root;
        [SerializeField] private RectMask2D rectMask;
        [SerializeField] private float smoothTime = 3f;
        [SerializeField] private float exponent = 2f;

        private Coroutine progressCoroutine;

        public void Show()
        {
            if (root != null) root.SetActive(true);
            SetProgress(0f);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        public void SetProgress(float progress)
        {
            if (progressBar != null)
                progressBar.fillAmount = progress;
            UpdateMask(progress);
        }

        public void SetProgressSmooth(Action onComplete = null)
        {
            if (progressCoroutine != null)
                StopCoroutine(progressCoroutine);
            progressCoroutine = StartCoroutine(SmoothProgress(onComplete));
        }

        private IEnumerator SmoothProgress(Action onComplete)
        {
            var start = progressBar.fillAmount;
            var elapsed = 0f;

            while (elapsed < smoothTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / smoothTime);
                var eased = Mathf.Pow(t, exponent);
                var p = Mathf.Lerp(start, 1f, eased);
                progressBar.fillAmount = p;
                UpdateMask(p);
                yield return null;
            }

            progressBar.fillAmount = 1f;
            UpdateMask(1f);
            progressCoroutine = null;
            onComplete?.Invoke();
        }

        private void UpdateMask(float progress)
        {
            if (rectMask)
            {
                var right = Mathf.Lerp(700f, 0f, progress);
                var pad = rectMask.padding;
                rectMask.padding = new Vector4(pad.x, pad.y, right, pad.w);
            }
        }
    }
}