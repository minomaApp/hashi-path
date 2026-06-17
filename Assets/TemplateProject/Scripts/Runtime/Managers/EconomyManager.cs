using System;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class EconomyManager : MonoBehaviour
    {
        [SerializeField] private int coinCount;
        [AudioClipName] public string coinSound;
        private int _coinSoundCount;

        private void Start()
        {
            //coinCount = PlayerPrefs.GetInt("coinAmount", 0);
            //UIManager.instance.UpdateCoinText(coinCount);
        }

        public void UpdateCoinAmount(int value)
        {
            //if (GameplayManager.instance.GetAudio() && _coinSoundCount % 5 == 0)
            //{
            //    AudioManager.instance.PlaySound(coinSound);
            //}

            //_coinSoundCount++;
            //coinCount += value;
            //UIManager.instance.UpdateCoinText(coinCount);
        }

        public void SaveCoin()
        {
            DOVirtual.DelayedCall(0.25f, () =>
            {
                UIManager.instance.CloseWinScreen(() =>
                {
                    LevelManager.instance.LevelIncrease();
                });
            });
            //PlayerPrefs.SetInt("coinAmount", coinCount);
            //DOVirtual.DelayedCall(1f,
            //    () => { UIManager.instance.CloseWinScreen(() => { LevelManager.instance.LevelIncrease(); }); });
        }
    }
}