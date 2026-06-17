// TutorialSequence.cs

using System;
using System.Collections.Generic;
using TemplateProject.Scripts.Data.SO;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    public class TutorialSequence : MonoBehaviour
    {
        public event Action OnComplete;

        private List<TutorialStep> _steps;
        private int _currentIndex;
        private GameObject _currentStepGO;

        public void Initialize(List<TutorialStep> steps)
        {
            _steps = steps;
            _currentIndex = 0;
            ShowStep(_currentIndex);
        }

        private void ShowStep(int idx)
        {
            var step = _steps[idx];
            if (_currentStepGO) Destroy(_currentStepGO);
            _currentStepGO = step.stepPrefab;
        }

        public void NextStep(StepType stepType)
        {
            if(_steps.Count <= _currentIndex) return;
            if(stepType != _steps[_currentIndex].stepType) return;
            _currentIndex++;
            if (_currentIndex < _steps.Count)
            {
                ShowStep(_currentIndex);
            }
            else
            {
                OnComplete?.Invoke();
            }
        }
    }
}