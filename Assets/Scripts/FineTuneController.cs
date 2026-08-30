using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class FineTuneController : MonoBehaviour
{
    [Header("UI Panel Reference")]
    public GameObject fineTunePanel;

    [Header("AR Camera Reference")]
    public Transform arCameraTransform;

    [Header("Target & Tuning Settings")]
    public GameObject currentMeccha;

    [Tooltip("Step distance in meters for each arrow click")]
    public float stepDistance = 0.05f;

    [Tooltip("Rotation angle in degrees per button click")]
    public float rotationAngle = 15f;

    private void Start()
    {
        if (fineTunePanel != null)
        {
            fineTunePanel.SetActive(false);
        }

        if (arCameraTransform == null && Camera.main != null)
        {
            arCameraTransform = Camera.main.transform;
        }
    }

    public void SetTargetMeccha(GameObject meccha)
    {
        currentMeccha = meccha;
    }

    public void ToggleFineTunePanel()
    {
        if (fineTunePanel != null)
        {
            bool isActive = !fineTunePanel.activeSelf;
            fineTunePanel.SetActive(isActive);

            // Re-anchor when closing panel via toggle
            if (!isActive)
            {
                ReAnchorCurrentMeccha();
            }
        }
    }

    public void OpenFineTunePanel()
    {
        if (fineTunePanel != null) fineTunePanel.SetActive(true);
    }

    public void CloseFineTunePanel()
    {
        if (fineTunePanel != null) fineTunePanel.SetActive(false);
        ReAnchorCurrentMeccha(); // Locks new position in AR space!
    }

    // --- Movement Methods ---

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

    // --- Rotation Methods ---

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

    // --- Re-Anchoring Logic ---

    public void ReAnchorCurrentMeccha()
    {
        if (currentMeccha == null) return;

        // 1. Destroy existing anchor before attaching a new one
        ARAnchor oldAnchor = currentMeccha.GetComponent<ARAnchor>();
        if (oldAnchor != null)
        {
            Destroy(oldAnchor);
        }

        // 2. Attach dynamic ARAnchor at updated location
        currentMeccha.AddComponent<ARAnchor>();
    }
}