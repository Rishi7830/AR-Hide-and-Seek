using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HunterGunShootController : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private HunterGunPickupController pickupController;

    [SerializeField]
    private Button shootButton;

    [SerializeField]
    private TMP_Text ammoRemain;

    [SerializeField]
    private GameObject ammoFinishedPanel;

    [Header("Ammo")]

    [SerializeField]
    private int maximumAmmo = 6;

    private int currentAmmo;

    [Header("Shoot Settings")]

    [SerializeField]
    private float fireCooldown = 0.15f;

    [Header("Muzzle Flash")]

    [Tooltip(
        "If enabled, the muzzle flash is forcibly positioned " +
        "at the gun muzzle every time the gun fires."
    )]
    [SerializeField]
    private bool forceMuzzleFlashPosition = true;

    [Tooltip(
        "Additional offset from the GunMuzzle position."
    )]
    [SerializeField]
    private Vector3 muzzleFlashOffset =
        Vector3.zero;

    [Header("Debug")]

    [SerializeField]
    private bool logDebug = true;
    private float nextAllowedShotTime = 0f;

    private void Start()
    {
        if (pickupController == null)
        {
            pickupController =
                FindFirstObjectByType<
                    HunterGunPickupController>();
        }

        // CONNECT SHOOT BUTTON

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

        // INITIALISE AMMO

        currentAmmo =
            maximumAmmo;
        if (ammoFinishedPanel != null)
        {
            ammoFinishedPanel.SetActive(
                false
            );
        }
        UpdateAmmoUI();
    }

    private void OnDisable()
    {
        if (shootButton != null)
        {
            shootButton.onClick.RemoveListener(
                Shoot
            );
        }
    }

    public void Shoot()
    {
        // CHECK AMMO

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

        // CHECK COOLDOWN

        if (
            Time.time <
            nextAllowedShotTime
        )
        {
            return;
        }

        // CHECK PICKUP CONTROLLER

        if (pickupController == null)
        {
            return;
        }

        // GET CURRENT GUN

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

        nextAllowedShotTime =
            Time.time +
            fireCooldown;

        currentAmmo--;

        UpdateAmmoUI();

        // FIND GUN MUZZLE

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

        // FIND MUZZLE FLASH

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

        // FORCE MUZZLE FLASH POSITION

        if (forceMuzzleFlashPosition)
        {
            Transform flashTransform =
                muzzleFlash.transform;

            // POSITION

            flashTransform.position =
                muzzle.position +
                muzzle.TransformDirection(
                    muzzleFlashOffset
                );

            flashTransform.rotation =
                muzzle.rotation;
        }

        // PLAY MUZZLE FLASH

        muzzleFlash.Stop(
            true,
            ParticleSystemStopBehavior
                .StopEmittingAndClear
        );

        muzzleFlash.Play();

        if (logDebug)
        {
            Debug.Log(
                "[HunterShoot] Fired " +
                heldGun.name
            );

            Debug.Log(
                "[HunterShoot] Muzzle position = " +
                muzzle.position
            );

            Debug.Log(
                "[HunterShoot] Flash position = " +
                muzzleFlash.transform.position
            );
        }

        if (currentAmmo <= 0)
        {
            ShowAmmoFinishedPopup();
        }
    }

    // UPDATE AMMO UI

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

    private void ShowAmmoFinishedPopup()
    {
        if (ammoFinishedPanel != null)
        {
            ammoFinishedPanel.SetActive(
                true
            );
        }
    }

    // FIND TRANSFORM RECURSIVELY

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

    // FIND PARTICLE SYSTEM RECURSIVELY

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

    // GET CURRENT AMMO

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    // GET MAXIMUM AMMO

    public int GetMaximumAmmo()
    {
        return maximumAmmo;
    }

    // RESET AMMO

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