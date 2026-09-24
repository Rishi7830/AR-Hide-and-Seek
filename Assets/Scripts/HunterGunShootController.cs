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

    // AMMO

    [Header("Ammo")]
    [SerializeField]
    private int maximumAmmo = 6;
    private int currentAmmo;

    [Header("Hunter Score")]
    [SerializeField]
    private HunterMecchaScoreController scoreController;

    // SHOOT SETTINGS

    [Header("Shoot Settings")]
    [SerializeField]
    private float fireCooldown = 0.15f;

    [Header("Reveal Phase")]
    [SerializeField]
    private HunterRevealPhaseController revealPhaseController;

    // MUZZLE FLASH

    [Header("Muzzle Flash")]
    [SerializeField]
    private bool forceMuzzleFlashPosition = true;

    [SerializeField]
    private Vector3 muzzleFlashOffset =
        Vector3.zero;

    // HIT DETECTION

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

    [Header("Debug")]
    [SerializeField]
    private bool logDebug = true;

    // INTERNAL STATE

    private float nextAllowedShotTime = 0f;

    private void Start()
    {
        // FIND PICKUP CONTROLLER

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

        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<
                    HunterMecchaScoreController>();
        }

        else
        {
            Debug.LogWarning(
                "[HunterShoot] " +
                "Shoot Button is not assigned."
            );
        }

        // INITIALISE AMMO

        currentAmmo = maximumAmmo;

        // HIDE AMMO POPUP

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

    // SHOOT

    public void Shoot()
    {
        if (
            revealPhaseController != null &&
            revealPhaseController.IsRevealActive()
        )
        {
            if (logDebug)
            {
                Debug.Log(
                    "[HunterShoot] Shooting disabled during Reveal Phase."
                );
            }
            return;
        }
        // CHECK AMMO

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

        // FIRE COOLDOWN

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

        // GET HELD GUN

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

        // SET COOLDOWN

        nextAllowedShotTime =
            Time.time +
            fireCooldown;

        // CONSUME AMMO

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

        PlayMuzzleFlash(
            muzzle,
            heldGun
        );

        PerformShotRaycast(
            muzzle,
            heldGun
        );

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

        // AMMO FINISHED

        if (currentAmmo <= 0)
        {
            ShowAmmoFinishedPopup();
        }
    }

    // PLAY MUZZLE FLASH

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

        // FORCE FLASH POSITION

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

        muzzleFlash.Stop(
            true,
            ParticleSystemStopBehavior
                .StopEmittingAndClear
        );

        muzzleFlash.Play();
    }

    // PERFORM SHOOT RAYCAST

    private void PerformShotRaycast(
    Transform muzzle,
    HunterGunPickup heldGun
    )
        {
        // SHOT ORIGIN

        Vector3 origin =
            muzzle.position;

        // SHOT DIRECTION

        Vector3 direction =
            muzzle.forward.normalized;

        // SHOW DEBUG RAY

        if (showDebugRay)
        {
            Debug.DrawRay(
                origin,
                direction * shootingRange,
                Color.red,
                2f
            );
        }

        float shootingRadius = 0.12f;

        if (
            Physics.SphereCast(
                origin,
                shootingRadius,
                direction,
                out RaycastHit hit,
                shootingRange,
                shootingLayers,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            // CHECK FOR MECCHA

            HunterMecchaTarget target =
                hit.collider
                    .GetComponentInParent<
                        HunterMecchaTarget>();

            // MECCHA HIT

            if (target != null)
            {
                bool newlyFound =
                    target.OnShot();

                if (
                    newlyFound &&
                    scoreController != null
                )
                {
                    scoreController.RegisterMecchaFound(
                        target
                    );
                }

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

            // SOMETHING ELSE WAS HIT

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

    // SHOW AMMO POPUP

    private void ShowAmmoFinishedPopup()
    {
        if (ammoFinishedPanel != null)
        {
            ammoFinishedPanel.SetActive(
                true
            );
        }
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
            "[HunterShoot] Ammo reset to " +
            maximumAmmo +
            "/" +
            maximumAmmo
        );
    }
}