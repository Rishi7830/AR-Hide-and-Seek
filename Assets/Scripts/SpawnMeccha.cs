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
        SpawnMecchaOnPlane(horizontalMecchaPrefab, Quaternion.identity);
    }

    public void SpawnVertical()
    {
        Quaternion tilt90OnX = Quaternion.Euler(90f, 0f, 0f);
        SpawnMecchaOnPlane(verticalMecchaPrefab, tilt90OnX);
    }

    private void SpawnMecchaOnPlane(GameObject prefabToSpawn, Quaternion extraRotation)
    {
        if (planeManager == null || prefabToSpawn == null)
        {
            Debug.LogWarning("Missing Plane Manager or Prefab reference.");
            return;
        }

        ARPlane nearestPlane = GetNearestAvailablePlane();

        if (nearestPlane != null)
        {
            // Close active fine-tuning session safely before creating new object
            if (fineTuneController != null)
            {
                fineTuneController.CloseFineTunePanel();
            }

            Vector3 spawnPosition = nearestPlane.center;
            Quaternion baseRotation;

            if (nearestPlane.alignment == PlaneAlignment.Vertical)
            {
                baseRotation = Quaternion.LookRotation(nearestPlane.normal);
            }
            else
            {
                Vector3 lookDirection = arCamera.transform.position - spawnPosition;
                lookDirection.y = 0;

                if (lookDirection == Vector3.zero)
                {
                    lookDirection = arCamera.transform.forward;
                    lookDirection.y = 0;
                }

                baseRotation = Quaternion.LookRotation(lookDirection);
            }

            Quaternion finalRotation = baseRotation * extraRotation;

            // 1. Instantiate prefab
            GameObject newMeccha = Instantiate(prefabToSpawn, spawnPosition, finalRotation);

            // 2. Attach ARAnchor FIRST before target registration
            if (anchorManager != null)
            {
                ARAnchor anchor = newMeccha.AddComponent<ARAnchor>();
                if (anchor == null)
                {
                    Debug.LogWarning("Failed to attach ARAnchor.");
                }
            }

            // 3. Register tracking
            spawnedMecchas.Add(newMeccha);
            OnMecchaSpawned(newMeccha);

            if (selectionPanel != null)
                selectionPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No available plane found in sight.");
        }
    }

    private ARPlane GetNearestAvailablePlane()
    {
        ARPlane closestPlane = null;
        float shortestDistance = float.MaxValue;
        Vector3 cameraPosition = arCamera.transform.position;

        foreach (var plane in planeManager.trackables)
        {
            if (plane.trackingState != TrackingState.Tracking)
                continue;

            bool isValidPlane = (plane.alignment == PlaneAlignment.HorizontalUp ||
                                 plane.alignment == PlaneAlignment.HorizontalDown ||
                                 plane.alignment == PlaneAlignment.Vertical);

            if (isValidPlane)
            {
                bool isOccupied = false;
                foreach (var spawned in spawnedMecchas)
                {
                    if (spawned != null && Vector3.Distance(plane.center, spawned.transform.position) < minDistanceBetweenSpawns)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    float distance = Vector3.Distance(cameraPosition, plane.center);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestPlane = plane;
                    }
                }
            }
        }

        return closestPlane;
    }

    public void DeleteMostRecentMeccha()
    {
        if (spawnedMecchas.Count > 0)
        {
            int lastIndex = spawnedMecchas.Count - 1;
            GameObject lastMeccha = spawnedMecchas[lastIndex];

            // 1. Close fine tuning panel if active
            if (fineTuneController != null)
            {
                fineTuneController.CloseFineTunePanel();
            }

            // 2. Remove from list
            spawnedMecchas.RemoveAt(lastIndex);

            // 3. Target remaining object
            GameObject prevMeccha = spawnedMecchas.Count > 0 ? spawnedMecchas[spawnedMecchas.Count - 1] : null;

            if (fineTuneController != null)
                fineTuneController.SetTargetMeccha(prevMeccha);

            if (coordinateDisplay != null)
                coordinateDisplay.SetTargetMeccha(prevMeccha);

            // 4. Destroy Object and Anchor
            if (lastMeccha != null)
            {
                ARAnchor anchor = lastMeccha.GetComponent<ARAnchor>();
                if (anchor != null)
                {
                    Destroy(anchor);
                }
                Destroy(lastMeccha);
            }
        }
    }

    public void OnMecchaSpawned(GameObject newlySpawnedMeccha)
    {
        if (fineTuneController != null)
        {
            fineTuneController.SetTargetMeccha(newlySpawnedMeccha);
        }

        if (coordinateDisplay != null)
        {
            coordinateDisplay.SetTargetMeccha(newlySpawnedMeccha);
        }
    }
}