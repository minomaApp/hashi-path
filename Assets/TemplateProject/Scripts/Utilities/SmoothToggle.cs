using System;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TemplateProject.Scripts.Utilities
{
    [RequireComponent(typeof(Slider))]
    public class SmoothToggle : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IPointerUpHandler
    {
        [Tooltip("Ses için true, titreşim için false")]
        public bool isAudioToggle = true;

        [Tooltip("Tween süresi (sn)")] public float tweenDuration = 0.3f;

        private Slider _slider;
        private bool _isDragging;
        private bool _isTweening;

        void Awake()
        {
            _slider = GetComponent<Slider>();
            // Inspector’daki OnValueChanged callback’lerini mutlaka silin!
            _slider.onValueChanged.RemoveAllListeners();
        }

        void Update()
        {
            CheckCorrection();
        }

        public void CheckCorrection()
        {
            if (_slider.value == 0 && GameplayManager.instance.GetAudio())
            {
                GameplayManager.instance.ToggleAudio();
            }else if (Math.Abs(_slider.value - 1) < 0.01f && !GameplayManager.instance.GetAudio())
            {
                GameplayManager.instance.ToggleAudio();
            }

            if (!isAudioToggle)
            {
                if (_slider.value == 0 && GameplayManager.instance.GetVibration())
                {
                    GameplayManager.instance.ToggleVibration();
                }else if (Math.Abs(_slider.value - 1f) < 0.01f && !GameplayManager.instance.GetVibration())
                {
                    GameplayManager.instance.ToggleVibration();
                }
            }
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_isTweening) e.Use();
            _isDragging = false;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_isTweening) e.Use();
            _isDragging = true;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_isTweening) return;
            float target = (_slider.value < 0.5f) ? 0f : 1f;
            AnimateTo(target);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (_isTweening) return;

            if (!_isDragging)
            {
                // **CLICK:** anında atla ve toggle’u çalıştır
                float newVal = (_slider.value < 0.5f) ? 1f : 0f;
                SetInstant(newVal);
            }
        }

        private void SetInstant(float target)
        {
            _slider.value = target;

            // Ses ya da titreşim durumunu hemen toggle et
            if (isAudioToggle)
                GameplayManager.instance.ToggleAudio();
            else
                GameplayManager.instance.ToggleVibration();

            // AudioListener.volume’ü de anında ayarlamak isterseniz:
            if (isAudioToggle)
                AudioListener.volume = target;
        }

        private void AnimateTo(float target)
        {
            _isTweening = true;

            var seq = DOTween.Sequence()
                .Append(_slider.DOValue(target, tweenDuration).SetEase(Ease.OutQuad));

            if (isAudioToggle)
                seq.Join(DOVirtual.Float(AudioListener.volume, target, tweenDuration, v => AudioListener.volume = v));

            seq.OnComplete(() =>
            {
                bool willBeOn = target > 0.5f;
                if (isAudioToggle)
                {
                    if (willBeOn != GameplayManager.instance.GetAudio())
                        GameplayManager.instance.ToggleAudio();
                }
                else
                {
                    if (willBeOn != GameplayManager.instance.GetVibration())
                        GameplayManager.instance.ToggleVibration();
                }

                _isTweening = false;
            });
        }
    }
}