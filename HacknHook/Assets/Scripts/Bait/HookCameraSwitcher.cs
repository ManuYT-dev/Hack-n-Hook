using UnityEngine;

/// <summary>
/// Creates a camera for each cast, parents it to the hook, and destroys it when the hook is gone.
/// Call HookWasThrown with the spawned hook's Transform.
/// </summary>
public sealed class HookCameraSwitcher : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector3 hookCameraLocalPosition = new Vector3(0f, 1.5f, -4f);
    [SerializeField] private Vector3 hookCameraLocalEulerAngles = new Vector3(10f, 0f, 0f);

    private Camera spawnedHookCamera;
    private Transform currentHook;
    private AudioListener mainAudioListener;
    private bool mainCameraWasEnabled;
    private bool mainListenerWasEnabled;
    private bool hookCameraIsActive;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            mainAudioListener = mainCamera.GetComponent<AudioListener>();
        }
    }

    private void LateUpdate()
    {
        // Unity's destroyed-object check makes this true after the hook GameObject is destroyed.
        if (hookCameraIsActive && currentHook == null)
        {
            HookWasRemoved();
        }
    }

    /// <summary>Creates a fresh hook camera for this cast and switches to it.</summary>
    public void HookWasThrown(Transform hookTransform)
    {
        // Clean up a previous cast first, so repeated casts never leave old cameras behind.
        HookWasRemoved();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainAudioListener = mainCamera.GetComponent<AudioListener>();
            }
        }

        if (mainCamera == null || hookTransform == null)
        {
            Debug.LogWarning("HookWasThrown needs a Main Camera and a valid hook Transform.", this);
            return;
        }

        currentHook = hookTransform;

        GameObject cameraObject = new GameObject("HookCamera");
        cameraObject.transform.SetParent(hookTransform, false);
        cameraObject.transform.localPosition = hookCameraLocalPosition;
        cameraObject.transform.localEulerAngles = hookCameraLocalEulerAngles;

        spawnedHookCamera = cameraObject.AddComponent<Camera>();
        spawnedHookCamera.CopyFrom(mainCamera);
        spawnedHookCamera.enabled = true;

        // Keep the usual single-listener setup while viewing through the temporary camera.
        mainCameraWasEnabled = mainCamera.enabled;
        if (mainAudioListener != null)
        {
            mainListenerWasEnabled = mainAudioListener.enabled;
            mainAudioListener.enabled = false;
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.enabled = false;
        hookCameraIsActive = true;
    }

    /// <summary>Call when fishing ends if the hook object remains alive in the scene.</summary>
    public void HookWasRemoved()
    {
        if (!hookCameraIsActive)
        {
            return;
        }

        if (mainCamera != null)
        {
            mainCamera.enabled = mainCameraWasEnabled;
        }

        if (mainAudioListener != null)
        {
            mainAudioListener.enabled = mainListenerWasEnabled;
        }

        if (spawnedHookCamera != null)
        {
            Destroy(spawnedHookCamera.gameObject);
        }

        spawnedHookCamera = null;
        currentHook = null;
        hookCameraIsActive = false;
    }
}
