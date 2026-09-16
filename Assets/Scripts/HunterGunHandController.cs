using UnityEngine;

public class HunterGunHandController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Camera arCamera;

    [Header("Hand")]
    [SerializeField]
    private ARHandLandmarkVisualizer handVisualizer;

    [Header("Gun Hand Anchor")]
    [SerializeField]
    private Transform gunHandAnchor;

    // =============================================================
    // POSITION
    // =============================================================

    [Header("Position")]

    [Tooltip(
        "Base distance of the virtual gun from the AR camera."
    )]
    [SerializeField]
    private float baseHandDepth = 0.5f;

    [Tooltip(
        "Controls how strongly hand size changes gun depth."
    )]
    [SerializeField]
    private float depthSensitivity = 1.5f;

    [Tooltip(
        "Minimum allowed distance from the camera."
    )]
    [SerializeField]
    private float minimumDepth = 0.25f;

    [Tooltip(
        "Maximum allowed distance from the camera."
    )]
    [SerializeField]
    private float maximumDepth = 2.0f;

    // =============================================================
    // SMOOTHING
    // =============================================================

    [Header("Smoothing")]

    [SerializeField]
    private float positionSmoothSpeed = 15f;

    // =============================================================
    // HAND SIZE REFERENCE
    // =============================================================

    [Header("Depth Calibration")]

    [Tooltip(
        "Reference palm size. Leave at 0 to auto-calibrate."
    )]
    [SerializeField]
    private float referencePalmSize = 0f;

    [SerializeField]
    private bool autoCalibrateDepth = true;

    private bool depthCalibrated = false;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        if (arCamera == null)
        {
            arCamera =
                Camera.main;
        }

        if (handVisualizer == null)
        {
            handVisualizer =
                FindFirstObjectByType<
                    ARHandLandmarkVisualizer>();
        }
    }

    // =============================================================
    // UPDATE
    // =============================================================

    private void Update()
    {
        if (
            arCamera == null ||
            handVisualizer == null ||
            gunHandAnchor == null
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // GET INDEX FINGERTIP
        // Landmark 8 = index fingertip.
        // ---------------------------------------------------------

        Vector2 fingertip =
            handVisualizer
                .GetLandmarkUIPosition(8);

        // ---------------------------------------------------------
        // CONVERT UI POSITION TO SCREEN POSITION
        // ---------------------------------------------------------

        Vector2 screenPosition =
            new Vector2(
                fingertip.x +
                Screen.width * 0.5f,

                fingertip.y +
                Screen.height * 0.5f
            );

        // ---------------------------------------------------------
        // CREATE CAMERA RAY
        // ---------------------------------------------------------

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        // ---------------------------------------------------------
        // ESTIMATE HAND DEPTH
        // ---------------------------------------------------------

        float palmSize =
            GetPalmSize();

        if (palmSize <= 0f)
        {
            return;
        }

        // ---------------------------------------------------------
        // AUTO CALIBRATE
        // ---------------------------------------------------------

        if (
            autoCalibrateDepth &&
            !depthCalibrated
        )
        {
            referencePalmSize =
                palmSize;

            depthCalibrated =
                true;

            Debug.Log(
                "[HunterHand] Depth calibrated. " +
                "Reference palm size = " +
                referencePalmSize
            );
        }

        if (referencePalmSize <= 0f)
        {
            referencePalmSize =
                palmSize;
        }

        // ---------------------------------------------------------
        // CALCULATE DEPTH RATIO
        // ---------------------------------------------------------

        float depthRatio =
            referencePalmSize /
            palmSize;

        // ---------------------------------------------------------
        // CONVERT RATIO TO WORLD DEPTH
        // ---------------------------------------------------------

        float targetDepth =
            baseHandDepth *
            Mathf.Pow(
                depthRatio,
                depthSensitivity
            );

        // ---------------------------------------------------------
        // CLAMP DEPTH
        // ---------------------------------------------------------

        targetDepth =
            Mathf.Clamp(
                targetDepth,
                minimumDepth,
                maximumDepth
            );

        // ---------------------------------------------------------
        // CALCULATE TARGET WORLD POSITION
        // ---------------------------------------------------------

        Vector3 targetPosition =
            ray.origin +
            ray.direction *
            targetDepth;

        // ---------------------------------------------------------
        // SMOOTH POSITION
        // ---------------------------------------------------------

        gunHandAnchor.position =
            Vector3.Lerp(
                gunHandAnchor.position,
                targetPosition,
                Time.deltaTime *
                positionSmoothSpeed
            );
    }

    // =============================================================
    // PALM SIZE
    // =============================================================

    private float GetPalmSize()
    {
        // ---------------------------------------------------------
        // Wrist = landmark 0
        // Index MCP = landmark 5
        // Middle MCP = landmark 9
        // ---------------------------------------------------------

        Vector2 wrist =
            handVisualizer
                .GetLandmarkUIPosition(0);

        Vector2 indexMCP =
            handVisualizer
                .GetLandmarkUIPosition(5);

        Vector2 middleMCP =
            handVisualizer
                .GetLandmarkUIPosition(9);

        // ---------------------------------------------------------
        // Calculate two palm dimensions.
        // ---------------------------------------------------------

        float wristToIndex =
            Vector2.Distance(
                wrist,
                indexMCP
            );

        float wristToMiddle =
            Vector2.Distance(
                wrist,
                middleMCP
            );

        // Average them for a more stable estimate.
        float palmSize =
            (
                wristToIndex +
                wristToMiddle
            ) *
            0.5f;

        return palmSize;
    }

    // =============================================================
    // RESET DEPTH CALIBRATION
    // =============================================================

    public void ResetDepthCalibration()
    {
        depthCalibrated =
            false;

        referencePalmSize =
            0f;

        Debug.Log(
            "[HunterHand] Depth calibration reset."
        );
    }
}