using System.Collections;
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

        [Header("Build Wave Animation")]
        [SerializeField] private bool playBuildWave = true;
        [SerializeField] private float buildWaveDuration = 0.35f;
        [SerializeField] private float buildWaveAmplitude = 0.12f;
        [SerializeField] private float buildWaveWidth = 0.28f;
        [SerializeField] private int buildWaveSegmentCount = 18;

        [Header("Cut Animation")]
        [SerializeField] private float cutAnimationDuration = 0.22f;
        [SerializeField] private float cutAnimationVerticalOffset = 0.02f;

        private int bridgeCount;
        private bool isFixed;
        private HashiVisualSettings visualSettings;

        private Vector3 startPoint;
        private Vector3 endPoint;
        private bool hasGeometry;

        private Coroutine buildWaveCoroutine;

        public int BridgeCount => bridgeCount;
        public float CutAnimationDuration => cutAnimationDuration;

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

        public void PlayBuildWave()
        {
            if (!playBuildWave)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!hasGeometry || bridgeCount <= 0)
            {
                return;
            }

            if (buildWaveCoroutine != null)
            {
                StopCoroutine(buildWaveCoroutine);
            }

            buildWaveCoroutine = StartCoroutine(BuildWaveRoutine());
        }

        public float PlayCutAnimation(Vector3 cutWorldPoint)
        {
            if (!hasGeometry || bridgeCount <= 0)
            {
                return 0f;
            }

            EnsureLines();

            LineRenderer sourceLine = bridgeCount > 1 ? secondLine : firstLine;

            if (sourceLine == null)
            {
                return 0f;
            }

            Vector3 lineStart;
            Vector3 lineEnd;

            GetLanePoints(
                bridgeCount > 1 ? 1 : 0,
                bridgeCount,
                out lineStart,
                out lineEnd);

            Vector3 cutPoint = BridgeGeometryUtility.ClosestPointOnSegment(
              cutWorldPoint,
              lineStart,
              lineEnd);

            cutPoint.y += cutAnimationVerticalOffset;

            CreateCutPiece(lineStart, cutPoint, true, sourceLine);
            CreateCutPiece(lineEnd, cutPoint, false, sourceLine);

            SetLineActive(sourceLine, false);

            return Mathf.Max(0.01f, cutAnimationDuration);
        }

        public void HideAllLines()
        {
            SetLineActive(firstLine, false);
            SetLineActive(secondLine, false);
        }

        private IEnumerator BuildWaveRoutine()
        {
            float elapsed = 0f;

            while (elapsed < buildWaveDuration)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(elapsed / buildWaveDuration);
                DrawWave(progress);

                yield return null;
            }

            buildWaveCoroutine = null;
            RefreshLines();
        }

        private void DrawWave(float progress)
        {
            if (bridgeCount <= 0)
            {
                return;
            }

            if (bridgeCount == 1)
            {
                Vector3 laneStart;
                Vector3 laneEnd;

                GetLanePoints(0, 1, out laneStart, out laneEnd);
                DrawWaveLine(firstLine, laneStart, laneEnd, progress);
                SetLineActive(secondLine, false);
                return;
            }

            Vector3 firstStart;
            Vector3 firstEnd;
            Vector3 secondStart;
            Vector3 secondEnd;

            GetLanePoints(0, 2, out firstStart, out firstEnd);
            GetLanePoints(1, 2, out secondStart, out secondEnd);

            DrawWaveLine(firstLine, firstStart, firstEnd, progress);
            DrawWaveLine(secondLine, secondStart, secondEnd, progress);
        }

        private void DrawWaveLine(
      LineRenderer line,
      Vector3 lineStart,
      Vector3 lineEnd,
      float progress)
        {
            if (line == null)
            {
                return;
            }

            line.gameObject.SetActive(true);
            line.useWorldSpace = true;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;

            Vector3 flatDirection = lineEnd - lineStart;
            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 direction = flatDirection.normalized;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;

            int count = Mathf.Max(4, buildWaveSegmentCount);
            line.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : i / (float)(count - 1);

                Vector3 point = Vector3.Lerp(lineStart, lineEnd, t);

                float distanceFromWave = Mathf.Abs(t - progress);
                float envelope = 1f - Mathf.Clamp01(distanceFromWave / buildWaveWidth);

                float wavePhase = (t - progress) / buildWaveWidth * Mathf.PI;
                float wave = Mathf.Sin(wavePhase) * envelope;

                point += side * wave * buildWaveAmplitude;

                line.SetPosition(i, point);
            }
        }

        private void CreateCutPiece(
            Vector3 fixedPoint,
            Vector3 cutPoint,
            bool moveEndToFixedPoint,
            LineRenderer sourceLine)
        {
            if (sourceLine == null)
            {
                return;
            }

            GameObject pieceObject = new GameObject("BridgeCutPiece");
            pieceObject.transform.SetParent(transform.parent, true);

            LineRenderer pieceLine = pieceObject.AddComponent<LineRenderer>();
            pieceLine.useWorldSpace = true;
            pieceLine.positionCount = 2;
            pieceLine.startWidth = lineWidth;
            pieceLine.endWidth = lineWidth;
            pieceLine.sharedMaterial = sourceLine.sharedMaterial;
            pieceLine.startColor = sourceLine.startColor;
            pieceLine.endColor = sourceLine.endColor;
            pieceLine.alignment = sourceLine.alignment;
            pieceLine.textureMode = sourceLine.textureMode;
            pieceLine.numCornerVertices = sourceLine.numCornerVertices;
            pieceLine.numCapVertices = sourceLine.numCapVertices;

            if (moveEndToFixedPoint)
            {
                pieceLine.SetPosition(0, fixedPoint);
                pieceLine.SetPosition(1, cutPoint);
            }
            else
            {
                pieceLine.SetPosition(0, cutPoint);
                pieceLine.SetPosition(1, fixedPoint);
            }

            StartCoroutine(AnimateCutPiece(
                pieceLine,
                pieceObject,
                fixedPoint,
                cutPoint,
                moveEndToFixedPoint));
        }

        private IEnumerator AnimateCutPiece(
            LineRenderer pieceLine,
            GameObject pieceObject,
            Vector3 fixedPoint,
            Vector3 cutPoint,
            bool moveEndToFixedPoint)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, cutAnimationDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 movingPoint = Vector3.Lerp(cutPoint, fixedPoint, t);

                if (pieceLine != null)
                {
                    if (moveEndToFixedPoint)
                    {
                        pieceLine.SetPosition(0, fixedPoint);
                        pieceLine.SetPosition(1, movingPoint);
                    }
                    else
                    {
                        pieceLine.SetPosition(0, movingPoint);
                        pieceLine.SetPosition(1, fixedPoint);
                    }
                }

                yield return null;
            }

            if (pieceObject != null)
            {
                Destroy(pieceObject);
            }
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

            if (bridgeCount == 1)
            {
                Vector3 laneStart;
                Vector3 laneEnd;

                GetLanePoints(0, 1, out laneStart, out laneEnd);

                SetLine(firstLine, laneStart, laneEnd);
                SetLineActive(secondLine, false);
                return;
            }

            Vector3 firstStart;
            Vector3 firstEnd;
            Vector3 secondStart;
            Vector3 secondEnd;

            GetLanePoints(0, 2, out firstStart, out firstEnd);
            GetLanePoints(1, 2, out secondStart, out secondEnd);

            SetLine(firstLine, firstStart, firstEnd);
            SetLine(secondLine, secondStart, secondEnd);
        }

        private void GetLanePoints(
            int laneIndex,
            int totalLaneCount,
            out Vector3 laneStart,
            out Vector3 laneEnd)
        {
            Vector3 flatDirection = endPoint - startPoint;
            flatDirection.y = 0f;

            float length = flatDirection.magnitude;

            if (length <= 0.0001f)
            {
                laneStart = startPoint;
                laneEnd = endPoint;
                return;
            }

            Vector3 direction = flatDirection / length;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;

            laneStart = startPoint + Vector3.up * verticalOffset;
            laneEnd = endPoint + Vector3.up * verticalOffset;

            if (endpointPadding > 0f)
            {
                laneStart += direction * endpointPadding;
                laneEnd -= direction * endpointPadding;
            }

            if (totalLaneCount <= 1)
            {
                return;
            }

            float halfSpacing = doubleLaneSpacing * 0.5f;
            Vector3 offset = laneIndex == 0
                ? side * -halfSpacing
                : side * halfSpacing;

            laneStart += offset;
            laneEnd += offset;
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
            buildWaveDuration = Mathf.Max(0.01f, buildWaveDuration);
            buildWaveAmplitude = Mathf.Max(0f, buildWaveAmplitude);
            buildWaveWidth = Mathf.Max(0.01f, buildWaveWidth);
            buildWaveSegmentCount = Mathf.Max(4, buildWaveSegmentCount);
            cutAnimationDuration = Mathf.Max(0.01f, cutAnimationDuration);
            cutAnimationVerticalOffset = Mathf.Max(0f, cutAnimationVerticalOffset);

            EnsureLines();
        }
#endif
    }
}