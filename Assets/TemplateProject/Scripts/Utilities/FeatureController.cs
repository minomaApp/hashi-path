using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TemplateProject.Scripts.Data.SO;
using TemplateProject.Scripts.Runtime.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TemplateProject.Scripts.Utilities
{
    public class FeatureController : MonoBehaviour
    {
        [Header("Cached References")] [SerializeField]
        private FeatureSettings featureSettings;


        [Header("UI")] [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        //[SerializeField] private TextMeshProUGUI infoTMP;
        [SerializeField] private Image rewardImage;
        [SerializeField] private Image rewardImageHidden;
        [SerializeField] private GameObject progressPanel;

        [Header("Feature Content State")]
        [SerializeField] private GameObject featureContentRoot;
        [SerializeField] private GameObject afterLastFeatureRoot;

        [Header("Animation Settings")] [SerializeField]
        private float fadeDuration = 0.5f;

        [SerializeField] private float fillDuration = 0.5f;
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private Vector3 shakeStrength = new Vector3(10f, 0f, 0f);
        [SerializeField] private float punchScale = 0.2f;

        private int[] featureUnlockLevels;
        private int[] prizeStageIndices;
        private Dictionary<int, Sprite> prizeSprites;
        private Dictionary<int, Sprite> prizeHiddenSprites;

        public static FeatureController instance;

        public void InitializeController()
        {
            MakeSingleton();

            CacheReferences();
        }

        private void MakeSingleton()
        {
            if (instance)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
            }
        }

        private void CacheReferences()
        {
            featureUnlockLevels = ABManager.FeatureUnlockLevels;

            if (featureUnlockLevels == null || featureUnlockLevels.Length == 0)
            {
                UIManager.instance.OpenNoFeatureWin();
                return;
            }

            UIManager.instance.OpenFeatureWin();


            HandleFeatureUnlockLevels(featureUnlockLevels);

            prizeStageIndices = featureSettings.features
                .Select(pl => Array.IndexOf(featureUnlockLevels, pl.unlockLevelNumber))
                .Where(idx => idx >= 0)
                .OrderBy(idx => idx)
                .ToArray();


            prizeSprites = featureSettings.features
                .Where(pl => pl.prizeSprite != null)
                .ToDictionary(pl => pl.unlockLevelNumber, pl => pl.prizeSprite);

            prizeHiddenSprites = featureSettings.features
                .Where(pl => pl.prizeHiddenSprite != null)
                .ToDictionary(pl => pl.unlockLevelNumber, pl => pl.prizeHiddenSprite);
        }

        private void HandleFeatureUnlockLevels(int[] levels)
        {
            for (var i = 0; i < levels.Length; i++)
            {
                featureSettings.features[i].unlockLevelNumber = levels[i];
            }
        }
        public async UniTask ShowWinPopup()
        {
            var currentLevel = LevelManager.instance.GetTotalLevelPlayed();

            if (featureUnlockLevels == null ||
                featureUnlockLevels.Length == 0 ||
                prizeStageIndices == null ||
                prizeStageIndices.Length == 0)
            {
                UIManager.instance.OpenNoFeatureWin();
                return;
            }

            var sortedPrizeIdx = prizeStageIndices
                .OrderBy(idx => featureUnlockLevels[idx])
                .ToList();

            if (sortedPrizeIdx.Count == 0)
            {
                UIManager.instance.OpenNoFeatureWin();
                return;
            }

            int lastPrizeIdx = sortedPrizeIdx[sortedPrizeIdx.Count - 1];
            int lastPrizeLevel = featureUnlockLevels[lastPrizeIdx];

            if (currentLevel > lastPrizeLevel)
            {
                UIManager.instance.OpenFeatureWin();
                SetFeatureContentState(false);
                return;
            }

            SetFeatureContentState(true);

            var stage = sortedPrizeIdx.FindIndex(idx => featureUnlockLevels[idx] >= currentLevel);

            if (stage < 0)
            {
                UIManager.instance.OpenFeatureWin();
                SetFeatureContentState(false);
                return;
            }

            UIManager.instance.OpenFeatureWin();

            var nextPrizeIdx = sortedPrizeIdx[stage];
            var nextPrizeLevel = featureUnlockLevels[nextPrizeIdx];
            var prevPrizeLevel = stage > 0
                ? featureUnlockLevels[sortedPrizeIdx[stage - 1]]
                : 0;

            if (prizeSprites.TryGetValue(nextPrizeLevel, out var spr))
            {
                rewardImage.sprite = spr;
            }

            if (prizeHiddenSprites.TryGetValue(nextPrizeLevel, out var sprH))
            {
                rewardImageHidden.sprite = sprH;
            }

            var goalAmount = nextPrizeLevel - prevPrizeLevel;
            if (goalAmount <= 0)
            {
                progressPanel.SetActive(false);
                goalAmount = 1;
            }
            else
            {
                progressPanel.SetActive(true);
            }

            var progressed = Mathf.Clamp(currentLevel - prevPrizeLevel, 0, goalAmount);
            var initial = Mathf.Clamp(progressed - 1, 0, goalAmount);

            progressBar.fillAmount = (float)initial / goalAmount;
            rewardImage.fillAmount = (float)initial / goalAmount;
            progressText.text = $"{initial}/{goalAmount}";

            var targetFraction = (float)progressed / goalAmount;

            await UniTask.Delay((int)(fadeDuration * 500));

            await rewardImage
                .DOFillAmount(targetFraction, fillDuration)
                .AsyncWaitForCompletion();

            await progressBar
                .DOFillAmount(targetFraction, fillDuration)
                .AsyncWaitForCompletion();

            progressText.text = $"{progressed}/{goalAmount}";

            if (progressed == goalAmount && prizeSprites.TryGetValue(currentLevel, out var spriteFinal))
            {
                rewardImage.sprite = spriteFinal;
                rewardImage.gameObject.SetActive(true);

                await rewardImage.rectTransform
                    .DOShakeRotation(shakeDuration, shakeStrength)
                    .AsyncWaitForCompletion();

                await rewardImage.rectTransform
                    .DOPunchScale(Vector3.one * punchScale, fillDuration / 2f)
                    .AsyncWaitForCompletion();
            }
        }

        private void SetFeatureContentState(bool showFeatureContent)
        {
            if (featureContentRoot != null)
            {
                featureContentRoot.SetActive(showFeatureContent);
            }

            if (afterLastFeatureRoot != null)
            {
                afterLastFeatureRoot.SetActive(!showFeatureContent);
            }
        }
    }
}