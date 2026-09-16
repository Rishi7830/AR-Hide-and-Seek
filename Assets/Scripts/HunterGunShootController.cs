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

    [Tooltip(
        "Minimum time between shots."
    )]
    [SerializeField]
    private float fireCooldown = 0.15f;

    // =============================================================
    // DEBUG
    // =============================================================

    [Header("Debug")]

    [SerializeField]
    private bool logDebug = true;

    // =============================================================
    // INTERNAL STATE
    // =============================================================

    private float nextAllowedShotTime = 0f;

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
        // HIDE AMMO FINISHED POPUP
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
    // ON DISABLE
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
        // NO AMMO
        // ---------------------------------------------------------

        if (currentAmmo <= 0)
        {
            ShowAmmoFinishedPopup();

            if (logDebug)
            {
                Debug.Log(
                    "[HunterShoot] " +
                    "Cannot shoot. Ammo finished."
                );
            }

            return;
        }

        // ---------------------------------------------------------
        // CHECK FIRE COOLDOWN
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

        // ---------------------------------------------------------
        // NO GUN
        // ---------------------------------------------------------

        if (heldGun == null)
        {
            if (logDebug)
            {
                Debug.Log(
                    "[HunterShoot] " +
                    "Shoot pressed, but no gun is held."
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
        // FIND MUZZLE FLASH
        // ---------------------------------------------------------

        ParticleSystem muzzleFlash =
            FindParticleSystemRecursive(
                muzzle,
                "MuzzleFlash"
            );

        if (muzzleFlash == null)
        {
            Debug.LogError(
                "[HunterShoot] " +
                "MuzzleFlash not found under " +
                heldGun.name
            );

            return;
        }

        // ---------------------------------------------------------
        // PLAY MUZZLE FLASH
        // ---------------------------------------------------------

        muzzleFlash.Stop(
            true,
            ParticleSystemStopBehavior
                .StopEmittingAndClear
        );

        muzzleFlash.Play();

        // ---------------------------------------------------------
        // DEBUG
        // ---------------------------------------------------------

        if (logDebug)
        {
            Debug.Log(
                "[HunterShoot] Fired " +
                heldGun.name +
                " | Ammo remaining: " +
                currentAmmo +
                "/" +
                maximumAmmo
            );
        }

        // ---------------------------------------------------------
        // SHOW POPUP WHEN LAST SHOT IS FIRED
        // ---------------------------------------------------------

        if (currentAmmo <= 0)
        {
            ShowAmmoFinishedPopup();
        }
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
    // SHOW AMMO FINISHED POPUP
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
            "[HunterShoot] " +
            "Ammo reset to " +
            maximumAmmo +
            "/" +
            maximumAmmo
        );
    }
}