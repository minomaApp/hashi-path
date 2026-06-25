using System.Collections;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.SO;
using TMPro;
using UnityEngine;

namespace HashiGame.Scripts.Runtime
{
    public class ChainBarrier : MonoBehaviour
    {
        [Header("Saved Data")]
        [SerializeField] private int barrierId;
        [SerializeField] private Vector2Int startCoordinate;
        [SerializeField] private Vector2Int endCoordinate;
        [SerializeField] private int unlockAfterCompletedIslandCount;
        [SerializeField] private Vector3 startWorldPosition;
        [SerializeField] private Vector3 endWorldPosition;

        [Header("Line Visual")]
        [SerializeField] private LineRenderer chainLine;
        [SerializeField] private float lineWidth = 0.14f;
        [SerializeField] private float verticalOffset = 0.15f;
        [SerializeField] private float endpointPadding = 0f;
        [SerializeField] private float blockingThickness = 0.12f;

        [Header("Text")]
        [SerializeField] private TMP_Text unlockRequirementText;
        [SerializeField] private float textVerticalOffset = 0.25f;
        [SerializeField] private Vector3 textEulerAngles = new Vector3(90f, 0f, 0f);

        [Header("Extra Visual Objects")]
        [SerializeField] private GameObject[] visualObjectsControlledByChainState;

        [Header("Disable Effect")]
        [SerializeField] private ParticleSystem[] particlesToPlayWhenDisabled;
        [SerializeField] private bool playDisableParticlesOnlyOnce = true;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string breakTriggerName = "Break";
        [SerializeField] private float breakVisualDelay = 0.35f;

        private HashiVisualSettings visualSettings;
        private bool isBlocking = true;
        private bool disableParticlesPlayed;

        public int BarrierId => barrierId;
        public Vector2Int StartCoordinate => startCoordinate;
        public Vector2Int EndCoordinate => endCoordinate;
        public int UnlockAfterCompletedIslandCount => unlockAfterCompletedIslandCount;
        public Vector3 StartWorldPosition => startWorldPosition;
        public Vector3 EndWorldPosition => endWorldPosition;
        public float BlockingThickness => blockingThickness;
        public bool IsBlocking => isBlocking;

        public void ConfigureLevelData(
            ChainBarrierData data,
            Vector3 firstWorldPosition,
            Vector3 secondWorldPosition,
            HashiVisualSettings settings)
        {
            if (data == null)
            {
                return;
            }

            barrierId = data.id;
            startCoordinate = data.startCoordinate;
            endCoordinate = data.endCoordinate;
            unlockAfterCompletedIslandCount = Mathf.Max(
                0,
                data.unlockAfterCompletedIslandCount);

            startWorldPosition = firstWorldPosition;
            endWorldPosition = secondWorldPosition;
            visualSettings = settings;
            isBlocking = true;
            disableParticlesPlayed = false;

            ApplyGeometry();
            ApplyVisualState();
        }

        public void PrepareRuntime(HashiVisualSettings settings)
        {
            visualSettings = settings;
            isBlocking = true;
            disableParticlesPlayed = false;

            ApplyGeometry();
            ApplyVisualState();
        }

        public bool TryUnlock(int completedIslandMilestoneCount)
        {
            if (!isBlocking)
            {
                return false;
            }

            RefreshRequirementText(completedIslandMilestoneCount);

            if (completedIslandMilestoneCount < unlockAfterCompletedIslandCount)
            {
                return false;
            }

            BreakPermanently();
            return true;
        }

        public void BreakPermanently()
        {
            if (!isBlocking)
            {
                return;
            }

            isBlocking = false;

            if (visualSettings != null &&
                visualSettings.chainBreakEffectPrefab != null)
            {
                Instantiate(
                    visualSettings.chainBreakEffectPrefab,
                    GetMidPoint(),
                    Quaternion.identity);
            }

            if (animator != null && !string.IsNullOrEmpty(breakTriggerName))
            {
                animator.SetTrigger(breakTriggerName);
            }

            if (Application.isPlaying && breakVisualDelay > 0f)
            {
                StartCoroutine(DisableVisualAfterDelay());
            }
            else
            {
                SetVisualActive(false);
            }
        }

        public void RefreshRequirementText(int completedIslandMilestoneCount)
        {
            if (unlockRequirementText == null)
            {
                return;
            }

            int remainingCount = Mathf.Max(
                0,
                unlockAfterCompletedIslandCount - completedIslandMilestoneCount);

            unlockRequirementText.text = remainingCount.ToString();
            ApplyTextPosition();
        }

