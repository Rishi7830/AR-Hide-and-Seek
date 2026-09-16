using UnityEngine;

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
    // AIM
    // =============================================================

    [Header("Aim")]

    [Tooltip(
        "Controls how far the muzzle direction is projected " +
        "when calculating the crosshair position."
    )]
    [SerializeField]
    private float aimProjectionDistance = 1.0f;

    // =============================================================
    // CROSSHAIR MOVEMENT
    // =============================================================

    [Header("Crosshair Movement")]

    [Tooltip(
        "Higher values make the crosshair follow the gun faster."
    )]
    [SerializeField]
    private float smoothTime = 0.03f;

    [SerializeField]
    private bool clampToScreen = true;

    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]

    [SerializeField]
    private bool logDebug = false;

    // =============================================================
    // INTERNAL STATE
    // =============================================================

    private Canvas crosshairCanvas;

    private HunterGunPickup currentHeldGun;

    private Transform currentGunMuzzle;

    private Vector2 crosshairVelocity;

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
                crosshair
                    .GetComponentInParent<Canvas>();
        }
    }

    // =============================================================
    // UPDATE
    // =============================================================

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
        // NEW GUN
        // ---------------------------------------------------------

        if (currentHeldGun != heldGun)
        {
            currentHeldGun =
                heldGun;

            currentGunMuzzle =
                FindGunMuzzleRecursive(
                    heldGun.transform
                );

            crosshairVelocity =
                Vector2.zero;

            if (logDebug)
            {
                if (currentGunMuzzle != null)
                {
                    Debug.Log(
                        "[HunterCrosshair] " +
                        "GunMuzzle found on " +
                        heldGun.name
                    );
                }
                else
                {
                    Debug.LogError(
                        "[HunterCrosshair] " +
                        "GunMuzzle not found on " +
                        heldGun.name
                    );
                }
            }
        }

        // ---------------------------------------------------------
        // MUZZLE CHECK
        // ---------------------------------------------------------

        if (currentGunMuzzle == null)
        {
            return;
        }

        // ---------------------------------------------------------
        // GET MUZZLE POSITION
        // ---------------------------------------------------------

        Vector3 muzzlePosition =
            currentGunMuzzle.position;

        // ---------------------------------------------------------
        // GET MUZZLE FORWARD
        // ---------------------------------------------------------

        Vector3 muzzleForward =
            currentGunMuzzle.forward.normalized;

        // ---------------------------------------------------------
        // CREATE A SECOND POINT SLIGHTLY IN FRONT
        //
        // This gives us the direction the muzzle is pointing
        // without pushing the point many metres into the scene.
        // ---------------------------------------------------------

        Vector3 forwardPoint =
            muzzlePosition +
            muzzleForward *
            aimProjectionDistance;

        // ---------------------------------------------------------
        // PROJECT BOTH POINTS INTO SCREEN SPACE
        // ---------------------------------------------------------

        Vector3 muzzleScreen =
            arCamera.WorldToScreenPoint(
                muzzlePosition
            );

        Vector3 forwardScreen =
            arCamera.WorldToScreenPoint(
                forwardPoint
            );

        // ---------------------------------------------------------
        // MAKE SURE BOTH POINTS ARE IN FRONT
        // ---------------------------------------------------------

        if (
            muzzleScreen.z <= 0f ||
            forwardScreen.z <= 0f
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // CALCULATE SCREEN-SPACE AIM DIRECTION
        // ---------------------------------------------------------

        Vector2 screenDirection =
            new Vector2(
                forwardScreen.x -
                muzzleScreen.x,

                forwardScreen.y -
                muzzleScreen.y
            );

        // ---------------------------------------------------------
        // IF THE DIRECTION IS TOO SMALL
        // ---------------------------------------------------------

        if (
            screenDirection.sqrMagnitude
            < 0.0001f
        )
        {
            // If the gun is pointing almost directly toward
            // the camera, use the muzzle's screen position.

            UpdateCrosshairPosition(
                new Vector2(
                    muzzleScreen.x,
                    muzzleScreen.y
                )
            );

            return;
        }

        screenDirection.Normalize();

        // ---------------------------------------------------------
        // CROSSHAIR DISTANCE FROM MUZZLE ON SCREEN
        //
        // This is deliberately controlled in SCREEN PIXELS.
        //
        // Therefore moving the gun left/right moves the
        // crosshair by approximately the same screen amount.
        // ---------------------------------------------------------

        float crosshairDistance = 0;
            // 250f;

        Vector2 targetScreenPosition =
            new Vector2(
                muzzleScreen.x,
                muzzleScreen.y
            ) +
            screenDirection *
            crosshairDistance;

        // ---------------------------------------------------------
        // CLAMP
        // ---------------------------------------------------------

        if (clampToScreen)
        {
            float margin =
                20f;

            targetScreenPosition.x =
                Mathf.Clamp(
                    targetScreenPosition.x,
                    margin,
                    Screen.width -
                    margin
                );

            targetScreenPosition.y =
                Mathf.Clamp(
                    targetScreenPosition.y,
                    margin,
                    Screen.height -
                    margin
                );
        }

        // ---------------------------------------------------------
        // UPDATE CROSSHAIR
        // ---------------------------------------------------------

        UpdateCrosshairPosition(
            targetScreenPosition
        );
    }

    // =============================================================
    // UPDATE CROSSHAIR POSITION
    // =============================================================

    private void UpdateCrosshairPosition(
        Vector2 screenPosition
    )
    {
        if (crosshair.parent == null)
        {
            return;
        }

        RectTransform parentRect =
            crosshair.parent
                as RectTransform;

        if (parentRect == null)
        {
            return;
        }

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
        // SCREEN -> CANVAS LOCAL
        // ---------------------------------------------------------

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
            // -----------------------------------------------------
            // SMOOTH CROSSHAIR
            // -----------------------------------------------------

            crosshair.anchoredPosition =
                Vector2.SmoothDamp(
                    crosshair.anchoredPosition,
                    localPoint,
                    ref crosshairVelocity,
                    smoothTime
                );
        }
    }

    // =============================================================
    // FIND GUN MUZZLE
    // =============================================================

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

        crosshairVelocity =
            Vector2.zero;
    }
}