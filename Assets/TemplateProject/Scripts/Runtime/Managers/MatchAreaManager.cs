using System.Collections.Generic;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Models;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class MatchAreaManager : MonoBehaviour
    {
        public static MatchAreaManager instance;

        [Header("Cached References")] 
        [SerializeField] private List<MatchArea> matchAreas;
        [SerializeField] private List<MatchArea> claimedMatchAreas;


        private void OnEnable()
        {
            GameplayManager.instance.onBusChangeDone += HandleNewGoal;
        }

        private void OnDisable()
        {
            GameplayManager.instance.onBusChangeDone -= HandleNewGoal;
        }

        private void Awake()
        {
            InitializeSingleton();
        }

        private void InitializeSingleton()
        {
            if (instance) return;
            instance = this;
        }

        private void HandleNewGoal()
        {
            foreach (var matchArea in matchAreas)
            {
                matchArea.HandleNewGoal();
            }

            DOVirtual.DelayedCall(0.15f, CheckMatchAreas);
        }

        public MatchArea GetEmptyArea()
        {
            return matchAreas.Find(x => !x.HasStickman() && !x.IsTaken() ? x : null);
        }

        private void CheckMatchAreas()
        {
            if (GameplayManager.instance.GetIsChangingGoal()) return;

            if (claimedMatchAreas.Count != matchAreas.Count || claimedMatchAreas.Count == 0 ||
                matchAreas.Count == 0) return;
            GameplayManager.instance.LoseGame(false);
        }

        public void AssignMatchArea(MatchArea claimedArea)
        {
            if (!claimedMatchAreas.Contains(claimedArea))
            {
                claimedMatchAreas.Add(claimedArea);
            }

            CheckMatchAreas();
        }

        public void RemoveMatchArea(MatchArea matchArea)
        {
            if (claimedMatchAreas.Contains(matchArea))
            {
                claimedMatchAreas.Remove(matchArea);
            }
        }
    }
}