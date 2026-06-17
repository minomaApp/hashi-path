using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class Conveyor : MonoBehaviour
    {
        [SerializeField] private List<MatchingObject> matchingObjects = new();
        [SerializeField] private Transform conveyorTipTransform;
        [SerializeField] private float objectSpacing;
        [SerializeField] private int maxObjectCountInConveyor;
        [SerializeField] private ObjectSpawner belongedSpawner;
        [SerializeField] private Renderer conveyorRenderer;

        public void AddMatchingObject(MatchingObject matchingObject)
        {
            if (matchingObjects.Contains(matchingObject)) return;
            matchingObjects.Add(matchingObject);
        }

        public void RemoveMatchingObject(MatchingObject matchingObject)
        {
            if (!matchingObjects.Contains(matchingObject)) return;
            matchingObjects.Remove(matchingObject);
        }

        private void OnEnable()
        {
            LevelManager.instance.onLevelLoadComplete += HandleSpawnOnStart;
        }

        private void OnDisable()
        {
            LevelManager.instance.onLevelLoadComplete -= HandleSpawnOnStart;
        }

        private void HandleSpawnOnStart()
        {
            StartCoroutine(HandleMovement(0.1f));
        }

        private IEnumerator HandleMovement(float delay = 0)
        {
            // _isChangingObject = true;
            var offset = 0f;
            var objectCount = Mathf.Min(maxObjectCountInConveyor, matchingObjects.Count);

            if (matchingObjects.Count > 0)
            {
                var matchingObject = matchingObjects[0];
                _ = GridManager.instance.HandleColumnColors(belongedSpawner.x, matchingObject);
            }
            else
            {
                _ = GridManager.instance.HandleColumnColors(belongedSpawner.x, null);
            }


            var oldXVal = conveyorRenderer.materials[0].GetTextureOffset("_BaseMap").x;
            var oldYVal = conveyorRenderer.materials[0].GetTextureOffset("_BaseMap").y;
            DOVirtual.Float(oldYVal, oldYVal - (objectSpacing * objectCount),
                delay == 0 ? 0.21f : (objectCount * 0.1f) + 0.21f,
                (val) => { conveyorRenderer.materials[0].SetTextureOffset("_BaseMap", new Vector2(oldXVal, val)); });
            for (var i = 0; i < objectCount; i++)
            {
                yield return new WaitForSeconds(delay);
                if (matchingObjects.Count < objectCount || objectCount <= 0) continue;
                if (i == 0)
                {
                    matchingObjects[i].transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
                }

                matchingObjects[i].transform.DOLocalMoveZ(conveyorTipTransform.localPosition.z - offset, 0.25f);

                belongedSpawner.RemoveMatchingObject(matchingObjects[i]);
                offset += objectSpacing;
            }
        }

        private void HandleMoveForward()
        {
            StartCoroutine(HandleMovement());
        }

        public List<MatchingObject> GetMatchingObjects()
        {
            return matchingObjects;
        }

        public MatchingObject GetHeadObject()
        {
            if (matchingObjects.Count <= 0) return null;
            var matchingObject = matchingObjects[0];
            return matchingObject;
        }

        public void RemoveMatchingObjectFromList(MatchingObject matchingObject)
        {
            if (!matchingObjects.Contains(matchingObject)) return;
            matchingObjects.Remove(matchingObject);

            if (matchingObjects.Count > 0)
            {
                matchingObjects[0].CloseSecret();
            }


            HandleMoveForward();
        }

        public int GetCount()
        {
            return matchingObjects.Count;
        }
    }
}