using UnityEngine;

public class HunterCrosshairController :
    MonoBehaviour
{
    // ============================================================
    // CAMERAS
    // ============================================================

    [Header("Cameras")]

    [SerializeField]
    private Camera arCamera;

    [Tooltip(
        "Left eye camera used for the stereo crosshair."
    )]
    [SerializeField]
    private Camera stereoLeftEyeCamera;

    [Tooltip(
        "Right eye camera used for the stereo crosshair."
    )]
    [SerializeField]
    private Camera stereoRightEyeCamera;

    // ============================================================
    // CROSSHAIRS
    // ============================================================

    [Header("Crosshairs")]

    [Tooltip(
        "Original single-screen crosshair. " +
        "Use this for normal non-stereo mode."
    )]
    [SerializeField]
    private RectTransform originalCrosshair;

    [Tooltip(
        "Left-eye crosshair used in stereo mode."
    )]
    [SerializeField]
    private RectTransform leftCrosshair;

    [Tooltip(
        "Right-eye crosshair used in stereo mode."
    )]
    [SerializeField]
    private RectTransform rightCrosshair;

    // ============================================================
    // GUN PICKUP
    // ============================================================

    [Header("Gun Pickup")]

    [SerializeField]
    private HunterGunPickupController
        pickupController;

    // ============================================================
    // AIM
    // ============================================================

    [Header("Aim")]

    [Tooltip(
        "Controls how far the muzzle direction is projected " +
        "when calculating the aim direction."
    )]
    [SerializeField]
    private float aimProjectionDistance =
        1.0f;

    // ============================================================
    // CROSSHAIR MOVEMENT
    // ============================================================

    [Header("Crosshair Movement")]

    [Tooltip(
        "Higher values make the crosshair follow the gun faster."
    )]
    [SerializeField]
    private float smoothTime =
        0.03f;

    [SerializeField]
    private bool clampToScreen =
        true;

    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool logDebug =
        false;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private Canvas
        originalCrosshairCanvas;

    private Canvas
        leftCrosshairCanvas;

    private Canvas
        rightCrosshairCanvas;

    private HunterGunPickup
        currentHeldGun;

    private Transform
        currentGunMuzzle;

    private Vector2
        originalCrosshairVelocity;

    private Vector2
        leftCrosshairVelocity;

    private Vector2
        rightCrosshairVelocity;

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        // --------------------------------------------------------
        // MAIN CAMERA
        // --------------------------------------------------------

        if (
            arCamera == null
        )
        {
            arCamera =
                Camera.main;
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

        // --------------------------------------------------------
        // FIND LEFT EYE CAMERA
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
        // FIND RIGHT EYE CAMERA
        // --------------------------------------------------------

        if (
            stereoRightEyeCamera == null
        )
        {
            GameObject rightEyeObject =
                GameObject.Find(
                    "RightEyeCamera"
                );

            if (
                rightEyeObject != null
            )
            {
                stereoRightEyeCamera =
                    rightEyeObject
                        .GetComponent<Camera>();
            }
        }

        // --------------------------------------------------------
        // CACHE CANVASES
        // --------------------------------------------------------

        CacheCrosshairCanvases();

        // --------------------------------------------------------
        // INITIAL CENTER
        // --------------------------------------------------------

        CentreStereoCrosshairs();

        if (
            logDebug
        )
        {
            Debug.Log(
                "[HunterCrosshair] " +
                "Initialized. " +
                "LeftEye=" +
                (
                    stereoLeftEyeCamera != null
                ) +
                " | RightEye=" +
                (
                    stereoRightEyeCamera != null
                )
            );
        }
    }

    // ============================================================
    // CACHE CANVASES
    // ============================================================

    private void CacheCrosshairCanvases()
    {
        if (
            originalCrosshair != null
        )
        {
            originalCrosshairCanvas =
                originalCrosshair
                    .GetComponentInParent<
                        Canvas
                    >();
        }

        if (
            leftCrosshair != null
        )
        {
            leftCrosshairCanvas =
                leftCrosshair
                    .GetComponentInParent<
                        Canvas
                    >();
        }

        if (
            rightCrosshair != null
        )
        {
            rightCrosshairCanvas =
                rightCrosshair
                    .GetComponentInParent<
                        Canvas
                    >();
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (
            pickupController == null
        )
        {
            return;
        }

        // --------------------------------------------------------
        // DETERMINE STEREO MODE
        // --------------------------------------------------------

        bool stereoActive =
            stereoLeftEyeCamera != null &&
            stereoRightEyeCamera != null &&
            stereoLeftEyeCamera.isActiveAndEnabled &&
            stereoRightEyeCamera.isActiveAndEnabled;

        // --------------------------------------------------------
        // GET HELD GUN
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
            currentHeldGun =
                null;

            currentGunMuzzle =
                null;

            // ----------------------------------------------------
            // Keep stereo crosshairs centered in their own halves.
            // ----------------------------------------------------

            if (
                stereoActive
            )
            {
                CentreStereoCrosshairs();
            }

            return;
        }

        // --------------------------------------------------------
        // NEW GUN
        // --------------------------------------------------------

        if (
            currentHeldGun != heldGun
        )
        {
            currentHeldGun =
                heldGun;

            currentGunMuzzle =
                FindGunMuzzleRecursive(
                    heldGun.transform
                );

            ResetCrosshairVelocities();

            if (
                logDebug
            )
            {
                Debug.Log(
                    "[HunterCrosshair] " +
                    "GunMuzzle found on " +
                    heldGun.name
                );
            }
        }

        // --------------------------------------------------------
        // MUZZLE CHECK
        // --------------------------------------------------------

        if (
            currentGunMuzzle == null
        )
        {
            if (
                stereoActive
            )
            {
                CentreStereoCrosshairs();
            }

            return;
        }

        // --------------------------------------------------------
        // STEREO
        // --------------------------------------------------------

        if (
            stereoActive
        )
        {
            UpdateStereoCrosshairs();

            return;
        }

        // --------------------------------------------------------
        // NORMAL
        // --------------------------------------------------------

        UpdateNormalCrosshair();
    }

    // ============================================================
    // NORMAL CROSSHAIR
    // ============================================================

    private void UpdateNormalCrosshair()
    {
        if (
            arCamera == null ||
            originalCrosshair == null
        )
        {
            return;
        }

        Vector3 muzzlePosition =
            currentGunMuzzle.position;

        Vector3 muzzleForward =
            currentGunMuzzle.forward
                .normalized;

        Vector3 forwardPoint =
            muzzlePosition +
            muzzleForward *
            aimProjectionDistance;

        Vector3 muzzleScreen =
            arCamera.WorldToScreenPoint(
                muzzlePosition
            );

        Vector3 forwardScreen =
            arCamera.WorldToScreenPoint(
                forwardPoint
            );

        if (
            muzzleScreen.z <= 0f ||
            forwardScreen.z <= 0f
        )
        {
            return;
        }

        Vector2 targetScreenPosition =
            CalculateTargetScreenPosition(
                muzzleScreen,
                forwardScreen
            );

        if (
            clampToScreen
        )
        {
            targetScreenPosition =
                ClampToCameraScreen(
                    targetScreenPosition,
                    arCamera
                );
        }

        UpdateCrosshairFromScreenPosition(
            originalCrosshair,
            originalCrosshairCanvas,
            targetScreenPosition,
            ref originalCrosshairVelocity
        );
    }

    // ============================================================
    // STEREO CROSSHAIRS
    // ============================================================

    private void UpdateStereoCrosshairs()
    {
        // --------------------------------------------------------
        // LEFT EYE
        // --------------------------------------------------------

        if (
            stereoLeftEyeCamera != null &&
            leftCrosshair != null
        )
        {
            UpdateCrosshairForEye(
                stereoLeftEyeCamera,
                leftCrosshair,
                leftCrosshairCanvas,
                ref leftCrosshairVelocity
            );
        }

        // --------------------------------------------------------
        // RIGHT EYE
        // --------------------------------------------------------

        if (
            stereoRightEyeCamera != null &&
            rightCrosshair != null
        )
        {
            UpdateCrosshairForEye(
                stereoRightEyeCamera,
                rightCrosshair,
                rightCrosshairCanvas,
                ref rightCrosshairVelocity
            );
        }
    }

    // ============================================================
    // UPDATE CROSSHAIR FOR ONE EYE
    // ============================================================

    private void UpdateCrosshairForEye(
        Camera eyeCamera,
        RectTransform crosshair,
        Canvas crosshairCanvas,
        ref Vector2 velocity
    )
    {
        if (
            eyeCamera == null ||
            crosshair == null ||
            crosshair.parent == null
        )
        {
            return;
        }

        // --------------------------------------------------------
        // PROJECT MUZZLE THROUGH THIS EYE
        // --------------------------------------------------------

        Vector3 muzzlePosition =
            currentGunMuzzle.position;

        Vector3 muzzleForward =
            currentGunMuzzle.forward
                .normalized;

        Vector3 forwardPoint =
            muzzlePosition +
            muzzleForward *
            aimProjectionDistance;

        Vector3 muzzleViewport =
            eyeCamera.WorldToViewportPoint(
                muzzlePosition
            );

        Vector3 forwardViewport =
            eyeCamera.WorldToViewportPoint(
                forwardPoint
            );

        if (
            muzzleViewport.z <= 0f ||
            forwardViewport.z <= 0f
        )
        {
            return;
        }

        // --------------------------------------------------------
        // CLAMP VIEWPORT
        // --------------------------------------------------------

        if (
            clampToScreen
        )
        {
            muzzleViewport.x =
                Mathf.Clamp01(
                    muzzleViewport.x
                );

            muzzleViewport.y =
                Mathf.Clamp01(
                    muzzleViewport.y
                );
        }

        // --------------------------------------------------------
        // CONVERT 0...1 VIEWPORT TO THE EYE CANVAS
        // --------------------------------------------------------

        RectTransform parentRect =
            crosshair.parent
                as RectTransform;

        if (
            parentRect == null
        )
        {
            return;
        }

        Rect parentRectArea =
            parentRect.rect;

        float localX =
            (
                muzzleViewport.x -
                0.5f
            ) *
            parentRectArea.width;

        float localY =
            (
                muzzleViewport.y -
                0.5f
            ) *
            parentRectArea.height;

        Vector2 targetLocalPosition =
            new Vector2(
                localX,
                localY
            );

        // --------------------------------------------------------
        // MOVE CROSSHAIR
        // --------------------------------------------------------

        crosshair.anchoredPosition =
            Vector2.SmoothDamp(
                crosshair.anchoredPosition,
                targetLocalPosition,
                ref velocity,
                smoothTime
            );

        if (
            logDebug
        )
        {
            Debug.Log(
                "[HunterCrosshair] " +
                eyeCamera.name +
                " | MuzzleViewport=" +
                muzzleViewport
            );
        }
    }

    // ============================================================
    // CALCULATE NORMAL AIM POSITION
    // ============================================================

    private Vector2
        CalculateTargetScreenPosition(
            Vector3 muzzleScreen,
            Vector3 forwardScreen
        )
    {
        Vector2 screenDirection =
            new Vector2(
                forwardScreen.x -
                muzzleScreen.x,

                forwardScreen.y -
                muzzleScreen.y
            );

        if (
            screenDirection.sqrMagnitude <
            0.0001f
        )
        {
            return new Vector2(
                muzzleScreen.x,
                muzzleScreen.y
            );
        }

        screenDirection.Normalize();

        // --------------------------------------------------------
        // PRESERVE ORIGINAL BEHAVIOR:
        // crosshair sits directly on muzzle projection.
        // --------------------------------------------------------

        float crosshairDistance =
            0f;

        return new Vector2(
            muzzleScreen.x,
            muzzleScreen.y
        ) +
        screenDirection *
        crosshairDistance;
    }

    // ============================================================
    // CLAMP NORMAL SCREEN POSITION
    // ============================================================

    private Vector2
        ClampToCameraScreen(
            Vector2 position,
            Camera camera
        )
    {
        if (
            camera == null
        )
        {
            return position;
        }

        float width =
            camera.pixelWidth;

        float height =
            camera.pixelHeight;

        float margin =
            20f;

        return new Vector2(
            Mathf.Clamp(
                position.x,
                margin,
                width -
                margin
            ),

            Mathf.Clamp(
                position.y,
                margin,
                height -
                margin
            )
        );
    }

    // ============================================================
    // UPDATE NORMAL UI CROSSHAIR
    // ============================================================

    private void UpdateCrosshairFromScreenPosition(
        RectTransform targetCrosshair,
        Canvas targetCanvas,
        Vector2 screenPosition,
        ref Vector2 velocity
    )
    {
        if (
            targetCrosshair == null ||
            targetCrosshair.parent == null
        )
        {
            return;
        }

        RectTransform parentRect =
            targetCrosshair.parent
                as RectTransform;

        if (
            parentRect == null
        )
        {
            return;
        }

        Camera canvasCamera =
            GetCanvasCamera(
                targetCanvas
            );

        if (
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPosition,
                    canvasCamera,
                    out Vector2 localPoint
                )
        )
        {
            targetCrosshair.anchoredPosition =
                Vector2.SmoothDamp(
                    targetCrosshair.anchoredPosition,
                    localPoint,
                    ref velocity,
                    smoothTime
                );
        }
    }

    // ============================================================
    // CENTER STEREO CROSSHAIRS
    // ============================================================

    private void CentreStereoCrosshairs()
    {
        // --------------------------------------------------------
        // LEFT CROSSHAIR
        // --------------------------------------------------------

        if (
            leftCrosshair != null
        )
        {
            leftCrosshair.anchoredPosition =
                Vector2.zero;
        }

        // --------------------------------------------------------
        // RIGHT CROSSHAIR
        // --------------------------------------------------------

        if (
            rightCrosshair != null
        )
        {
            rightCrosshair.anchoredPosition =
                Vector2.zero;
        }

        // --------------------------------------------------------
        // STOP SMOOTHING VELOCITY
        // --------------------------------------------------------

        leftCrosshairVelocity =
            Vector2.zero;

        rightCrosshairVelocity =
            Vector2.zero;
    }

    // ============================================================
    // GET CANVAS CAMERA
    // ============================================================

    private Camera GetCanvasCamera(
        Canvas canvas
    )
    {
        if (
            canvas == null
        )
        {
            return null;
        }

        if (
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
        )
        {
            return null;
        }

        return canvas.worldCamera;
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
    // RESET
    // ============================================================

    public void ResetCrosshairToCentre()
    {
        // --------------------------------------------------------
        // NORMAL
        // --------------------------------------------------------

        if (
            originalCrosshair != null
        )
        {
            originalCrosshair
                .anchoredPosition =
                Vector2.zero;
        }

        // --------------------------------------------------------
        // STEREO LEFT
        // --------------------------------------------------------

        if (
            leftCrosshair != null
        )
        {
            leftCrosshair
                .anchoredPosition =
                Vector2.zero;
        }

        // --------------------------------------------------------
        // STEREO RIGHT
        // --------------------------------------------------------

        if (
            rightCrosshair != null
        )
        {
            rightCrosshair
                .anchoredPosition =
                Vector2.zero;
        }

        ResetCrosshairVelocities();
    }

    // ============================================================
    // RESET VELOCITIES
    // ============================================================

    private void ResetCrosshairVelocities()
    {
        originalCrosshairVelocity =
            Vector2.zero;

        leftCrosshairVelocity =
            Vector2.zero;

        rightCrosshairVelocity =
            Vector2.zero;
    }
}