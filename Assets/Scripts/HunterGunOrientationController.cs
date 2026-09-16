using UnityEngine;

public class HunterGunOrientationController : MonoBehaviour
{
    // =============================================================
    // REFERENCES
    // =============================================================

    [Header("References")]

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private ARHandLandmarkVisualizer handVisualizer;

    [SerializeField]
    private HunterGunPickupController pickupController;

    [Header("Gun")]

    [SerializeField]
    private Transform gunHandAnchor;

    // =============================================================
    // AIM SETTINGS
    // =============================================================

    [Header("Aim")]

    [Tooltip(
        "Controls how strongly the index finger direction " +
        "affects the gun's aiming direction."
    )]
    [SerializeField]
    private float aimStrength = 0.75f;

    [Tooltip(
        "Higher values make the gun rotate more smoothly " +
        "while still following the hand."
    )]
    [SerializeField]
    private float smoothing = 20f;

    // =============================================================
    // INTERNAL STATE
    // =============================================================

    private HunterGunPickup currentGun;

    private Transform currentGunMuzzle;

    private Vector2 filteredIndexMCP;

    private Vector2 filteredIndexTip;

    private bool handInitialized = false;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        // ---------------------------------------------------------
        // FIND CAMERA
        // ---------------------------------------------------------

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        // ---------------------------------------------------------
        // FIND HAND VISUALIZER
        // ---------------------------------------------------------

        if (handVisualizer == null)
        {
            handVisualizer =
                FindFirstObjectByType<
                    ARHandLandmarkVisualizer>();
        }

        // ---------------------------------------------------------
        // FIND PICKUP CONTROLLER
        // ---------------------------------------------------------

        if (pickupController == null)
        {
            pickupController =
                FindFirstObjectByType<
                    HunterGunPickupController>();
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
            pickupController == null ||
            gunHandAnchor == null
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // FIND CURRENTLY HELD GUN
        // ---------------------------------------------------------

        HunterGunPickup heldGun =
            pickupController.GetHeldGun();

        // ---------------------------------------------------------
        // NO GUN
        // ---------------------------------------------------------

        if (heldGun == null)
        {
            currentGun = null;
            currentGunMuzzle = null;
            handInitialized = false;

            return;
        }

        // ---------------------------------------------------------
        // NEW GUN
        // ---------------------------------------------------------

        if (currentGun != heldGun)
        {
            currentGun =
                heldGun;

            currentGunMuzzle =
                FindGunMuzzle(
                    heldGun.transform
                );

            handInitialized =
                false;

            if (currentGunMuzzle == null)
            {
                Debug.LogError(
                    "[HunterAim] " +
                    "GunMuzzle could not be found on " +
                    heldGun.name
                );

                return;
            }

            Debug.Log(
                "[HunterAim] GunMuzzle found on " +
                heldGun.name
            );
        }

        // ---------------------------------------------------------
        // SAFETY CHECK
        // ---------------------------------------------------------

        if (currentGunMuzzle == null)
        {
            return;
        }

        // ---------------------------------------------------------
        // GET INDEX FINGER LANDMARKS
        //
        // Landmark 5 = Index MCP
        // Landmark 8 = Index Tip
        // ---------------------------------------------------------

        Vector2 indexMCP =
            handVisualizer
                .GetLandmarkUIPosition(5);

        Vector2 indexTip =
            handVisualizer
                .GetLandmarkUIPosition(8);

        // ---------------------------------------------------------
        // INITIALIZE FILTER
        // ---------------------------------------------------------

        if (!handInitialized)
        {
            filteredIndexMCP =
                indexMCP;

            filteredIndexTip =
                indexTip;

            handInitialized =
                true;
        }

        // ---------------------------------------------------------
        // SMOOTH LANDMARK MOVEMENT
        // ---------------------------------------------------------

        float filter =
            1f -
            Mathf.Exp(
                -30f *
                Time.deltaTime
            );

        filteredIndexMCP =
            Vector2.Lerp(
                filteredIndexMCP,
                indexMCP,
                filter
            );

        filteredIndexTip =
            Vector2.Lerp(
                filteredIndexTip,
                indexTip,
                filter
            );

        // ---------------------------------------------------------
        // CALCULATE INDEX FINGER DIRECTION
        // ---------------------------------------------------------

        Vector2 screenDirection =
            filteredIndexTip -
            filteredIndexMCP;

        if (
            screenDirection.sqrMagnitude <
            0.0001f
        )
        {
            return;
        }

        screenDirection.Normalize();

        // ---------------------------------------------------------
        // CONVERT SCREEN DIRECTION TO WORLD DIRECTION
        //
        // X = Camera Right
        // Y = Camera Up
        // Forward keeps the weapon generally pointing into
        // the AR scene rather than sideways.
        // ---------------------------------------------------------

        Vector3 screenAimDirection =
            arCamera.transform.right *
            screenDirection.x;

        screenAimDirection +=
            arCamera.transform.up *
            screenDirection.y;

        screenAimDirection +=
            arCamera.transform.forward *
            aimStrength;

        screenAimDirection.Normalize();

        // ---------------------------------------------------------
        // ALIGN THE ACTUAL GUN MUZZLE WITH THE AIM DIRECTION
        // ---------------------------------------------------------

        Vector3 currentMuzzleDirection =
            currentGunMuzzle.forward;

        if (
            currentMuzzleDirection.sqrMagnitude <
            0.0001f
        )
        {
            return;
        }

        currentMuzzleDirection.Normalize();

        Quaternion correction =
            Quaternion.FromToRotation(
                currentMuzzleDirection,
                screenAimDirection
            );

        Quaternion targetRotation =
            correction *
            gunHandAnchor.rotation;

        // ---------------------------------------------------------
        // SMOOTH ROTATION
        // ---------------------------------------------------------

        float rotationLerp =
            1f -
            Mathf.Exp(
                -smoothing *
                Time.deltaTime
            );

        gunHandAnchor.rotation =
            Quaternion.Slerp(
                gunHandAnchor.rotation,
                targetRotation,
                rotationLerp
            );
    }

    // =============================================================
    // FIND GUN MUZZLE
    // =============================================================

    private Transform FindGunMuzzle(
        Transform parent
    )
    {
        if (parent == null)
        {
            return null;
        }

        // ---------------------------------------------------------
        // CHECK CURRENT OBJECT
        // ---------------------------------------------------------

        if (
            parent.name ==
            "GunMuzzle"
        )
        {
            return parent;
        }

        // ---------------------------------------------------------
        // SEARCH CHILDREN
        // ---------------------------------------------------------

        for (
            int i = 0;
            i < parent.childCount;
            i++
        )
        {
            Transform result =
                FindGunMuzzle(
                    parent.GetChild(i)
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}