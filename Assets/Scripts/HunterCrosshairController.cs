using UnityEngine;

public class HunterCrosshairController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private RectTransform crosshair;

    [SerializeField]
    private HunterGunPickupController pickupController;

    [Header("Aim")]
    [SerializeField]
    private float rayDistance = 50f;

    [Header("Crosshair Movement")]
    [SerializeField]
    private float smoothSpeed = 15f;

    [Header("Debug")]
    [SerializeField]
    private bool hideCrosshairWithoutGun = false;

    private Transform currentGunMuzzle;

    private void Start()
    {
        // ---------------------------------------------------------
        // FIND AR CAMERA
        // ---------------------------------------------------------

        if (arCamera == null)
        {
            arCamera =
                Camera.main;
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

    private void Update()
    {
        if (
            arCamera == null ||
            crosshair == null ||
            pickupController == null
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
            currentGunMuzzle = null;

            if (hideCrosshairWithoutGun)
            {
                crosshair.gameObject.SetActive(
                    false
                );
            }

            return;
        }

        // Crosshair becomes visible when gun exists.
        crosshair.gameObject.SetActive(
            true
        );

        // ---------------------------------------------------------
        // FIND GUN MUZZLE
        // ---------------------------------------------------------

        currentGunMuzzle =
            heldGun.transform.Find(
                "GunMuzzle"
            );

        if (currentGunMuzzle == null)
        {
            Debug.LogWarning(
                "[HunterCrosshair] " +
                "GunMuzzle not found on " +
                heldGun.name
            );

            return;
        }

        // ---------------------------------------------------------
        // GUN AIM ORIGIN
        // ---------------------------------------------------------

        Vector3 muzzlePosition =
            currentGunMuzzle.position;

        // ---------------------------------------------------------
        // GUN AIM DIRECTION
        // ---------------------------------------------------------

        Vector3 muzzleDirection =
            currentGunMuzzle.forward;

        // ---------------------------------------------------------
        // SHOOT AIM RAY
        // ---------------------------------------------------------

        Vector3 targetPoint;

        if (
            Physics.Raycast(
                muzzlePosition,
                muzzleDirection,
                out RaycastHit hit,
                rayDistance
            )
        )
        {
            // Gun hits something.
            targetPoint =
                hit.point;
        }
        else
        {
            // Gun does not hit anything.
            targetPoint =
                muzzlePosition +
                muzzleDirection *
                rayDistance;
        }

        // ---------------------------------------------------------
        // WORLD POSITION -> SCREEN POSITION
        // ---------------------------------------------------------

        Vector3 screenPoint =
            arCamera.WorldToScreenPoint(
                targetPoint
            );

        // ---------------------------------------------------------
        // CHECK TARGET IN FRONT OF CAMERA
        // ---------------------------------------------------------

        if (screenPoint.z < 0f)
        {
            return;
        }

        // ---------------------------------------------------------
        // MOVE CROSSHAIR
        // ---------------------------------------------------------

        Vector3 targetScreenPosition =
            new Vector3(
                screenPoint.x,
                screenPoint.y,
                crosshair.position.z
            );

        crosshair.position =
            Vector3.Lerp(
                crosshair.position,
                targetScreenPosition,
                Time.deltaTime *
                smoothSpeed
            );
    }
}