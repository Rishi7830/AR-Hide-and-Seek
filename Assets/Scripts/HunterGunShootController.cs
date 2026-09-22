using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HunterGunShootController : MonoBehaviour
{
    // =============================================================
    // REFERENCES
    // =============================================================

    [Header("References")]

    [SerializeField]
    private HunterGunPickupController pickupController;

    [SerializeField]
    private Button shootButton;

    [SerializeField]
    private TMP_Text ammoRemain;

    [SerializeField]
    private GameObject ammoFinishedPanel;

    // =============================================================
    // AMMO
    // =============================================================

    [Header("Ammo")]

    [SerializeField]
    private int maximumAmmo = 6;

    private int currentAmmo;

    // =============================================================
    // SHOOT SETTINGS
    // =============================================================

    [Header("Shoot Settings")]

    [SerializeField]
    private float fireCooldown = 0.15f;

    // =============================================================
    // MUZZLE FLASH
    // =============================================================

    [Header("Muzzle Flash")]

    [SerializeField]
    private bool forceMuzzleFlashPosition = true;

    [SerializeField]
    private Vector3 muzzleFlashOffset =
        Vector3.zero;

    // =============================================================
    // HIT DETECTION
    // =============================================================

    [Header("Hit Detection")]

    [Tooltip(
        "Maximum distance the gun can shoot."
    )]
    [SerializeField]
    private float shootingRange = 50f;

    [Tooltip(
        "Layers that can be hit by the shooting ray."
    )]
    [SerializeField]
    private LayerMask shootingLayers =
        ~0;

    [Tooltip(
        "Show the shooting ray in the Scene view."
    )]
    [SerializeField]
    private bool showDebugRay = true;

    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]

    [SerializeField]
    private bool logDebug = true;

    // =============================================================
    // INTERNAL STATE
    // =============================================================

    private float nextAllowedShotTime =
        0f;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        // ---------------------------------------------------------
        // FIND PICKUP CONTROLLER
        // ---------------------------------------------------------

        if (pickupController == null)
        {
            pickupController =
                FindFirstObjectByType<
                    HunterGunPickupController>();
        }

        // ---------------------------------------------------------
        // CONNECT SHOOT BUTTON
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.onClick.AddListener(
                Shoot
            );
        }
        else
        {
            Debug.LogWarning(
                "[HunterShoot] " +
                "Shoot Button is not assigned."
            );
        }

        // ---------------------------------------------------------
        // INITIALISE AMMO
        // ---------------------------------------------------------

        currentAmmo =
            maximumAmmo;

        // ---------------------------------------------------------
        // HIDE AMMO POPUP
        // ---------------------------------------------------------

        if (ammoFinishedPanel != null)
        {
            ammoFinishedPanel.SetActive(
                false
            );
        }

        // ---------------------------------------------------------
        // UPDATE UI
        // ---------------------------------------------------------

        UpdateAmmoUI();
    }

    // =============================================================
    // DISABLE
    // =============================================================

    private void OnDisable()
    {
        if (shootButton != null)
        {
            shootButton.onClick.RemoveListener(
                Shoot
            );
        }
    }

    // =============================================================
    // SHOOT
    // =============================================================

    public void Shoot()
    {
        // ---------------------------------------------------------
        // CHECK AMMO
        // ---------------------------------------------------------

        if (currentAmmo <= 0)
        {
            ShowAmmoFinishedPopup();

            if (logDebug)
            {
                Debug.Log(
                    "[HunterShoot] " +
                    "Ammo finished."
                );
            }

            return;
        }

        // ---------------------------------------------------------
        // FIRE COOLDOWN
        // ---------------------------------------------------------

        if (
            Time.time <
            nextAllowedShotTime
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // CHECK PICKUP CONTROLLER
        // ---------------------------------------------------------

        if (pickupController == null)
        {
            return;
        }

        // ---------------------------------------------------------
        // GET HELD GUN
        // ---------------------------------------------------------

        HunterGunPickup heldGun =
            pickupController.GetHeldGun();

        if (heldGun == null)
        {
            if (logDebug)
            {
                Debug.Log(
                    "[HunterShoot] " +
                    "No gun is currently held."
                );
            }

            return;
        }

        // ---------------------------------------------------------
        // SET COOLDOWN
        // ---------------------------------------------------------

        nextAllowedShotTime =
            Time.time +
            fireCooldown;

        // ---------------------------------------------------------
        // CONSUME AMMO
        // ---------------------------------------------------------

        currentAmmo--;

        UpdateAmmoUI();

        // ---------------------------------------------------------
        // FIND GUN MUZZLE
        // ---------------------------------------------------------

        Transform muzzle =
            FindChildRecursive(
                heldGun.transform,
                "GunMuzzle"
            );

        if (muzzle == null)
        {
            Debug.LogError(
                "[HunterShoot] " +
                "GunMuzzle not found on " +
                heldGun.name
            );

            return;
        }

        // ---------------------------------------------------------
        // MUZZLE FLASH
        // ---------------------------------------------------------

        PlayMuzzleFlash(
            muzzle,
            heldGun
        );

        // ---------------------------------------------------------
        // SHOOT MECCHA
        // ---------------------------------------------------------

        PerformShotRaycast(
            muzzle,
            heldGun
        );

        // ---------------------------------------------------------
        // DEBUG
        // ---------------------------------------------------------

        if (logDebug)
        {
            Debug.Log(
                "[HunterShoot] Fired " +
                heldGun.name +
                " | Ammo = " +
                currentAmmo +
                "/" +
                maximumAmmo
            );
        }

        // ---------------------------------------------------------
        // AMMO FINISHED
        // ---------------------------------------------------------

        if (currentAmmo <= 0)
        {
            ShowAmmoFinishedPopup();
        }
    }

    // =============================================================
    // PLAY MUZZLE FLASH
    // =============================================================

    private void PlayMuzzleFlash(
        Transform muzzle,
        HunterGunPickup heldGun
    )
    {
        ParticleSystem muzzleFlash =
            FindParticleSystemRecursive(
                muzzle,
                "MuzzleFlash"
            );

        if (muzzleFlash == null)
        {
            Debug.LogError(
                "[HunterShoot] " +
                "MuzzleFlash not found on " +
                heldGun.name
            );

            return;
        }

        // ---------------------------------------------------------
        // FORCE FLASH POSITION
        // ---------------------------------------------------------

        if (forceMuzzleFlashPosition)
        {
            Transform flashTransform =
                muzzleFlash.transform;

            flashTransform.position =
                muzzle.position +
                muzzle.TransformDirection(
                    muzzleFlashOffset
                );

            flashTransform.rotation =
                muzzle.rotation;
        }

        // ---------------------------------------------------------
        // PLAY
        // ---------------------------------------------------------

        muzzleFlash.Stop(
            true,
            ParticleSystemStopBehavior
                .StopEmittingAndClear
        );

        muzzleFlash.Play();
    }

    // =============================================================
    // PERFORM SHOOT RAYCAST
    // =============================================================

    private void PerformShotRaycast(
        Transform muzzle,
        HunterGunPickup heldGun
    )
    {
        Vector3 origin =
            muzzle.position;

        Vector3 direction =
            muzzle.forward.normalized;

        // ---------------------------------------------------------
        // DEBUG RAY
        // ---------------------------------------------------------

        if (showDebugRay)
        {
            Debug.DrawRay(
                origin,
                direction * shootingRange,
                Color.red,
                2f
            );
        }

        // ---------------------------------------------------------
        // PHYSICS RAYCAST
        // ---------------------------------------------------------

        if (
            Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                shootingRange,
                shootingLayers,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            // -----------------------------------------------------
            // LOOK FOR MECCHA TARGET
            // -----------------------------------------------------

            HunterMecchaTarget target =
                hit.collider
                    .GetComponentInParent<
                        HunterMecchaTarget>();

            if (target != null)
            {
                target.OnShot();

                if (logDebug)
                {
                    Debug.Log(
                        "[HunterShoot] " +
                        "SHOT MECCHA: " +
                        target.name
                    );
                }

                return;
            }

            // -----------------------------------------------------
            // SOMETHING ELSE WAS HIT
            // -----------------------------------------------------

            if (logDebug)
            {
                Debug.Log(
                    "[HunterShoot] " +
                    "Shot hit: " +
                    hit.collider.name
                );
            }
        }
        else
        {
            if (logDebug)
            {
                Debug.Log(
                    "[HunterShoot] " +
                    "Shot missed."
                );
            }
        }
    }

    // =============================================================
    // FIND TRANSFORM RECURSIVELY
    // =============================================================

    private Transform FindChildRecursive(
        Transform parent,
        string targetName
    )
    {
        if (parent == null)
        {
            return null;
        }

        if (
            parent.name ==
            targetName
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
                FindChildRecursive(
                    parent.GetChild(i),
                    targetName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    // =============================================================
    // FIND PARTICLE SYSTEM RECURSIVELY
    // =============================================================

    private ParticleSystem
        FindParticleSystemRecursive(
            Transform parent,
            string targetName
        )
    {
        if (parent == null)
        {
            return null;
        }

        if (
            parent.name ==
            targetName
        )
        {
            ParticleSystem particle =
                parent.GetComponent<
                    ParticleSystem>();

            if (particle != null)
            {
                return particle;
            }
        }

        for (
            int i = 0;
            i < parent.childCount;
            i++
        )
        {
            ParticleSystem result =
                FindParticleSystemRecursive(
                    parent.GetChild(i),
                    targetName
                );

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    // =============================================================
    // UPDATE AMMO UI
    // =============================================================

    private void UpdateAmmoUI()
    {
        if (ammoRemain == null)
        {
            return;
        }

        ammoRemain.text =
            currentAmmo +
            "/" +
            maximumAmmo;
    }

    // =============================================================
    // SHOW AMMO POPUP
    // =============================================================

    private void ShowAmmoFinishedPopup()
    {
        if (ammoFinishedPanel != null)
        {
            ammoFinishedPanel.SetActive(
                true
            );
        }
    }

    // =============================================================
    // GET CURRENT AMMO
    // =============================================================

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    // =============================================================
    // GET MAXIMUM AMMO
    // =============================================================

    public int GetMaximumAmmo()
    {
        return maximumAmmo;
    }

    // =============================================================
    // RESET AMMO
    // =============================================================

    public void ResetAmmo()
    {
        currentAmmo =
            maximumAmmo;

        nextAllowedShotTime =
            0f;

        if (ammoFinishedPanel != null)
        {
            ammoFinishedPanel.SetActive(
                false
            );
        }

        UpdateAmmoUI();

        Debug.Log(
            "[HunterShoot] Ammo reset to " +
            maximumAmmo +
            "/" +
            maximumAmmo
        );
    }
}