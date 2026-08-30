using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MecchaFineTuneController : MonoBehaviour
{
    [Header("UI Panel Reference")]
    public GameObject fineTunePanel;

    [Header("AR Camera Reference")]
    public Transform arCameraTransform;
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;

    [Header("Feedback Materials")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Target & Tuning Settings")]
    public GameObject currentMeccha;
    public float stepDistance = 0.05f;
    public float rotationAngle = 15f;

    // Material tracking per renderer
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private bool isFineTuningActive = false;
    private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    private void Start()
    {
        if (fineTunePanel != null) fineTunePanel.SetActive(false);

        if (arCameraTransform == null && Camera.main != null)
            arCameraTransform = Camera.main.transform;

        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
    }

    private void Update()
    {
        if (isFineTuningActive && currentMeccha != null)
        {
            UpdatePlacementStatusFeedback();
        }
    }

    public void SetTargetMeccha(GameObject meccha)
    {
        if (currentMeccha == meccha) return;

        // Reset materials on previous target if switching
        if (currentMeccha != null)
        {
            RestoreOriginalMaterials();
        }

        currentMeccha = meccha;

        if (currentMeccha == null)
        {
            isFineTuningActive = false;
            if (fineTunePanel != null) fineTunePanel.SetActive(false);
            return;
        }

        if (isFineTuningActive)
        {
            CacheOriginalMaterials();
            UpdatePlacementStatusFeedback();
        }
    }

    public void ToggleFineTunePanel()
    {
        if (fineTunePanel != null)
        {
            bool isActive = !fineTunePanel.activeSelf;
            if (isActive) OpenFineTunePanel();
            else CloseFineTunePanel();
        }
    }

    public void OpenFineTunePanel()
    {
        if (fineTunePanel != null) fineTunePanel.SetActive(true);

        isFineTuningActive = true;

        if (currentMeccha != null)
        {
            CacheOriginalMaterials();
            UpdatePlacementStatusFeedback();
        }
    }

    public void CloseFineTunePanel()
    {
        isFineTuningActive = false;

        if (fineTunePanel != null) fineTunePanel.SetActive(false);

        RestoreOriginalMaterials();
        ReAnchorCurrentMeccha();
    }

    private void CacheOriginalMaterials()
    {
        originalMaterials.Clear();
        if (currentMeccha == null) return;

        Renderer[] renderers = currentMeccha.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                originalMaterials[r] = r.sharedMaterials;
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (currentMeccha == null) return;

        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.materials = entry.Value;
            }
        }

        originalMaterials.Clear();
    }

    private void ApplySingleMaterialToTarget(Material targetMat)
    {
        if (currentMeccha == null || targetMat == null) return;

        Renderer[] renderers = currentMeccha.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                Material[] matArray = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < matArray.Length; i++)
                {
                    matArray[i] = targetMat;
                }
                r.materials = matArray;
            }
        }
    }

    private void UpdatePlacementStatusFeedback()
    {
        bool isOnPlane = CheckIfMecchaIsOnPlane();

        if (isOnPlane)
        {
            ApplySingleMaterialToTarget(validMaterial);
        }
        else
        {
            ApplySingleMaterialToTarget(invalidMaterial);
        }
    }

    //private bool CheckIfMecchaIsOnPlane()
    //{
    //if (raycastManager == null || currentMeccha == null) return false;

    //Vector3 rayOrigin = currentMeccha.transform.position + Vector3.up * 0.1f;
    //Ray ray = new Ray(rayOrigin, Vector3.down);

    //if (raycastManager.Raycast(ray, s_Hits, TrackableType.Planes))
    //{
    //return true;
    //}

    //Ray wallRay = new Ray(currentMeccha.transform.position + currentMeccha.transform.forward * 0.1f, -currentMeccha.transform.forward);
    //if (raycastManager.Raycast(wallRay, s_Hits, TrackableType.PlaneWithinBounds))
    //{
    //return true;
    //}

    //return false;
    //}

    private bool CheckIfMecchaIsOnPlane()
    {
        if (raycastManager == null || currentMeccha == null) return false;

        // 1. Set origin slightly above the Meccha's base position
        Vector3 rayOrigin = currentMeccha.transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        // Max distance allowed (0.1m offset + 0.05m tolerance threshold)
        float rayLength = 0.15f; // Green only when base is <= 5cm of the AR Detected plane

        // 2. Perform raycast using rayLength to restrict ray distance
        if (raycastManager.Raycast(ray, s_Hits, TrackableType.Planes))
        {
            // Check if the hit point is within range of the rayLength
            if (s_Hits[0].distance <= rayLength)
            {
                float distanceToPlane = Mathf.Abs(currentMeccha.transform.position.y - s_Hits[0].pose.position.y);

                // 3. Return true only when touching the surface within tolerance
                if (distanceToPlane <= 0.05f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Movement Controls

    public void MoveUp()
    {
        if (currentMeccha == null) return;
        currentMeccha.transform.position += Vector3.up * stepDistance;
    }

    public void MoveDown()
    {
        if (currentMeccha == null) return;
        currentMeccha.transform.position += Vector3.down * stepDistance;
    }

    public void MoveLeft()
    {
        if (currentMeccha == null || arCameraTransform == null) return;
        currentMeccha.transform.position -= arCameraTransform.right * stepDistance;
    }

    public void MoveRight()
    {
        if (currentMeccha == null || arCameraTransform == null) return;
        currentMeccha.transform.position += arCameraTransform.right * stepDistance;
    }

    public void MoveFront()
    {
        if (currentMeccha == null || arCameraTransform == null) return;
        Vector3 camForward = arCameraTransform.forward;
        camForward.y = 0;
        currentMeccha.transform.position += camForward.normalized * stepDistance;
    }

    public void MoveBack()
    {
        if (currentMeccha == null || arCameraTransform == null) return;
        Vector3 camForward = arCameraTransform.forward;
        camForward.y = 0;
        currentMeccha.transform.position -= camForward.normalized * stepDistance;
    }

    // Rotation Controls

    public void RotateHorizontal()
    {
        if (currentMeccha == null) return;
        currentMeccha.transform.Rotate(Vector3.up, rotationAngle, Space.World);
    }

    public void RotateVertical()
    {
        if (currentMeccha == null || arCameraTransform == null) return;
        currentMeccha.transform.Rotate(arCameraTransform.right, rotationAngle, Space.World);
    }

    // Re-Anchoring Logic

    public void ReAnchorCurrentMeccha()
    {
        if (currentMeccha == null) return;

        ARAnchor oldAnchor = currentMeccha.GetComponent<ARAnchor>();
        if (oldAnchor != null)
        {
            Destroy(oldAnchor);
        }
        currentMeccha.AddComponent<ARAnchor>();
    }
}