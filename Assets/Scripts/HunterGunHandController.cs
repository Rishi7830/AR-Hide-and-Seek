using UnityEngine;

public class HunterGunHandController :
    MonoBehaviour
{
    // ============================================================
    // CAMERA
    // ============================================================

    [Header("Camera")]

    [SerializeField]
    private Camera arCamera;

    [Tooltip(
        "Left eye camera used for stereo hand-to-gun positioning."
    )]
    [SerializeField]
    private Camera stereoLeftEyeCamera;

    // ============================================================
    // HAND VISUALIZATION
    // ============================================================

    [Header("Hand Visualization")]

    [Tooltip(
        "Original hand landmark visualizer. " +
        "Keep this assigned as the normal-mode fallback."
    )]
    [SerializeField]
    private ARHandLandmarkVisualizer
        handVisualizer;

    [Tooltip(
        "Stereo hand landmark visualizer. " +
        "Used automatically when enabled."
    )]
    [SerializeField]
    private StereoARHandLandmarkVisualizer
        stereoHandVisualizer;

    // ============================================================
    // GUN PICKUP
    // ============================================================

    [Header("Gun Pickup")]

    [SerializeField]
    private HunterGunPickupController
        pickupController;

    // ============================================================
    // GUN HAND ANCHOR
    // ============================================================

    [Header("Gun Hand Anchor")]

    [SerializeField]
    private Transform gunHandAnchor;

    // ============================================================
    // POSITION
    // ============================================================

    [Header("Position")]

    [Tooltip(
        "Base distance between the camera and the virtual gun."
    )]
    [SerializeField]
    private float handDepth =
        0.5f;

    [Tooltip(
        "Moves the gun slightly relative to the index fingertip."
    )]
    [SerializeField]
    private Vector3 gripOffset =
        new Vector3(
            0f,
            -0.05f,
            0f
        );

    // ============================================================
    // DEPTH
    // ============================================================

    [Header("Depth")]

    [SerializeField]
    private bool useDepthEstimation =
        true;

    [SerializeField]
    private float depthSensitivity =
        1.2f;

    [SerializeField]
    private float minimumDepth =
        0.30f;

    [SerializeField]
    private float maximumDepth =
        1.50f;

    // ============================================================
    // DEPTH-BASED GUN SCALE
    // ============================================================

    [Header("Depth-Based Gun Scale")]

    [Tooltip(
        "Scale of the gun at the handDepth reference distance."
    )]
    [SerializeField]
    private float baseGunScale =
        1.0f;

    [Tooltip(
        "Smallest gun scale allowed when the hand is far away."
    )]
    [SerializeField]
    private float minimumGunScale =
        0.65f;

    [Tooltip(
        "Largest gun scale allowed when the hand is close."
    )]
    [SerializeField]
    private float maximumGunScale =
        1.50f;

    [Tooltip(
        "Higher values make the gun size change more strongly " +
        "with hand depth."
    )]
    [SerializeField]
    private float scaleSensitivity =
        1.0f;

    [Tooltip(
        "Speed at which the gun scale follows the calculated " +
        "depth-based target scale."
    )]
    [SerializeField]
    private float scaleSmoothSpeed =
        12f;

    // ============================================================
    // SMOOTHING
    // ============================================================

    [Header("Smoothing")]

    [Tooltip(
        "Higher values make the gun follow the finger more tightly."
    )]
    [SerializeField]
    private float positionSmoothSpeed =
        35f;

    // ============================================================
    // MUZZLE ALIGNMENT
    // ============================================================

    [Header("Muzzle Alignment")]

    [Tooltip(
        "If the gun points backwards, enable this once and " +
        "the controller will align the muzzle with the active camera."
    )]
    [SerializeField]
    private bool automaticallyAlignMuzzle =
        true;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private Vector2
        smoothedIndexTip;

    private Vector2
        smoothedWrist;

    private Vector2
        smoothedIndexMCP;

    private Vector2
        smoothedMiddleMCP;

    private bool
        landmarksInitialized =
        false;

    private float
        referencePalmSize =
        0f;

    private bool
        depthInitialized =
        false;

    private HunterGunPickup
        currentGun;

    private Transform
        currentGunMuzzle;

    private bool
        currentGunAligned =
        false;

    private Vector3
        originalGunScale;

    private bool
        originalGunScaleStored =
        false;

    private float
        currentGunScaleMultiplier =
        1f;

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        // --------------------------------------------------------
        // MAIN AR CAMERA
        // --------------------------------------------------------

        if (
            arCamera == null
        )
        {
            arCamera =
                Camera.main;
        }

        // --------------------------------------------------------
        // ORIGINAL HAND VISUALIZER
        // --------------------------------------------------------

        if (
            handVisualizer == null
        )
        {
            handVisualizer =
                FindFirstObjectByType<
                    ARHandLandmarkVisualizer
                >();
        }

        // --------------------------------------------------------
        // STEREO HAND VISUALIZER
        // --------------------------------------------------------

        if (
            stereoHandVisualizer == null
        )
        {
            stereoHandVisualizer =
                FindFirstObjectByType<
                    StereoARHandLandmarkVisualizer
                >();
        }

        // --------------------------------------------------------
        // LEFT EYE CAMERA
        // --------------------------------------------------------

        if (
            stereoLeftEyeCamera == null
        )
        {
            GameObject leftEyeObject =
                GameObject.Find(
                    "LeftEyeCamera"
                );

            if (
                leftEyeObject != null
            )
            {
                stereoLeftEyeCamera =
                    leftEyeObject
                        .GetComponent<Camera>();
            }
        }

        // --------------------------------------------------------
        // PICKUP CONTROLLER
        // --------------------------------------------------------

        if (
            pickupController == null
        )
        {
            pickupController =
                FindFirstObjectByType<
                    HunterGunPickupController
                >();
        }

        Debug.Log(
            "[HunterGunHand] " +
            "Initialized. " +
            "OriginalHand=" +
            (
                handVisualizer != null
            ) +
            " | StereoHand=" +
            (
                stereoHandVisualizer != null
            ) +
            " | StereoLeftEye=" +
            (
                stereoLeftEyeCamera != null
            )
        );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (
            pickupController == null ||
            gunHandAnchor == null
        )
        {
            return;
        }

        // --------------------------------------------------------
        // DETERMINE ACTIVE MODE
        // --------------------------------------------------------

        bool stereoActive =
            stereoHandVisualizer != null &&
            stereoHandVisualizer
                .isActiveAndEnabled;

        // --------------------------------------------------------
        // NORMAL MODE REQUIRES ORIGINAL VISUALIZER
        // --------------------------------------------------------

        if (
            !stereoActive &&
            (
                arCamera == null ||
                handVisualizer == null
            )
        )
        {
            return;
        }

        // --------------------------------------------------------
        // STEREO MODE REQUIRES LEFT EYE CAMERA
        // --------------------------------------------------------

        if (
            stereoActive &&
            (
                stereoLeftEyeCamera == null ||
                stereoHandVisualizer == null
            )
        )
        {
            return;
        }

        // --------------------------------------------------------
        // GET CURRENTLY HELD GUN
        // --------------------------------------------------------

        HunterGunPickup heldGun =
            pickupController.GetHeldGun();

        // --------------------------------------------------------
        // NO GUN
        // --------------------------------------------------------

        if (
            heldGun == null
        )
        {
            currentGun =
                null;

            currentGunMuzzle =
                null;

            currentGunAligned =
                false;

            return;
        }

        // --------------------------------------------------------
        // NEW GUN
        // --------------------------------------------------------

        if (
            currentGun != heldGun
        )
        {
            currentGun =
                heldGun;

            currentGunMuzzle =
                FindGunMuzzleRecursive(
                    heldGun.transform
                );

            currentGunAligned =
                false;

            landmarksInitialized =
                false;

            depthInitialized =
                false;

            referencePalmSize =
                0f;

            // ----------------------------------------------------
            // STORE ORIGINAL SCALE
            // ----------------------------------------------------

            originalGunScale =
                heldGun.transform.localScale;

            originalGunScaleStored =
                true;

            currentGunScaleMultiplier =
                1f;

            Debug.Log(
                "[HunterGunHand] " +
                "New gun detected: " +
                heldGun.name +
                " | Stereo=" +
                stereoActive
            );
        }

        // --------------------------------------------------------
        // MUZZLE CHECK
        // --------------------------------------------------------

        if (
            currentGunMuzzle == null
        )
        {
            return;
        }

        // ========================================================
        // GET HAND LANDMARKS
        // ========================================================

        Vector2 indexTip;
        Vector2 wrist;
        Vector2 indexMCP;
        Vector2 middleMCP;

        if (
            stereoActive
        )
        {
            // ----------------------------------------------------
            // STEREO
            // ----------------------------------------------------

            indexTip =
                stereoHandVisualizer
                    .GetLandmarkUIPosition(
                        8
                    );

            wrist =
                stereoHandVisualizer
                    .GetLandmarkUIPosition(
                        0
                    );

            indexMCP =
                stereoHandVisualizer
                    .GetLandmarkUIPosition(
                        5
                    );

            middleMCP =
                stereoHandVisualizer
                    .GetLandmarkUIPosition(
                        9
                    );
        }
        else
        {
            // ----------------------------------------------------
            // NORMAL
            // ----------------------------------------------------

            indexTip =
                handVisualizer
                    .GetLandmarkUIPosition(
                        8
                    );

            wrist =
                handVisualizer
                    .GetLandmarkUIPosition(
                        0
                    );

            indexMCP =
                handVisualizer
                    .GetLandmarkUIPosition(
                        5
                    );

            middleMCP =
                handVisualizer
                    .GetLandmarkUIPosition(
                        9
                    );
        }

        // ========================================================
        // SMOOTH LANDMARKS
        // ========================================================

        SmoothLandmarks(
            indexTip,
            wrist,
            indexMCP,
            middleMCP
        );

        // ========================================================
        // CALCULATE DEPTH
        // ========================================================

        float depth =
            CalculateDepth();

        // ========================================================
        // UPDATE DEPTH-BASED SCALE
        // ========================================================

        UpdateGunScale(
            depth
        );

        // ========================================================
        // CALCULATE GUN POSITION
        // ========================================================

        Vector3 targetPosition;

        if (
            stereoActive
        )
        {
            targetPosition =
                CalculateStereoGunPosition(
                    depth
                );
        }
        else
        {
            targetPosition =
                CalculateNormalGunPosition(
                    depth
                );
        }

        // ========================================================
        // SMOOTH GUN POSITION
        // ========================================================

        float positionLerp =
            1f -
            Mathf.Exp(
                -positionSmoothSpeed *
                Time.deltaTime
            );

        gunHandAnchor.position =
            Vector3.Lerp(
                gunHandAnchor.position,
                targetPosition,
                positionLerp
            );

        // ========================================================
        // KEEP GUN FACING ACTIVE CAMERA
        // ========================================================

        Camera activeCamera =
            stereoActive
                ? stereoLeftEyeCamera
                : arCamera;

        if (
            activeCamera != null
        )
        {
            gunHandAnchor.rotation =
                activeCamera.transform.rotation;
        }

        // ========================================================
        // ALIGN MUZZLE ONCE
        // ========================================================

        if (
            automaticallyAlignMuzzle &&
            currentGunMuzzle != null &&
            !currentGunAligned
        )
        {
            AlignGunMuzzle(
                activeCamera
            );

            currentGunAligned =
                true;
        }
    }

    // ============================================================
    // UPDATE GUN SCALE
    // ============================================================

    private void UpdateGunScale(
        float depth
    )
    {
        if (
            currentGun == null ||
            !originalGunScaleStored
        )
        {
            return;
        }

        // --------------------------------------------------------
        // DEPTH RELATIVE TO REFERENCE DEPTH
        //
        // At handDepth:
        //
        // depthRatio = 1
        // scale = baseGunScale
        // --------------------------------------------------------

        float safeReferenceDepth =
            Mathf.Max(
                0.01f,
                handDepth
            );

        float depthRatio =
            safeReferenceDepth /
            Mathf.Max(
                0.01f,
                depth
            );

        // --------------------------------------------------------
        // APPLY SCALE SENSITIVITY
        // --------------------------------------------------------

        float targetScaleMultiplier =
            Mathf.Pow(
                depthRatio,
                scaleSensitivity
            );

        // --------------------------------------------------------
        // LIMIT SCALE
        // --------------------------------------------------------

        targetScaleMultiplier =
            Mathf.Clamp(
                targetScaleMultiplier *
                baseGunScale,
                minimumGunScale,
                maximumGunScale
            );

        // --------------------------------------------------------
        // SMOOTH SCALE
        // --------------------------------------------------------

        float scaleLerp =
            1f -
            Mathf.Exp(
                -scaleSmoothSpeed *
                Time.deltaTime
            );

        currentGunScaleMultiplier =
            Mathf.Lerp(
                currentGunScaleMultiplier,
                targetScaleMultiplier,
                scaleLerp
            );

        // --------------------------------------------------------
        // APPLY TO ACTUAL GUN
        // --------------------------------------------------------

        currentGun.transform.localScale =
            originalGunScale *
            currentGunScaleMultiplier;
    }

    // ============================================================
    // NORMAL GUN POSITION
    // ============================================================

    private Vector3
        CalculateNormalGunPosition(
            float depth
        )
    {
        Vector2 screenPosition =
            new Vector2(
                smoothedIndexTip.x +
                Screen.width *
                0.5f,

                smoothedIndexTip.y +
                Screen.height *
                0.5f
            );

        screenPosition.x =
            Mathf.Clamp(
                screenPosition.x,
                0f,
                Screen.width
            );

        screenPosition.y =
            Mathf.Clamp(
                screenPosition.y,
                0f,
                Screen.height
            );

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        Vector3 targetPosition =
            ray.origin +
            ray.direction *
            depth;

        targetPosition =
            ApplyGripOffset(
                targetPosition,
                arCamera
            );

        return targetPosition;
    }

    // ============================================================
    // STEREO GUN POSITION
    // ============================================================

    private Vector3
        CalculateStereoGunPosition(
            float depth
        )
    {
        float eyeWidth =
            stereoLeftEyeCamera.pixelWidth;

        float eyeHeight =
            stereoLeftEyeCamera.pixelHeight;

        if (
            eyeWidth <= 0f ||
            eyeHeight <= 0f
        )
        {
            return gunHandAnchor.position;
        }

        Vector2 eyeScreenPosition =
            new Vector2(
                smoothedIndexTip.x +
                eyeWidth *
                0.5f,

                smoothedIndexTip.y +
                eyeHeight *
                0.5f
            );

        eyeScreenPosition.x =
            Mathf.Clamp(
                eyeScreenPosition.x,
                0f,
                eyeWidth
            );

        eyeScreenPosition.y =
            Mathf.Clamp(
                eyeScreenPosition.y,
                0f,
                eyeHeight
            );

        Ray ray =
            stereoLeftEyeCamera
                .ScreenPointToRay(
                    eyeScreenPosition
                );

        Vector3 targetPosition =
            ray.origin +
            ray.direction *
            depth;

        targetPosition =
            ApplyGripOffset(
                targetPosition,
                stereoLeftEyeCamera
            );

        return targetPosition;
    }

    // ============================================================
    // APPLY GRIP OFFSET
    // ============================================================

    private Vector3
        ApplyGripOffset(
            Vector3 position,
            Camera activeCamera
        )
    {
        if (
            activeCamera == null
        )
        {
            return position;
        }

        position +=
            activeCamera.transform.right *
            gripOffset.x;

        position +=
            activeCamera.transform.up *
            gripOffset.y;

        position +=
            activeCamera.transform.forward *
            gripOffset.z;

        return position;
    }

    // ============================================================
    // SMOOTH LANDMARKS
    // ============================================================

    private void SmoothLandmarks(
        Vector2 indexTip,
        Vector2 wrist,
        Vector2 indexMCP,
        Vector2 middleMCP
    )
    {
        if (
            !landmarksInitialized
        )
        {
            smoothedIndexTip =
                indexTip;

            smoothedWrist =
                wrist;

            smoothedIndexMCP =
                indexMCP;

            smoothedMiddleMCP =
                middleMCP;

            landmarksInitialized =
                true;

            return;
        }

        float smoothing =
            1f -
            Mathf.Exp(
                -35f *
                Time.deltaTime
            );

        smoothedIndexTip =
            Vector2.Lerp(
                smoothedIndexTip,
                indexTip,
                smoothing
            );

        smoothedWrist =
            Vector2.Lerp(
                smoothedWrist,
                wrist,
                smoothing
            );

        smoothedIndexMCP =
            Vector2.Lerp(
                smoothedIndexMCP,
                indexMCP,
                smoothing
            );

        smoothedMiddleMCP =
            Vector2.Lerp(
                smoothedMiddleMCP,
                middleMCP,
                smoothing
            );
    }

    // ============================================================
    // DEPTH
    // ============================================================

    private float
        CalculateDepth()
    {
        if (
            !useDepthEstimation
        )
        {
            return handDepth;
        }

        float wristToIndex =
            Vector2.Distance(
                smoothedWrist,
                smoothedIndexMCP
            );

        float wristToMiddle =
            Vector2.Distance(
                smoothedWrist,
                smoothedMiddleMCP
            );

        float palmSize =
            (
                wristToIndex +
                wristToMiddle
            ) *
            0.5f;

        if (
            palmSize <
            0.0001f
        )
        {
            return handDepth;
        }

        // --------------------------------------------------------
        // FIRST VALID HAND FRAME
        // --------------------------------------------------------

        if (
            !depthInitialized
        )
        {
            referencePalmSize =
                palmSize;

            depthInitialized =
                true;

            return handDepth;
        }

        // --------------------------------------------------------
        // RELATIVE DEPTH
        // --------------------------------------------------------

        float depthRatio =
            referencePalmSize /
            palmSize;

        float calculatedDepth =
            handDepth *
            Mathf.Pow(
                depthRatio,
                depthSensitivity
            );

        // --------------------------------------------------------
        // LIMIT DEPTH
        // --------------------------------------------------------

        return Mathf.Clamp(
            calculatedDepth,
            minimumDepth,
            maximumDepth
        );
    }

    // ============================================================
    // ALIGN GUN MUZZLE
    // ============================================================

    private void AlignGunMuzzle(
        Camera activeCamera
    )
    {
        if (
            currentGun == null ||
            currentGunMuzzle == null ||
            gunHandAnchor == null ||
            activeCamera == null
        )
        {
            return;
        }

        Vector3 localMuzzleForward =
            currentGun.transform
                .InverseTransformDirection(
                    currentGunMuzzle.forward
                );

        if (
            localMuzzleForward.sqrMagnitude <
            0.0001f
        )
        {
            return;
        }

        localMuzzleForward.Normalize();

        Quaternion muzzleCorrection =
            Quaternion.FromToRotation(
                localMuzzleForward,
                Vector3.forward
            );

        // --------------------------------------------------------
        // APPLY ACTIVE CAMERA ROTATION
        // --------------------------------------------------------

        gunHandAnchor.rotation =
            activeCamera.transform.rotation *
            muzzleCorrection;

        Debug.Log(
            "[HunterGunHand] " +
            "Gun muzzle aligned with active camera."
        );
    }

    // ============================================================
    // FIND GUN MUZZLE
    // ============================================================

    private Transform
        FindGunMuzzleRecursive(
            Transform parent
        )
    {
        if (
            parent == null
        )
        {
            return null;
        }

        if (
            parent.name ==
            "GunMuzzle"
        )
        {
            return parent;
        }

        for (
            int i = 0;
            i < parent.childCount;
            i++
        )
        {
            Transform result =
                FindGunMuzzleRecursive(
                    parent.GetChild(i)
                );

            if (
                result != null
            )
            {
                return result;
            }
        }

        return null;
    }

    // ============================================================
    // RESET DEPTH
    // ============================================================

    public void ResetDepthCalibration()
    {
        referencePalmSize =
            0f;

        depthInitialized =
            false;

        Debug.Log(
            "[HunterGunHand] " +
            "Depth calibration reset."
        );
    }
}