using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class MatchingObject : MonoBehaviour
    {
        #region Variables

        [Header("Cached References")] [SerializeField]
        private Renderer objectRenderer;

        [SerializeField] private EnumHolder.GameColor color;
        [SerializeField] private ObjectSpawner belongedSpawner;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private GameObject questionMark;
        [SerializeField] private TrailRenderer trailRenderer;
        [AudioClipName] [SerializeField] private string placeSound;

        [Header("Parameters")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] private float arcHeight = 2f;

        private Transform _target;
        private BoxContainer _targetContainer;
        private bool _canMove;
        private bool _isPlaced;
        public bool isSecret;

        private Vector3 _startPos;
        private Vector3 _startScale;
        private float _travelTime;
        private float _elapsedTime;

        #endregion

        #region Properties

        public EnumHolder.GameColor Color
        {
            get => color;
            set => color = value;
        }

        public ObjectSpawner BelongedSpawner => belongedSpawner;

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            SetTrailColor();
            DisableTrail();
        }

        private void SetTrailColor()
        {
            trailRenderer.material.SetColor("_BaseColor", gameColors.editorColors[(int)color]);
        }

        private void Update()
        {
            if (_isPlaced)
            {
                if (!Mathf.Approximately(transform.localPosition.z, -0.002f))
                {
                    transform.localPosition = new Vector3(0f, 0f, -0.002f);
                }
            }

            if (_target && _canMove)
            {
                MoveToTarget();
            }
        }

        #endregion

        #region Custom Methods

        public void Init(ObjectSpawner spawner, bool secret)
        {
            belongedSpawner = spawner;
            isSecret = secret;
            questionMark.SetActive(isSecret);

            HandleColorSet();
        }

        private void HandleColorSet()
        {
            var material = isSecret ? gameColors.secretColor : gameColors.chainMaterials[(int)color];
            objectRenderer.sharedMaterial = material;
        }

        public void CloseSecret()
        {
            if (!isSecret) return;
            isSecret = false;
            questionMark.SetActive(false);
            objectRenderer.sharedMaterial = gameColors.chainMaterials[(int)color];
        }

        private void MoveToTarget()
        {
            _elapsedTime += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsedTime / _travelTime);

            var currentTargetPos = _target.position;
            var currentTargetScale = _startScale;
            var linearPos = Vector3.Lerp(_startPos, currentTargetPos, t);
            var linearScale = Vector3.Lerp(_startScale, currentTargetScale, t);
            var heightOffset = 4f * arcHeight * t * (1f - t);
            var scaleOffset = 3f * arcHeight * t * (1f - t);
            transform.position = linearPos + Vector3.up * heightOffset;
            transform.localScale = linearScale + Vector3.one * scaleOffset;
            ;

            if (t >= 1f)
                PlaceToTarget();
        }

        private void PlaceToTarget()
        {
            _canMove = false;
            if (!_targetContainer) return;
            _targetContainer.blackPlaceholder.SetActive(false);
            transform.SetParent(_target);
            transform.localPosition = Vector3.zero;
            _targetContainer.PlayParticle();
            _targetContainer.HandleCap(this, placeSound);
            _isPlaced = true;
        }

        public void SetTarget(Transform currentTarget, BoxContainer boxContainer)
        {
            transform.SetParent(null);
            _targetContainer = boxContainer;
            _target = currentTarget;
            _canMove = true;
            EnableTrail();
            _startPos = transform.position;
            _startScale = transform.localScale;
            _elapsedTime = 0f;
            _travelTime = moveSpeed;
            if (_travelTime <= 0f) _travelTime = 0.1f;
        }


        private void EnableTrail()
        {
            trailRenderer.emitting = true;
        }

        public void DisableTrail()
        {
            trailRenderer.emitting = false;
        }

        #endregion
    }
}