using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Vuforia;

public class ARModeSwitcher : MonoBehaviour
{
    [Header("AR Foundation Components")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Vuforia Components")]
    [SerializeField] private VuforiaMonoBehaviour vuforiaBehaviour;
    [SerializeField] private GameObject paletteImageTarget;

    private bool isPaintMode = false;

    private void Start()
    {
        // Start in AR Foundation Plane Mode by default
        SetPaintMode(false);
    }

    public void ToggleARMode()
    {
        SetPaintMode(!isPaintMode);
    }

    public void SetPaintMode(bool enablePaintMode)
    {
        isPaintMode = enablePaintMode;

        // 1. Toggle AR Plane Detection & Raycasting
        if (planeManager != null) planeManager.enabled = !isPaintMode;
        if (raycastManager != null) raycastManager.enabled = !isPaintMode;

        // Hide/Show existing detected AR planes to free up rendering overhead
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(!isPaintMode);
            }
        }

        // 2. Toggle Vuforia Tracking & Image Target
        if (vuforiaBehaviour != null) vuforiaBehaviour.enabled = isPaintMode;
        if (paletteImageTarget != null) paletteImageTarget.SetActive(isPaintMode);

        Debug.Log(isPaintMode ? "Switched to Paint Mode (Vuforia Active)" : "Switched to Plane Mode (AR Foundation Active)");
    }
}