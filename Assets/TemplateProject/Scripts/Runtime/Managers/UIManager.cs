using System;
using System.Collections;
using System.Collections.Generic;
using BoxPuller.Scripts.Runtime.Managers;
using DG.Tweening;
using TemplateProject.Scripts.Data.SO;
using TemplateProject.Scripts.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance;

        [Header("Cached References")] [SerializeField]
        private Image screenTransitionImage;

        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject loseScreen;
        [SerializeField] private GameObject levelCompleteConfetti;
        [SerializeField] private GameObject winScreen;

        [Header("Win References")] [SerializeField]
        private GameObject noFeatureWinParent;

        [SerializeField] private GameObject featureWinParent;

        [Header("Timer References")] [SerializeField]
        private GameObject timerParent;

        [SerializeField] private Animator clockAnimator;

        [SerializeField] private TextMeshProUGUI timerTMP;
        private bool isTimerBlinking;

        [Header("Start Screen References")] [SerializeField]
        private TextMeshProUGUI startScreenLevelTMP;

        [SerializeField] private TextMeshProUGUI startScreenTimerTMP;

        [Header("Level Text References")] [SerializeField]
        private GameObject levelTextParent;

        [SerializeField] private TextMeshProUGUI levelTMP;

        [SerializeField] private TextMeshProUGUI winLevelTMP;
        [SerializeField] private TextMeshProUGUI loseLevelTMP;

        [Header("Restart References")] [SerializeField]
        private GameObject restartButton;

        [Header("Settings References")] [SerializeField]
        private GameObject settingsPanel;

        [Header("Economy References")] [SerializeField]
        private TextMeshProUGUI coinTMP;

        [SerializeField] private GameObject coinParticle;
        [SerializeField] private GameObject coinParent;

        [Header("Settings Button Images")]
        [Space(10)] [SerializeField] private GameObject settingsButton;   
        [SerializeField] private Image audioButtonImage;
        [SerializeField] private Image vibrationButtonImage;

        [SerializeField] private Sprite audioOnSprite;
        [SerializeField] private Sprite audioOffSprite;

        [SerializeField] private Sprite vibrationOnSprite;
        [SerializeField] private Sprite vibrationOffSprite;

        //[SerializeField] private Slider audioSlider;
        //[SerializeField] private Slider vibrationSlider;

        [Header("Debug Settings")] [SerializeField]
        private bool isDebug;

        private int _debugClickCounter;
        [SerializeField] private GameObject debugPanel;

        private void Awake()
        {
            InitializeSingleton();
        }

        //private void Start()
        //{
        //    clockAnimator.speed = 0;
        //}
        private void Start()
        {
            if (clockAnimator != null)
            {
                clockAnimator.speed = 0;
            }

            if (timerParent != null)
            {
                timerParent.SetActive(false);
            }

            if (coinParent != null)
            {
                coinParent.SetActive(false);
            }
        }
        private void InitializeSingleton()
        {
            if (instance) return;
            instance = this;
        }

        public void OpenTransition(Action callback)
        {
            CloseTimer();
            CloseLevelText();
            DisableSettingsButton();
            DisableRestartButton();
            DisableCoinParent();
            CloseWinScreen(null);
            CloseLoseScreen(null);
            var color = screenTransitionImage.color;
            screenTransitionImage.DOColor(new Color(color.r, color.g, color.b, 1f), 0.5f).OnComplete(() =>
            {
                callback?.Invoke();
            });
        }


        public void CloseTransition(Action callback)
        {
            var color = screenTransitionImage.color;
            screenTransitionImage.DOColor(new Color(color.r, color.g, color.b, 0f), 0.5f).OnComplete(() =>
            {
                callback?.Invoke();

                if (LevelCacheManager.Instance)
                {
                    _ = LevelCacheManager.Instance.LoadSingleLevelByLogicalIndex(LevelManager.instance.GetLevelIndex() +
                        5);
                }
            });
        }

        public void CloseLoadingScreen()
        {
            loadingScreen.SetActive(false);
        }

        public void OpenStartScreen()
        {
            LevelManager.instance.isGamePlayable = false;
            startScreen.transform.parent.gameObject.SetActive(true);
            startScreen.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        public void CloseStartScreen()
        {
            startScreen.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                startScreen.transform.parent.gameObject.SetActive(false);
                LevelManager.instance.isGamePlayable = true;
            });
        }


        public void HandleTimer()
        {
            if (TimeManager.instance == null)
            {
                return;
            }

            if (TimeManager.instance.GetIsTimerActive())
            {
                return;
            }

            TimeManager.instance.StartTimer();

            if (clockAnimator != null)
            {
                clockAnimator.speed = 1;
            }

            StopBlinkTimer();
        }

        public void OpenLoseScreen()
        {
            UpdateEndScreenLevelTexts();
            loseScreen.transform.parent.gameObject.SetActive(true);
            loseScreen.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        private void CloseLoseScreen(Action callBack)
        {
            loseScreen.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                loseScreen.transform.parent.gameObject.SetActive(false);
                callBack?.Invoke();
            });
        }

        public void CloseWinScreen(Action callBack)
        {
            winScreen.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                winScreen.transform.parent.gameObject.SetActive(false);
                callBack?.Invoke();
            });
        }

        private void OpenWinScreen(Action callBack)
        {
            UpdateEndScreenLevelTexts();
            winScreen.transform.parent.gameObject.SetActive(true);
            _ = FeatureController.instance.ShowWinPopup();
            winScreen.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                callBack?.Invoke();
            });
        }

        public void RestartButton()
        {
            CloseLoseScreen(() => { LevelManager.instance.RestartLevel(); });
        }

        public void LevelCompleteEvents()
        {
            levelCompleteConfetti.SetActive(true);

            OpenWinScreen(null);
            TutorialController.instance.HandleInput(StepType.Classic);
        }

        public void OpenTimer()
        {
            if (timerParent == null)
            {
                return;
            }

            timerParent.SetActive(true);
            timerParent.transform.localScale = Vector3.zero;
            timerParent.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

            if (clockAnimator != null)
            {
                clockAnimator.speed = 1;
            }

            StopBlinkTimer();
        }

        private void CloseTimer()
        {
            isTimerBlinking = false;

            if (timerTMP != null)
            {
                DOTween.Kill(timerTMP);
                timerTMP.color = Color.white;
            }

            if (timerParent == null)
                return;

            timerParent.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
            {
                timerParent.SetActive(false);
            });

            //timerParent.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
            //{
            //    isTimerBlinking = false;
            //    StopCoroutine(BlinkTimer());
            //    timerParent.SetActive(false);
            //});
        }

        public TextMeshProUGUI GetTimerTMP()
        {
            return timerTMP;
        }

        public void OpenLevelText()
        {
            levelTextParent.SetActive(true);
            levelTextParent.transform.DOScale(Vector3.one * 0.45199f, 0.15f).SetEase(Ease.OutBack);
        }

        private void CloseLevelText()
        {
            levelTextParent.SetActive(false);
        }

        public TextMeshProUGUI GetLevelTMP()
        {
            return levelTMP;
        }

        public void EnableSettingsButton()
        {
            settingsButton.SetActive(true);
        }

        private void DisableSettingsButton()
        {
            settingsButton.SetActive(false);
        }

        public void EnableRestartButton()
        {
            restartButton.SetActive(true);
        }

        private void DisableRestartButton()
        {
            restartButton.SetActive(false);
        }

        public void OpenSettingsMenu()
        {
            RefreshSettingsButtonSprites();
            settingsPanel.transform.parent.gameObject.SetActive(true);
            settingsPanel.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
            TimeManager.instance.PauseTimer();
            LevelManager.instance.isGamePlayable = false;
        }

        public void CloseSettingsMenu()
        {
            settingsPanel.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
            {
                settingsPanel.transform.parent.gameObject.SetActive(false);
                if (TimeManager.instance != null)
                {
                    TimeManager.instance.StartTimer();
                }
                LevelManager.instance.isGamePlayable = true;
            });
        }

        //public void ToggleAudio()
        //{
        //    GameplayManager.instance.ToggleAudio();
        //}

        //public void ToggleVibration()
        //{
        //    GameplayManager.instance.ToggleVibration();
        //}

        public void ToggleAudio()
        {
            if (GameplayManager.instance == null)
                return;

            GameplayManager.instance.ToggleAudio();
            RefreshSettingsButtonSprites();
        }

        public void ToggleVibration()
        {
            if (GameplayManager.instance == null)
                return;

            GameplayManager.instance.ToggleVibration();
            RefreshSettingsButtonSprites();
        }

        public void HandleSwitches(bool isAudioOn, bool isVibrationOn)
        {
            //audioSlider.value = isAudioOn ? 1 : 0;
            //vibrationSlider.value = isVibrationOn ? 1 : 0;
            RefreshSettingsButtonSprites();

        }

        private void RefreshSettingsButtonSprites()
        {
            if (GameplayManager.instance == null)
                return;

            bool isAudioOn = GameplayManager.instance.GetAudio();
            bool isVibrationOn = GameplayManager.instance.GetVibration();

            if (audioButtonImage != null)
            {
                audioButtonImage.sprite = isAudioOn
                    ? audioOnSprite
                    : audioOffSprite;
            }

            if (vibrationButtonImage != null)
            {
                vibrationButtonImage.sprite = isVibrationOn
                    ? vibrationOnSprite
                    : vibrationOffSprite;
            }
        }

        private IEnumerator BlinkTimer()
        {
            yield return new WaitForSeconds(1f);
            if (TimeManager.instance.GetIsTimerActive() && TimeManager.instance.GetTimeLeft() > 10f)
            {
                StopBlinkTimer();
                yield break;
            }

            timerTMP.DOColor(Color.red, 0.15f).OnComplete(() =>
            {
                timerTMP.DOColor(Color.white, 0.15f).SetDelay(0.15f);
            });


            StartCoroutine(BlinkTimer());
        }

        private void StopBlinkTimer()
        {
            isTimerBlinking = false;

            if (timerTMP != null)
            {
                DOTween.Kill(timerTMP);
                timerTMP.color = Color.white;
            }

            //StopCoroutine(BlinkTimer());
            //isTimerBlinking = false;
            //DOTween.Kill(timerTMP);
            //timerTMP.color = Color.white;
        }

        public void StartBlinkTimer()
        {
            if (isTimerBlinking)
            {
                return;
            }

            if (timerTMP == null)
            {
                return;
            }

            isTimerBlinking = true;
            StartCoroutine(BlinkTimer());
        }

        public TextMeshProUGUI GetStartLevelTMP()
        {
            return startScreenLevelTMP;
        }

        public TextMeshProUGUI GetStartLevelTimeTMP()
        {
            return startScreenTimerTMP;
        }

        public void TryEnableDebug()
        {
            if (!isDebug) return;
            _debugClickCounter++;
            if (_debugClickCounter >= 3)
            {
                OpenDebugMenu();
            }
        }

        private void OpenDebugMenu()
        {
            debugPanel.SetActive(true);
        }

        public void CloseDebugMenu()
        {
            _debugClickCounter = 0;
            debugPanel.SetActive(false);
        }

        public void OnClickNextButton()
        {
            //coinParticle.SetActive(true);
            CloseWinScreen(() =>
            {
                LevelManager.instance.LevelIncrease();
            });
        }

        public void OpenPrivacyPolicy()
        {
            Application.OpenURL("https://www.tiligamestudio.com/about-7");
        }

        public void UpdateCoinText(int coinCount)
        {
            //coinTMP.transform.DOScale(Vector3.one * 1.15f, 0.02f).OnComplete(() =>
            //{
            //    coinTMP.text = coinCount.ToString();
            //    coinTMP.transform.DOScale(Vector3.one, 0.02f);
            //});
        }

        public void PauseTimer()
        {
            if (clockAnimator != null)
            {
                clockAnimator.speed = 0;
            }
        }

        public void EnableCoinParent()
        {
            //coinParent.SetActive(true);
        }

        private void DisableCoinParent()
        {
            if (coinParent != null)
            {
                coinParent.SetActive(false);
            }
        }

        public void OpenNoFeatureWin()
        {
            featureWinParent.SetActive(false);
            noFeatureWinParent.SetActive(true);
        }

        public void OpenFeatureWin()
        {
            noFeatureWinParent.SetActive(false);
            featureWinParent.SetActive(true);
        }
        private void UpdateEndScreenLevelTexts()
        {
            if (LevelManager.instance == null)
                return;

            string levelText = "Level " + LevelManager.instance.GetTotalLevelPlayed();

            if (winLevelTMP != null)
            {
                winLevelTMP.text = levelText;
            }

            if (loseLevelTMP != null)
            {
                loseLevelTMP.text = levelText;
            }
        }

        public void PlayUIButtonVibration()
        {
            if (VibrationManager.instance == null)
                return;

            VibrationManager.instance.UIButtonClick();
        }
    }  
}