using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Managers;
using Unity.Mathematics;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class Stickman : MonoBehaviour
    {
        [Header("Cached References")] 
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private Material startMat;
        [SerializeField] private Material secretMat;
        [SerializeField] private GameObject secretQuestionMark;
        [SerializeField] private Outline stickmanOutline;
        [SerializeField] private StickmanMovement stickmanMovement;
        [SerializeField] private GameObject reservedCap;
        [SerializeField] private GameObject disappearVFX;
        [SerializeField] private GameObject wrongSelectionObject;
        [SerializeField] private GridBase belongedGrid;
        private GameObject currentWrongObject;

        [Header("Parameters")] 
        [SerializeField] private EnumHolder.GameColor stickmanColorType;
        [SerializeField] private bool isMoving;
        [SerializeField] private bool hasPath;
        [SerializeField] private bool isSecret;
        [SerializeField] private bool isReserved;

        public void Init(EnumHolder.GameColor colorType, bool secret, bool reserved, GridBase gridCell)
        {
            isSecret = secret;
            isReserved = reserved;
            SetColor(colorType);
            belongedGrid = gridCell;
        }

        private void SetColor(EnumHolder.GameColor colorType)
        {
            stickmanColorType = colorType;
            startMat = gameColors.activeMaterials[(int)stickmanColorType];

            if (isSecret)
            {
                secretQuestionMark.SetActive(true);
            }

            if (isReserved)
            {
                reservedCap.SetActive(isReserved && !isSecret);
            }

            var material = isSecret ? secretMat : gameColors.activeMaterials[(int)stickmanColorType];
            skinnedMeshRenderer.sharedMaterial = material;
        }


        public void ResetColor()
        {
            if (isSecret)
            {
                isSecret = false;
                secretQuestionMark.SetActive(false);
            }

            if (isReserved)
            {
                reservedCap.SetActive(true);
            }

            skinnedMeshRenderer.material = startMat;
        }

        public void GoToBus(List<GridBase> path)
        {
            transform.SetParent(null);
            isMoving = true;
            DissociateStickman();
            // var currentBus = GameplayManager.instance.GetCurrentBus();
            // GameplayManager.instance.AddStickmanThroughBus(this);

            if (path != null)
            {
                // var pathPositions = HandlePathPositions(path, currentBus.GetEntranceTransform().position);
                // stickmanMovement.Run(pathPositions, JumpToBus);
            }
            else
            {
                // var pathPositions = new[] { transform.position, currentBus.GetEntranceTransform().position };
                // stickmanMovement.Run(pathPositions, JumpToBus);
            }
        }

        private void DissociateStickman()
        {
            belongedGrid.DissociateContainer();
            belongedGrid = null;
        }

        private Vector3[] HandlePathPositions(List<GridBase> gridBases, Vector3 endPos)
        {
            var newList = gridBases.Select(gridBase => gridBase.transform.position).ToList();
            newList.Insert(0, transform.position);
            newList.Add(endPos);
            return newList.ToArray();
        }

        private void JumpToBus()
        {
            var thisTransform = transform;
            thisTransform.SetParent(null);
            var vfx = Instantiate(disappearVFX, thisTransform.position + new Vector3(0f, 2f, 1f),
                thisTransform.rotation);
            Destroy(vfx, 2f);
            gameObject.SetActive(false);
            // GameplayManager.instance.RemoveStickmanThroughBus(this);
            // GameplayManager.instance.GetCurrentBus().GetStickman(isReserved);
        }


        public void GoToMatchArea(MatchArea matchArea, Transform matchAreaPosition, List<GridBase> path)
        {
            isMoving = true;
            transform.SetParent(null);
            DissociateStickman();
            AssignToMatchArea(matchArea);

            if (path != null)
            {
                matchArea.SetTaken(true);
                var pathPositions = HandlePathPositions(path, matchAreaPosition.position);
                stickmanMovement.Run(pathPositions, PlaceToMatchArea);
            }
            else
            {
                matchArea.SetTaken(true);
                var pathPositions = new[] { transform.position, matchAreaPosition.position };
                stickmanMovement.Run(pathPositions, PlaceToMatchArea);
            }
        }

        private void AssignToMatchArea(MatchArea matchArea)
        {
            belongedGrid = matchArea;
            matchArea.AddStickman(this);
        }

        private void PlaceToMatchArea()
        {
            MatchAreaManager.instance.AssignMatchArea(belongedGrid as MatchArea);
            stickmanMovement.Stop();
            var thisTransform = transform;
            thisTransform.SetParent(belongedGrid.transform);
            thisTransform.localPosition = Vector3.zero;
            thisTransform.localEulerAngles = Vector3.zero;
        }

        public EnumHolder.GameColor GetColor()
        {
            return stickmanColorType;
        }

        public void EnableInteraction()
        {
            stickmanOutline.enabled = true;
            hasPath = true;
        }

        public void DisableInteraction()
        {
            stickmanOutline.enabled = false;
            hasPath = false;
        }

        public bool GetHasPath()
        {
            return hasPath;
        }

        public GridBase GetBelongedGrid()
        {
            return belongedGrid;
        }

        public bool GetIsMoving()
        {
            return isMoving;
        }

        public bool GetIsSecret()
        {
            return isSecret;
        }

        public bool GetIsReserved()
        {
            return isReserved;
        }

        public void WrongSelection()
        {
            if (currentWrongObject) return;
            var wrongObject = Instantiate(wrongSelectionObject, transform.position + new Vector3(0f, 1f, 0f),
                quaternion.identity);
            wrongObject.SetActive(true);
            currentWrongObject = wrongObject;
            Destroy(wrongObject, 1f);
        }
    }
}