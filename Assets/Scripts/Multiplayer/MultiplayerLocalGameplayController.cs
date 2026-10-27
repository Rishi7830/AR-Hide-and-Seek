using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class MultiplayerLocalGameplayController :
    MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkState;

    [SerializeField]
    private MultiplayerRoleController roleController;

    [Header("Hider")]
    [SerializeField]
    private SpawnMeccha spawnMeccha;

    [SerializeField]
    private MecchaFineTuneController
        fineTuneController;

    [SerializeField]
    private ARFoundationPaintManager
        paintManager;

    [SerializeField]
    private ARPlaneManager planeManager;

    [Header("Seeker Hand Tracking")]
    [SerializeField]
    private GameObject mediaPipeHandManager;

    [SerializeField]
    private GameObject handTrackingCanvas;

    [SerializeField]
    private StereoViewController
    stereoController;

    [Header("Seeker Gun Systems")]
    [SerializeField]
    private GameObject hunterGunManager;

    [SerializeField]
    private GameObject hunterGunHandController;

    [SerializeField]
    private GameObject hunterGunPickupController;

    [SerializeField]
    private GameObject hunterGunOrientationController;

    [SerializeField]
    private GameObject hunterCrosshairController;

    [SerializeField]
    private GameObject hunterGunShootController;

    [SerializeField]
    private GameObject hunterMecchaScoreController;

    private MultiplayerPhase previousPhase =
        (MultiplayerPhase)(-1);

    private MultiplayerRole previousRole =
        (MultiplayerRole)(-1);

    private void Awake()
    {
        // Hider systems start disabled.
        if (spawnMeccha != null)
        {
            spawnMeccha.enabled =
                false;
        }

        if (fineTuneController != null)
        {
            fineTuneController.enabled =
                false;
        }

        if (paintManager != null)
        {
            paintManager.enabled =
                false;
        }

        SetPlaneSystemActive(
            false
        );

        // Hand tracking starts disabled.
        SetHandTrackingActive(
            false
        );

        // Gun rack spawning starts disabled.
        SetGunManagerActive(
            false
        );

        // Gun hand-following starts disabled.
        SetGunFollowSystemsActive(
            false
        );

        // Combat systems start disabled.
        SetGunCombatSystemsActive(
            false
        );

        SetMecchaScoreSystemActive(
            false
        );
    }

    private void Update()
    {
        if (
            networkState == null ||
            roleController == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        MultiplayerPhase phase =
            networkState.Phase;

        MultiplayerRole role =
            roleController.GetLocalRole();

        if (
            phase == previousPhase &&
            role == previousRole
        )
        {
            return;
        }

        previousPhase =
            phase;

        previousRole =
            role;

        ApplyState(
            role,
            phase
        );
    }

    private void ApplyState(
        MultiplayerRole role,
        MultiplayerPhase phase
    )
    {
        bool hiderGameplay =
            role ==
                MultiplayerRole.Hider &&
            phase ==
                MultiplayerPhase.HiderHiding;

        bool seekerSearching =
            role ==
                MultiplayerRole.Seeker &&
            phase ==
                MultiplayerPhase.SeekerSearching;

        bool seekerReveal =
            role ==
                MultiplayerRole.Seeker &&
            phase ==
                MultiplayerPhase.Reveal;

        bool seekerResult =
            role ==
                MultiplayerRole.Seeker &&
            phase ==
                MultiplayerPhase.Result;

        // Keep hand tracking active through
        // searching, reveal and result.
        bool seekerHandTracking =
            seekerSearching ||
            seekerReveal ||
            seekerResult;

        // Keep the held gun attached to the
        // tracked hand through reveal/result.
        bool seekerGunFollowing =
            seekerSearching ||
            seekerReveal ||
            seekerResult;

        // Shooting and crosshair are only
        // available during active searching.
        bool seekerCombat =
            seekerSearching;

        // Score information can remain active
        // throughout the Seeker end phases.
        bool seekerScore =
            seekerSearching ||
            seekerReveal ||
            seekerResult;

        // Hider spawning.
        if (spawnMeccha != null)
        {
            spawnMeccha.enabled =
                hiderGameplay;
        }

        // Hider fine tuning.
        if (fineTuneController != null)
        {
            fineTuneController.enabled =
                hiderGameplay;
        }

        // AR planes only during Hider gameplay.
        SetPlaneSystemActive(
            hiderGameplay
        );

        // Painting only during Hider gameplay.
        if (paintManager != null)
        {
            if (hiderGameplay)
            {
                paintManager.enabled =
                    true;
            }
            else
            {
                paintManager
                    .CleanupHiderObjects();

                paintManager.enabled =
                    false;
            }
        }

        // MediaPipe and hand visualization.
        SetHandTrackingActive(
            seekerHandTracking
        );

        // Gun rack spawning only occurs
        // during actual Seeker gameplay.
        SetGunManagerActive(
            seekerSearching
        );

        // Keep the already-held gun attached
        // to the hand during Reveal and Result.
        SetGunFollowSystemsActive(
            seekerGunFollowing
        );

        // Disable shooting and crosshair
        // once searching finishes.
        SetGunCombatSystemsActive(
            seekerCombat
        );

        SetMecchaScoreSystemActive(
            seekerScore
        );

        // Stereo is enabled for the entire Seeker experience,
        // including AprilTag scanning, coordinate setup,
        // waiting, searching, reveal and result.
        bool seekerStereo =
            role ==
                MultiplayerRole.Seeker;

        if (
            stereoController != null
        )
        {
            if (seekerStereo)
            {
                stereoController.EnableStereo();
            }
            else
            {
                stereoController.DisableStereo();
            }
        }

        Debug.Log(
            "[MultiplayerLocal] Role=" +
            role +
            " | Phase=" +
            phase +
            " | HiderGameplay=" +
            hiderGameplay +
            " | HandTracking=" +
            seekerHandTracking +
            " | GunFollowing=" +
            seekerGunFollowing +
            " | Combat=" +
            seekerCombat
        );
    }

    private void SetHandTrackingActive(
        bool active
    )
    {
        if (mediaPipeHandManager != null)
        {
            mediaPipeHandManager.SetActive(
                active
            );
        }

        if (handTrackingCanvas != null)
        {
            handTrackingCanvas.SetActive(
                active
            );
        }
    }

    private void SetGunManagerActive(
        bool active
    )
    {
        if (hunterGunManager != null)
        {
            hunterGunManager.SetActive(
                active
            );
        }
    }

    private void SetGunFollowSystemsActive(
        bool active
    )
    {
        if (hunterGunHandController != null)
        {
            hunterGunHandController.SetActive(
                active
            );
        }

        if (hunterGunPickupController != null)
        {
            hunterGunPickupController.SetActive(
                active
            );
        }

        if (
            hunterGunOrientationController !=
            null
        )
        {
            hunterGunOrientationController
                .SetActive(
                    active
                );
        }
    }

    private void SetGunCombatSystemsActive(
        bool active
    )
    {
        if (hunterCrosshairController != null)
        {
            hunterCrosshairController
                .SetActive(
                    active
                );
        }

        if (hunterGunShootController != null)
        {
            hunterGunShootController
                .SetActive(
                    active
                );
        }
    }

    private void SetMecchaScoreSystemActive(
        bool active
    )
    {
        if (
            hunterMecchaScoreController !=
            null
        )
        {
            hunterMecchaScoreController
                .SetActive(
                    active
                );
        }
    }

    private void SetPlaneSystemActive(
        bool active
    )
    {
        if (planeManager == null)
        {
            return;
        }

        planeManager.enabled =
            active;

        foreach (
            ARPlane plane
            in planeManager.trackables
        )
        {
            if (plane != null)
            {
                plane.gameObject.SetActive(
                    active
                );
            }
        }
    }
}