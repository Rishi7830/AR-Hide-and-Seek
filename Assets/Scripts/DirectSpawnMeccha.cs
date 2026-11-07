using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DirectSpawnMeccha : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField]
    private ARPlaneManager planeManager;

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private ARAnchorManager anchorManager;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject horizontalMecchaPrefab;

    [SerializeField]
    private GameObject verticalMecchaPrefab;

    [Header("Legacy UI Panel")]
    [Tooltip(
        "Old Meccha selection panel. " +
        "It is now kept disabled because Spawn directly creates " +
        "a horizontal Meccha."
    )]
    [SerializeField]
    private GameObject selectionPanel;

    [Header("Fine-Tune Controller")]
    public MecchaFineTuneController fineTuneController;

    [Header("Spawn Settings")]
    [Tooltip(
        "Minimum distance in meters required between newly spawned Mecchas."
    )]
    [SerializeField]
    private float minDistanceBetweenSpawns =
        0.5f;

    [Header("Coordinate Display")]
    public MecchaCoordinateDisplay coordinateDisplay;

    [Header("Counter Manager")]
    public MecchaCounterManager counterManager;

    private readonly List<GameObject> spawnedMecchas =
        new List<GameObject>();

    [Header("Multiplayer")]
    [SerializeField]
    private NetworkMecchaManager networkMecchaManager;

    private void Start()
    {
        if (arCamera == null)
        {
            arCamera =
                Camera.main;
        }

        // The old Horizontal / Vertical selection panel
        // is no longer used.
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }

    // ============================================================
    // MAIN SPAWN BUTTON
    // ============================================================

    public void OpenSelectionPanel()
    {
        // This method is intentionally retained so the existing
        // Unity Button OnClick reference does not need to change.
        //
        // Previously:
        // Spawn Button -> OpenSelectionPanel -> choose H/V.
        //
        // Now:
        // Spawn Button -> OpenSelectionPanel -> Horizontal Meccha.

        SpawnHorizontal();
    }

    // Optional cleaner method if you want to reassign the
    // Spawn Button in the Inspector later.
    public void OnSpawnButtonClicked()
    {
        SpawnHorizontal();
    }

    // ============================================================
    // SPAWN HORIZONTAL MECCHA
    // ============================================================

    public void SpawnHorizontal()
    {
        SpawnMecchaOnPlane(
            horizontalMecchaPrefab,
            Quaternion.identity,
            0
        );
    }

    // ============================================================
    // SPAWN VERTICAL MECCHA
    // ============================================================

    // Kept for backwards compatibility.
    // Nothing in the new Hider UI needs to call this.
    public void SpawnVertical()
    {
        Quaternion tilt90OnX =
            Quaternion.Euler(
                90f,
                0f,
                0f
            );

        SpawnMecchaOnPlane(
            verticalMecchaPrefab,
            tilt90OnX,
            1
        );
    }

    // ============================================================
    // SPAWN MECCHA ON AR PLANE
    // ============================================================

    private void SpawnMecchaOnPlane(
        GameObject prefabToSpawn,
        Quaternion extraRotation,
        int mecchaType
    )
    {
        if (
            planeManager == null ||
            prefabToSpawn == null
        )
        {
            Debug.LogWarning(
                "Missing Plane Manager or Prefab reference."
            );

            return;
        }

        if (arCamera == null)
        {
            Debug.LogWarning(
                "AR Camera reference is missing."
            );

            return;
        }

        ARPlane nearestPlane =
            GetNearestAvailablePlane();

        if (nearestPlane == null)
        {
            Debug.LogWarning(
                "No available plane found in sight."
            );

            return;
        }

        // Close any active fine-tuning session.
        if (fineTuneController != null)
        {
            fineTuneController
                .CloseFineTunePanel();
        }

        // Calculate spawn position / rotation.
        Vector3 spawnPosition =
            nearestPlane.center;

        Quaternion baseRotation;

        if (
            nearestPlane.alignment ==
            PlaneAlignment.Vertical
        )
        {
            baseRotation =
                Quaternion.LookRotation(
                    nearestPlane.normal
                );
        }
        else
        {
            Vector3 lookDirection =
                arCamera.transform.position -
                spawnPosition;

            // Keep the Meccha upright on horizontal planes.
            lookDirection.y =
                0f;

            if (
                lookDirection.sqrMagnitude <
                0.0001f
            )
            {
                lookDirection =
                    arCamera.transform.forward;

                lookDirection.y =
                    0f;
            }

            if (
                lookDirection.sqrMagnitude <
                0.0001f
            )
            {
                lookDirection =
                    Vector3.forward;
            }

            baseRotation =
                Quaternion.LookRotation(
                    lookDirection.normalized
                );
        }

        Quaternion finalRotation =
            baseRotation *
            extraRotation;

        // Create anchor container.
        GameObject anchorObject =
            new GameObject(
                "MecchaAnchor"
            );

        anchorObject.transform
            .SetPositionAndRotation(
                spawnPosition,
                finalRotation
            );

        // Add ARAnchor using the API supported by the project.
        ARAnchor newAnchor =
            anchorObject.AddComponent<
                ARAnchor
            >();

        if (newAnchor == null)
        {
            Debug.LogWarning(
                "Failed to create ARAnchor."
            );

            Destroy(
                anchorObject
            );

            return;
        }

        // Create Meccha as child of the anchor.
        GameObject newMeccha =
            Instantiate(
                prefabToSpawn,
                anchorObject.transform
            );

        if (newMeccha == null)
        {
            Debug.LogWarning(
                "Failed to instantiate Meccha."
            );

            Destroy(
                anchorObject
            );

            return;
        }

        // Meccha starts exactly at the anchor.
        newMeccha.transform.localPosition =
            Vector3.zero;

        newMeccha.transform.localRotation =
            Quaternion.identity;

        // Preserve prefab scale.
        // We intentionally do not modify localScale.

        Debug.Log(
            "Meccha spawned successfully. " +
            "Anchor: " +
            anchorObject.name +
            " | Meccha: " +
            newMeccha.name
        );

        // Register Meccha.
        spawnedMecchas.Add(
            newMeccha
        );

        if (
            networkMecchaManager != null
        )
        {
            networkMecchaManager
                .RegisterHiderMeccha(
                    newMeccha,
                    mecchaType
                );
        }

        OnMecchaSpawned(
            newMeccha
        );

        // Update counter.
        if (counterManager != null)
        {
            counterManager
                .IncrementCount();
        }

        // Safety: ensure the old selection panel
        // remains hidden.
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(
                false
            );
        }
    }

    // ============================================================
    // FIND NEAREST AVAILABLE AR PLANE
    // ============================================================

    private ARPlane GetNearestAvailablePlane()
    {
        if (planeManager == null)
        {
            return null;
        }

        if (arCamera == null)
        {
            return null;
        }

        ARPlane closestPlane =
            null;

        float shortestDistance =
            float.MaxValue;

        Vector3 cameraPosition =
            arCamera.transform.position;

        foreach (
            ARPlane plane
            in planeManager.trackables
        )
        {
            // Only use currently tracked planes.
            if (
                plane.trackingState !=
                TrackingState.Tracking
            )
            {
                continue;
            }

            bool isValidPlane =
                plane.alignment ==
                    PlaneAlignment.HorizontalUp ||
                plane.alignment ==
                    PlaneAlignment.HorizontalDown ||
                plane.alignment ==
                    PlaneAlignment.Vertical;

            if (!isValidPlane)
            {
                continue;
            }

            // Check whether this plane is already occupied.
            bool isOccupied =
                false;

            foreach (
                GameObject spawned
                in spawnedMecchas
            )
            {
                if (spawned == null)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        plane.center,
                        spawned.transform.position
                    );

                if (
                    distance <
                    minDistanceBetweenSpawns
                )
                {
                    isOccupied =
                        true;

                    break;
                }
            }

            if (isOccupied)
            {
                continue;
            }

            // Find closest valid plane.
            float cameraDistance =
                Vector3.Distance(
                    cameraPosition,
                    plane.center
                );

            if (
                cameraDistance <
                shortestDistance
            )
            {
                shortestDistance =
                    cameraDistance;

                closestPlane =
                    plane;
            }
        }

        return closestPlane;
    }

    // ============================================================
    // DELETE MOST RECENT MECCHA
    // ============================================================

    public void DeleteMostRecentMeccha()
    {
        if (
            spawnedMecchas.Count ==
            0
        )
        {
            return;
        }

        int lastIndex =
            spawnedMecchas.Count -
            1;

        GameObject lastMeccha =
            spawnedMecchas[
                lastIndex
            ];

        // Close fine-tune panel.
        if (fineTuneController != null)
        {
            fineTuneController
                .CloseFineTunePanel();
        }

        // Remove from tracking list.
        spawnedMecchas.RemoveAt(
            lastIndex
        );

        if (counterManager != null)
        {
            counterManager
                .DecrementCount();
        }

        // Target previous Meccha.
        GameObject prevMeccha =
            spawnedMecchas.Count > 0
                ? spawnedMecchas[
                    spawnedMecchas.Count - 1
                ]
                : null;

        if (fineTuneController != null)
        {
            fineTuneController
                .SetTargetMeccha(
                    prevMeccha
                );
        }

        if (coordinateDisplay != null)
        {
            coordinateDisplay
                .SetTargetMeccha(
                    prevMeccha
                );
        }

        if (
            networkMecchaManager != null &&
            lastMeccha != null
        )
        {
            networkMecchaManager
                .RemoveHiderMeccha(
                    lastMeccha
                );
        }

        // Delete Meccha and its anchor.
        if (lastMeccha != null)
        {
            Transform parent =
                lastMeccha.transform.parent;

            if (parent != null)
            {
                ARAnchor parentAnchor =
                    parent.GetComponent<
                        ARAnchor
                    >();

                if (parentAnchor != null)
                {
                    // Because the Meccha is a child
                    // of the anchor, destroying the
                    // anchor also destroys the Meccha.
                    Destroy(
                        parentAnchor.gameObject
                    );

                    Debug.Log(
                        "Deleted Meccha and its ARAnchor."
                    );
                }
                else
                {
                    // Safety fallback.
                    Destroy(
                        lastMeccha
                    );

                    Debug.Log(
                        "Deleted Meccha without parent ARAnchor."
                    );
                }
            }
            else
            {
                // Safety fallback.
                Destroy(
                    lastMeccha
                );

                Debug.Log(
                    "Deleted Meccha with no parent."
                );
            }
        }
    }

    // ============================================================
    // MECCHA SPAWNED
    // ============================================================

    public void OnMecchaSpawned(
        GameObject newlySpawnedMeccha
    )
    {
        if (
            newlySpawnedMeccha == null
        )
        {
            return;
        }

        if (fineTuneController != null)
        {
            fineTuneController
                .SetTargetMeccha(
                    newlySpawnedMeccha
                );
        }

        if (coordinateDisplay != null)
        {
            coordinateDisplay
                .SetTargetMeccha(
                    newlySpawnedMeccha
                );
        }
    }
}