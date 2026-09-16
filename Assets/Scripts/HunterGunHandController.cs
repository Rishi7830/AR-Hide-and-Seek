using UnityEngine;

public class HunterGunHandController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Camera arCamera;

    [Header("Hand")]
    [SerializeField]
    private ARHandLandmarkVisualizer handVisualizer;

    [Header("Gun Pickup")]
    [SerializeField]
    private HunterGunPickupController pickupController;

    [Header("Gun Hand Anchor")]
    [SerializeField]
    private Transform gunHandAnchor;

    [Header("Position")]

    [Tooltip(
        "Base distance between the camera and the virtual gun."
    )]
    [SerializeField]
    private float handDepth = 0.5f;

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

    [Header("Depth")]

    [SerializeField]
    private bool useDepthEstimation = true;

    [SerializeField]
    private float depthSensitivity = 1.2f;

    [SerializeField]
    private float minimumDepth = 0.30f;

    [SerializeField]
    private float maximumDepth = 1.50f;

    [Header("Smoothing")]

    [Tooltip(
        "Higher values make the gun follow the finger more tightly."
    )]
    [SerializeField]
    private float positionSmoothSpeed = 35f;

    [Header("Muzzle Alignment")]

    [Tooltip(
        "If the gun points backwards, enable this once and " +
        "the controller will align the muzzle with the camera."
    )]
    [SerializeField]
    private bool automaticallyAlignMuzzle = true;

    // INTERNAL STATE

    private Vector2 smoothedIndexTip;
    private Vector2 smoothedWrist;
    private Vector2 smoothedIndexMCP;
    private Vector2 smoothedMiddleMCP;
    private bool landmarksInitialized = false;
    private float referencePalmSize = 0f;
    private bool depthInitialized = false;
    private HunterGunPickup currentGun;
    private Transform currentGunMuzzle;
    private bool currentGunAligned = false;

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

        if (pickupController == null)
        {
            pickupController =
                FindFirstObjectByType<
                    HunterGunPickupController>();
        }
    }

    private void Update()
    {
        if (
            arCamera == null ||
            handVisualizer == null ||
            pickupController == null ||
            gunHandAnchor == null
        )
        {
            return;
        }

        // GET CURRENTLY HELD GUN

        HunterGunPickup heldGun =
            pickupController.GetHeldGun();

        // NO GUN

        if (heldGun == null)
        {
            currentGun = null;
            currentGunMuzzle = null;
            currentGunAligned = false;

            return;
        }

        if (currentGun != heldGun)
        {
            currentGun =
                heldGun;

            currentGunMuzzle =
                FindGunMuzzleRecursive(
                    heldGun.transform
                );

            currentGunAligned = false;

            landmarksInitialized = false;
            depthInitialized = false;

            referencePalmSize = 0f;
        }

        // GET INDEX TIP

        Vector2 indexTip =
            handVisualizer
                .GetLandmarkUIPosition(8);

        // GET PALM LANDMARKS

        Vector2 wrist =
            handVisualizer
                .GetLandmarkUIPosition(0);

        Vector2 indexMCP =
            handVisualizer
                .GetLandmarkUIPosition(5);

        Vector2 middleMCP =
            handVisualizer
                .GetLandmarkUIPosition(9);

        SmoothLandmarks(
            indexTip,
            wrist,
            indexMCP,
            middleMCP
        );

        // CALCULATE DEPTH

        float depth =
            CalculateDepth();

        // INDEX FINGER SCREEN POSITION

        Vector2 screenPosition =
            new Vector2(
                smoothedIndexTip.x +
                Screen.width * 0.5f,

                smoothedIndexTip.y +
                Screen.height * 0.5f
            );

        // CONVERT SCREEN POSITION TO CAMERA RAY

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        Vector3 targetPosition =
            ray.origin +
            ray.direction *
            depth;

        // APPLY GRIP OFFSET

        targetPosition +=
            arCamera.transform.right *
            gripOffset.x;

        targetPosition +=
            arCamera.transform.up *
            gripOffset.y;

        targetPosition +=
            arCamera.transform.forward *
            gripOffset.z;

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

        // KEEP GUN FACING CAMERA

        gunHandAnchor.rotation =
            arCamera.transform.rotation;

        if (
            automaticallyAlignMuzzle &&
            currentGunMuzzle != null &&
            !currentGunAligned
        )
        {
            AlignGunMuzzle();

            currentGunAligned =
                true;
        }
    }

    // SMOOTH LANDMARKS

    private void SmoothLandmarks(
        Vector2 indexTip,
        Vector2 wrist,
        Vector2 indexMCP,
        Vector2 middleMCP
    )
    {
        if (!landmarksInitialized)
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

    // DEPTH

    private float CalculateDepth()
    {
        if (!useDepthEstimation)
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

        // FIRST VALID HAND FRAME

        if (!depthInitialized)
        {
            referencePalmSize =
                palmSize;

            depthInitialized =
                true;

            return handDepth;
        }

        // RELATIVE DEPTH

        float depthRatio =
            referencePalmSize /
            palmSize;

        float calculatedDepth =
            handDepth *
            Mathf.Pow(
                depthRatio,
                depthSensitivity
            );

        // LIMIT DEPTH

        return Mathf.Clamp(
            calculatedDepth,
            minimumDepth,
            maximumDepth
        );
    }

    // ALIGN GUN MUZZLE

    private void AlignGunMuzzle()
    {
        if (
            currentGun == null ||
            currentGunMuzzle == null ||
            gunHandAnchor == null ||
            arCamera == null
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

        // APPLY CAMERA ROTATION + CORRECTION

        gunHandAnchor.rotation =
            arCamera.transform.rotation *
            muzzleCorrection;

        Debug.Log(
            "[HunterGunHand] " +
            "Gun muzzle aligned with rear camera."
        );
    }

    // FIND GUN MUZZLE

    private Transform FindGunMuzzleRecursive(
        Transform parent
    )
    {
        if (parent == null)
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

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    // RESET

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