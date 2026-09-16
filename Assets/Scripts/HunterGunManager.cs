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
    private string gunSpawnImageName =
        "HunterGunSpawn";

    // =============================================================
    // PERSISTENCE
    // =============================================================

    [Header("Persistence")]

    [Tooltip(
        "Once the gun rack is spawned, it remains in the AR world " +
        "even when the Hunter image is no longer tracked."
    )]
    [SerializeField]
    private bool keepSpawnedGunRackPermanently = true;

    // =============================================================
    // INTERNAL STATE
    // =============================================================

    private GameObject spawnedGunRack;

    private bool gunRackHasBeenSpawned = false;

    // =============================================================
    // AWAKE
    // =============================================================

    private void Awake()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager =
                FindFirstObjectByType<
                    ARTrackedImageManager>();
        }
    }

    // =============================================================
    // ENABLE
    // =============================================================

    private void OnEnable()
    {
        if (trackedImageManager != null)
        {
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
        // DO NOTHING.
        //
        // The gun rack has already been detached from the tracked
        // image and therefore remains in AR world space.
        // ---------------------------------------------------------

        foreach (
            var removedEntry
            in args.removed
        )
        {
            // Intentionally empty.
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
        // CHECK THAT THIS IS OUR HUNTER IMAGE
        // ---------------------------------------------------------

        if (
            trackedImage.referenceImage.name
            != gunSpawnImageName
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // STOP IF THE GUN RACK ALREADY EXISTS
        // ---------------------------------------------------------

        if (gunRackHasBeenSpawned)
        {
            return;
        }

        // ---------------------------------------------------------
        // WAIT UNTIL IMAGE IS ACTUALLY TRACKED
        // ---------------------------------------------------------

        if (
            trackedImage.trackingState
            != TrackingState.Tracking
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // CHECK PREFAB
        // ---------------------------------------------------------

        if (gunRackPrefab == null)
        {
            Debug.LogError(
                "[HunterGunManager] " +
                "Gun Rack Prefab has not been assigned."
            );

            return;
        }

        // ---------------------------------------------------------
        // SPAWN DIRECTLY IN WORLD SPACE
        // ---------------------------------------------------------

        spawnedGunRack =
            Instantiate(
                gunRackPrefab,
                trackedImage.transform.position,
                trackedImage.transform.rotation
            );

        // ---------------------------------------------------------
        // RESET SCALE
        // ---------------------------------------------------------

        spawnedGunRack.transform.localScale =
            Vector3.one;

        // ---------------------------------------------------------
        // MARK AS SPAWNED
        // ---------------------------------------------------------

        gunRackHasBeenSpawned =
            true;

        Debug.Log(
            "[HunterGunManager] " +
            "Hunter Gun Rack spawned."
        );

        Debug.Log(
            "[HunterGunManager] Spawn Position: " +
            spawnedGunRack.transform.position
        );

        Debug.Log(
            "[HunterGunManager] Spawn Rotation: " +
            spawnedGunRack.transform.rotation
        );

        // ---------------------------------------------------------
        // DETACH FROM TRACKED IMAGE
        // ---------------------------------------------------------

        if (keepSpawnedGunRackPermanently)
        {
            DetachGunRackFromTrackedImage();
        }
    }

    // =============================================================
    // DETACH GUN RACK
    // =============================================================

    private void DetachGunRackFromTrackedImage()
    {
        if (spawnedGunRack == null)
        {
            return;
        }

        // Remove parent while preserving the current
        // world position, rotation and scale.

        spawnedGunRack.transform.SetParent(
            null,
            true
        );

        Debug.Log(
            "[HunterGunManager] " +
            "Gun Rack detached from ARTrackedImage."
        );

        Debug.Log(
            "[HunterGunManager] " +
            "Gun Rack will remain permanently " +
            "placed in the AR world."
        );
    }

    // =============================================================
    // GET SPAWNED GUN RACK
    // =============================================================

    public GameObject GetSpawnedGunRack()
    {
        return spawnedGunRack;
    }

    // =============================================================
    // CHECK IF GUN RACK EXISTS
    // =============================================================

    public bool HasSpawnedGunRack()
    {
        return gunRackHasBeenSpawned;
    }

    // =============================================================
    // RESET GUN RACK
    // =============================================================

    public void ResetGunRack()
    {
        if (spawnedGunRack != null)
        {
            Destroy(
                spawnedGunRack
            );

            spawnedGunRack =
                null;
        }

        gunRackHasBeenSpawned =
            false;

        Debug.Log(
            "[HunterGunManager] " +
            "Gun Rack reset."
        );
    }
}