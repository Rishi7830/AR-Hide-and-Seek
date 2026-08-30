using UnityEngine;
using TMPro; // Use UnityEngine.UI if using standard UI Text

public class MecchaCoordinateDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Text component to display the coordinates (TextMeshPro recommended)")]
    public TextMeshProUGUI coordinateText;

    [Header("AR Camera Reference")]
    public Transform arCameraTransform;

    [Header("Target Meccha Reference")]
    public GameObject currentMeccha;

    private Vector3 initialCameraPosition;
    private bool isOriginSet = false;

    private void Start()
    {
        if (arCameraTransform == null && Camera.main != null)
        {
            arCameraTransform = Camera.main.transform;
        }

        // Capture starting camera position as (0, 0, 0) relative origin
        if (arCameraTransform != null)
        {
            initialCameraPosition = arCameraTransform.position;
            isOriginSet = true;
        }
    }

    private void Update()
    {
        // Continuously update text if target Meccha exists
        if (currentMeccha != null && coordinateText != null)
        {
            Vector3 relativePos = GetRelativeCoordinates(currentMeccha.transform.position);
            coordinateText.text = $"Meccha Pos: X: {relativePos.x:F2}m | Y: {relativePos.y:F2}m | Z: {relativePos.z:F2}m";
        }
    }

    /// <summary>
    /// Updates active Meccha reference when spawned or selected
    /// </summary>
    public void SetTargetMeccha(GameObject meccha)
    {
        currentMeccha = meccha;
        if (currentMeccha == null && coordinateText != null)
        {
            coordinateText.text = "Meccha Pos: N/A";
        }
    }

    /// <summary>
    /// Calculates offset relative to starting camera position
    /// </summary>
    public Vector3 GetRelativeCoordinates(Vector3 worldPosition)
    {
        if (!isOriginSet && arCameraTransform != null)
        {
            initialCameraPosition = arCameraTransform.position;
            isOriginSet = true;
        }

        return worldPosition - initialCameraPosition;
    }
}