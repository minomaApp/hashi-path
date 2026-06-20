using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Data.SO;
using TMPro;
using UnityEngine;

namespace HashiGame.Scripts.Runtime
{
    public class IslandNode : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private int requiredBridgeCount = 1;
        [SerializeField] private EnumHolder.IslandBridgeMode bridgeMode = EnumHolder.IslandBridgeMode.SingleOnly;
        [SerializeField] private bool startsLocked;
        [SerializeField] private int unlockAfterCompletedIslandCount;

        [Header("Connection")]
        [SerializeField] private Transform connectionPoint;
        [SerializeField] private float connectionBlockRadius = 0.45f;

        [Header("Texts")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text lockRequirementText;
        [SerializeField] private bool showCurrentOverRequired = true;

        [Header("Visuals")]
        [SerializeField] private Renderer[] islandRenderers;
        [SerializeField] private GameObject lockVisual;
        [SerializeField] private GameObject singleBridgeEmblem;
        [SerializeField] private GameObject doubleBridgeEmblem;

        private readonly List<BridgeConnection> connections = new List<BridgeConnection>();
        private HashiVisualSettings visualSettings;
        private bool isLocked;
        private bool isCompleted;
        private int currentBridgeCount;

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
        public Vector3 ConnectionPosition => connectionPoint != null ? connectionPoint.position : transform.position;
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
            unlockAfterCompletedIslandCount = Mathf.Max(0, data.unlockAfterCompletedIslandCount);
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

            if (visualSettings != null && visualSettings.islandUnlockEffectPrefab != null)
            {
                Instantiate(
                    visualSettings.islandUnlockEffectPrefab,
                    transform.position,
                    Quaternion.identity);
            }
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

        private void ResetRuntimeState()
        {
            connections.Clear();
            currentBridgeCount = 0;
            isCompleted = false;
            isLocked = startsLocked;
            UpdateProgressText();
        }

        private void ApplyVisualState()
        {
            if (lockVisual != null)
            {
                lockVisual.SetActive(isLocked);
            }

            if (singleBridgeEmblem != null)
            {
                singleBridgeEmblem.SetActive(
                    bridgeMode == EnumHolder.IslandBridgeMode.SingleOnly);
            }

            if (doubleBridgeEmblem != null)
            {
                doubleBridgeEmblem.SetActive(
                    bridgeMode == EnumHolder.IslandBridgeMode.DoubleAllowed);
            }

            if (lockRequirementText != null)
            {
                lockRequirementText.gameObject.SetActive(isLocked);

                if (isLocked)
                {
                    RefreshLockRequirementText(0);
                }
            }

            Material targetMaterial = null;

            if (visualSettings != null)
            {
                if (isLocked)
                {
                    targetMaterial = visualSettings.lockedIslandMaterial;
                }
                else if (currentBridgeCount > requiredBridgeCount)
                {
                    targetMaterial = visualSettings.failedIslandMaterial;
                }
                else if (isCompleted)
                {
                    targetMaterial = visualSettings.completedIslandMaterial;
                }
                else
                {
                    targetMaterial = visualSettings.normalIslandMaterial;
                }
            }

            if (targetMaterial == null || islandRenderers == null)
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            requiredBridgeCount = Mathf.Max(1, requiredBridgeCount);
            unlockAfterCompletedIslandCount = Mathf.Max(0, unlockAfterCompletedIslandCount);
            connectionBlockRadius = Mathf.Max(0.01f, connectionBlockRadius);
            UpdateProgressText();
        }
#endif
    }
}
