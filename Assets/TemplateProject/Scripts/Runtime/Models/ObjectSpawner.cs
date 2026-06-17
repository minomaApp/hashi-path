using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
#endif

namespace TemplateProject.Scripts.Runtime.Models
{
    public class ObjectSpawner : MonoBehaviour
    {
        public int x;
        public List<MatchingObject> matchingObjects;
        public GameObject tunnelDoor;
        public GameObject placeHolder;
        public Transform objectSpawnTransform;

        [FormerlySerializedAs("towerCountTMP")]
        public TextMeshPro objectCountTMP;

        public Conveyor conveyor;

#if UNITY_EDITOR

        public void Init(int xVal, List<MatchingObject> objects)
        {
            x = xVal;
            matchingObjects = objects;
            foreach (var matchingObject in matchingObjects)
            {
                conveyor.AddMatchingObject(matchingObject);
                matchingObject.transform.localPosition = objectSpawnTransform.localPosition;
            }

            HandleObjectCountTMP();
            DeactivatePlaceholder();
        }

        private void DeactivatePlaceholder()
        {
            placeHolder.SetActive(false);
        }
#endif

        public void RemoveMatchingObject(MatchingObject matchingObject)
        {
            if (!matchingObjects.Contains(matchingObject)) return;
            matchingObjects.Remove(matchingObject);
            HandleObjectCountTMP();
        }

        private void HandleObjectCountTMP()
        {
            objectCountTMP.text = matchingObjects.Count.ToString();
        }
    }
}