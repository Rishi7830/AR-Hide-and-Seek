using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class HunterGunRackManager : MonoBehaviour
{
    [Header("Shared Origin")]

    [SerializeField]
    private SharedAROriginManager
        sharedOriginManager;

    [SerializeField]
    private ARAnchorManager
        anchorManager;

    [Header("Gun Rack")]

    [SerializeField]
    private GameObject
        gunRackPrefab;

    [Header("Spawn Buttons")]

    [Tooltip(
        "Original single-screen Spawn Gun button."
    )]
    [SerializeField]
    private Button spawnGunButton;

    [Tooltip(
        "Left-eye Spawn Gun button for stereo mode."
    )]
    [SerializeField]
    private Button leftSpawnGunButton;

    [Tooltip(
        "Right-eye Spawn Gun button for stereo mode."
    )]
    [SerializeField]
    private Button rightSpawnGunButton;

    [Header("Spawn Position")]

    [Tooltip(
        "Distance to the right of the Initial Coordinates origin."
    )]
    [SerializeField]
    private float rightOffset =
        0.15f;

    [Tooltip(
        "Optional vertical offset from the Initial Coordinates origin."
    )]
    [SerializeField]
    private float verticalOffset =
        0f;

    [Tooltip(
        "Optional forward offset from the Initial Coordinates origin."
    )]
    [SerializeField]
    private float forwardOffset =
        0f;

    [Header("Spawn Rotation")]

    [Tooltip(
        "Additional local rotation applied to the gun rack."
    )]
    [SerializeField]
    private Vector3 rotationOffset =
        Vector3.zero;

    [Header("Debug")]

    [SerializeField]
    private bool logDebug =
        true;

    private GameObject
        gunRackAnchorObject;

    private ARAnchor
        gunRackAnchor;

    private GameObject
        spawnedGunRack;

    private bool
        gunRackHasBeenSpawned =
        false;

    private bool
        buttonsConnected =
        false;

    private void Awake()
    {
        if (
            sharedOriginManager == null
        )
        {
            sharedOriginManager =
                FindFirstObjectByType<
                    SharedAROriginManager
                >();
        }

        if (
            anchorManager == null
        )
        {
            anchorManager =
                FindFirstObjectByType<
                    ARAnchorManager
                >();
        }
    }

    private void OnEnable()
    {
        ConnectButtons();
    }

    private void OnDisable()
    {
        DisconnectButtons();
    }

    private void ConnectButtons()
    {
        if (
            buttonsConnected
        )
        {
            return;
        }

        if (
            spawnGunButton != null
        )
        {
            spawnGunButton.onClick
                .AddListener(
                    SpawnGunRack
                );
        }

        if (
            leftSpawnGunButton != null
        )
        {
            leftSpawnGunButton.onClick
                .AddListener(
                    SpawnGunRack
                );
        }

        if (
            rightSpawnGunButton != null
        )
        {
            rightSpawnGunButton.onClick
                .AddListener(
                    SpawnGunRack
                );
        }

        buttonsConnected =
            true;
    }

    private void DisconnectButtons()
    {
        if (
            !buttonsConnected
        )
        {
            return;
        }

        if (
            spawnGunButton != null
        )
        {
            spawnGunButton.onClick
                .RemoveListener(
                    SpawnGunRack
                );
        }

        if (
            leftSpawnGunButton != null
        )
        {
            leftSpawnGunButton.onClick
                .RemoveListener(
                    SpawnGunRack
                );
        }

        if (
            rightSpawnGunButton != null
        )
        {
            rightSpawnGunButton.onClick
                .RemoveListener(
                    SpawnGunRack
                );
        }

        buttonsConnected =
            false;
    }

    public void SpawnGunRack()
    {
        if (
            gunRackHasBeenSpawned
        )
        {
            if (
                logDebug
            )
            {
                Debug.Log(
                    "[HunterGunManager] " +
                    "Gun rack already spawned."
                );
            }

            return;
        }

        if (
            sharedOriginManager == null ||
            !sharedOriginManager.HasOrigin ||
            sharedOriginManager.SharedOrigin ==
                null
        )
        {
            Debug.LogWarning(
                "[HunterGunManager] " +
                "Cannot spawn gun rack because " +
                "SharedOrigin is not ready."
            );

            return;
        }

        if (
            gunRackPrefab == null
        )
        {
            Debug.LogError(
                "[HunterGunManager] " +
                "Gun Rack Prefab has not been assigned."
            );

            return;
        }

        if (
            anchorManager == null
        )
        {
            Debug.LogError(
                "[HunterGunManager] " +
                "ARAnchorManager has not been assigned."
            );

            return;
        }

        if (
            !anchorManager.enabled
        )
        {
            Debug.LogError(
                "[HunterGunManager] " +
                "ARAnchorManager is disabled."
            );

            return;
        }

        Transform sharedOrigin =
            sharedOriginManager.SharedOrigin;

        // Calculate the rack position once
        // relative to the Initial Coordinates origin.
        Vector3 spawnPosition =
            sharedOrigin.position +
            sharedOrigin.right *
            rightOffset +
            Vector3.up *
            verticalOffset +
            sharedOrigin.forward *
            forwardOffset;

        Quaternion spawnRotation =
            sharedOrigin.rotation *
            Quaternion.Euler(
                rotationOffset
            );

        CreateIndependentGunRackAnchor(
            spawnPosition,
            spawnRotation
        );
    }

    private void CreateIndependentGunRackAnchor(
        Vector3 position,
        Quaternion rotation
    )
    {
        gunRackAnchorObject =
            new GameObject(
                "GunRackARAnchor"
            );

        gunRackAnchorObject.transform
            .SetPositionAndRotation(
                position,
                rotation
            );

        gunRackAnchorObject.transform.localScale =
            Vector3.one;

        // This becomes a completely separate
        // AR anchor from the Initial Coordinates origin.
        gunRackAnchor =
            gunRackAnchorObject
                .AddComponent<ARAnchor>();

        if (
            gunRackAnchor == null
        )
        {
            Debug.LogError(
                "[HunterGunManager] " +
                "Failed to create Gun Rack ARAnchor."
            );

            Destroy(
                gunRackAnchorObject
            );

            gunRackAnchorObject =
                null;

            return;
        }

        // Spawn the rack as a child of its own anchor.
        spawnedGunRack =
            Instantiate(
                gunRackPrefab,
                gunRackAnchor.transform
            );

        spawnedGunRack.transform.localPosition =
            Vector3.zero;

        spawnedGunRack.transform.localRotation =
            Quaternion.identity;

        spawnedGunRack.transform.localScale =
            Vector3.one;

        gunRackHasBeenSpawned =
            true;

        if (
            logDebug
        )
        {
            Debug.Log(
                "[HunterGunManager] " +
                "Gun Rack spawned with its own ARAnchor."
            );

            Debug.Log(
                "[HunterGunManager] " +
                "Spawn Position = " +
                spawnedGunRack.transform.position
            );

            Debug.Log(
                "[HunterGunManager] " +
                "SharedOrigin Position = " +
                sharedOriginManager
                    .SharedOrigin
                    .position
            );

            Debug.Log(
                "[HunterGunManager] " +
                "Right Offset = " +
                rightOffset +
                " metres."
            );
        }
    }

    public GameObject GetSpawnedGunRack()
    {
        return spawnedGunRack;
    }

    public bool HasSpawnedGunRack()
    {
        return gunRackHasBeenSpawned;
    }

    public void ResetGunRack()
    {
        if (
            spawnedGunRack != null
        )
        {
            Destroy(
                spawnedGunRack
            );

            spawnedGunRack =
                null;
        }

        if (
            gunRackAnchorObject != null
        )
        {
            Destroy(
                gunRackAnchorObject
            );

            gunRackAnchorObject =
                null;
        }

        gunRackAnchor =
            null;

        gunRackHasBeenSpawned =
            false;

        if (
            logDebug
        )
        {
            Debug.Log(
                "[HunterGunManager] " +
                "Gun Rack reset."
            );
        }
    }
}