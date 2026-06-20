using BoxPuller.Scripts.Data.SO;
using UnityEngine;

namespace HashiGame.Scripts.Runtime
{
    public class BridgePreviewController : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float width = 0.12f;
        [SerializeField] private float verticalOffset = 0.2f;

        private HashiVisualSettings visualSettings;

        public bool IsVisible => lineRenderer != null && lineRenderer.enabled;

        private void Awake()
        {
            EnsureLineRenderer();
            Hide();
        }

        public void Setup(HashiVisualSettings settings)
        {
            visualSettings = settings;
            EnsureLineRenderer();
            Hide();
        }

        public void Show(Vector3 startPoint, Vector3 endPoint, bool isValid)
        {
            EnsureLineRenderer();

            startPoint.y += verticalOffset;
            endPoint.y += verticalOffset;

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;

            ApplyState(isValid);
        }

        public void Hide()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        private void ApplyState(bool isValid)
        {
            if (lineRenderer == null)
            {
                return;
            }

            Material material = null;
            Color color = isValid ? Color.green : Color.red;

            if (visualSettings != null)
            {
                material = isValid
                    ? visualSettings.validPreviewMaterial
                    : visualSettings.invalidPreviewMaterial;

                color = isValid
                    ? visualSettings.validPreviewColor
                    : visualSettings.invalidPreviewColor;
            }

            if (material != null)
            {
                lineRenderer.sharedMaterial = material;
            }

            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            width = Mathf.Max(0.01f, width);
        }
#endif
    }
}
