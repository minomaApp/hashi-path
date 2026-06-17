using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    [RequireComponent(typeof(BoxContainerGroup))]
    public class BoxContainerMovement : MonoBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float lerpSpeed = 10f;

        [SerializeField] private float headSpeed = 10f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float snapDuration = 0.2f;
        [SerializeField] private float followSpeed = 10f;
        [SerializeField] private BoxContainerGroup group;
        private List<BoxContainer> _containers;
        public bool IsDragging { get; private set; }
        private bool _draggingTail;
        private bool _isMoving;
        private bool _pendingSnap;
        private readonly List<Vector2Int> _headHistory = new List<Vector2Int>();

        private readonly List<Vector2Int> _tailHistory = new List<Vector2Int>();


        private void Awake()
        {
            lerpSpeed = 5f;
            rotationSpeed = 10f;
            snapDuration = 0.2f;
        }

        public void Init(BoxContainerGroup containerGroup)
        {
            group = containerGroup;
        }

        public void StartDrag(BoxContainer clicked)
        {
            _containers = group.GetContainers();
            var idx = _containers.IndexOf(clicked);
            if (idx < 0) return;
            if (idx != 0 && idx != _containers.Count - 1) return;
            _draggingTail = (idx == _containers.Count - 1);
            if (_draggingTail)
                _containers.Reverse();
            IsDragging = true;
            _isMoving = false;
            _pendingSnap = false;
        }

        public void Drag(Vector3 worldPos)
        {
            worldPos.y = 0f;
            if (!IsDragging || _containers == null) return;
            if (_isMoving) return;

            var headCell = GridManager.instance.WorldToCell(_containers[0].transform.position);
            var pointerCell = GridManager.instance.WorldToCell(worldPos);
            var diff = pointerCell - headCell;

            // 1. Serbest hareket: işaretçi aynı hücredeyse baş kutuyu hücre içinde hareket ettir
            if (diff == Vector2Int.zero)
            {
                // Baş kutunun bulunduğu hücrenin merkezi ve boyutu
                Vector3 cellCenter = GridManager.instance.GetCellCenter(headCell);
                Vector3 neighborCenter = GridManager.instance.GetCellCenter(headCell + new Vector2Int(1, 0));
                float cellWidth = Vector3.Distance(cellCenter, neighborCenter);
                if (cellWidth < 0.001f)
                {
                    neighborCenter = GridManager.instance.GetCellCenter(headCell + new Vector2Int(0, 1));
                    cellWidth = Vector3.Distance(cellCenter, neighborCenter);
                }

                if (cellWidth < 0.001f)
                {
                    cellWidth = 1f;
                }

                float halfWidth = cellWidth / 2f;
                // Baş kutunun hareket edebileceği sınırlar (hücre kenarları)
                // Pointer konumunu hücre sınırları içine sıkıştır
                float minX = cellCenter.x - halfWidth;
                float maxX = cellCenter.x + halfWidth;
                float minZ = cellCenter.z - halfWidth;
                float maxZ = cellCenter.z + halfWidth;
                Vector3 clampedPos = new Vector3(
                    Mathf.Clamp(worldPos.x, minX, maxX),
                    0f,
                    Mathf.Clamp(worldPos.z, minZ, maxZ)
                );
                // Baş kutuyu işaretçi yönünde hücre içinde hareket ettir
                _containers[0].transform.position = Vector3.Lerp(
                    _containers[0].transform.position, clampedPos, Time.deltaTime * lerpSpeed
                );

                // Baş kutuyu işaretçinin yönüne bakacak şekilde döndür (rotasyonu ±90° ile sınırla)
                Vector3 toPointer = clampedPos - _containers[0].transform.position;
                if (toPointer.sqrMagnitude > 0.0001f)
                {
                    float currentY = _containers[0].transform.rotation.eulerAngles.y;
                    float desiredAngle = Mathf.Atan2(toPointer.x, toPointer.z) * Mathf.Rad2Deg;
                    float deltaAngle = Mathf.DeltaAngle(currentY, desiredAngle);
                    deltaAngle = Mathf.Clamp(deltaAngle, -90f, 90f);
                    float targetAngle = currentY + deltaAngle;
                    Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);
                    _containers[0].transform.rotation = Quaternion.RotateTowards(
                        _containers[0].transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }

                // Arkadaki kutuların mesafesini koruması için hareket ettir
                float desiredDistance = cellWidth;
                for (int i = 1; i < _containers.Count; i++)
                {
                    var prev = _containers[i - 1];
                    var curr = _containers[i];
                    Vector3 direction = prev.transform.position - curr.transform.position;
                    direction.y = 0f;
                    float currentDist = direction.magnitude;

                    if (currentDist > desiredDistance)
                    {
                        // Mesafe açıldı: arkadaki kutuyu öne doğru hareket ettir
                        float moveStep = followSpeed * Time.deltaTime;
                        if (currentDist - desiredDistance < moveStep)
                        {
                            moveStep = currentDist - desiredDistance;
                        }

                        curr.transform.position += direction.normalized * moveStep;
                    }
                    else if (currentDist < desiredDistance)
                    {
                        // Mesafe kapandı: arkadaki kutuyu geriye it
                        float moveStep = followSpeed * Time.deltaTime;
                        if (desiredDistance - currentDist < moveStep)
                        {
                            moveStep = desiredDistance - currentDist;
                        }

                        if (i == _containers.Count - 1)
                        {
                            // Eğer son kutu sınırdaysa ve arkada boş hücre yoksa, dışarı itmiyoruz
                            Vector2Int tailCell = GridManager.instance.WorldToCell(curr.transform.position);
                            Vector2Int prevCell = GridManager.instance.WorldToCell(prev.transform.position);
                            Vector2Int tailDir = tailCell - prevCell;
                            Vector3 neighborPos = GridManager.instance.GetCellCenter(tailCell + tailDir);
                            if (neighborPos != Vector3.zero)
                            {
                                curr.transform.position -= direction.normalized * moveStep;
                            }
                        }
                        else
                        {
                            curr.transform.position -= direction.normalized * moveStep;
                        }
                    }
                }

                return;
            }

            // 2. Hücreler arası hareket: işaretçi komşu bir hücreye geçerse adım at
            Vector2Int stepDir;
            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                stepDir = new Vector2Int(diff.x > 0 ? 1 : -1, 0);
            else
                stepDir = new Vector2Int(0, diff.y > 0 ? 1 : -1);

            var targetCell = headCell + stepDir;
            var nextCell = GridManager.instance.WorldToCell(_containers[1].transform.position);

            if (targetCell == nextCell)
            {
                // Baş kutu kendi gövdesine doğru geri gitmeye çalışıyorsa (ters yönde)
                MoveBackwardStep().Forget();
            }
            else
            {
                // Baş kutu ileri gitmeye çalışıyor (yeni hücreye doğru)
                var occupiedCells = new HashSet<Vector2Int>(_containers.Select(c =>
                    GridManager.instance.WorldToCell(c.transform.position)));
                if (occupiedCells.Contains(targetCell)) return;

                if (_containers.Count > 1)
                {
                    var headCellNow = GridManager.instance.WorldToCell(_containers[0].transform.position);
                    var secondCellNow = GridManager.instance.WorldToCell(_containers[1].transform.position);
                    if (headCellNow == secondCellNow)
                    {
                        Vector2Int prevCell = headCellNow - stepDir;
                        _containers[1].transform.position = GridManager.instance.GetCellCenter(prevCell);
                    }
                }

                MoveForwardStep(stepDir).Forget();
            }
        }

        public void EndDrag()
        {
            IsDragging = false;
            if (_isMoving)
            {
                _pendingSnap = true;
            }
            else
            {
                StartCoroutine(SmoothSnap());
            }
        }

        private IEnumerator SmoothSnap()
        {
            var startPositions = _containers.Select(c => c.transform.position).ToArray();
            var endPositions = _containers
                .Select(c => GridManager.instance.GetCellCenter(
                    GridManager.instance.WorldToCell(c.transform.position)))
                .ToArray();
            var startRots = _containers.Select(c => c.transform.rotation).ToArray();
            var endRots = new Quaternion[_containers.Count];
            for (var i = 0; i < _containers.Count; i++)
            {
                var y = startRots[i].eulerAngles.y;
                var snapped = Mathf.Round(y / 90f) * 90f;
                endRots[i] = Quaternion.Euler(0f, snapped, 0f);
            }

            var elapsed = 0f;
            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / snapDuration);
                for (var i = 0; i < _containers.Count; i++)
                {
                    _containers[i].transform.position = Vector3.Lerp(
                        startPositions[i], endPositions[i], t);
                    _containers[i].transform.rotation = Quaternion.Slerp(
                        startRots[i], endRots[i], t);
                }

                yield return null;
            }

            for (var i = 0; i < _containers.Count; i++)
            {
                _containers[i].transform.position = endPositions[i];
                _containers[i].transform.rotation = endRots[i];
            }
        }

        private void RotateListToFront(BoxContainer clicked)
        {
            _containers = group.GetContainers();
            var idx = _containers.IndexOf(clicked);
            if (idx < 0) return;
            if (idx == _containers.Count - 1)
                _containers.Reverse();
            else if (idx > 0)
            {
                var head = _containers.Skip(idx).ToList();
                head.AddRange(_containers.Take(idx));
                _containers = head;
            }
        }

        private async UniTask MoveForwardStep(Vector2Int direction)
        {
            _isMoving = true;
            var oldPositions = _containers.Select(c => c.transform.position).ToArray();
            var oldCells = _containers.Select(c => GridManager.instance.WorldToCell(c.transform.position)).ToArray();
            var n = _containers.Count;
            var newCells = new Vector2Int[n];

            newCells[0] = oldCells[0] + direction;
            for (var i = 1; i < n; i++)
            {
                newCells[i] = oldCells[i - 1];
            }

            var freedCell = oldCells[n - 1];
            if (_draggingTail)
                _tailHistory.Add(freedCell);
            else
                _headHistory.Add(freedCell);

            var targetPositions = new Vector3[n];
            for (var i = 0; i < n; i++)
            {
                targetPositions[i] = GridManager.instance.GetCellCenter(newCells[i]);
            }

            var targetRotations = new Quaternion[n];
            for (var i = 0; i < n; i++)
            {
                var moveVec = targetPositions[i] - oldPositions[i];
                moveVec.y = 0f;
                if (moveVec.sqrMagnitude > 0.0001f)
                {
                    // Rotasyonu mevcut yönelimden itibaren ±90° ile sınırla
                    var desiredAngle = Mathf.Atan2(moveVec.x, moveVec.z) * Mathf.Rad2Deg;
                    var currentY = _containers[i].transform.eulerAngles.y;
                    var deltaAngle = Mathf.DeltaAngle(currentY, desiredAngle);
                    deltaAngle = Mathf.Clamp(deltaAngle, -90f, 90f);
                    var targetAngle = currentY + deltaAngle;
                    targetRotations[i] = Quaternion.Euler(0f, targetAngle, 0f);
                }
                else
                {
                    targetRotations[i] = _containers[i].transform.rotation;
                }
            }

            var stepDistance = Vector3.Distance(oldPositions[0], targetPositions[0]);
            var moveDuration = (headSpeed > 0f) ? (stepDistance / headSpeed) : 0.1f;
            var tweenTasks = new List<Task>();
            for (var i = 0; i < n; i++)
            {
                tweenTasks.Add(_containers[i].transform.DOMove(targetPositions[i], moveDuration).SetEase(Ease.Linear)
                    .AsyncWaitForCompletion());
                tweenTasks.Add(_containers[i].transform.DORotateQuaternion(targetRotations[i], moveDuration)
                    .SetEase(Ease.Linear).AsyncWaitForCompletion());
            }

            await Task.WhenAll(tweenTasks);
            _isMoving = false;
            // if (!IsDragging && _pendingSnap)
            // {
            //     _pendingSnap = false;
            //     if (gameObject.activeInHierarchy)
            //         StartCoroutine(SmoothSnap());
            // }
        }

        private async UniTask MoveBackwardStep()
        {
            _isMoving = true;
            var oldPositions = _containers.Select(c => c.transform.position).ToArray();
            var oldCells = _containers.Select(c => GridManager.instance.WorldToCell(c.transform.position)).ToArray();
            var n = _containers.Count;
            var newCells = new Vector2Int[n];

            var history = _draggingTail ? _tailHistory : _headHistory;
            Vector2Int newEndCell;
            if (history.Count > 0)
            {
                newEndCell = history[^1];
                history.RemoveAt(history.Count - 1);
            }
            else
            {
                var endCell = oldCells[n - 1];
                var prevCell = oldCells[n - 2];
                var dir = endCell - prevCell;
                if (dir.x != 0) dir = new Vector2Int(dir.x > 0 ? 1 : -1, 0);
                else if (dir.y != 0) dir = new Vector2Int(0, dir.y > 0 ? 1 : -1);
                var cw = new Vector2Int(dir.y, -dir.x);
                var ccw = new Vector2Int(-dir.y, dir.x);
                var candidates = new Vector2Int[]
                {
                    endCell + dir,
                    endCell + cw,
                    endCell + ccw
                };
                newEndCell = Vector2Int.zero;
                var occupiedSet = new HashSet<Vector2Int>(oldCells);
                foreach (var cand in candidates)
                {
                    if (occupiedSet.Contains(cand)) continue;
                    var center = GridManager.instance.GetCellCenter(cand);
                    if (center == Vector3.zero) continue;
                    newEndCell = cand;
                    break;
                }

                if (newEndCell == Vector2Int.zero)
                {
                    _isMoving = false;
                    return;
                }
            }

            newCells[n - 1] = newEndCell;
            for (var i = 0; i < n - 1; i++)
            {
                newCells[i] = oldCells[i + 1];
            }

            var targetPositions = new Vector3[n];
            for (var i = 0; i < n; i++)
            {
                targetPositions[i] = GridManager.instance.GetCellCenter(newCells[i]);
            }

            var targetRotations = new Quaternion[n];
            for (var i = 0; i < n; i++)
            {
                var moveVec = targetPositions[i] - oldPositions[i];
                moveVec.y = 0f;
                if (moveVec.sqrMagnitude > 0.0001f)
                {
                    // Rotasyonu mevcut yönelimden itibaren ±90° ile sınırla
                    var desiredAngle = Mathf.Atan2(moveVec.x, moveVec.z) * Mathf.Rad2Deg;
                    var currentY = _containers[i].transform.eulerAngles.y;
                    var deltaAngle = Mathf.DeltaAngle(currentY, desiredAngle);
                    deltaAngle = Mathf.Clamp(deltaAngle, -90f, 90f);
                    var targetAngle = currentY + deltaAngle;
                    targetRotations[i] = Quaternion.Euler(0f, targetAngle, 0f);
                }
                else
                {
                    targetRotations[i] = _containers[i].transform.rotation;
                }
            }

            var stepDistance2 = Vector3.Distance(oldPositions[0], targetPositions[0]);
            var moveDuration2 = (headSpeed > 0f) ? (stepDistance2 / headSpeed) : 0.1f;
            var tweenTasks2 = new List<Task>();
            for (var i = 0; i < n; i++)
            {
                tweenTasks2.Add(_containers[i].transform.DOMove(targetPositions[i], moveDuration2).SetEase(Ease.Linear)
                    .AsyncWaitForCompletion());
                tweenTasks2.Add(_containers[i].transform.DORotateQuaternion(targetRotations[i], moveDuration2)
                    .SetEase(Ease.Linear).AsyncWaitForCompletion());
            }

            await Task.WhenAll(tweenTasks2);
            _isMoving = false;
            // if (!IsDragging && _pendingSnap)
            // {
            //     _pendingSnap = false;
            //     if (gameObject.activeInHierarchy)
            //         StartCoroutine(SmoothSnap());
            // }
        }
    }
}