using UnityEngine;
using UnityEngine.UI;

public class HunterCrosshairController : MonoBehaviour
{
    // =============================================================
    // REFERENCES
    // =============================================================

    [Header("References")]
    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private RectTransform crosshair;

    [SerializeField]
    private HunterGunPickupController pickupController;

    // =============================================================
    // AIM SETTINGS
    // =============================================================

    [Header("Aim")]
    [SerializeField]
    private float rayDistance = 50f;

    // =============================================================
    // MOVEMENT
    // =============================================================

    [Header("Crosshair Movement")]
    [SerializeField]
    private float smoothSpeed = 20f;

    [Tooltip(
        "Keeps the crosshair inside the visible screen."
    )]
    [SerializeField]
    private bool clampToScreen = true;

    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]
    [SerializeField]
    private bool logDebug = false;

    // =============================================================
    // INTERNAL REFERENCES
    // =============================================================

    private Canvas crosshairCanvas;

    private RectTransform canvasRect;

    private Transform currentGunMuzzle;

    private HunterGunPickup currentHeldGun;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        // ---------------------------------------------------------
        // CAMERA
        // ---------------------------------------------------------

        if (arCamera == null)
        {
            arCamera =
                Camera.main;
        }

        // ---------------------------------------------------------
        // PICKUP CONTROLLER
        // ---------------------------------------------------------

        if (pickupController == null)
        {
            pickupController =
                FindFirstObjectByType<
                    HunterGunPickupController>();
        }

        // ---------------------------------------------------------
        // CROSSHAIR CANVAS
        // ---------------------------------------------------------

        if (crosshair != null)
        {
            crosshairCanvas =
                crosshair.GetComponentInParent<Canvas>();

            if (crosshairCanvas != null)
            {
                canvasRect =
                    crosshairCanvas
                        .GetComponent<RectTransform>();
            }
        }

        // ---------------------------------------------------------
        // ERROR CHECKS
        // ---------------------------------------------------------

        if (arCamera == null)
        {
            Debug.LogError(
                "[HunterCrosshair] " +
                "AR Camera was not found."
            );
        }

        if (crosshair == null)
        {
            Debug.LogError(
                "[HunterCrosshair] " +
                "Crosshair reference is missing."
            );
        }

        if (pickupController == null)
        {
            Debug.LogError(
                "[HunterCrosshair] " +
                "HunterGunPickupController " +
                "was not found."
            );
        }
    }

    // =============================================================
    // UPDATE
    // =============================================================

    private void Update()
    {
        // ---------------------------------------------------------
        // BASIC CHECK
        // ---------------------------------------------------------

        if (
            arCamera == null ||
            crosshair == null ||
            pickupController == null
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // GET HELD GUN
        // ---------------------------------------------------------

        HunterGunPickup heldGun =
            pickupController.GetHeldGun();

        // ---------------------------------------------------------
        // NO GUN
        // ---------------------------------------------------------

        if (heldGun == null)
        {
            currentHeldGun = null;
            currentGunMuzzle = null;

            return;
        }

        // ---------------------------------------------------------
        // DETECT NEW HELD GUN
        // ---------------------------------------------------------

        if (currentHeldGun != heldGun)
        {
            currentHeldGun =
                heldGun;

            currentGunMuzzle =
                FindGunMuzzleRecursive(
                    heldGun.transform
                );

            if (logDebug)
            {
                if (currentGunMuzzle != null)
                {
                    Debug.Log(
                        "[HunterCrosshair] " +
                        "Found GunMuzzle on " +
                        heldGun.name
                    );
                }
                else
                {
                    Debug.LogError(
                        "[HunterCrosshair] " +
                        "Could NOT find GunMuzzle on " +
                        heldGun.name
                    );
                }
            }
        }

        // ---------------------------------------------------------
        // MAKE SURE MUZZLE EXISTS
        // ---------------------------------------------------------

        if (currentGunMuzzle == null)
        {
            return;
        }

        // ---------------------------------------------------------
        // GET MUZZLE POSITION + DIRECTION
        // ---------------------------------------------------------

        Vector3 muzzlePosition =
            currentGunMuzzle.position;

        Vector3 muzzleDirection =
            currentGunMuzzle.forward;

        // ---------------------------------------------------------
        // SAFETY CHECK
        // ---------------------------------------------------------

        if (
            muzzleDirection.sqrMagnitude
            < 0.0001f
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // AIM RAY
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
            targetPoint =
                hit.point;
        }
        else
        {
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
        // TARGET IS BEHIND CAMERA
        // ---------------------------------------------------------

        if (screenPoint.z <= 0f)
        {
            if (logDebug)
            {
                Debug.Log(
                    "[HunterCrosshair] " +
                    "Aim target is behind camera."
                );
            }

            return;
        }

        // ---------------------------------------------------------
        // CLAMP TO SCREEN
        // ---------------------------------------------------------

        if (clampToScreen)
        {
            float margin = 20f;

            screenPoint.x =
                Mathf.Clamp(
                    screenPoint.x,
                    margin,
                    Screen.width - margin
                );

            screenPoint.y =
                Mathf.Clamp(
                    screenPoint.y,
                    margin,
                    Screen.height - margin
                );
        }

        // ---------------------------------------------------------
        // CONVERT SCREEN POSITION TO CANVAS POSITION
        // ---------------------------------------------------------

        UpdateCrosshairPosition(
            screenPoint
        );
    }

    // =============================================================
    // FIND GUN MUZZLE RECURSIVELY
    // =============================================================

    private Transform FindGunMuzzleRecursive(
        Transform parent
    )
    {
        if (parent == null)
        {
            return null;
        }

        // ---------------------------------------------------------
        // CHECK THIS OBJECT
        // ---------------------------------------------------------

        if (parent.name == "GunMuzzle")
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

    // =============================================================
    // UPDATE CROSSHAIR UI POSITION
    // =============================================================

    private void UpdateCrosshairPosition(
        Vector3 screenPoint
    )
    {
        // ---------------------------------------------------------
        // SCREEN SPACE OVERLAY
        // ---------------------------------------------------------

        Camera canvasCamera = null;

        if (
            crosshairCanvas != null &&
            crosshairCanvas.renderMode
                != RenderMode.ScreenSpaceOverlay
        )
        {
            canvasCamera =
                crosshairCanvas.worldCamera;
        }

        // ---------------------------------------------------------
        // CONVERT SCREEN -> CANVAS LOCAL POSITION
        // ---------------------------------------------------------

        RectTransform parentRect =
            crosshair.parent
                as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        if (
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPoint,
                    canvasCamera,
                    out Vector2 localPoint
                )
        )
        {
            Vector2 target =
                localPoint;

            // -----------------------------------------------------
            // SMOOTH MOVEMENT
            // -----------------------------------------------------

            crosshair.anchoredPosition =
                Vector2.Lerp(
                    crosshair.anchoredPosition,
                    target,
                    Time.deltaTime *
                    smoothSpeed
                );
        }
    }

    // =============================================================
    // RESET CROSSHAIR
    // =============================================================

    public void ResetCrosshairToCentre()
    {
        if (crosshair == null)
        {
            return;
        }

        crosshair.anchoredPosition =
            Vector2.zero;
    }
}