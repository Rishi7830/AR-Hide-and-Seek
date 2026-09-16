using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class HunterGunManager : MonoBehaviour
{
    // =============================================================
    // AR IMAGE TRACKING
    // =============================================================

    [Header("AR Image Tracking")]
    [SerializeField]
    private ARTrackedImageManager trackedImageManager;

    // =============================================================
    // GUN RACK
    // =============================================================

    [Header("Gun Rack")]
    [SerializeField]
    private GameObject gunRackPrefab;

    // =============================================================
    // TRACKED IMAGE SETTINGS
    // =============================================================

    [Header("Tracked Image Settings")]
    [SerializeField]
    private string gunSpawnImageName = "HunterGunSpawn";

    // =============================================================
    // INTERNAL VARIABLES
    // =============================================================

    private GameObject spawnedGunRack;

    private ARTrackedImage currentTrackedImage;

    // =============================================================
    // AWAKE
    // =============================================================

    private void Awake()
    {
        // Find the ARTrackedImageManager automatically
        // if it has not been assigned in the Inspector.

        if (trackedImageManager == null)
        {
            trackedImageManager =
                FindFirstObjectByType<ARTrackedImageManager>();
        }
    }

    // =============================================================
    // ENABLE
    // =============================================================

    private void OnEnable()
    {
        if (trackedImageManager != null)
        {
            // AR Foundation 6:
            // trackablesChanged is a UnityEvent.
            trackedImageManager.trackablesChanged
                .AddListener(
                    OnTrackedImagesChanged
                );
        }
    }

    // =============================================================
    // DISABLE
    // =============================================================

    private void OnDisable()
    {
        if (trackedImageManager != null)
        {
            // Remove the listener when this object is disabled.

            trackedImageManager.trackablesChanged
                .RemoveListener(
                    OnTrackedImagesChanged
                );
        }
    }

    // =============================================================
    // TRACKED IMAGE CHANGES
    // =============================================================

    private void OnTrackedImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args
    )
    {
        // ---------------------------------------------------------
        // ADDED
        // ---------------------------------------------------------

        foreach (
            ARTrackedImage trackedImage
            in args.added
        )
        {
            HandleTrackedImage(
                trackedImage
            );
        }

        // ---------------------------------------------------------
        // UPDATED
        // ---------------------------------------------------------

        foreach (
            ARTrackedImage trackedImage
            in args.updated
        )
        {
            HandleTrackedImage(
                trackedImage
            );
        }

        // ---------------------------------------------------------
        // REMOVED
        //
        // In AR Foundation 6, removed contains:
        //
        // KeyValuePair<TrackableId, ARTrackedImage>
        //
        // rather than ARTrackedImage directly.
        // ---------------------------------------------------------

        foreach (
            var removedEntry
            in args.removed
        )
        {
            ARTrackedImage removedImage =
                removedEntry.Value;

            if (
                currentTrackedImage != null &&
                removedImage == currentTrackedImage
            )
            {
                HideGunRack();

                currentTrackedImage =
                    null;
            }
        }
    }

    // =============================================================
    // HANDLE TRACKED IMAGE
    // =============================================================

    private void HandleTrackedImage(
        ARTrackedImage trackedImage
    )
    {
        if (trackedImage == null)
        {
            return;
        }

        // ---------------------------------------------------------
        // CHECK IMAGE NAME
        // ---------------------------------------------------------

        if (
            trackedImage.referenceImage.name !=
            gunSpawnImageName
        )
        {
            return;
        }

        // Remember the current tracked image.

        currentTrackedImage =
            trackedImage;

        // ---------------------------------------------------------
        // CREATE GUN RACK ON FIRST DETECTION
        // ---------------------------------------------------------

        if (spawnedGunRack == null)
        {
            if (gunRackPrefab == null)
            {
                Debug.LogError(
                    "[HunterGunManager] " +
                    "Gun Rack Prefab is not assigned."
                );

                return;
            }

            spawnedGunRack =
                Instantiate(
                    gunRackPrefab
                );

            // Parent it to the tracked image.

            spawnedGunRack.transform.SetParent(
                trackedImage.transform,
                false
            );

            // Match tracked-image origin.

            spawnedGunRack.transform.localPosition =
                Vector3.zero;

            spawnedGunRack.transform.localRotation =
                Quaternion.identity;

            spawnedGunRack.transform.localScale =
                Vector3.one;
        }
        else
        {
            // Make sure the existing rack follows
            // the currently tracked image.

            spawnedGunRack.transform.SetParent(
                trackedImage.transform,
                false
            );

            spawnedGunRack.transform.localPosition =
                Vector3.zero;

            spawnedGunRack.transform.localRotation =
                Quaternion.identity;

            spawnedGunRack.transform.localScale =
                Vector3.one;
        }

        // ---------------------------------------------------------
        // CHECK TRACKING STATE
        // ---------------------------------------------------------

        if (
            trackedImage.trackingState ==
            TrackingState.Tracking
        )
        {
            spawnedGunRack.SetActive(
                true
            );
        }
        else
        {
            HideGunRack();
        }
    }

    // =============================================================
    // HIDE GUN RACK
    // =============================================================

    private void HideGunRack()
    {
        if (spawnedGunRack != null)
        {
            spawnedGunRack.SetActive(
                false
            );
        }
    }

    // =============================================================
    // GET SPAWNED GUN RACK
    // =============================================================

    public GameObject GetSpawnedGunRack()
    {
        return spawnedGunRack;
    }
}