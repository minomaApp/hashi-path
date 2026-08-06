using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LensDecreaseOnX : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Lens Settings")]
    [SerializeField] private float startLens = 64f;
    [SerializeField] private float targetLens = 18.5f;
    [SerializeField] private float transitionDuration = 1f;

    private Coroutine lensCoroutine;

    private void Start()
    {
        if (cinemachineCamera == null)
        {
            Debug.LogError("Cinemachine Camera is missing.", this);
            enabled = false;
            return;
        }

        SetLens(startLens);
    }

    private void Update()
    {
        if (IsXPressed())
        {
            StartLensChange();
        }
    }

    private bool IsXPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.xKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.X))
        {
            return true;
        }
#endif

        return false;
    }

    private void StartLensChange()
    {
        Debug.Log("X key detected.");

        if (lensCoroutine != null)
        {
            StopCoroutine(lensCoroutine);
        }

        lensCoroutine = StartCoroutine(ChangeLens());
    }

    private IEnumerator ChangeLens()
    {
        float currentLens =
            cinemachineCamera.Lens.OrthographicSize;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / transitionDuration
            );

            float newLens = Mathf.Lerp(
                currentLens,
                targetLens,
                progress
            );

            SetLens(newLens);

            yield return null;
        }

        SetLens(targetLens);

        Debug.Log("Lens change completed.");

        lensCoroutine = null;
    }

    private void SetLens(float value)
    {
        LensSettings lensSettings =
            cinemachineCamera.Lens;

        lensSettings.OrthographicSize = value;

        cinemachineCamera.Lens = lensSettings;
    }

    [ContextMenu("Test Lens Change")]
    private void TestLensChange()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode first.");
            return;
        }

        StartLensChange();
    }
}