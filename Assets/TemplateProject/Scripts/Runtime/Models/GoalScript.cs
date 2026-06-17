using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Managers;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class GoalScript : MonoBehaviour
    {
        [Header("Cached References")] 
        [SerializeField] private List<Renderer> goalRenderers;
        [SerializeField] private List<GoalSlot> goalSlots;
        [SerializeField] private EnumHolder.GameColor goalColor;
        [SerializeField] private Material filledSeatMaterial;
        [SerializeField] private GameObject goalParentObject;
        [SerializeField] private ParticleSystem completeConfetti;
        [SerializeField] private Animator carDoorAnimator;
        [SerializeField] private Transform goalEntranceTransform;
        [SerializeField] private GameColors gameColors;

        [Header("Reserve Settings")] 
        [SerializeField] private GameObject flagParent;
        [SerializeField] private GameObject reservedFlagParent;
        [SerializeField] private GameObject doneFlagParent;
        [SerializeField] private TextMeshPro reservedFlagTMP;
        [SerializeField] private int reserveCount;

        [Header("Parameters")] 
        [SerializeField] private int seatedStickmanCount; 
        [SerializeField] private int comingStickmanCount;
        [SerializeField] private int emptySeatIndex;
        [AudioClipName] public string completeSound;
        [AudioClipName] public string placeSound;
        private static readonly int startMovement = Animator.StringToHash("startMovement");

        public void Init(EnumHolder.GameColor colorType, int count)
        {
            goalColor = colorType;
            HandleColorSet();
            HandleReserved(count);
        }

        private void HandleColorSet()
        {
            var sharedMat = gameColors.activeMaterials[(int)goalColor];
            foreach (var goalRenderer in goalRenderers)
            {
                var materialArray = goalRenderer.sharedMaterials;
                materialArray[0] = sharedMat;
                goalRenderer.sharedMaterials = materialArray;
            }
        }

        private void HandleReserved(int count)
        {
            if (count == 0) return;
            reserveCount = count;
            flagParent.SetActive(true);
            reservedFlagParent.SetActive(true);
            reservedFlagTMP.text = "R  " + reserveCount;
        }

        private void CompleteAnimation()
        {
            var localScale = goalParentObject.transform.localScale;
            goalParentObject.transform.DOScale(localScale * 1.1f, 0.15f).OnComplete(() =>
            {
                completeConfetti.Play();

                if (AudioManager.instance)
                {
                    AudioManager.instance.PlaySound(completeSound);

                }

                goalParentObject.transform.DOScale(localScale, 0.15f).OnComplete(() =>
                {
                    // DOVirtual.DelayedCall(0.25f, GameplayManager.instance.MoveToNextGoal);
                });
            });
        }


        public void GetStickman(bool reserved)
        {
            if (GameplayManager.instance.GetIsChangingGoal()) return;
            
            HandleCarDoorAnimation();
            HandleSeat(reserved);
            AddStickman(1, reserved);
            IncreaseSeatIndex();
            
            if (seatedStickmanCount != 3 || GameplayManager.instance.GetIsChangingGoal()) return;
            
            GameplayManager.instance.SetIsChangingGoal(true);
            DOVirtual.DelayedCall(0.3f, CompleteAnimation);
        }

        private void HandleCarDoorAnimation()
        {
            carDoorAnimator.SetTrigger(startMovement);
        }

        private void HandleSeat(bool reserved)
        {
            var seat = GetEmptySeat();
            seat.stickmanRenderer.material.color = gameColors.activeMaterials[(int)goalColor].color;
            seat.seatedStickman.SetActive(true);
            seat.seatedStickmanHat.SetActive(reserved);
               
            if (AudioManager.instance)
            {
                AudioManager.instance.PlaySound(placeSound);

            }
            
            foreach (var seatRenderer in seat.seatRenderers)
            {
                seatRenderer.material = filledSeatMaterial;
            }

            var localScale = seat.seatParent.transform.localScale;

            seat.seatParent.transform.DOScale(localScale * 1.1f, 0.15f).OnComplete(() =>
            {
                seat.seatParent.transform.DOScale(localScale, 0.15f);
            });
         
        }

        private GoalSlot GetEmptySeat()
        {
            return goalSlots[emptySeatIndex];
        }

        public int GetComingStickmanCount()
        {
            return comingStickmanCount;
        }

        public void AddComingStickman(int value)
        {
            comingStickmanCount += value;
        }

        private void AddStickman(int addValue, bool reserved)
        {
            seatedStickmanCount += addValue;
            if (reserved)
            {
                HandleReservedSit();
            }
        }

        public void DecreaseReservedCount()
        {
            reserveCount--;
        }

        private void HandleReservedSit()
        {
            if (reserveCount <= 0)
            {
                CompleteReserve();
            }
        }

        private void CompleteReserve()
        {
            reservedFlagParent.SetActive(false);
            doneFlagParent.SetActive(true);
        }


        private void IncreaseSeatIndex()
        {
            emptySeatIndex++;
        }

        public EnumHolder.GameColor GetColor()
        {
            return goalColor;
        }

        public Transform GetEntranceTransform()
        {
            return goalEntranceTransform;
        }

        public int GetReservedCount()
        {
            return reserveCount;
        }

        public bool IsLastSeat()
        {
            return emptySeatIndex == goalSlots.Count - 1 || comingStickmanCount == goalSlots.Count - 1;
        }
    }

    [System.Serializable]
    public class GoalSlot
    {
        public GameObject seatParent;
        public List<Renderer> seatRenderers;
        public GameObject seatedStickman;
        public GameObject seatedStickmanHat;
        public Renderer stickmanRenderer;
    }
}