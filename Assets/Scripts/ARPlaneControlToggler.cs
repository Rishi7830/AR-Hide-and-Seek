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
        if (planeManager != null)
        {
            planeManager.enabled = enableState;

            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(enableState);
            }
        }
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
    {
        // Ensure newly added planes inherit the current visibility toggle state
        foreach (var newPlane in eventArgs.added)
        {
            newPlane.gameObject.SetActive(arePlanesVisible);
        }
    }
}