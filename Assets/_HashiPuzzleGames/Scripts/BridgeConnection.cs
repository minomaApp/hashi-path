using BoxPuller.Scripts.Data.SO;
using UnityEngine;
using System.Collections;

namespace HashiGame.Scripts.Runtime
{
    public class BridgeConnection : MonoBehaviour
    {
        [Header("Saved Data")]
        [SerializeField] private Vector2Int startCoordinate;
        [SerializeField] private Vector2Int endCoordinate;
        [SerializeField] private int bridgeCount = 1;
        [SerializeField] private bool isFixed;

        [Header("References")]
        [SerializeField] private IslandNode startIsland;
        [SerializeField] private IslandNode endIsland;
        [SerializeField] private BridgeVisual bridgeVisual;

        public Vector2Int StartCoordinate => startCoordinate;
        public Vector2Int EndCoordinate => endCoordinate;
        public int BridgeCount => bridgeCount;
        public bool IsFixed => isFixed;
        public IslandNode StartIsland => startIsland;
        public IslandNode EndIsland => endIsland;
        public Vector3 StartWorldPosition => startIsland != null
            ? startIsland.ConnectionPosition
            : transform.position;
        public Vector3 EndWorldPosition => endIsland != null
            ? endIsland.ConnectionPosition
            : transform.position;

        public void ConfigureLevelData(
            IslandNode firstIsland,
            IslandNode secondIsland,
            int count,
            bool fixedBridge,
            HashiVisualSettings visualSettings)
        {
            startIsland = firstIsland;
            endIsland = secondIsland;

            if (startIsland != null)
            {
                startCoordinate = startIsland.Coordinate;
            }

            if (endIsland != null)
            {
                endCoordinate = endIsland.Coordinate;
            }

            bridgeCount = Mathf.Clamp(count, 1, 2);
            isFixed = fixedBridge;

            EnsureVisual();
            RefreshVisual(visualSettings);
        }

        public bool BindRuntime(
            IslandNode firstIsland,
            IslandNode secondIsland,
            HashiVisualSettings visualSettings)
        {
            if (firstIsland == null || secondIsland == null)
            {
                return false;
            }

            startIsland = firstIsland;
            endIsland = secondIsland;
            startCoordinate = firstIsland.Coordinate;
            endCoordinate = secondIsland.Coordinate;
            bridgeCount = Mathf.Clamp(bridgeCount, 1, 2);

            EnsureVisual();
            RefreshVisual(visualSettings);
            return true;
        }

        public void SetBridgeCount(int count, HashiVisualSettings visualSettings)
        {
            bridgeCount = Mathf.Clamp(count, 0, 2);
            EnsureVisual();

            if (bridgeVisual != null)
            {
                bridgeVisual.SetBridgeCount(bridgeCount);
                bridgeVisual.SetFixedState(isFixed);
            }

            if (bridgeCount > 0)
            {
                RefreshVisual(visualSettings);
            }
        }

        public void PlayBuildWave()
        {
            EnsureVisual();

            if (bridgeVisual != null)
            {
                bridgeVisual.PlayBuildWave();
            }
        }

        public float PlayCutAnimation(Vector3 cutWorldPoint)
        {
            EnsureVisual();

            if (bridgeVisual == null)
            {
                return 0f;
            }

            return bridgeVisual.PlayCutAnimation(cutWorldPoint);
        }

        public void PlayCutAndDestroy(Vector3 cutWorldPoint)
        {
            float delay = PlayCutAnimation(cutWorldPoint);

            if (bridgeVisual != null)
            {
                bridgeVisual.HideAllLines();
            }

            if (Application.isPlaying && delay > 0f)
            {
                StartCoroutine(DestroyAfterDelay(delay));
            }
            else
            {
                DestroyNow();
            }
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            DestroyNow();
        }

        private void DestroyNow()
        {
            if (gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
        public bool Connects(IslandNode first, IslandNode second)
        {
            return (startIsland == first && endIsland == second) ||
                   (startIsland == second && endIsland == first);
        }

        public bool SharesIslandWith(BridgeConnection other)
        {
            if (other == null)
            {
                return false;
            }

            return startIsland == other.startIsland ||
                   startIsland == other.endIsland ||
                   endIsland == other.startIsland ||
                   endIsland == other.endIsland;
        }

        private void EnsureVisual()
        {
            if (bridgeVisual == null)
            {
                bridgeVisual = GetComponent<BridgeVisual>();
            }
        }

        private void RefreshVisual(HashiVisualSettings visualSettings)
        {
            if (bridgeVisual == null || startIsland == null || endIsland == null)
            {
                return;
            }

            bridgeVisual.Configure(
                startIsland.ConnectionPosition,
                endIsland.ConnectionPosition,
                bridgeCount,
                isFixed,
                visualSettings);
        }
    }
}
