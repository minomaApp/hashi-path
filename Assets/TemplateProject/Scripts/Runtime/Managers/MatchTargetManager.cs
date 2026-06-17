using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data.Enums;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Models;
using TemplateProject.Scripts.Utilities.EditorUtilities.InspectorAttributes;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class MatchTargetManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform spawnPosition;
        [SerializeField] private Transform waitPosition;
        [SerializeField] private Transform matchPosition;
    
        [Header("Parameters")]
        [SerializeField] private int minimumMatchTargetCount;
        [SerializeField] private float matchTargetMovementDuration;
        [SerializeField] private Ease matchTargetMovementEase;
        [SerializeField] private Vector3 positionOffset;
    
        [Header("Info")]
        [ReadOnly] [SerializeField] private Placeable activeTarget;
        [ReadOnly] [SerializeField] private Placeable nextTarget;
        [ReadOnly] [SerializeField] private List<Placeable> matchTargetsList;
        [ReadOnly] [SerializeField] private EnumHolder.GameColor[] colors;

        public Placeable GetActiveTarget() => activeTarget;

        public static MatchTargetManager Instance;
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void Init(List<Placeable> matchTargetsList)
        {
            this.matchTargetsList = new List<Placeable>(matchTargetsList);

            foreach (var target in matchTargetsList) target.transform.position = spawnPosition.position;
            matchTargetsList[0].transform.position = waitPosition.position;

            activeTarget = matchTargetsList[0];
            nextTarget = matchTargetsList[1];

            colors = new EnumHolder.GameColor[Enum.GetValues(typeof(EnumHolder.GameColor)).Length - 1]; //Exclude None
            for (var i = 0; i < colors.Length; i++) colors[i] = (EnumHolder.GameColor) i + 1; //Exclude None
        }

        private void Start()
        {
            MoveTargets();
        }
    
        private void UpdateActiveTarget()
        {
            matchTargetsList.RemoveAt(0);
            activeTarget = nextTarget;
            nextTarget = matchTargetsList[1];
        }

        private async UniTask MoveTargets()
        {
            if (!activeTarget) return;
            activeTarget.transform.DOMove(matchPosition.position, matchTargetMovementDuration).SetEase(matchTargetMovementEase);
            await nextTarget.transform.DOMove(waitPosition.position + positionOffset, matchTargetMovementDuration).SetEase(matchTargetMovementEase);
        }
    }
}
