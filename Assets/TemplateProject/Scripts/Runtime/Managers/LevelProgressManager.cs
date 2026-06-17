using System.Collections.Generic;
using System.Threading.Tasks;
using TemplateProject.Scripts.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class LevelProgressManager : MonoBehaviour
    {
        [Header("Cached References")] [SerializeField]
        private VerticalLayoutGroup verticalLayoutGroup;

        [SerializeField] private List<LevelProgressContainer> levelCircles;
        [SerializeField] private List<LevelProgressContainer> roadRectangles;
        [SerializeField] private Image levelImage;
        [SerializeField] private Image roadImage;
        [SerializeField] private Transform mapParent;

        [Header("Parameters")] [SerializeField]
        private int levelCount;

        [SerializeField] private float yStart = -900f;
        [SerializeField] private float yDiff = 230f;

        private void Awake()
        {
            GetData();
            CreateMap();
        }

        private async void GetData()
        {
            levelCount = await GetAddressableGroupEntryCount("LevelsGroup");
        }

        private async Task<int> GetAddressableGroupEntryCount(string label)
        {
            AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> handle =
                Addressables.LoadResourceLocationsAsync(label);

            await handle.Task;

            int count = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result.Count : 0;

            Addressables.Release(handle);
            return count;
        }

        private void CreateMap()
        {
            var currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);

            levelCount += currentLevel;

            verticalLayoutGroup.padding.top = -(int)yStart;
            for (var i = 0; i < levelCount + 2; i++)
            {
                var newLevelContainer = Instantiate(levelImage.gameObject, Vector3.zero, Quaternion.identity,
                    mapParent).GetComponent<LevelProgressContainer>();
                newLevelContainer.gameObject.transform.localPosition = new Vector3(0f, yStart, 0f);
                levelCircles.Add(newLevelContainer);
                newLevelContainer.levelTMP.text = "Level " + (i + 1);
                yStart += yDiff;
                var newRoadRectangle = Instantiate(roadImage.gameObject, Vector3.zero, Quaternion.identity,
                    mapParent).GetComponent<LevelProgressContainer>();
                newRoadRectangle.gameObject.transform.localPosition = new Vector3(0f, yStart, 0f);
                roadRectangles.Add(newRoadRectangle);
                yStart += yDiff;
            }

            HandleCurrentLevel(currentLevel);
        }

        private void HandleCurrentLevel(int currentLevelIndex)
        {
            for (var i = 0; i <= currentLevelIndex; i++)
            {
                levelCircles[i].insideImage.fillAmount = 1f;
                if (i == 0) continue;
                verticalLayoutGroup.padding.top += 475;
                roadRectangles[i - 1].insideImage.fillAmount = 1f;
            }
        }

        [ContextMenu("IncreaseLevelIndex")]
        public void IncreaseLevelIndex()
        {
            PlayerPrefs.SetInt("CurrentLevel", PlayerPrefs.GetInt("CurrentLevel", 0) + 1);
        }
    }
}