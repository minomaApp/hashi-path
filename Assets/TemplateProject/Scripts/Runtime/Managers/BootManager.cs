using System.Threading.Tasks;
using TemplateProject.Scripts.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class BootManager : MonoBehaviour
    {
        [SerializeField] private string preloadLabel = "Level";
        [SerializeField] private string nextSceneName = "GameplayScene";
        [SerializeField] private bool showProgress = true;
        [SerializeField] private LoadingUI loadingUI;

        private async void Start()
        {
            if (showProgress && loadingUI != null)
                loadingUI.Show();

            await Addressables.InitializeAsync().Task;
            // Debug.Log("[BootManager] Addressables initialized.");

            await ABManager.Instance.RefreshDataWithoutElephant();
            await LevelCacheManager.Instance.PreloadInitialLevelsWindow(
                PlayerPrefs.GetInt("CurrentLevel", 0),
                5, null
            );

            if (showProgress && loadingUI != null)
            {
                var tcs = new TaskCompletionSource<bool>();
                loadingUI.SetProgressSmooth(() => tcs.SetResult(true));
                await tcs.Task;
                loadingUI.Hide();
            }

            await SceneManager.LoadSceneAsync(nextSceneName);
        }
    }
}