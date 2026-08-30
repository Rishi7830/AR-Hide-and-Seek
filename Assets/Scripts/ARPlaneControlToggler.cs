using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARPlaneManager))]
public class ARPlaneControlToggler : MonoBehaviour
{
    private ARPlaneManager planeManager;

    [Header("UI Reference")]
    public Button togglePlanesButton;

    private bool arePlanesVisible = true;

    private void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
        if (planeManager != null)
            planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (planeManager != null)
            planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void Start()
    {
        if (togglePlanesButton != null)
            togglePlanesButton.onClick.AddListener(TogglePlanes);

        SetPlanesState(arePlanesVisible);
    }

    public void TogglePlanes()
    {
        arePlanesVisible = !arePlanesVisible;
        SetPlanesState(arePlanesVisible);
    }

    private void SetPlanesState(bool enableState)
    {
        if (planeManager == null) return;

        // 1. Toggle the plane manager subsystem (stops/starts active plane detection)
        planeManager.enabled = enableState;

        if (!enableState)
        {
            // 2. Destroy existing plane GameObjects from the scene hierarchy
            foreach (var plane in planeManager.trackables)
            {
                Destroy(plane.gameObject);
            }

            // 3. Clear all stored plane references so ARFoundation rescans from scratch
            planeManager.SetTrackablesActive(false);
        }
        else
        {
            // Re-enable detection pool for new planes
            planeManager.SetTrackablesActive(true);
        }
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
    {
        // Only active when enabled
        if (!arePlanesVisible)
        {
            foreach (var newPlane in eventArgs.added)
            {
                newPlane.gameObject.SetActive(false);
            }
        }
    }
}