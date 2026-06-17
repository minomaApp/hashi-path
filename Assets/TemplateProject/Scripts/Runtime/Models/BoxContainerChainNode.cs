using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using TMPro;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class BoxContainerChainNode : Placeable
    {
        #region Variables

        [SerializeField] protected GridBase currentGridBase;
        [SerializeField] protected GridBase prevGridBase;
        [SerializeField] protected Vector3 lastPosition;

        [SerializeField] protected BoxContainerChain chain;

        [SerializeField] protected BoxCollider boxCollider;

        [SerializeField] protected BoxContainerChainNode forwardNode;
        [SerializeField] protected BoxContainerChainNode backwardNode;
        [SerializeField] private BoxContainer boxContainer;
        private float deltaToGrid = 0.5f;

        #endregion


        #region Properties

        public BoxContainerChain ContainerChain => chain;
        public GridBase CurrentGridBase => currentGridBase;
        public GridBase PrevGridBase => prevGridBase;

        public BoxContainerChainNode ForwardNode
        {
            get => forwardNode;
            set => forwardNode = value;
        }

        public BoxContainerChainNode BackwardNode
        {
            get => backwardNode;
            set => backwardNode = value;
        }

        public ParticleSystem iceDecreaseParticle;
        public ParticleSystem iceBrokeParticle;
        public TextMeshPro iceText;

        public BoxContainer BoxContainer
        {
            get => boxContainer;
            set => boxContainer = value;
        }

        #endregion


        #region Creator Setup

        public void Init(BoxContainerChain chain)
        {
            this.chain = chain;
        }

        #endregion


        #region Custom Methods

        public void SetGrid(GridBase gridBase)
        {
            if (currentGridBase && currentGridBase.ownedPlaceable == this)
            {
                currentGridBase.SetEmpty();
            }

            prevGridBase = currentGridBase;
            currentGridBase = gridBase;
            currentGridBase.SetOccupied(this);
        }


        public BoxContainerChainNode GetNextNode(bool isHoldingHead)
        {
            if (isHoldingHead)
            {
                return backwardNode;
            }
            else
            {
                return forwardNode;
            }
        }


        public void UpdateIce(int iceCount)
        {
            iceText.transform.DOKill();
            iceText.text = iceCount.ToString();
            iceText.transform.DOScale(Vector3.one * 1.18f, 0.2f).OnComplete(() =>
            {
                iceText.transform.DOScale(Vector3.one, 0.1f).SetDelay(0.2f);
            });
            if (iceCount <= 0)
            {
                iceText.gameObject.SetActive(false);
                iceBrokeParticle.Play();
            }
            else
            {
                iceText.gameObject.SetActive(true);
                iceDecreaseParticle.Play();
            }
        }

        #endregion


#if UNITY_EDITOR


        private void OnDrawGizmos()
        {
            //sphere
            /*Gizmos.color = Color.magenta;
        var pos = CurrentGridBase.transform.position;
        pos.y += 2f;
        Gizmos.DrawCube(pos, Vector3.one * 0.2f);
        /*
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + CurrentForward);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(CurrentTarget,  0.05f);*/
        }


#endif
        public void CheckForColorMatch(GridBase gridBase)
        {
            if (boxContainer.GetContainerColor() == gridBase.currentColor)
            {
                if (boxContainer.ownedObject) return;
                var conveyor = GridManager.instance.GetObjectSpawners()[gridBase.GetXAxis()].conveyor;
                var matchingObject = conveyor.GetHeadObject();
                if (!matchingObject)
                {
                    if (conveyor.GetCount() <= 0)
                    {
                        _ = GridManager.instance.HandleColumnColors(gridBase.GetXAxis(), null);
                    }


                    return;
                }

                if (matchingObject.Color != boxContainer.GetContainerColor()) return;

                conveyor.RemoveMatchingObjectFromList(matchingObject);
                boxContainer.GetObjectToContainer(matchingObject, chain);
            }
        }
    }
}