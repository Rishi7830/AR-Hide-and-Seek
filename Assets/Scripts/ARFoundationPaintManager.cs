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
    [Tooltip(
        "If true, the palette stays anchored in world space even " +
        "when the target leaves the camera view."
    )]
    [SerializeField] private bool keepPaletteAnchored = true;

    private GameObject spawnedPalette;
    private GameObject spawnedBrush;

    private void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged
                .AddListener(
                    OnTrackablesChanged
                );
        }
        // Remove Hider runtime objects when manager is disabled.
        CleanupHiderObjects();
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged
                .RemoveListener(
                    OnTrackablesChanged
                );
        }
    }

    private void OnTrackablesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs
    )
    {
        foreach (
            ARTrackedImage trackedImage
            in eventArgs.added
        )
        {
            UpdateTrackedObject(
                trackedImage
            );
        }

        foreach (
            ARTrackedImage trackedImage
            in eventArgs.updated
        )
        {
            UpdateTrackedObject(
                trackedImage
            );
        }
    }

    private void UpdateTrackedObject(
        ARTrackedImage trackedImage
    )
    {
        bool isTracking =
            trackedImage.trackingState ==
            TrackingState.Tracking;

        string imageName =
            trackedImage.referenceImage.name;

        // Palette target
        if (
            imageName ==
            "PaletteTarget_v"
        )
        {
            if (
                spawnedPalette == null &&
                isTracking
            )
            {
                spawnedPalette =
                    Instantiate(
                        palettePrefab,
                        trackedImage.transform.position,
                        trackedImage.transform.rotation
                    );

                if (keepPaletteAnchored)
                {
                    spawnedPalette.transform.parent =
                        null;
                }
                else
                {
                    spawnedPalette.transform.SetParent(
                        trackedImage.transform
                    );
                }
            }
            else if (
                spawnedPalette != null
            )
            {
                if (keepPaletteAnchored)
                {
                    if (isTracking)
                    {
                        spawnedPalette.transform.position =
                            trackedImage.transform.position;

                        spawnedPalette.transform.rotation =
                            trackedImage.transform.rotation;
                    }

                    spawnedPalette.SetActive(true);
                }
                else
                {
                    spawnedPalette.SetActive(
                        isTracking
                    );
                }
            }
        }
        // Brush tip target
        else if (
            imageName ==
            "BrushTipTarget"
        )
        {
            if (
                spawnedBrush == null &&
                isTracking
            )
            {
                spawnedBrush =
                    Instantiate(
                        brushTipPrefab,
                        trackedImage.transform
                    );

                spawnedBrush.transform.localPosition =
                    Vector3.zero;

                spawnedBrush.transform.localRotation =
                    Quaternion.identity;
            }

            if (
                spawnedBrush != null
            )
            {
                spawnedBrush.SetActive(
                    isTracking
                );
            }
        }
    }

    // Removes the spawned Hider palette.
    public void RemoveSpawnedPalette()
    {
        if (spawnedPalette != null)
        {
            Destroy(
                spawnedPalette
            );

            spawnedPalette = null;
        }

        Debug.Log(
            "[PaintManager] Spawned palette removed."
        );
    }

    // Removes the spawned physical brush.
    public void RemoveSpawnedBrush()
    {
        if (spawnedBrush != null)
        {
            Destroy(
                spawnedBrush
            );

            spawnedBrush = null;
        }

        Debug.Log(
            "[PaintManager] Spawned brush removed."
        );
    }

    // Removes all Hider runtime objects.
    public void CleanupHiderObjects()
    {
        RemoveSpawnedPalette();
        RemoveSpawnedBrush();

        Debug.Log(
            "[PaintManager] Hider objects cleaned up."
        );
    }

    // Allows the Hider system to start fresh next round.
    public void ResetPaintManager()
    {
        CleanupHiderObjects();
    }

    public GameObject GetSpawnedPalette()
    {
        return spawnedPalette;
    }

    public GameObject GetSpawnedBrush()
    {
        return spawnedBrush;
    }
}