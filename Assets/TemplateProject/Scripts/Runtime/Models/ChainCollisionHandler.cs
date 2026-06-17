using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Data.SO;
using TemplateProject.Scripts.Interfaces;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Utilities;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class ChainCollisionHandler : MonoSingleton<ChainCollisionHandler>
    {
        #region Variables

        private BoxContainerChain _chain;
        public GridBase currentHoveringGrid;

        private RaycastOperations raycastOperations;

        [Header("Selecting")] [SerializeField] private LayerMask chainLayer;
        [SerializeField] private LayerMask gridLayer;
        [SerializeField] private float selectRadius;
        [SerializeField] private float speed = 1f;
        [AudioClipName] public string popSound;
        [SerializeField] private LayerMask collisionLayer;
        Vector3 _lastFingerPosition;
        public List<Collider> selfSkipColliders = new();
#if UNITY_EDITOR
        private Vector3? debugSpherePosition = null;
        private float debugSphereRadius = 0.3f;
#endif

        #endregion


        #region Unity Methods

        private void Start()
        {
            InputManager.Instance.onFingerDown += OnFingerDown;
            InputManager.Instance.onFingerHold += OnFingerHold;
            InputManager.Instance.onFingerUp += OnFingerUp;

            raycastOperations = new RaycastOperations(Camera.main);
        }

        #endregion


        #region Custom Methods

        private void OnFingerDown()
        {
            if (!ShouldProcessInput()) return;


            var mousePosition = Input.mousePosition;
            var gridBase = raycastOperations.GetObjectOfTypeWithinAllNonAlloc<GridBase>(gridLayer.value, mousePosition);
            var chainNode = gridBase?.ownedPlaceable as BoxContainerChainNode;
            var fingerWorldPosition = raycastOperations.GetFingerPointOnGridPlane(mousePosition);

            if (!chainNode) return;


            _chain = chainNode.ContainerChain;
            if (_chain.IsInputBlocked)
            {
                _chain = null;
                return;
            }

            if (LevelManager.instance.isTutorialOn)
            {
                TutorialController.instance.HandleInput(StepType.Classic);
            }

            if (AudioManager.instance)
            {
                AudioManager.instance.PlaySound(popSound, true, false, 0.5f);
            }

            UIManager.instance.HandleTimer();
            _chain.SetCurrentHead(chainNode);
            _chain.OnSelect();

            selfSkipColliders.Clear();
            selfSkipColliders = _chain.GetSkipCollisionColliders();
            if (fingerWorldPosition != null) _lastFingerPosition = fingerWorldPosition.Value;
        }


        private void OnFingerHold()
        {
            if (!_chain) return;

            var mousePosition = Input.mousePosition;
            currentHoveringGrid =
                raycastOperations.GetObjectOfTypeWithinAllNonAlloc<GridBase>(gridLayer.value, mousePosition);

            var fingerWorldPosition = raycastOperations.GetFingerPointOnGridPlane(mousePosition);

            if (fingerWorldPosition != null)
            {
                var rawDelta = fingerWorldPosition.Value - _lastFingerPosition;
                rawDelta.y = 0;

                var clampedDelta = CheckCasts(rawDelta, _chain.CurrentHead);
                if (!_chain) return;

                clampedDelta.x = Mathf.Clamp(clampedDelta.x, -0.35f, 0.35f);
                clampedDelta.z = Mathf.Clamp(clampedDelta.z, -0.35f, 0.35f);
                _chain.OnUpdate(clampedDelta, currentHoveringGrid);
            }

            if (fingerWorldPosition != null) _lastFingerPosition = fingerWorldPosition.Value;
        }


        private void OnFingerUp()
        {
            if (!_chain) return;
            _chain.OnRelease();
            _chain = null;
        }


        private Vector3 CheckCasts(Vector3 inputDelta, BoxContainerChainNode head)
        {
            const float _directionOffset = 0.05f;
            const float _boxSize = 1f / 3.5f;

            var allowedDelta = inputDelta;
            var startPosition = head.transform.position;
            var halfExtents = Vector3.one * _boxSize;
            halfExtents.y = 0.435f;

            var hits = new RaycastHit[10];

            if (!Mathf.Approximately(inputDelta.x, 0f))
            {
                var direction = Vector3.right * Mathf.Sign(inputDelta.x);
                var origin = startPosition + direction * _directionOffset;

                var hitCount = Physics.BoxCastNonAlloc(origin, halfExtents, direction, hits, Quaternion.identity, 100f,
                    collisionLayer);

                for (var i = 0; i < hitCount; i++)
                {
                    var hit = hits[i];

                    if (SkipSelfHit(hit, head))
                    {
                        continue;
                    }

                    var adjustedDistance = Mathf.Max(0, hit.distance - 0.01f);
                    if (adjustedDistance < Mathf.Abs(allowedDelta.x))
                    {
                        allowedDelta.x = direction.x * adjustedDistance;
                    }
                }
            }

            if (!Mathf.Approximately(inputDelta.z, 0f))
            {
                var direction = Vector3.forward * Mathf.Sign(inputDelta.z);
                var origin = startPosition + direction * _directionOffset;

                var hitCount = Physics.BoxCastNonAlloc(origin, halfExtents, direction, hits, Quaternion.identity, 100f,
                    collisionLayer);

                for (var i = 0; i < hitCount; i++)
                {
                    var hit = hits[i];
                    if (SkipSelfHit(hit, head)) continue;

                    var adjustedDistance = Mathf.Max(0, hit.distance - 0.01f);
                    if (adjustedDistance < Mathf.Abs(allowedDelta.z))
                    {
                        allowedDelta.z = direction.z * adjustedDistance;
                    }
                }
            }

            allowedDelta.y = 0;
            return allowedDelta;
        }


        private bool SkipSelfHit(RaycastHit hit, BoxContainerChainNode head)
        {
            return selfSkipColliders.Contains(hit.collider);
        }

        public BoxContainerChain GetCurrentHoldingChain()
        {
            return _chain;
        }

        private bool ShouldProcessInput()
        {
            return LevelManager.instance.isGamePlayable
                   && !LevelManager.instance.isLevelFailed;
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (debugSpherePosition != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(debugSpherePosition.Value, debugSphereRadius);
            }
        }
#endif

        #endregion


        public void ForceRelease()
        {
            OnFingerUp();
        }
    }
}