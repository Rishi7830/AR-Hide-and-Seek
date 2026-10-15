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

    [Header("Seeker")]
    [SerializeField]
    private GameObject mediaPipeHandManager;

    [SerializeField]
    private GameObject handTrackingCanvas;

    private MultiplayerPhase previousPhase =
        (MultiplayerPhase)(-1);

    private MultiplayerRole previousRole =
        (MultiplayerRole)(-1);

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

        bool seekerGameplay =
            role ==
                MultiplayerRole.Seeker &&
            phase ==
                MultiplayerPhase.SeekerSearching;

        // Hider spawning is only active during Hider gameplay.
        if (spawnMeccha != null)
        {
            spawnMeccha.enabled =
                hiderGameplay;
        }

        // Hider fine tuning is only active during Hider gameplay.
        if (fineTuneController != null)
        {
            fineTuneController.enabled =
                hiderGameplay;
        }

        // Plane detection is only required while the Hider is hiding.
        if (planeManager != null)
        {
            planeManager.enabled =
                hiderGameplay;
        }

        // Painting is only available during Hider gameplay.
        if (paintManager != null)
        {
            if (hiderGameplay)
            {
                paintManager.enabled = true;
            }
            else
            {
                paintManager.CleanupHiderObjects();
                paintManager.enabled = false;
            }
        }

        // MediaPipe is only required during Seeker gameplay.
        if (mediaPipeHandManager != null)
        {
            mediaPipeHandManager.SetActive(
                seekerGameplay
            );
        }

        // Hand landmark visualization is only required for Seeker.
        if (handTrackingCanvas != null)
        {
            handTrackingCanvas.SetActive(
                seekerGameplay
            );
        }

        Debug.Log(
            "[MultiplayerLocal] Role: " +
            role +
            " | Phase: " +
            phase +
            " | HiderGameplay: " +
            hiderGameplay +
            " | SeekerGameplay: " +
            seekerGameplay
        );
    }
}