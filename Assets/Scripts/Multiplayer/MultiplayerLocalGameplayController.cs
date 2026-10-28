using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class MultiplayerLocalGameplayController :
    MonoBehaviour
{
    // ============================================================
    // NETWORK
    // ============================================================

    [Header("Network")]

    [SerializeField]
    private NetworkGameState networkState;

    [SerializeField]
    private MultiplayerRoleController roleController;

    // ============================================================
    // HIDER
    // ============================================================

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
    private ARPlaneManager
        planeManager;

    // ============================================================
    // SEEKER HAND TRACKING
    // ============================================================

    [Header("Seeker Hand Tracking")]

    [Tooltip(
        "Main MediaPipe hand tracking manager."
    )]
    [SerializeField]
    private GameObject
        mediaPipeHandManager;

    [Tooltip(
        "Original single-screen hand tracking canvas. " +
        "Used by normal non-stereo gameplay."
    )]
    [SerializeField]
    private GameObject
        handTrackingCanvas;

    [Tooltip(
        "Stereo hand landmark visualizer GameObject. " +
        "Contains StereoARHandLandmarkVisualizer."
    )]
    [SerializeField]
    private GameObject
        stereoHandLandmark;

    [SerializeField]
    private StereoViewController
        stereoController;

    // ============================================================
    // SEEKER GUN SYSTEMS
    // ============================================================

    [Header("Seeker Gun Systems")]

    [SerializeField]
    private GameObject
        hunterGunManager;

    [SerializeField]
    private GameObject
        hunterGunHandController;

    [SerializeField]
    private GameObject
        hunterGunPickupController;

    [SerializeField]
    private GameObject
        hunterGunOrientationController;

    [SerializeField]
    private GameObject
        hunterCrosshairController;

    [SerializeField]
    private GameObject
        hunterGunShootController;

    [SerializeField]
    private GameObject
        hunterMecchaScoreController;

    // ============================================================
    // STATE
    // ============================================================

    private MultiplayerPhase previousPhase =
        (MultiplayerPhase)(-1);

    private MultiplayerRole previousRole =
        (MultiplayerRole)(-1);

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // HIDER SYSTEMS START DISABLED
        // --------------------------------------------------------

        if (
            spawnMeccha != null
        )
        {
            spawnMeccha.enabled =
                false;
        }

        if (
            fineTuneController != null
        )
        {
            fineTuneController.enabled =
                false;
        }

        if (
            paintManager != null
        )
        {
            paintManager.enabled =
                false;
        }

        SetPlaneSystemActive(
            false
        );

        // --------------------------------------------------------
        // HAND TRACKING STARTS DISABLED
        // --------------------------------------------------------

        SetHandTrackingActive(
            false,
            false
        );

        // --------------------------------------------------------
        // GUN SYSTEMS START DISABLED
        // --------------------------------------------------------

        SetGunManagerActive(
            false
        );

        SetGunFollowSystemsActive(
            false
        );

        SetGunCombatSystemsActive(
            false
        );

        SetMecchaScoreSystemActive(
            false
        );

        // --------------------------------------------------------
        // STEREO STARTS DISABLED
        // --------------------------------------------------------

        if (
            stereoController != null
        )
        {
            stereoController.DisableStereo();
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================

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

    // ============================================================
    // APPLY STATE
    // ============================================================

    private void ApplyState(
        MultiplayerRole role,
        MultiplayerPhase phase
    )
    {
        // --------------------------------------------------------
        // HIDER GAMEPLAY
        // --------------------------------------------------------

        bool hiderGameplay =
            role ==
                MultiplayerRole.Hider &&
            phase ==
                MultiplayerPhase.HiderHiding;

        // --------------------------------------------------------
        // SEEKER PHASES
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // SEEKER HAND TRACKING
        // --------------------------------------------------------

        bool seekerHandTracking =
            seekerSearching ||
            seekerReveal ||
            seekerResult;

        // --------------------------------------------------------
        // GUN FOLLOWING
        // --------------------------------------------------------

        bool seekerGunFollowing =
            seekerSearching ||
            seekerReveal ||
            seekerResult;

        // --------------------------------------------------------
        // COMBAT
        // --------------------------------------------------------

        bool seekerCombat =
            seekerSearching;

        // --------------------------------------------------------
        // SCORE
        // --------------------------------------------------------

        bool seekerScore =
            seekerSearching ||
            seekerReveal ||
            seekerResult;

        // ========================================================
        // DETERMINE WHETHER THIS SCENE USES STEREO
        // ========================================================
        //
        // IMPORTANT:
        //
        // We do NOT assume every Seeker is stereo.
        //
        // A Seeker is stereo only when this scene actually has
        // a StereoViewController assigned.
        //
        // This allows your old non-stereo scene to continue using
        // the original hand tracking.
        // ========================================================

        bool seekerStereo =
            role ==
                MultiplayerRole.Seeker &&
            stereoController != null;

        // ========================================================
        // HIDER SPAWNING
        // ========================================================

        if (
            spawnMeccha != null
        )
        {
            spawnMeccha.enabled =
                hiderGameplay;
        }

        // ========================================================
        // HIDER FINE TUNING
        // ========================================================

        if (
            fineTuneController != null
        )
        {
            fineTuneController.enabled =
                hiderGameplay;
        }

        // ========================================================
        // AR PLANES
        // ========================================================

        SetPlaneSystemActive(
            hiderGameplay
        );

        // ========================================================
        // PAINTING
        // ========================================================

        if (
            paintManager != null
        )
        {
            if (
                hiderGameplay
            )
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

        // ========================================================
        // HAND TRACKING
        // ========================================================

        SetHandTrackingActive(
            seekerHandTracking,
            seekerStereo
        );

        // ========================================================
        // GUN RACK
        // ========================================================

        SetGunManagerActive(
            seekerSearching
        );

        // ========================================================
        // GUN FOLLOWING
        // ========================================================

        SetGunFollowSystemsActive(
            seekerGunFollowing
        );

        // ========================================================
        // COMBAT
        // ========================================================

        SetGunCombatSystemsActive(
            seekerCombat
        );

        // ========================================================
        // SCORE
        // ========================================================

        SetMecchaScoreSystemActive(
            seekerScore
        );

        // ========================================================
        // STEREO
        // ========================================================

        if (
            stereoController != null
        )
        {
            if (
                seekerStereo
            )
            {
                stereoController.EnableStereo();
            }
            else
            {
                stereoController.DisableStereo();
            }
        }

        // ========================================================
        // DEBUG
        // ========================================================

        Debug.Log(
            "[MultiplayerLocal] " +
            "Role=" +
            role +
            " | Phase=" +
            phase +
            " | HiderGameplay=" +
            hiderGameplay +
            " | Stereo=" +
            seekerStereo +
            " | HandTracking=" +
            seekerHandTracking +
            " | OriginalHandCanvas=" +
            (
                handTrackingCanvas != null &&
                handTrackingCanvas.activeSelf
            ) +
            " | StereoHand=" +
            (
                stereoHandLandmark != null &&
                stereoHandLandmark.activeSelf
            ) +
            " | GunFollowing=" +
            seekerGunFollowing +
            " | Combat=" +
            seekerCombat
        );
    }

    // ============================================================
    // HAND TRACKING
    // ============================================================

    private void SetHandTrackingActive(
        bool active,
        bool stereoMode
    )
    {
        // --------------------------------------------------------
        // MEDIA PIPE DETECTOR
        // --------------------------------------------------------

        if (
            mediaPipeHandManager != null
        )
        {
            mediaPipeHandManager.SetActive(
                active
            );
        }

        // --------------------------------------------------------
        // ORIGINAL HAND TRACKING CANVAS
        // --------------------------------------------------------
        //
        // Normal Seeker:
        //     ON
        //
        // Stereo Seeker:
        //     OFF
        // --------------------------------------------------------

        if (
            handTrackingCanvas != null
        )
        {
            handTrackingCanvas.SetActive(
                active &&
                !stereoMode
            );
        }

        // --------------------------------------------------------
        // STEREO HAND VISUALIZER
        // --------------------------------------------------------
        //
        // Normal Seeker:
        //     OFF
        //
        // Stereo Seeker:
        //     ON
        // --------------------------------------------------------

        if (
            stereoHandLandmark != null
        )
        {
            stereoHandLandmark.SetActive(
                active &&
                stereoMode
            );
        }
    }

    // ============================================================
    // GUN MANAGER
    // ============================================================

    private void SetGunManagerActive(
        bool active
    )
    {
        if (
            hunterGunManager != null
        )
        {
            hunterGunManager.SetActive(
                active
            );
        }
    }

    // ============================================================
    // GUN FOLLOW SYSTEMS
    // ============================================================

    private void SetGunFollowSystemsActive(
        bool active
    )
    {
        if (
            hunterGunHandController != null
        )
        {
            hunterGunHandController.SetActive(
                active
            );
        }

        if (
            hunterGunPickupController != null
        )
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

    // ============================================================
    // GUN COMBAT SYSTEMS
    // ============================================================

    private void SetGunCombatSystemsActive(
        bool active
    )
    {
        if (
            hunterCrosshairController !=
            null
        )
        {
            hunterCrosshairController
                .SetActive(
                    active
                );
        }

        if (
            hunterGunShootController !=
            null
        )
        {
            hunterGunShootController
                .SetActive(
                    active
                );
        }
    }

    // ============================================================
    // MECCHA SCORE
    // ============================================================

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

    // ============================================================
    // AR PLANE SYSTEM
    // ============================================================

    private void SetPlaneSystemActive(
        bool active
    )
    {
        if (
            planeManager == null
        )
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
            if (
                plane != null
            )
            {
                plane.gameObject.SetActive(
                    active
                );
            }
        }
    }
}