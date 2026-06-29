using System.Linq;
using TemplateProject.Scripts.Data.SO;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    public class TutorialController : MonoBehaviour
    {
        [SerializeField] public TutorialSettings tutorialSettings;
        [SerializeField] private Transform canvasParent;

        private GameObject _currentTutorialParent;
        private TutorialSequence _sequence;

        public static TutorialController instance;

        public bool HasActiveTutorial => _sequence != null;
        
        private void Awake()
        {
            MakeSingleton();
        }

        private void MakeSingleton()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public void StartTutorial(int levelIndex, int playedIndex)
        {
            if (LevelManager.instance != null)
            {
                LevelManager.instance.isTutorialOn = false;
            }

            if (tutorialSettings == null)
            {
                Debug.LogWarning("[TutorialController] TutorialSettings atanmadý. Tutorial baþlatýlmadý.");
                return;
            }

            if (tutorialSettings.tutorialLevels == null || tutorialSettings.tutorialLevels.Count == 0)
            {
                Debug.LogWarning("[TutorialController] TutorialSettings içinde tutorialLevels boþ. Tutorial baþlatýlmadý.");
                return;
            }

            // Tutorial sadece ilk oynanýþta açýlsýn istiyorsan bu kontrol kalsýn.
            // Örnek: levelIndex 2 ise playedIndex 3 olduðunda tutorial çalýþýr.
            if (playedIndex != levelIndex + 1)
            {
                return;
            }

            bool hasTutorialLevel = tutorialSettings.tutorialLevels
                .Any(t => t.tutorialIndex == levelIndex);

            if (!hasTutorialLevel)
            {
                Debug.Log($"[TutorialController] Bu level için tutorial yok. LevelIndex: {levelIndex}");
                return;
            }

            var levelData = tutorialSettings.tutorialLevels
                .First(t => t.tutorialIndex == levelIndex);

            if (levelData.tutorialParent == null)
            {
                Debug.LogWarning($"[TutorialController] TutorialParent atanmadý. LevelIndex: {levelIndex}");
                return;
            }

            if (levelData.tutorialParent.tutorialSteps == null ||
                levelData.tutorialParent.tutorialSteps.Count == 0)
            {
                Debug.LogWarning($"[TutorialController] Tutorial step yok. LevelIndex: {levelIndex}");
                return;
            }

            if (canvasParent == null)
            {
                Debug.LogWarning("[TutorialController] CanvasParent atanmadý. Tutorial baþlatýlmadý.");
                return;
            }

            ClearCurrentTutorial();

            if (LevelManager.instance != null)
            {
                LevelManager.instance.isTutorialOn = true;
            }

            var go = Instantiate(levelData.tutorialParent.gameObject, canvasParent);
            _currentTutorialParent = go;

            _sequence = go.GetComponent<TutorialSequence>();

            if (_sequence == null)
            {
                _sequence = go.AddComponent<TutorialSequence>();
            }

            _sequence.Initialize(levelData.tutorialParent.tutorialSteps);
            _sequence.OnComplete += EndTutorial;
        }

        private void EndTutorial()
        {
            if (_sequence != null)
            {
                _sequence.OnComplete -= EndTutorial;
            }

            if (_currentTutorialParent != null)
            {
                _currentTutorialParent.SetActive(false);
            }

            _sequence = null;
            _currentTutorialParent = null;

            if (LevelManager.instance != null)
            {
                LevelManager.instance.isTutorialOn = false;
            }
        }

        private void ClearCurrentTutorial()
        {
            if (_sequence != null)
            {
                _sequence.OnComplete -= EndTutorial;
                _sequence = null;
            }

            if (_currentTutorialParent != null)
            {
                Destroy(_currentTutorialParent);
                _currentTutorialParent = null;
            }
        }

        public void HandleInput(StepType stepType)
        {
            if (!CanHandleInput(stepType))
            {
                return;
            }

            _sequence.NextStep(stepType);
        }

        public bool CanHandleInput(StepType stepType)
        {
            if (_sequence == null)
            {
                return false;
            }

            return _sequence.IsWaitingForStep(stepType);
        }
    }
}