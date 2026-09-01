using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARFoundationPaintManager : MonoBehaviour
{
    [Header("AR Foundation")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject palettePrefab;
    [SerializeField] private GameObject brushTipPrefab;

    [Header("Persistence Settings")]
    [Tooltip("If true, the palette stays anchored in world space even when the target leaves the camera view.")]
    [SerializeField] private bool keepPaletteAnchored = true;

    private GameObject spawnedPalette;
    private GameObject spawnedBrush;

    private void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            UpdateTrackedObject(trackedImage);
        }
        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateTrackedObject(trackedImage);
        }
    }

    private void UpdateTrackedObject(ARTrackedImage trackedImage)
    {
        bool isTracking = trackedImage.trackingState == TrackingState.Tracking;
        string imageName = trackedImage.referenceImage.name;

        // PALETTE TARGET
        if (imageName == "PaletteTarget_v")
        {
            if (spawnedPalette == null && isTracking)
            {
                // Instantiate at the exact world position & rotation of the detected image
                spawnedPalette = Instantiate(palettePrefab, trackedImage.transform.position, trackedImage.transform.rotation);

                if (keepPaletteAnchored)
                {
                    // Unparent so AR Foundation doesn't disable/hide it when target is out of view
                    spawnedPalette.transform.parent = null;
                }
                else
                {
                    spawnedPalette.transform.SetParent(trackedImage.transform);
                }
            }
            else if (spawnedPalette != null)
            {
                if (keepPaletteAnchored)
                {
                    // Optionally update position only when tracking is active to correct minor drift
                    if (isTracking)
                    {
                        spawnedPalette.transform.position = trackedImage.transform.position;
                        spawnedPalette.transform.rotation = trackedImage.transform.rotation;
                    }
                    // Keep active permanently once instantiated
                    spawnedPalette.SetActive(true);
                }
                else
                {
                    spawnedPalette.SetActive(isTracking);
                }
            }
        }
        // BRUSH TIP TARGET
        else if (imageName == "BrushTipTarget")
        {
            // The physical brush should follow the physical marker dynamically
            if (spawnedBrush == null && isTracking)
            {
                spawnedBrush = Instantiate(brushTipPrefab, trackedImage.transform);
                spawnedBrush.transform.localPosition = Vector3.zero;
                spawnedBrush.transform.localRotation = Quaternion.identity;
            }

            if (spawnedBrush != null)
            {
                spawnedBrush.SetActive(isTracking);
            }
        }
    }
}