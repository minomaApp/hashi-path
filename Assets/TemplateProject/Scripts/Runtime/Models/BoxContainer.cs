using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Runtime.Managers;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class BoxContainer : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private Material startMat;
        [SerializeField] private Material secretMat;
        [SerializeField] private GameObject secretQuestionMark;
        [SerializeField] private Outline containerOutline;
        [SerializeField] private GridBase belongedGrid;
        [SerializeField] private BoxContainerChain belongedChain;
        [SerializeField] private Transform objectPlacementTransform;
        [SerializeField] public MatchingObject ownedObject;
        [SerializeField] public GameObject blackPlaceholder;
        [SerializeField] private GameObject containerCap;
        [SerializeField] private List<Renderer> ballRenderers;
        [SerializeField] public GameObject placeParticle;


        [Header("Parameters")] [SerializeField]
        private EnumHolder.GameColor containerColorType;

        public EnumHolder.GameColor ContainerColorType
        {
            get => containerColorType;
            set => containerColorType = value;
        }

        [SerializeField] private bool isSecret;

        private void Start()
        {
            if (containerCap.TryGetComponent(out Renderer capRenderer))
            {
                capRenderer.sharedMaterial = gameColors.chainMaterials[(int)containerColorType];
            }
        }

        public void Init(EnumHolder.GameColor colorType, bool secret, GridBase gridCell,
            BoxContainerChain toAssignChain)
        {
            isSecret = secret;
            SetColor(colorType);
            belongedGrid = gridCell;
            belongedChain = toAssignChain;
        }

        private void SetColor(EnumHolder.GameColor colorType)
        {
            containerColorType = colorType;
            startMat = gameColors.activeMaterials[(int)containerColorType];

            if (isSecret)
            {
                secretQuestionMark.SetActive(true);
            }

            var material = isSecret ? secretMat : gameColors.activeMaterials[(int)containerColorType];
            var insideMat = gameColors.chainInsideMaterials[(int)containerColorType];
            var sharedMaterials = meshRenderer.sharedMaterials;
            sharedMaterials[0] = material;
            sharedMaterials[1] = insideMat;
            meshRenderer.sharedMaterials = sharedMaterials;
        }

        public void ResetColor()
        {
            if (isSecret)
            {
                isSecret = false;
                secretQuestionMark.SetActive(false);
            }

            meshRenderer.material = startMat;
        }

        private void DissociateContainer()
        {
            belongedGrid.DissociateContainer();
            belongedGrid = null;
        }

        public void EnableOutline()
        {
            containerOutline.enabled = true;
        }

        public void DisableOutline()
        {
            containerOutline.enabled = false;
        }

        public bool GetIsSecret()
        {
            return isSecret;
        }

        public GridBase GetBelongedGrid()
        {
            return belongedGrid;
        }

        public BoxContainerChain GetChain()
        {
            return belongedChain;
        }

        public EnumHolder.GameColor GetContainerColor()
        {
            return containerColorType;
        }

        public void GetObjectToContainer(MatchingObject matchingObject, BoxContainerChain chain)
        {
            if (ownedObject) return;
            if (!belongedChain)
            {
                belongedChain = chain;
            }

            ownedObject = matchingObject;
            matchingObject.SetTarget(objectPlacementTransform, this);
        }

        public void HandleCap(MatchingObject matchingObject, string placeSound)
        {
            containerCap.SetActive(true);
            containerCap.transform.DOScale(new Vector3(1f, 1f, containerCap.transform.localScale.z), 0.15f)
                .OnComplete(() =>
                {
                    containerCap.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), 0.15f).OnComplete(() =>
                    {
                        belongedChain.IncreaseCompleteCount();
                        matchingObject.DisableTrail();
                        AudioManager.instance.PlaySound(placeSound, true, false, 0.5f);
                        VibrationManager.instance.Light();
                    });
                }).SetDelay(0.15f);
        }

        public void PlayParticle()
        {
            placeParticle.SetActive(true);
        }
    }
}