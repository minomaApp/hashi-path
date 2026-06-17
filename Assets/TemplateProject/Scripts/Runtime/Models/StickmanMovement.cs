using System;
using System.Linq;
using TemplateProject.Scripts.Interfaces;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class StickmanMovement : MonoBehaviour, IPathFollower
    {
        [Header("Cached References")]        
        [SerializeField] private Animator anim;

        [Header("Parameters")]
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float stoppingDistance = 0.1f;
        private Vector3[] path;
        private int currentWaypointIndex;
        
        [Header("Flags")]
        private bool isMoving;
        private bool isPathSet;
        
        [Header("Actions")]        
        private Action onCompleteCallback;
        
        [Header("Constants")]
        private static readonly int isRunning = Animator.StringToHash("isRunning");

        private void OnEnable()
        {
            GameplayManager.instance.onGameLost += StopMovement;
        }

        private void OnDisable()
        {
            GameplayManager.instance.onGameLost -= StopMovement;
        }

        private void Update()
        {
            if (!isPathSet) return;
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (isMoving && path != null && currentWaypointIndex < path.Length)
            {
                MoveTowardsTarget();
            }
            else
            {
                if (LevelManager.instance.isLevelFailed) return;
                FinalizeMovement();
            }
        }

        private void FinalizeMovement()
        {
            StopMovement();
            isPathSet = false;
            if (onCompleteCallback == null) return;
            onCompleteCallback.Invoke();
            onCompleteCallback = null;
        }

        private void MoveTowardsTarget()
        {
            var targetPosition = path[currentWaypointIndex];
            var direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                transform.rotation =
                    Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, runSpeed * Time.deltaTime);

            if (!(Vector3.Distance(transform.position, targetPosition) <= stoppingDistance)) return;
            currentWaypointIndex++;
            if (currentWaypointIndex < path.Length) return;
            FinalizeMovement();
        }

        public void SetPath(Vector3[] newPath, Action onComplete = null)
        {
            path = newPath;
            currentWaypointIndex = 0;
            isPathSet = true;
            isMoving = true;
            onCompleteCallback = onComplete;
        }

        public void ChangePath(Vector3[] newPath, Action onComplete = null)
        {
            StopMovement();

            if (path != null)
            {
                var newPathList = path.ToList();
                newPathList.Add(newPath[^1]);
                path = newPathList.ToArray();
            }
            else
            {
                path = newPath;
            }

            isPathSet = true;

            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, path.Length - 1);
            onCompleteCallback = onComplete;
            StartMovement();
        }

        private void StopMovement()
        {
            if (path != null)
            {
                if (currentWaypointIndex >= path.Length - 1)
                {
                    currentWaypointIndex = 0;
                    path = null;
                }
                else
                {
                    var newList = path.ToList();
                    newList.RemoveAt(newList.Count - 1);
                    path = newList.ToArray();
                }
            }
            else
            {
                currentWaypointIndex = 0;
                path = null;
            }

            isPathSet = false;
            anim.SetBool(isRunning, false);
            isMoving = false;
        }

        private void StartMovement()
        {
            isMoving = true;
            anim.SetBool(isRunning, true);
        }

        public void Run(Vector3[] newPath, Action onComplete = null)
        {
            if (isMoving)
            {
                ChangePath(newPath, onComplete);
                return;
            }
            
            SetPath(newPath, onComplete);
            StartMovement();
        }

        public void Stop()
        {
            StopMovement();
        }
    }
}