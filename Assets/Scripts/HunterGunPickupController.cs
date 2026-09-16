using UnityEngine;

public class HunterGunPickupController : MonoBehaviour
{
    // =============================================================
    // REFERENCES
    // =============================================================

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
    // RAYCAST
    // =============================================================

    [Header("Raycast")]
    [SerializeField]
    private float rayDistance = 10f;

    // =============================================================
    // PICKUP SETTINGS
    // =============================================================

    [Header("Pickup Settings")]

    [Tooltip(
        "How long the player must continuously point at a gun " +
        "before it is picked up."
    )]
    [SerializeField]
    private float pickupHoldTime = 0.5f;

    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]
    [SerializeField]
    private bool logPickup = true;

    // =============================================================
    // INTERNAL STATE
    // =============================================================

    private HunterGunPickup heldGun;

    private HunterGunPickup hoveredGun;

    private float pickupTimer = 0f;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
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
        // ALREADY HOLDING A GUN
        // ---------------------------------------------------------

        if (heldGun != null)
        {
            return;
        }

        // ---------------------------------------------------------
        // FIND GUN UNDER INDEX FINGER
        // ---------------------------------------------------------

        HunterGunPickup currentTarget =
            FindGunUnderIndexFinger();

        // ---------------------------------------------------------
        // NO GUN TARGETED
        // ---------------------------------------------------------

        if (currentTarget == null)
        {
            hoveredGun = null;

            pickupTimer = 0f;

            return;
        }

        // ---------------------------------------------------------
        // NEW GUN TARGETED
        // ---------------------------------------------------------

        if (hoveredGun != currentTarget)
        {
            hoveredGun =
                currentTarget;

            pickupTimer = 0f;

            if (logPickup)
            {
                Debug.Log(
                    "[Hunter] Pointing at gun: " +
                    hoveredGun.name
                );
            }
        }

        // ---------------------------------------------------------
        // HOLD-TO-PICKUP TIMER
        // ---------------------------------------------------------

        pickupTimer +=
            Time.deltaTime;

        // ---------------------------------------------------------
        // PICKUP AFTER HOLD TIME
        // ---------------------------------------------------------

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

    // =============================================================
    // FIND GUN UNDER INDEX FINGER
    // =============================================================

    private HunterGunPickup
        FindGunUnderIndexFinger()
    {
        // ---------------------------------------------------------
        // LANDMARK 8 = INDEX FINGERTIP
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
        // RAYCAST
        // ---------------------------------------------------------

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
                        HunterGunPickup>();

            return gun;
        }

        return null;
    }

    // =============================================================
    // PICKUP
    // =============================================================

    private void PickupGun(
        HunterGunPickup gun
    )
    {
        if (gun == null)
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

        if (logPickup)
        {
            Debug.Log(
                "[Hunter] Gun picked up: " +
                gun.name
            );
        }
    }

    // =============================================================
    // GET HELD GUN
    // =============================================================

    public HunterGunPickup GetHeldGun()
    {
        return heldGun;
    }

    // =============================================================
    // GET HOVERED GUN
    // =============================================================

    public HunterGunPickup GetHoveredGun()
    {
        return hoveredGun;
    }

    // =============================================================
    // GET PICKUP PROGRESS
    // =============================================================

    public float GetPickupProgress()
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

    // =============================================================
    // DROP GUN
    // =============================================================

    public void DropHeldGun()
    {
        if (heldGun == null)
        {
            return;
        }

        heldGun.Drop();

        if (logPickup)
        {
            Debug.Log(
                "[Hunter] Gun dropped: " +
                heldGun.name
            );
        }

        heldGun = null;
        hoveredGun = null;
        pickupTimer = 0f;
    }
}