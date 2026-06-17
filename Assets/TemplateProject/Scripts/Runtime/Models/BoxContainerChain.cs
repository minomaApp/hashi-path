using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Data.SO;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Data.SO;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Utilities;
using TemplateProject.Scripts.Utilities.EditorUtilities.InspectorAttributes;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class BoxContainerChain : MonoBehaviour
    {
        #region Variables

        [SerializeField] private LayerMask gridLayer;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float pathSpeed = 10f;

        public EnumHolder.GameColor color;

        public List<BoxContainerChainNode> chainNodes;

        public List<Node> nodes;
        public List<Node> midNodes;

        [HideInInspector] public Material nodeMaterial;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private GamePrefabs gamePrefabs;
        public int foodCount;

        [SerializeField] private float delayBetweenNodes = 0.006f;
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private float yIncrement = 1.5f;
        [SerializeField] private int completeCount;
        [AudioClipName] [SerializeField] private string containerSnapSound;
        [ReadOnly] [SerializeField] private bool _isFrozen = false;
        [ReadOnly] [SerializeField] private int _iceCount;

        [ReadOnly] [SerializeField] public bool isHoldingHead = true;
        [ReadOnly] [SerializeField] public bool isPushingBack;
        private BoxContainerChainNode _currentHoldingHeadNode;
        private BoxContainerChainNode _currentSnappingHeadNode;

        private bool isAbleToPushBack = true;
        private bool isOnPathMovement;

        private GridBase pathTargetGridBase;
        private List<GridBase> path;

        private GridManager _gridManagerInstance;
        private Pathfinder<GridBase> _pathfinder;
        private Sequence mexicanWaveSequence;
        private PoolManager _poolManagerInstance;

        #endregion


        #region Properties

        public BoxContainerChainNode CurrentHead => _currentHoldingHeadNode;

        public int TotalNodeCount => chainNodes.Count;

        private float NodeOffset => (chainNodes.Count - 1) / (float)(TotalNodeCount - 1);

        public bool IsFrozen
        {
            get => _isFrozen;
            set => _isFrozen = value;
        }

        public int IceCount
        {
            get => _iceCount;
            set => _iceCount = value;
        }

        public bool IsInputBlocked => _isFrozen;

        public List<BoxContainerChainNode> Cells => chainNodes;

        public List<Node> AllNodes => nodes.Concat(midNodes).ToList();

        private bool IsChainLongerThanThreeAndEven => chainNodes.Count > 3 && chainNodes.Count % 2 == 0;

        #endregion


        #region Creator Setup

        public void Init(Material material, GridBase[,] gridBase)
        {
            nodeMaterial = material;

            for (var i = 0; i < chainNodes.Count; i++)
            {
                var chainCell = chainNodes[i];
                if (i + 1 < chainNodes.Count)
                    chainCell.BackwardNode = chainNodes[i + 1];
                if (i - 1 >= 0)
                    chainCell.ForwardNode = chainNodes[i - 1];
            }

            _gridManagerInstance = FindFirstObjectByType<GridManager>();

            _currentHoldingHeadNode = chainNodes[0];

            //SpawnChainRings();
        }


        public void AddCell(BoxContainerChainNode chainCell)
        {
            chainNodes.Add(chainCell);
        }


        public void ResetNodes()
        {
            nodes.Clear();
            midNodes.Clear();
        }


        public void AddNode(Node ringScript)
        {
            nodes.Add(ringScript);
        }

        public void AddMidNode(Node ringScript)
        {
            midNodes.Add(ringScript);
        }

        #endregion


        #region Unity Methods

        private void Start()
        {
            _gridManagerInstance = GridManager.Instance;
            _pathfinder = new Pathfinder<GridBase>(_gridManagerInstance);
            _poolManagerInstance = PoolManager.instance;

            chainNodes.ForEach(cell => cell.color = color);
            _gridManagerInstance.chains ??= new List<BoxContainerChain>();
            if (!_gridManagerInstance.chains.Contains(this))
            {
                _gridManagerInstance.chains.Add(this);
            }

            if (_isFrozen)
            {
                _gridManagerInstance.OnChainComplete += BreakIce;
            }
        }


        private void OnDestroy()
        {
            _gridManagerInstance.OnChainComplete -= BreakIce;
        }

        #endregion


        #region Custom Methods

        public void OnSelect()
        {
            isAbleToPushBack = true;
            isOnPathMovement = false;
            pathTargetGridBase = null;
            chainNodes.ForEach(cell => cell.CurrentGridBase.SetHover(true));
        }


        public void OnUpdate(Vector3? fingerDelta, GridBase hoveredGrid)
        {
            if (hoveredGrid && hoveredGrid != pathTargetGridBase &&
                !_gridManagerInstance.IsNeighbor(hoveredGrid, _currentHoldingHeadNode.CurrentGridBase))
            {
                if ((hoveredGrid.IsEmpty() || hoveredGrid.ownedPlaceable.color == color))
                {
                    path = _pathfinder.FindPathGridBase(_currentHoldingHeadNode.CurrentGridBase, hoveredGrid, color);

                    if (path is { Count: > 0 })
                    {
                        pathTargetGridBase = hoveredGrid;
                        isOnPathMovement = true;
                        if (path[0] == _currentHoldingHeadNode.CurrentGridBase)
                        {
                            path.RemoveAt(0);
                        }
                    }
                }
            }


            if (isOnPathMovement)
            {
                isPushingBack = false;
                _currentSnappingHeadNode = _currentHoldingHeadNode;
                if (path is null or { Count: <= 0 })
                {
                    isOnPathMovement = false;
                    return;
                }


                _currentHoldingHeadNode.transform.position = Vector3.MoveTowards(
                    _currentHoldingHeadNode.transform.position, path[0].transform.position, Time.deltaTime * pathSpeed);
                UpdateNodePositions(speed);

                if (Vector3.Distance(_currentHoldingHeadNode.transform.position, path[0].transform.position) < 0.15f)
                {
                    SnapHead(path[0]);
                    path.RemoveAt(0);
                }
            }
            else
            {
                if (fingerDelta == null)
                {
                    return;
                }

                var delta = fingerDelta.Value;
                if (delta.magnitude == 0)
                {
                    return;
                }

                var desiredPosition = _currentHoldingHeadNode.transform.position + delta;
                var directionToDesired = (desiredPosition - _currentHoldingHeadNode.transform.position).normalized;
                var directionFromPrev = (_currentHoldingHeadNode.transform.position - _currentHoldingHeadNode
                    .GetNextNode(isHoldingHead).CurrentGridBase.transform.position).normalized;
                var dot = Vector3.Dot(directionToDesired, directionFromPrev);
                isPushingBack = dot < 0.1f;


                if (isPushingBack && !isAbleToPushBack)
                {
                    var distanceToCurrentGrid = Vector3.Distance(_currentHoldingHeadNode.transform.position,
                        _currentHoldingHeadNode.CurrentGridBase.transform.position);
                    if (distanceToCurrentGrid >= 0.15f)
                    {
                        return;
                    }
                }

                _currentHoldingHeadNode.transform.position =
                    Vector3.MoveTowards(_currentHoldingHeadNode.transform.position, desiredPosition,
                        Time.deltaTime * speed);


                TrySnapHeadWithBoxCast();
                UpdateNodePositions(speed);
            }

            InstantUpdateMidNodes();
        }


        public void OnRelease()
        {
            _currentHoldingHeadNode.transform.position = _currentHoldingHeadNode.CurrentGridBase.transform.position;

            _currentHoldingHeadNode.transform.DOMove(_currentHoldingHeadNode.CurrentGridBase.transform.position, 0.1f)
                .OnUpdate(InstantUpdateMidNodes).OnComplete(InstantUpdateMidNodes);
            chainNodes.ForEach(cell =>
            {
                cell.transform.position = cell.CurrentGridBase.transform.position;
                cell.CurrentGridBase.SetHover(false);
            });

            var pts = CalculateChainNodePositions();
            for (var i = 0; i < nodes.Count; i++)
            {
                nodes[i].transform.DOMove(pts[i], 0.1f);
            }
        }


        #region Snapping

        private void TrySnapHeadWithBoxCast()
        {
            var halfCellSize = 0.1f;

            _currentSnappingHeadNode = _currentHoldingHeadNode;
            if (isPushingBack)
            {
                _currentSnappingHeadNode = GetUnselectedHead();
            }

            var castOrigin = _currentSnappingHeadNode.transform.position + Vector3.up * 3f;
            var halfExtents = Vector3.one * halfCellSize;
            var castDirection = Vector3.down;
            var castDistance = 5f;

            var hits = Physics.BoxCastAll(
                castOrigin,
                halfExtents,
                castDirection,
                Quaternion.identity,
                castDistance,
                gridLayer
            );

            foreach (var hit in hits)
            {
                if (!hit.collider.TryGetComponent(out GridBase newGrid)) continue;
                if (!newGrid.IsEmpty())
                {
                    if (newGrid.ownedPlaceable.color != color || !newGrid.ownedPlaceable.isActive)
                    {
                        continue;
                    }


                    if (chainNodes.Contains(newGrid.ownedPlaceable))
                    {
                        int targetIndex;

                        if (isHoldingHead)
                        {
                            if (isPushingBack)
                            {
                                targetIndex = 0;
                            }
                            else
                            {
                                targetIndex = chainNodes.Count - 1;
                            }
                        }
                        else
                        {
                            if (isPushingBack)
                            {
                                targetIndex = chainNodes.Count - 1;
                            }
                            else
                            {
                                targetIndex = 0;
                            }
                        }


                        var isSnapCondition = newGrid.ownedPlaceable == chainNodes[targetIndex] &&
                                              IsChainLongerThanThreeAndEven;


                        if (!isSnapCondition)
                        {
                            continue;
                        }
                    }
                }


                if (!_gridManagerInstance.IsNeighbor(_currentSnappingHeadNode.CurrentGridBase, newGrid))
                {
                    continue;
                }

                SnapHead(newGrid);

                break;
            }
        }


        private void SnapHead(GridBase gridBase)
        {
            if (gridBase == _currentSnappingHeadNode.CurrentGridBase)
            {
                return;
            }

            _currentSnappingHeadNode.CurrentGridBase.SetHover(false);
            SnapAllBodyPartsToGrid();
            _currentSnappingHeadNode.SetGrid(gridBase);
            _currentSnappingHeadNode.CheckForColorMatch(gridBase);
            _currentSnappingHeadNode.CurrentGridBase.SetHover(true);


            AudioManager.instance.PlaySound(containerSnapSound, true, false, 0.5f);
            // HapticManager.Instance.TriggerHaptic(HapticType.RigidImpact);
        }


        private void SnapAllBodyPartsToGrid()
        {
            chainNodes.ForEach(cell => cell.CurrentGridBase.SetHover(false));

            int start, end, step;

            if (isHoldingHead)
            {
                if (isPushingBack)
                {
                    start = 0;
                    end = chainNodes.Count;
                    step = 1;
                }
                else
                {
                    start = chainNodes.Count - 1;
                    end = -1;
                    step = -1;
                }
            }
            else
            {
                if (isPushingBack)
                {
                    start = chainNodes.Count - 1;
                    end = -1;
                    step = -1;
                }
                else
                {
                    start = 0;
                    end = chainNodes.Count;
                    step = 1;
                }
            }

            for (int i = start; i != end; i += step)
            {
                var cell = chainNodes[i];
                if (cell == _currentSnappingHeadNode)
                {
                    continue;
                }

                var targetGrid = chainNodes[i + step].CurrentGridBase;

                cell.SetGrid(targetGrid);
                cell.CheckForColorMatch(targetGrid);
                if (cell != _currentHoldingHeadNode)
                {
                    cell.transform.position = targetGrid.transform.position;
                }

                cell.CurrentGridBase.SetHover(true);
            }
        }

        #endregion


        #region Rings

        public List<Vector3> CalculateChainNodePositions(GridBase[,] gridBases = null)
        {
            var ringPositions = new List<Vector3>();

            var currentCellIndex = 0;
            var currentCell = chainNodes[currentCellIndex];
            var nextCell = chainNodes[1];
            var currentPosition = currentCell.transform.position;
            ringPositions.Add(currentPosition);
            nodes[0].SetTargetPos(nextCell.CurrentGridBase.transform.position);

            for (var i = 1; i < TotalNodeCount; i++)
            {
                var moveDistance = NodeOffset;
                var directionToNextCell =
                    (nextCell.CurrentGridBase.transform.position - currentCell.CurrentGridBase.transform.position)
                    .normalized;

                if (currentCellIndex == 0)
                {
                    directionToNextCell = (nextCell.CurrentGridBase.transform.position - currentCell.transform.position)
                        .normalized;
                }

                var distanceToNextCell = Vector3.Distance(currentPosition, nextCell.CurrentGridBase.transform.position);
                if (distanceToNextCell <= NodeOffset)
                {
                    currentCellIndex++;

                    if (currentCellIndex < chainNodes.Count) currentCell = chainNodes[currentCellIndex];

                    if (currentCellIndex < chainNodes.Count - 1)
                    {
                        nextCell = chainNodes[currentCellIndex + 1];
                        directionToNextCell =
                            (nextCell.CurrentGridBase.transform.position -
                             currentCell.CurrentGridBase.transform.position).normalized;
                    }
                    else
                    {
                        var freeGrid = GetUnselectedHead().PrevGridBase;
                        if (!freeGrid || !freeGrid.IsEmpty())
                        {
                            if (!_gridManagerInstance)
                            {
                                _gridManagerInstance = GridManager.instance;
                            }

                            freeGrid =
                                _gridManagerInstance.GetEmptyNeighbor(GetUnselectedHead().CurrentGridBase.Position);
                        }

                        if (!freeGrid)
                        {
                            //directionToNextCell = Vector3.zero;
                            distanceToNextCell = NodeOffset;
                        }
                        else
                        {
                            directionToNextCell =
                                (freeGrid.transform.position - currentCell.CurrentGridBase.transform.position)
                                .normalized;
                        }
                    }

                    moveDistance = NodeOffset - distanceToNextCell;

                    currentPosition = currentCell.CurrentGridBase.transform.position;
                }

                currentPosition += directionToNextCell * moveDistance;
                ringPositions.Add(currentPosition);
                nodes[i].SetTargetPos(nextCell.CurrentGridBase.transform.position);
            }

            return ringPositions;
        }

        private void UpdateNodePositions(float currentSpeed)
        {
            var headToHeadGridDirection =
                (_currentHoldingHeadNode.transform.position -
                 _currentHoldingHeadNode.CurrentGridBase.transform.position).normalized;
            var secondCell = _currentHoldingHeadNode.GetNextNode(isHoldingHead);
            var secondCellToHeadDirection = (_currentHoldingHeadNode.CurrentGridBase.transform.position -
                                             secondCell.CurrentGridBase.transform.position).normalized;
            var dot = Vector3.Dot(headToHeadGridDirection, secondCellToHeadDirection);

            var isHeadOnHalfSpace = dot <= 0;


            var currentCellIndex = chainNodes.IndexOf(_currentHoldingHeadNode);
            var currentPosition = _currentHoldingHeadNode.transform.position;

            var nodeTarget = _currentHoldingHeadNode.CurrentGridBase.transform.position;


            var currentNodeIndex = isHoldingHead ? 0 : nodes.Count - 1;
            var step = isHoldingHead ? 1 : -1;
            var endIndex = isHoldingHead ? nodes.Count : -1;

            if (isHeadOnHalfSpace)
            {
                nodeTarget = _currentHoldingHeadNode.GetNextNode(isHoldingHead).CurrentGridBase.transform.position;
                currentCellIndex += step;
            }

            nodes[currentNodeIndex].transform.position = currentPosition;

            nodes[currentNodeIndex].SetTargetPos(nodeTarget);
            nodes[currentNodeIndex].ResetForward();
            nodes[currentNodeIndex].transform.position = Vector3.MoveTowards(nodes[currentNodeIndex].transform.position,
                currentPosition, Time.deltaTime * speed);

            var currentMoveDistance = NodeOffset;

            currentNodeIndex += step;

            while (currentNodeIndex != endIndex)
            {
                var node = nodes[currentNodeIndex];
                var prevNode = nodes[currentNodeIndex - step];
                var nextPosition = currentPosition + prevNode.GetTargetForward() * NodeOffset;

                if (IsPositionPassedDot(nextPosition, prevNode))
                {
                    var distanceToTarget = Vector3.Distance(currentPosition, prevNode.targetPos);
                    currentMoveDistance = Mathf.Abs(NodeOffset - distanceToTarget);
                    currentPosition = prevNode.targetPos;
                    currentCellIndex += step;

                    if (currentCellIndex >= 0 && currentCellIndex < chainNodes.Count)
                    {
                        nodeTarget = chainNodes[currentCellIndex].CurrentGridBase.transform.position;
                        node.SetTargetForward((nodeTarget - currentPosition).normalized);
                        node.SetTargetPos(nodeTarget);
                    }
                    else
                    {
                        var freeGrid = GetUnselectedHead().PrevGridBase;
                        if (!freeGrid || !freeGrid.IsEmpty())
                        {
                            freeGrid = _gridManagerInstance.GetEmptyNeighbor(GetUnselectedHead().CurrentGridBase
                                .Position);
                        }

                        if (freeGrid)
                        {
                            nodeTarget = freeGrid.transform.position;
                            node.SetTargetForward((nodeTarget - currentPosition).normalized);
                            node.SetTargetPos(nodeTarget);
                            GetUnselectedHead().transform.position = nodes[endIndex - step].transform.position;
                        }
                        else
                        {
                            if (_gridManagerInstance.IsNeighbor(_currentHoldingHeadNode.CurrentGridBase,
                                    GetUnselectedHead().CurrentGridBase) && IsChainLongerThanThreeAndEven)
                            {
                                nodeTarget = _currentHoldingHeadNode.CurrentGridBase.transform.position;
                                node.SetTargetForward((nodeTarget - currentPosition).normalized);
                                node.SetTargetPos(nodeTarget);
                                GetUnselectedHead().transform.position = nodes[endIndex - step].transform.position;
                            }
                            else
                            {
                                isAbleToPushBack = false;
                                node.SetTargetForward(Vector3.zero);
                            }
                        }
                    }
                }
                else
                {
                    node.SetTargetPos(prevNode.targetPos);
                    node.SetTargetForward(prevNode.GetTargetForward());
                    currentMoveDistance = NodeOffset;
                }

                currentPosition += node.GetTargetForward() * currentMoveDistance;

                node.transform.position = Vector3.MoveTowards(node.transform.position, currentPosition,
                    Time.deltaTime * currentSpeed);

                currentNodeIndex += step;
            }
        }


        private void InstantUpdateMidNodes()
        {
            for (var i = 0; i < midNodes.Count; i++)
            {
                var midNode = midNodes[i];
                var midPos = midNode.transform.position;
                midPos.y = 0;
                var ringPos = nodes[i].transform.position;
                ringPos.y = 0;
                var directionToNode = (ringPos - midPos).normalized;
                midNodes[i].transform.rotation = Quaternion.LookRotation(directionToNode);
                midNode.transform.eulerAngles =
                    new Vector3(midNode.transform.eulerAngles.x, midNode.transform.eulerAngles.y, 90);
            }
        }

        #endregion


        #region Utility

        public void IncreaseCompleteCount()
        {
            completeCount++;
            if (completeCount < nodes.Count) return;
            WaveAnim(false).OnComplete(() =>
            {
                DOVirtual.DelayedCall(0.25f, () =>
                {
                    foreach (var chainNode in chainNodes)
                    {
                        chainNode.CurrentGridBase.SetEmpty();
                    }

                    var currentChain = ChainCollisionHandler.Instance.GetCurrentHoldingChain();
                    if (currentChain && currentChain == this)
                    {
                        ChainCollisionHandler.Instance.ForceRelease();
                    }

                    gameObject.SetActive(false);
                });
            });
            GridManager.instance.IncreaseCompleteChainCount();
        }

        public List<Collider> GetSkipCollisionColliders()
        {
            var colliders = new List<Collider>();

            var start = isHoldingHead ? 0 : nodes.Count - 1;
            var step = isHoldingHead ? 1 : -1;
            var endIndex = isHoldingHead ? Mathf.Min(nodes.Count - 1, 3) : Mathf.Max(0, nodes.Count - 3);

            if (nodes.Count < 3)
            {
                endIndex = isHoldingHead ? 2 : -1;
            }

            for (var i = start; i != endIndex; i += step)
            {
                var ring = nodes[i];
                colliders.Add(ring.collider);
            }


            if (!IsChainLongerThanThreeAndEven) return colliders;
            colliders.Add(isHoldingHead ? nodes[^1].collider : nodes[0].collider);


            return colliders;
        }


        public void SetCurrentHead(BoxContainerChainNode chainCell)
        {
            var index = chainNodes.IndexOf(chainCell);
            if (index < chainNodes.Count / 2)
            {
                isHoldingHead = true;
                _currentHoldingHeadNode = chainNodes[0];
            }
            else
            {
                isHoldingHead = false;
                _currentHoldingHeadNode = chainNodes[^1];
            }
        }


        private bool IsPositionPassedDot(Vector3 position, Node ring)
        {
            var directionToTarget = (ring.targetPos - position).normalized;
            var dot = Vector3.Dot(directionToTarget, ring.GetTargetForward());
            return dot <= 0;
        }


        private BoxContainerChainNode GetUnselectedHead()
        {
            return _currentHoldingHeadNode == chainNodes[0] ? chainNodes[^1] : chainNodes[0];
        }


        private async UniTask<ParticleSystem> PlayParticle(string tag, Transform parent, Vector3 position,
            bool makeColored = false)
        {
            var particle = _poolManagerInstance.GetPoolObject(tag).GetComponent<ParticleSystem>();
            particle.transform.SetParent(parent);
            particle.transform.position = position;
            var main = particle.main;
            if (makeColored)
            {
                main.startColor = nodeMaterial.color;
            }

            var totalTime = main.startLifetime.constant + 0.2f;

            particle.Play();
            await UniTask.WaitForSeconds(totalTime);

            PoolManager.instance.ReleaseObject(tag, particle.gameObject);

            return particle;
        }

        private Tween WaveAnim(bool growFromEnd)
        {
            int start, end, step;
            if (growFromEnd)
            {
                start = 0;
                end = nodes.Count;
                step = 1;
            }
            else
            {
                start = nodes.Count - 1;
                end = -1;
                step = -1;
            }

            if (mexicanWaveSequence != null)
            {
                mexicanWaveSequence.Kill();
            }

            mexicanWaveSequence = DOTween.Sequence();
            var delay = 0f;
            for (var i = start; i != end; i += step)
            {
                var node = nodes[i];

                if (i >= 0 && i < midNodes.Count)
                {
                    delay += delayBetweenNodes / 2;
                    var midNode = midNodes[i];
                    var oldScale = midNode.visual.transform.localScale;
                    mexicanWaveSequence.Join(midNode.visual.transform.DOScale(oldScale * 1.15f, animationDuration)
                        .SetDelay(delay).OnComplete(() =>
                        {
                            midNode.visual.transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
                        }));
                    mexicanWaveSequence.Join(midNode.visual.transform.DOLocalMoveX(yIncrement, animationDuration)
                        .SetDelay(delay).SetEase(Ease.OutQuad).OnComplete(() =>
                        {
                            midNode.visual.transform.DOLocalMoveX(0, animationDuration).SetEase(Ease.InQuad);
                        }));
                }

                var oldNodeScale = node.visual.transform.localScale;
                mexicanWaveSequence.Join(node.visual.transform.DOScale(oldNodeScale * 1.15f, animationDuration)
                    .SetDelay(delay).OnComplete(() =>
                    {
                        var confetti = Instantiate(gamePrefabs.completeConfettiPrefab,
                            node.transform.position + new Vector3(0f, 1.5f, 0f), Quaternion.identity) as GameObject;
                        confetti.transform.localEulerAngles = new Vector3(-100f, 0f, 0f);
                        Destroy(confetti, 2f);
                        node.visual.transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                            .OnComplete(() =>
                            {
                                foreach (var cell in chainNodes)
                                {
                                    cell.CurrentGridBase.SetEmpty();
                                }
                            });
                    }));
                mexicanWaveSequence.Join(
                    node.visual.transform.DOLocalMoveY(yIncrement, animationDuration).SetDelay(delay)
                        .SetEase(Ease.OutQuad).OnComplete(() =>
                        {
                            node.visual.transform.DOLocalMoveY(0, animationDuration).SetEase(Ease.InQuad);
                        }));


                delay += delayBetweenNodes;
            }

            mexicanWaveSequence.OnUpdate(InstantUpdateMidNodes);
            return mexicanWaveSequence;
        }

        #endregion

        #endregion

        #region Ice

        private void BreakIce()
        {
            if (!_isFrozen) return;
            _iceCount--;

            if (_iceCount <= 0)
            {
                _isFrozen = false;
                _iceCount = 0;

                foreach (var node in nodes)
                {
                    var materials = new Material[node.visual.materials.Length];
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = nodeMaterial;
                    }

                    node.visual.materials = materials;
                    // node.visual.materials[1] = gameColors.chainInsideMaterials[(int)color];

                    foreach (var subRenderer in node.subRenderers)
                    {
                        subRenderer.material = gameColors.ballMaterial;
                    }

                    node.insideBoxRenderer.material = gameColors.chainInsideMaterials[(int)color];
                }

                foreach (var midNode in midNodes)
                {
                    midNode.visual.material = gameColors.connectorMaterial;
                }
                
                if (LevelManager.instance.isTutorialOn)
                {
                    TutorialController.instance.HandleInput(StepType.EventTriggered);
                }
            }

            chainNodes.ForEach(cell => { cell.UpdateIce(_iceCount); });
        }

        #endregion
    }
}