using System;
using TMPro;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager instance;

        [Header("Cached References")]
        private TextMeshProUGUI timerTMP;

        [Header("Parameters")]
        [SerializeField] private float levelTime;
        [SerializeField] private bool isTimerActive;
        [SerializeField] private bool hasTimerStarted;

        private bool lowTimeBlinkStarted;

        private void Awake()
        {
            if (!instance)
            {
                instance = this;
            }
        }

        private void Update()
        {
            HandleTimer();
        }

        private void HandleTimer()
        {
            if (!isTimerActive)
            {
                return;
            }

            if (LevelManager.instance == null)
            {
                return;
            }

            if (!LevelManager.instance.isGamePlayable ||
                LevelManager.instance.isLevelFailed)
            {
                return;
            }

            levelTime -= Time.deltaTime;

            if (timerTMP != null)
            {
                timerTMP.text = TimeSpan.FromSeconds(
                    Mathf.Max(0, (int)levelTime)).ToString(@"m\:ss");
            }

            if (levelTime <= 10f && !lowTimeBlinkStarted)
            {
                lowTimeBlinkStarted = true;

                if (UIManager.instance != null)
                {
                    UIManager.instance.StartBlinkTimer();
                }
            }

            if (levelTime > 0f)
            {
                return;
            }

            levelTime = 0f;
            PauseTimer();

            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.LoseGame(true);
            }
        }

        public void SetTimer(int time)
        {
            levelTime = Mathf.Max(0, time);
            isTimerActive = false;
            hasTimerStarted = false;
            lowTimeBlinkStarted = false;
            RefreshTimerText();
        }

        public void StartTimer()
        {
            if (levelTime <= 0f)
            {
                return;
            }

            hasTimerStarted = true;
            isTimerActive = true;
            RefreshTimerText();
        }

        public void PauseTimer()
        {
            isTimerActive = false;

            if (UIManager.instance != null)
            {
                UIManager.instance.PauseTimer();
            }
        }

        public bool GetIsTimerActive()
        {
            return isTimerActive;
        }
        public bool HasTimerStarted()
        {
            return hasTimerStarted;
        }

        public void SetTimerTMP(
            TextMeshProUGUI timer,
            TextMeshProUGUI startScreenTimerTMP)
        {
            timerTMP = timer;

            if (startScreenTimerTMP != null)
            {
                startScreenTimerTMP.gameObject.SetActive(true);
                startScreenTimerTMP.text =
                    "Level Time: " +
                    TimeSpan.FromSeconds((int)levelTime).ToString(@"m\:ss");
            }

            if (timerTMP != null)
            {
                timerTMP.gameObject.SetActive(true);
                RefreshTimerText();
            }
        }

        public float GetTimeLeft()
        {
            return levelTime;
        }

        private void RefreshTimerText()
        {
            if (timerTMP == null)
            {
                return;
            }

            timerTMP.text = TimeSpan.FromSeconds(
                Mathf.Max(0, (int)levelTime)).ToString(@"m\:ss");
        }
    }
}