using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnMeccha : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARAnchorManager anchorManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject horizontalMecchaPrefab;
    [SerializeField] private GameObject verticalMecchaPrefab;

    [Header("UI Panel")]
    [SerializeField] private GameObject selectionPanel;

    [Header("Fine-Tune Controller")]
    public MecchaFineTuneController fineTuneController;

    [Header("Spawn Settings")]
    [Tooltip("Minimum distance in meters required between newly spawned Mecchas.")]
    [SerializeField] private float minDistanceBetweenSpawns = 0.5f;

    [Header("Coordinate Display")]
    public MecchaCoordinateDisplay coordinateDisplay;

    [Header("Counter Manager")]
    public MecchaCounterManager counterManager;

    private List<GameObject> spawnedMecchas = new List<GameObject>();

    private void Start()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    public void OpenSelectionPanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
    }

    public void SpawnHorizontal()
    {
        SpawnMecchaOnPlane(
            horizontalMecchaPrefab,
            Quaternion.identity
        );
    }

    public void SpawnVertical()
    {
        Quaternion tilt90OnX =
            Quaternion.Euler(90f, 0f, 0f);

        SpawnMecchaOnPlane(
            verticalMecchaPrefab,
            tilt90OnX
        );
    }

    private void SpawnMecchaOnPlane(
        GameObject prefabToSpawn,
        Quaternion extraRotation
    )
    {
        if (planeManager == null || prefabToSpawn == null)
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

        // CLOSE ANY ACTIVE FINE-TUNING SESSION

        if (fineTuneController != null)
        {
            fineTuneController.CloseFineTunePanel();
        }

        // CALCULATE SPAWN POSITION / ROTATION

        Vector3 spawnPosition =
            nearestPlane.center;

        Quaternion baseRotation;

        if (nearestPlane.alignment ==
            PlaneAlignment.Vertical)
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

            // Keep the Meccha upright on horizontal planes
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection =
                    arCamera.transform.forward;

                lookDirection.y = 0f;
            }

            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                lookDirection = Vector3.forward;
            }

            baseRotation =
                Quaternion.LookRotation(
                    lookDirection.normalized
                );
        }

        Quaternion finalRotation =
            baseRotation * extraRotation;

        // CREATE ANCHOR CONTAINER

        GameObject anchorObject =
            new GameObject("MecchaAnchor");

        anchorObject.transform.SetPositionAndRotation(
            spawnPosition,
            finalRotation
        );

        // Add ARAnchor using the API supported by the project.
        ARAnchor newAnchor =
            anchorObject.AddComponent<ARAnchor>();

        if (newAnchor == null)
        {
            Debug.LogWarning(
                "Failed to create ARAnchor."
            );

            Destroy(anchorObject);
            return;
        }

        // CREATE MECCHA AS CHILD OF THE ANCHOR

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

            Destroy(anchorObject);
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
            "Anchor: " + anchorObject.name +
            " | Meccha: " + newMeccha.name
        );

        // REGISTER MECCHA

        spawnedMecchas.Add(newMeccha);

        OnMecchaSpawned(newMeccha);

        // UPDATE COUNTER

        if (counterManager != null)
        {
            counterManager.IncrementCount();
        }

        // CLOSE SELECTION PANEL

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }
    }

    private ARPlane GetNearestAvailablePlane()
    {
        if (planeManager == null)
            return null;

        if (arCamera == null)
            return null;

        ARPlane closestPlane = null;

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
                continue;

            // CHECK WHETHER THIS PLANE IS ALREADY OCCUPIED

            bool isOccupied = false;

            foreach (
                GameObject spawned
                in spawnedMecchas
            )
            {
                if (spawned == null)
                    continue;

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
                    isOccupied = true;
                    break;
                }
            }

            if (isOccupied)
                continue;

            // FIND CLOSEST VALID PLANE

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

                closestPlane = plane;
            }
        }

        return closestPlane;
    }

    public void DeleteMostRecentMeccha()
    {
        if (spawnedMecchas.Count == 0)
            return;

        int lastIndex =
            spawnedMecchas.Count - 1;

        GameObject lastMeccha =
            spawnedMecchas[lastIndex];

        // CLOSE FINE-TUNE PANEL

        if (fineTuneController != null)
        {
            fineTuneController.CloseFineTunePanel();
        }

        // REMOVE FROM TRACKING LIST

        spawnedMecchas.RemoveAt(lastIndex);

        if (counterManager != null)
        {
            counterManager.DecrementCount();
        }

        // TARGET PREVIOUS MECCHA

        GameObject prevMeccha =
            spawnedMecchas.Count > 0
                ? spawnedMecchas[
                    spawnedMecchas.Count - 1
                ]
                : null;

        if (fineTuneController != null)
        {
            fineTuneController.SetTargetMeccha(
                prevMeccha
            );
        }

        if (coordinateDisplay != null)
        {
            coordinateDisplay.SetTargetMeccha(
                prevMeccha
            );
        }

        // DELETE MECCHA AND ITS ANCHOR

        if (lastMeccha != null)
        {
            Transform parent =
                lastMeccha.transform.parent;

            if (parent != null)
            {
                ARAnchor parentAnchor =
                    parent.GetComponent<ARAnchor>();

                if (parentAnchor != null)
                {
                    // Because the Meccha is a child of the
                    // anchor, destroying the anchor also
                    // destroys the Meccha.
                    Destroy(parentAnchor.gameObject);

                    Debug.Log(
                        "Deleted Meccha and its ARAnchor."
                    );
                }
                else
                {
                    // Safety fallback.
                    Destroy(lastMeccha);

                    Debug.Log(
                        "Deleted Meccha without parent ARAnchor."
                    );
                }
            }
            else
            {
                // Safety fallback.
                Destroy(lastMeccha);

                Debug.Log(
                    "Deleted Meccha with no parent."
                );
            }
        }
    }

    public void OnMecchaSpawned(
        GameObject newlySpawnedMeccha
    )
    {
        if (newlySpawnedMeccha == null)
            return;

        if (fineTuneController != null)
        {
            fineTuneController.SetTargetMeccha(
                newlySpawnedMeccha
            );
        }

        if (coordinateDisplay != null)
        {
            coordinateDisplay.SetTargetMeccha(
                newlySpawnedMeccha
            );
        }
    }
}