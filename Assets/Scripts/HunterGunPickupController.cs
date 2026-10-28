using UnityEngine;

public class HunterGunPickupController :
    MonoBehaviour
{
    // ============================================================
    // CAMERA
    // ============================================================

    [Header("Camera")]

    [SerializeField]
    private Camera arCamera;

    [Tooltip(
        "Left eye camera used for stereo gun pickup."
    )]
    [SerializeField]
    private Camera stereoLeftEyeCamera;

    // ============================================================
    // HAND VISUALIZATION
    // ============================================================

    [Header("Hand Visualization")]

    [Tooltip(
        "Original single-screen hand landmark visualizer. " +
        "Keep this assigned for normal mode."
    )]
    [SerializeField]
    private ARHandLandmarkVisualizer
        handVisualizer;

    [Tooltip(
        "Stereo hand landmark visualizer. " +
        "Used automatically when active."
    )]
    [SerializeField]
    private StereoARHandLandmarkVisualizer
        stereoHandVisualizer;

    // ============================================================
    // GUN HAND ANCHOR
    // ============================================================

    [Header("Gun Hand Anchor")]

    [SerializeField]
    private Transform gunHandAnchor;

    // ============================================================
    // RAYCAST
    // ============================================================

    [Header("Raycast")]

    [SerializeField]
    private float rayDistance =
        10f;

    // ============================================================
    // PICKUP SETTINGS
    // ============================================================

    [Header("Pickup Settings")]

    [Tooltip(
        "How long the player must continuously point at a gun " +
        "before it is picked up."
    )]
    [SerializeField]
    private float pickupHoldTime =
        0.5f;

    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private bool logPickup =
        true;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private HunterGunPickup
        heldGun;

    private HunterGunPickup
        hoveredGun;

    private float
        pickupTimer =
        0f;

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
        // STEREO LEFT EYE CAMERA
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

        Debug.Log(
            "[HunterPickup] " +
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
            gunHandAnchor == null
        )
        {
            return;
        }

        // --------------------------------------------------------
        // ALREADY HOLDING A GUN
        // --------------------------------------------------------

        if (
            heldGun != null
        )
        {
            return;
        }

        // --------------------------------------------------------
        // FIND GUN UNDER INDEX FINGER
        // --------------------------------------------------------

        HunterGunPickup currentTarget =
            FindGunUnderIndexFinger();

        // --------------------------------------------------------
        // NO GUN TARGETED
        // --------------------------------------------------------

        if (
            currentTarget == null
        )
        {
            hoveredGun =
                null;

            pickupTimer =
                0f;

            return;
        }

        // --------------------------------------------------------
        // NEW GUN TARGETED
        // --------------------------------------------------------

        if (
            hoveredGun != currentTarget
        )
        {
            hoveredGun =
                currentTarget;

            pickupTimer =
                0f;

            if (
                logPickup
            )
            {
                Debug.Log(
                    "[Hunter] " +
                    "Pointing at gun: " +
                    hoveredGun.name
                );
            }
        }

        // --------------------------------------------------------
        // HOLD-TO-PICKUP TIMER
        // --------------------------------------------------------

        pickupTimer +=
            Time.deltaTime;

        // --------------------------------------------------------
        // PICKUP AFTER HOLD TIME
        // --------------------------------------------------------

        if (
            pickupTimer >=
            pickupHoldTime
        )
        {
            PickupGun(
                hoveredGun
            );
        }
    }

    // ============================================================
    // FIND GUN UNDER INDEX FINGER
    // ============================================================

    private HunterGunPickup
        FindGunUnderIndexFinger()
    {
        bool stereoActive =
            stereoHandVisualizer != null &&
            stereoHandVisualizer
                .isActiveAndEnabled;

        // ========================================================
        // STEREO MODE
        // ========================================================

        if (
            stereoActive
        )
        {
            return FindGunUnderIndexFingerStereo();
        }

        // ========================================================
        // NORMAL MODE
        // ========================================================

        return FindGunUnderIndexFingerNormal();
    }

    // ============================================================
    // NORMAL PICKUP
    // ============================================================

    private HunterGunPickup
        FindGunUnderIndexFingerNormal()
    {
        if (
            arCamera == null ||
            handVisualizer == null
        )
        {
            return null;
        }

        // --------------------------------------------------------
        // LANDMARK 8 = INDEX FINGERTIP
        // --------------------------------------------------------

        Vector2 fingertip =
            handVisualizer
                .GetLandmarkUIPosition(
                    8
                );

        // --------------------------------------------------------
        // ORIGINAL FULL-SCREEN COORDINATE SYSTEM
        // --------------------------------------------------------

        Vector2 screenPosition =
            new Vector2(
                fingertip.x +
                Screen.width *
                0.5f,

                fingertip.y +
                Screen.height *
                0.5f
            );

        screenPosition =
            ClampScreenPosition(
                screenPosition,
                Screen.width,
                Screen.height
            );

        // --------------------------------------------------------
        // MAIN CAMERA RAY
        // --------------------------------------------------------

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        return RaycastForGun(
            ray
        );
    }

    // ============================================================
    // STEREO PICKUP
    // ============================================================

    private HunterGunPickup
        FindGunUnderIndexFingerStereo()
    {
        if (
            stereoHandVisualizer == null ||
            stereoLeftEyeCamera == null
        )
        {
            return null;
        }

        // --------------------------------------------------------
        // LANDMARK 8 = INDEX FINGERTIP
        // --------------------------------------------------------

        Vector2 leftEyeLandmark =
            stereoHandVisualizer
                .GetLandmarkUIPosition(
                    8
                );

        // --------------------------------------------------------
        // LEFT CANVAS / LEFT EYE
        //
        // Your SeekerLeftCanvas is:
        //
        // Screen Space - Camera
        // Render Camera = LeftEyeCamera
        // Reference Resolution = 1920 x 1080
        //
        // Therefore the StereoHand landmark is converted directly
        // from its centered UI coordinate into the left eye's
        // 1920 x 1080 screen coordinates.
        // --------------------------------------------------------

        float eyeWidth =
            stereoLeftEyeCamera.pixelWidth;

        float eyeHeight =
            stereoLeftEyeCamera.pixelHeight;

        if (
            eyeWidth <= 0f ||
            eyeHeight <= 0f
        )
        {
            return null;
        }

        Vector2 eyeScreenPosition =
            new Vector2(
                leftEyeLandmark.x +
                eyeWidth *
                0.5f,

                leftEyeLandmark.y +
                eyeHeight *
                0.5f
            );

        eyeScreenPosition =
            ClampScreenPosition(
                eyeScreenPosition,
                eyeWidth,
                eyeHeight
            );

        // --------------------------------------------------------
        // LEFT EYE CAMERA RAY
        // --------------------------------------------------------

        Ray ray =
            stereoLeftEyeCamera
                .ScreenPointToRay(
                    eyeScreenPosition
                );

        return RaycastForGun(
            ray
        );
    }

    // ============================================================
    // RAYCAST FOR GUN
    // ============================================================

    private HunterGunPickup
        RaycastForGun(
            Ray ray
        )
    {
        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance
            )
        )
        {
            HunterGunPickup gun =
                hit.collider
                    .GetComponentInParent<
                        HunterGunPickup
                    >();

            if (
                gun != null
            )
            {
                return gun;
            }
        }

        return null;
    }

    // ============================================================
    // CLAMP SCREEN POSITION
    // ============================================================

    private Vector2
        ClampScreenPosition(
            Vector2 position,
            float width,
            float height
        )
    {
        return new Vector2(
            Mathf.Clamp(
                position.x,
                0f,
                width
            ),

            Mathf.Clamp(
                position.y,
                0f,
                height
            )
        );
    }

    // ============================================================
    // PICKUP
    // ============================================================

    private void PickupGun(
        HunterGunPickup gun
    )
    {
        if (
            gun == null
        )
        {
            return;
        }

        heldGun =
            gun;

        pickupTimer =
            0f;

        gun.Pickup(
            gunHandAnchor
        );

        if (
            logPickup
        )
        {
            Debug.Log(
                "[Hunter] " +
                "Gun picked up: " +
                gun.name
            );
        }
    }

    // ============================================================
    // GET HELD GUN
    // ============================================================

    public HunterGunPickup
        GetHeldGun()
    {
        return heldGun;
    }

    // ============================================================
    // GET HOVERED GUN
    // ============================================================

    public HunterGunPickup
        GetHoveredGun()
    {
        return hoveredGun;
    }

    // ============================================================
    // GET PICKUP PROGRESS
    // ============================================================

    public float
        GetPickupProgress()
    {
        if (
            hoveredGun == null ||
            pickupHoldTime <= 0f
        )
        {
            return 0f;
        }

        return Mathf.Clamp01(
            pickupTimer /
            pickupHoldTime
        );
    }

    // ============================================================
    // DROP GUN
    // ============================================================

    public void DropHeldGun()
    {
        if (
            heldGun == null
        )
        {
            return;
        }

        heldGun.Drop();

        if (
            logPickup
        )
        {
            Debug.Log(
                "[Hunter] " +
                "Gun dropped: " +
                heldGun.name
            );
        }

        heldGun =
            null;

        hoveredGun =
            null;

        pickupTimer =
            0f;
    }
}