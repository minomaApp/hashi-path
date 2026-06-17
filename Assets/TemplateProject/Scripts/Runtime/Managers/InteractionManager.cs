using BoxPuller.Scripts.Data;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Data.SO;
using TemplateProject.Scripts.Runtime.Models;
using TemplateProject.Scripts.Utilities;
using UnityEngine;

// using TemplateProject.Scripts.Runtime.Models.Marble;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("Cached References")] private Camera _mainCam;
        [SerializeField] private BoxContainer currentlySelectedContainer;

        [Header("Parameters")] public LayerMask containerLayer;
        [AudioClipName] public string popSound;
        [SerializeField] private bool isHolding;


        private void Start()
        {
            AssignMainCam();
        }

        private void AssignMainCam()
        {
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (!ShouldProcessInput()) return;

            if (Input.GetMouseButtonDown(0) && !currentlySelectedContainer && !isHolding)
            {
                // HandleTimerStart();
                ProcessRaycastInteraction();
                isHolding = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isHolding = false;

                if (currentlySelectedContainer)
                {
                    currentlySelectedContainer.GetChain().OnRelease();
                    // group.DisableOutlines();
                    currentlySelectedContainer = null;
                }
            }

            if (currentlySelectedContainer && isHolding)
            {
                HandleContainerDrag();
            }
        }

        private void HandleContainerDrag()
        {
            // currentlySelectedContainer.GetChain().OnUpdate();
        }


        public void SetLevelContainer(LevelContainer container)
        {
            // levelContainer = container;
        }

        private bool ShouldProcessInput()
        {
            return LevelManager.instance.isGamePlayable
                   && !LevelManager.instance.isLevelFailed;
        }

        void ProcessRaycastInteraction()
        {
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);

            if (!TryRayCast(ray, out var hitInfo, containerLayer)) return;
            if (!hitInfo.transform || !hitInfo.transform.CompareTag("BoxContainer")) return;
            TrySelectContainer(hitInfo);
        }

        private bool TryRayCast(Ray ray, out RaycastHit hitInfo, LayerMask layer)
        {
            return Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layer);
        }

        private void TrySelectContainer(RaycastHit hitInfo)
        {
            if (!hitInfo.transform.TryGetComponent(out BoxContainer container)) return;

            if (!container.GetBelongedGrid()) return;
            // var containerGroup = container.GetContainerGroup();
            // if (!containerGroup) return;
            // if (containerGroup.GetFirstContainer() != container &&
            //     containerGroup.GetLastContainer() != container) return;


            // if (!marble.GetHasPath() && marble.GetBelongedGrid().GetYAxis() != 0)
            // {
            //     // marble.WrongSelection();
            //     if (VibrationManager.instance)
            //     {
            //         VibrationManager.instance.Medium();
            //     }
            //
            //     return;
            // }

            if (LevelManager.instance.isTutorialOn)
            {
                TutorialController.instance.HandleInput(StepType.Classic);
            }

            if (AudioManager.instance)
            {
                AudioManager.instance.PlaySound(popSound);
            }

            currentlySelectedContainer = container;
            currentlySelectedContainer.GetChain().OnSelect();
        }
        
        private Vector3 GetMouseWorldPosition()
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);

            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : Vector3.zero;
        }
    }
}