using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    public class ScreenshotTaker : MonoBehaviour
    {
        private readonly List<Vector2Int> resolutions = new List<Vector2Int>
        {
            new Vector2Int(2064, 2752),
            new Vector2Int(1320, 2868)
        };

        private string screenshotsPath;

        private void Start()
        {
            screenshotsPath = Path.Combine(Application.dataPath, "ScreenShots");

            if (!Directory.Exists(screenshotsPath))
            {
                Directory.CreateDirectory(screenshotsPath);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                StartCoroutine(TakeScreenshots());
            }
        }

        private IEnumerator TakeScreenshots()
        {
            foreach (var res in resolutions)
            {
                yield return StartCoroutine(TakeScreenshot(res.x, res.y));
            }
        }

        private IEnumerator TakeScreenshot(int width, int height)
        {
            string folderPath = Path.Combine(screenshotsPath, $"{width} × {height}");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");

            yield return new WaitForEndOfFrame(); // Frame'in tamamen renderlanmasını bekle

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("Main Camera not found!");
                yield break;
            }

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            byte[] bytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Destroy(screenshot);

            Debug.Log($"Screenshot saved: {filePath}");
        }
    }
}
