using BoxPuller.Scripts.Data.SO;
using UnityEngine;

namespace HashiGame.Scripts.Runtime
{
    public class BridgeVisual : MonoBehaviour
    {
        [Header("Point To Point Lines")]
        [SerializeField] private LineRenderer firstLine;
        [SerializeField] private LineRenderer secondLine;

        [Header("Geometry")]
        [SerializeField] private float lineWidth = 0.12f;
        [SerializeField] private float doubleLaneSpacing = 0.22f;
        [SerializeField] private float verticalOffset = 0.05f;
        [SerializeField] private float endpointPadding = 0f;

        private int bridgeCount;
        private bool isFixed;
        private HashiVisualSettings visualSettings;

        private Vector3 startPoint;
        private Vector3 endPoint;
        private bool hasGeometry;

        public int BridgeCount => bridgeCount;

        public void Configure(
            Vector3 newStartPoint,
            Vector3 newEndPoint,
            int count,
            bool fixedBridge,
            HashiVisualSettings settings)
        {
            visualSettings = settings;
            isFixed = fixedBridge;

            EnsureLines();

            bridgeCount = Mathf.Clamp(count, 0, 2);

            SetGeometry(newStartPoint, newEndPoint);
            ApplyMaterial();
            RefreshLines();
        }

        public void SetGeometry(Vector3 newStartPoint, Vector3 newEndPoint)
        {
            startPoint = newStartPoint;
            endPoint = newEndPoint;
            hasGeometry = true;

            RefreshLines();
        }

        public void SetBridgeCount(int count)
        {
            bridgeCount = Mathf.Clamp(count, 0, 2);
            RefreshLines();
        }

        public void SetFixedState(bool fixedBridge)
        {
            isFixed = fixedBridge;
            ApplyMaterial();
        }

        private void EnsureLines()
        {
            if (firstLine != null && secondLine != null)
            {
                return;
            }

            LineRenderer[] lines = GetComponentsInChildren<LineRenderer>(true);

            if (firstLine == null && lines.Length > 0)
            {
                firstLine = lines[0];
            }

            if (secondLine == null && lines.Length > 1)
            {
                secondLine = lines[1];
            }
        }

        private void RefreshLines()
        {
            EnsureLines();

            if (!hasGeometry)
            {
                SetLineActive(firstLine, false);
                SetLineActive(secondLine, false);
                return;
            }

            if (bridgeCount <= 0)
            {
                SetLineActive(firstLine, false);
                SetLineActive(secondLine, false);
                return;
            }

            Vector3 flatDirection = endPoint - startPoint;
            flatDirection.y = 0f;

            float length = flatDirection.magnitude;
            if (length <= 0.0001f)
            {
                SetLineActive(firstLine, false);
                SetLineActive(secondLine, false);
                return;
            }

            Vector3 direction = flatDirection / length;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;

            Vector3 start = startPoint + Vector3.up * verticalOffset;
            Vector3 end = endPoint + Vector3.up * verticalOffset;

            if (endpointPadding > 0f)
            {
                start += direction * endpointPadding;
                end -= direction * endpointPadding;
            }

            if (bridgeCount == 1)
            {
                SetLine(firstLine, start, end);
                SetLineActive(secondLine, false);
                return;
            }

            float halfSpacing = doubleLaneSpacing * 0.5f;

            Vector3 firstOffset = side * -halfSpacing;
            Vector3 secondOffset = side * halfSpacing;

            SetLine(firstLine, start + firstOffset, end + firstOffset);
            SetLine(secondLine, start + secondOffset, end + secondOffset);
        }

        private void SetLine(LineRenderer line, Vector3 start, Vector3 end)
        {
            if (line == null)
            {
                return;
            }

            line.gameObject.SetActive(true);
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private void SetLineActive(LineRenderer line, bool active)
        {
            if (line == null)
            {
                return;
            }

            line.gameObject.SetActive(active);
        }

        private void ApplyMaterial()
        {
            EnsureLines();

            if (visualSettings == null)
            {
                return;
            }

            Material material = isFixed
                ? visualSettings.fixedBridgeMaterial
                : visualSettings.normalBridgeMaterial;

            if (material == null)
            {
                return;
            }

            if (firstLine != null)
            {
                firstLine.sharedMaterial = material;
            }

            if (secondLine != null)
            {
                secondLine.sharedMaterial = material;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            lineWidth = Mathf.Max(0.001f, lineWidth);
            doubleLaneSpacing = Mathf.Max(0f, doubleLaneSpacing);
            endpointPadding = Mathf.Max(0f, endpointPadding);

            EnsureLines();
        }
#endif
    }
}