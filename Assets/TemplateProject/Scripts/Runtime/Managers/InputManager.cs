using System;
using System.Collections.Generic;
using TemplateProject.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class InputManager : MonoSingleton<InputManager>
    {
        public Action onFingerDown;
        public Action onFingerHold;
        public Action onFingerUp;


        #region Variables

        [Header("Debug")] public bool isFirstInput;

        [Header("Manual Input Block Counter")] public int manualInputBlockCounter;

        // For UI blocking
        private PointerEventData _eventData;
        private List<RaycastResult> _raycastList;

        #endregion


        private void Update()
        {
            if (!LevelManager.instance.isGamePlayable) return;

            if (manualInputBlockCounter > 0) return;

            // Selecting
            if (Input.GetMouseButtonDown(0) && !UIInputBlock())
            {
                if (!isFirstInput) isFirstInput = true;
                onFingerDown?.Invoke();
            }


            // Swerve 
            if (Input.GetMouseButton(0))
            {
                onFingerHold?.Invoke();
            }


            // Release
            if (Input.GetMouseButtonUp(0))
            {
                onFingerUp?.Invoke();
            }
        }


        private bool UIInputBlock()
        {
            if (Input.touchCount <= 0) return false;
            var touch = Input.GetTouch(0);
            return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }


        private bool UIInputBlock2()
        {
            _raycastList.Clear();
            EventSystem.current.RaycastAll(_eventData, _raycastList);
            return _raycastList.Count > 0;
        }
    }
}