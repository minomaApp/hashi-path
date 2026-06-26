using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Data.SO;
using TMPro;
using UnityEngine;
using System.Collections;

namespace HashiGame.Scripts.Runtime
{
    [Serializable]
    public class IslandMaterialTarget
    {
        public Renderer renderer;
        [Min(0)] public int materialIndex;
    }

    public class IslandNode : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private int requiredBridgeCount = 1;
        [SerializeField]
        private EnumHolder.IslandBridgeMode bridgeMode =
            EnumHolder.IslandBridgeMode.SingleOnly;
        [SerializeField] private bool startsLocked;
        [SerializeField] private int unlockAfterCompletedIslandCount;

        [Header("Connection")]
        [SerializeField] private Transform connectionPoint;
        [SerializeField] private float connectionBlockRadius = 0.45f;

        [Header("Texts")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text lockRequirementText;
        [SerializeField] private bool showCurrentOverRequired = true;

        [Header("Material Targets")]
        [SerializeField] private IslandMaterialTarget[] materialTargets;

        [Header("Lock Visual State")]
        [SerializeField] private GameObject lockVisual;
        [SerializeField] private GameObject[] visibleWhenLocked;
        [SerializeField] private GameObject[] visibleWhenUnlocked;

        [Header("Unlock Particle")]
        [SerializeField] private ParticleSystem[] unlockParticles;

        [Header("Locked Feedback")]
        [SerializeField] private Transform lockedShakeTarget;
        [SerializeField] private float lockedShakeDuration = 0.3f;
        [SerializeField] private float lockedShakeStrength = 0.08f;
        [SerializeField] private int lockedShakeVibrations =2;
        [SerializeField] private Vector3 lockedShakeLocalAxis = Vector3.right;

        [HideInInspector]
        [SerializeField] private Renderer[] islandRenderers;

        private readonly List<BridgeConnection> connections =
            new List<BridgeConnection>();

        private HashiVisualSettings visualSettings;
        private bool isLocked;
        private bool isCompleted;
        private int currentBridgeCount;
        private bool unlockParticlesPlayed;

        private Coroutine lockedShakeCoroutine;
        private Vector3 lockedShakeStartLocalPosition;

        public bool IsOverfilled => currentBridgeCount > requiredBridgeCount;
        public Vector2Int Coordinate => coordinate;
        public int RequiredBridgeCount => requiredBridgeCount;
        public int CurrentBridgeCount => currentBridgeCount;
        public int RemainingBridgeCount => requiredBridgeCount - currentBridgeCount;
        public EnumHolder.IslandBridgeMode BridgeMode => bridgeMode;
        public bool StartsLocked => startsLocked;
        public bool IsLocked => isLocked;
        public bool IsCompleted => isCompleted;
        public int UnlockAfterCompletedIslandCount => unlockAfterCompletedIslandCount;
        public float ConnectionBlockRadius => connectionBlockRadius;

        public Vector3 ConnectionPosition =>
            connectionPoint != null ? connectionPoint.position : transform.position;

        public IReadOnlyList<BridgeConnection> Connections => connections;

        public void ConfigureLevelData(
            IslandCellData data,
            HashiVisualSettings settings)
        {
            if (data == null)
            {
                return;
            }

            coordinate = new Vector2Int(data.x, data.y);
            requiredBridgeCount = Mathf.Max(1, data.requiredBridgeCount);
            bridgeMode = data.bridgeMode;
            startsLocked = data.startsLocked;
            unlockAfterCompletedIslandCount =
                Mathf.Max(0, data.unlockAfterCompletedIslandCount);
            visualSettings = settings;

            ResetRuntimeState();
            ApplyVisualState();
        }

        public void PrepareRuntime(HashiVisualSettings settings)
        {
            visualSettings = settings;
            ResetRuntimeState();
            ApplyVisualState();
        }

        public void RegisterConnection(BridgeConnection connection)
        {
            if (connection == null || connections.Contains(connection))
            {
                return;
            }

            connections.Add(connection);
        }

        public void UnregisterConnection(BridgeConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            connections.Remove(connection);
        }

        public int CalculateConnectedBridgeCount()
        {
            int result = 0;

            for (int i = connections.Count - 1; i >= 0; i--)
            {
                BridgeConnection connection = connections[i];

                if (connection == null)
                {
                    connections.RemoveAt(i);
                    continue;
                }

                result += connection.BridgeCount;
            }

            return result;
        }

        public bool SetCurrentBridgeCount(int value)
        {
            bool wasCompleted = isCompleted;

            currentBridgeCount = Mathf.Max(0, value);
            isCompleted = currentBridgeCount == requiredBridgeCount;

            UpdateProgressText();
            ApplyVisualState();

            return !wasCompleted && isCompleted;
        }

        public bool TryUnlock(int completedIslandMilestoneCount)
        {
            if (!isLocked)
            {
                return false;
            }

            RefreshLockRequirementText(completedIslandMilestoneCount);

            if (completedIslandMilestoneCount < unlockAfterCompletedIslandCount)
            {
                return false;
            }

            UnlockPermanently();
            return true;
        }

        public void UnlockPermanently()
        {
            if (!isLocked)
            {
                return;
            }

            isLocked = false;
            ApplyVisualState();
            PlayUnlockParticles();

            if (visualSettings != null &&
                visualSettings.islandUnlockEffectPrefab != null)
            {
                Instantiate(
                    visualSettings.islandUnlockEffectPrefab,
                    transform.position,
                    Quaternion.identity);
            }
        }
        public void PlayLockedShake()
        {
            Transform target = GetLockedShakeTarget();

            if (target == null)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (lockedShakeCoroutine != null)
            {
                StopCoroutine(lockedShakeCoroutine);
                target.localPosition = lockedShakeStartLocalPosition;
            }

            lockedShakeStartLocalPosition = target.localPosition;
            lockedShakeCoroutine = StartCoroutine(LockedShakeRoutine(target));
        }

        private IEnumerator LockedShakeRoutine(Transform target)
        {
            Vector3 axis = lockedShakeLocalAxis;

            if (axis.sqrMagnitude <= 0.0001f)
            {
                axis = Vector3.right;
            }

            axis.Normalize();

            float duration = Mathf.Max(0.01f, lockedShakeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float time = Mathf.Clamp01(elapsed / duration);
                float fade = 1f - time;
                float wave = Mathf.Sin(
                    time *
                    lockedShakeVibrations *
                    Mathf.PI *
                    2f);

                target.localPosition =
                    lockedShakeStartLocalPosition +
                    axis * wave * lockedShakeStrength * fade;

                yield return null;
            }

            target.localPosition = lockedShakeStartLocalPosition;
            lockedShakeCoroutine = null;
        }
        private Transform GetLockedShakeTarget()
        {
            return lockedShakeTarget != null
                ? lockedShakeTarget
                : transform;
        }

        public int GetMaximumBridgeCountWith(IslandNode other)
        {
            if (other == null)
            {
                return 0;
            }

            bool bothAllowDouble =
                bridgeMode == EnumHolder.IslandBridgeMode.DoubleAllowed &&
                other.bridgeMode == EnumHolder.IslandBridgeMode.DoubleAllowed;

            return bothAllowDouble ? 2 : 1;
        }

        public void RefreshLockRequirementText(int completedIslandMilestoneCount)
        {
            if (lockRequirementText == null)
            {
                return;
            }

            int remainingCount = Mathf.Max(
                0,
                unlockAfterCompletedIslandCount - completedIslandMilestoneCount);

            lockRequirementText.text = remainingCount.ToString();
        }

        //private void ResetRuntimeState()
        //{
        //    connections.Clear();
        //    currentBridgeCount = 0;
        //    isCompleted = false;
        //    isLocked = startsLocked;
        //    unlockParticlesPlayed = false;
        //    UpdateProgressText();
        //}

        private void ResetRuntimeState()
        {
            StopLockedShake(false);

            connections.Clear();
            currentBridgeCount = 0;
            isCompleted = false;
            isLocked = startsLocked;
            unlockParticlesPlayed = false;
            UpdateProgressText();
        }

        private void StopLockedShake(bool restorePosition)
        {
            Transform target = GetLockedShakeTarget();

            if (lockedShakeCoroutine != null)
            {
                StopCoroutine(lockedShakeCoroutine);
                lockedShakeCoroutine = null;
            }

            if (restorePosition && target != null)
            {
                target.localPosition = lockedShakeStartLocalPosition;
            }
        }
        private void ApplyVisualState()
        {
            ApplyLockVisualState();

            Material targetMaterial = GetTargetMaterial();

            if (targetMaterial != null)
            {
                ApplyMaterial(targetMaterial);
            }
        }

        private void ApplyLockVisualState()
        {
            SetObjectActive(lockVisual, isLocked);
            SetObjectArrayActive(visibleWhenLocked, isLocked);
            SetObjectArrayActive(visibleWhenUnlocked, !isLocked);

            if (lockRequirementText != null)
            {
                lockRequirementText.gameObject.SetActive(isLocked);

                if (isLocked)
                {
                    RefreshLockRequirementText(0);
                }
            }
        }
        private void PlayUnlockParticles()
        {
            if (unlockParticles == null)
            {
                return;
            }

            for (int i = 0; i < unlockParticles.Length; i++)
            {
                ParticleSystem particle = unlockParticles[i];

                if (particle == null)
                {
                    continue;
                }

                particle.gameObject.SetActive(true);
                particle.Clear(true);
                particle.Play(true);
            }
        }
        private Material GetTargetMaterial()
        {
            if (visualSettings == null)
            {
                return null;
            }

            if (isLocked)
            {
                return visualSettings.lockedIslandMaterial;
            }

            if (currentBridgeCount > requiredBridgeCount)
            {
                return visualSettings.failedIslandMaterial;
            }

            if (isCompleted)
            {
                return visualSettings.completedIslandMaterial;
            }

            return visualSettings.normalIslandMaterial;
        }

        private void ApplyMaterial(Material targetMaterial)
        {
            bool usedMaterialTargets = false;

            if (materialTargets != null)
            {
                for (int i = 0; i < materialTargets.Length; i++)
                {
                    IslandMaterialTarget target = materialTargets[i];

                    if (target == null || target.renderer == null)
                    {
                        continue;
                    }

                    ApplyMaterialToRendererSlot(
                        target.renderer,
                        target.materialIndex,
                        targetMaterial);

                    usedMaterialTargets = true;
                }
            }

            if (usedMaterialTargets)
            {
                return;
            }

            if (islandRenderers == null)
            {
                return;
            }

            for (int i = 0; i < islandRenderers.Length; i++)
            {
                if (islandRenderers[i] != null)
                {
                    islandRenderers[i].sharedMaterial = targetMaterial;
                }
            }
        }

        private static void ApplyMaterialToRendererSlot(
            Renderer targetRenderer,
            int materialIndex,
            Material targetMaterial)
        {
            if (targetRenderer == null || targetMaterial == null)
            {
                return;
            }

            Material[] materials = targetRenderer.sharedMaterials;

            if (materials == null || materials.Length == 0)
            {
                return;
            }

            int safeIndex = Mathf.Clamp(
                materialIndex,
                0,
                materials.Length - 1);

            materials[safeIndex] = targetMaterial;
            targetRenderer.sharedMaterials = materials;
        }

        private static void SetObjectActive(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
        }

        private static void SetObjectArrayActive(GameObject[] targets, bool value)
        {
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                SetObjectActive(targets[i], value);
            }
        }

        private void UpdateProgressText()
        {
            if (progressText == null)
            {
                return;
            }

            progressText.text = showCurrentOverRequired
                ? currentBridgeCount + "/" + requiredBridgeCount
                : requiredBridgeCount.ToString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            requiredBridgeCount = Mathf.Max(1, requiredBridgeCount);
            unlockAfterCompletedIslandCount =
                Mathf.Max(0, unlockAfterCompletedIslandCount);
            connectionBlockRadius = Mathf.Max(0.01f, connectionBlockRadius);

            lockedShakeDuration = Mathf.Max(0.01f, lockedShakeDuration);
            lockedShakeStrength = Mathf.Max(0f, lockedShakeStrength);
            lockedShakeVibrations = Mathf.Max(1, lockedShakeVibrations);

            if (lockedShakeLocalAxis.sqrMagnitude <= 0.0001f)
            {
                lockedShakeLocalAxis = Vector3.right;
            }
        }
#endif
    }
}