        private IEnumerator DisableVisualAfterDelay()
        {
            yield return new WaitForSeconds(breakVisualDelay);
            SetVisualActive(false);
        }

        private void ApplyGeometry()
        {
            EnsureLineRenderer();

            Vector3 flatDirection = endWorldPosition - startWorldPosition;
            flatDirection.y = 0f;

            float length = flatDirection.magnitude;
            if (length <= 0.0001f)
            {
                SetVisualActive(false);
                return;
            }

            Vector3 direction = flatDirection / length;

            Vector3 start = startWorldPosition + Vector3.up * verticalOffset;
            Vector3 end = endWorldPosition + Vector3.up * verticalOffset;

            if (endpointPadding > 0f)
            {
                start += direction * endpointPadding;
                end -= direction * endpointPadding;
            }

            Vector3 midpoint = GetMidPoint();
            transform.position = midpoint;

            if (chainLine != null)
            {
                chainLine.useWorldSpace = true;
                chainLine.positionCount = 2;
                chainLine.startWidth = lineWidth;
                chainLine.endWidth = lineWidth;
                chainLine.SetPosition(0, start);
                chainLine.SetPosition(1, end);
            }

            ApplyTextPosition();
        }

        private void ApplyVisualState()
        {
            EnsureLineRenderer();
            SetVisualActive(isBlocking);
            ApplyMaterial();
            RefreshRequirementText(0);
        }

        private void ApplyMaterial()
        {
            EnsureLineRenderer();

            if (visualSettings == null ||
                visualSettings.chainMaterial == null ||
                chainLine == null)
            {
                return;
            }

            chainLine.sharedMaterial = visualSettings.chainMaterial;
        }

        private void ApplyTextPosition()
        {
            if (unlockRequirementText == null)
            {
                return;
            }

            Vector3 textPosition = GetMidPoint();
            textPosition.y += textVerticalOffset;

            unlockRequirementText.transform.position = textPosition;
            unlockRequirementText.transform.eulerAngles = textEulerAngles;
        }
        private void SetVisualActive(bool value)
        {
            EnsureLineRenderer();

            if (!value && !isBlocking)
            {
                PlayDisableParticles();
            }

            if (chainLine != null)
            {
                chainLine.enabled = value;
            }

            if (unlockRequirementText != null)
            {
                unlockRequirementText.gameObject.SetActive(value);
            }

            SetObjectArrayActive(visualObjectsControlledByChainState, value);
        }

        private void EnsureLineRenderer()
        {
            if (chainLine != null)
            {
                return;
            }

            chainLine = GetComponentInChildren<LineRenderer>(true);

            if (chainLine == null)
            {
                GameObject lineObject = new GameObject("ChainLine");
                lineObject.transform.SetParent(transform, false);
                chainLine = lineObject.AddComponent<LineRenderer>();
            }
        }

        private Vector3 GetMidPoint()
        {
            Vector3 midpoint = (startWorldPosition + endWorldPosition) * 0.5f;
            midpoint.y = Mathf.Max(
                startWorldPosition.y,
                endWorldPosition.y) + verticalOffset;

            return midpoint;
        }

        private static void SetObjectArrayActive(GameObject[] targets, bool value)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                targets[i].SetActive(value);
            }
        }

        private void PlayDisableParticles()
        {
            if (playDisableParticlesOnlyOnce && disableParticlesPlayed)
            {
                return;
            }

            disableParticlesPlayed = true;

            if (particlesToPlayWhenDisabled == null)
            {
                return;
            }

            for (int i = 0; i < particlesToPlayWhenDisabled.Length; i++)
            {
                ParticleSystem particle = particlesToPlayWhenDisabled[i];

                if (particle == null)
                {
                    continue;
                }

                particle.gameObject.SetActive(true);
                particle.Clear(true);
                particle.Play(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            unlockAfterCompletedIslandCount = Mathf.Max(
                0,
                unlockAfterCompletedIslandCount);

            lineWidth = Mathf.Max(0.001f, lineWidth);
            verticalOffset = Mathf.Max(0f, verticalOffset);
            endpointPadding = Mathf.Max(0f, endpointPadding);
            blockingThickness = Mathf.Max(0f, blockingThickness);

            EnsureLineRenderer();
        }
#endif
    }
}