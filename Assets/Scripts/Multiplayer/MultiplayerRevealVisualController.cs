using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerRevealVisualController :
    MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkState;

    [SerializeField]
    private MultiplayerRoleController roleController;

    [Header("Reveal UI")]
    [SerializeField]
    private GameObject revealPhasePanel;

    [SerializeField]
    private Image timerRing;

    [SerializeField]
    private TMP_Text timerText;

    [SerializeField]
    private Button shootButton;

    [Header("Reveal Settings")]
    [SerializeField]
    private float revealDuration = 30f;

    [SerializeField]
    private float pulseSpeed = 2f;

    private MultiplayerPhase previousPhase =
        (MultiplayerPhase)(-1);

    private HunterMecchaTarget[] mecchaTargets;

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

        // Reveal visuals exist only on the Seeker phone.
        if (!roleController.IsSeeker())
        {
            return;
        }

        MultiplayerPhase phase =
            networkState.Phase;

        if (
            phase != previousPhase
        )
        {
            HandlePhaseChanged(
                previousPhase,
                phase
            );

            previousPhase =
                phase;
        }

        if (
            phase ==
            MultiplayerPhase.Reveal
        )
        {
            UpdateRevealVisuals();
        }
    }

    private void HandlePhaseChanged(
        MultiplayerPhase oldPhase,
        MultiplayerPhase newPhase
    )
    {
        if (
            newPhase ==
            MultiplayerPhase.Reveal
        )
        {
            BeginRevealVisuals();
        }

        if (
            oldPhase ==
                MultiplayerPhase.Reveal &&
            newPhase !=
                MultiplayerPhase.Reveal
        )
        {
            EndRevealVisuals();
        }
    }

    private void BeginRevealVisuals()
    {
        if (
            revealPhasePanel != null
        )
        {
            revealPhasePanel.SetActive(
                true
            );
        }

        if (
            shootButton != null
        )
        {
            shootButton.interactable =
                false;
        }

        mecchaTargets =
            FindObjectsByType<
                HunterMecchaTarget
            >(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (
            HunterMecchaTarget target
            in mecchaTargets
        )
        {
            if (
                target != null &&
                !target.IsHit()
            )
            {
                target.StartRevealPulse();
            }
        }

        Debug.Log(
            "[MultiplayerReveal] " +
            "Reveal visuals started."
        );
    }

    private void UpdateRevealVisuals()
    {
        float remaining =
            networkState
                .GetRevealRemainingTime();

        if (
            timerRing != null &&
            revealDuration > 0f
        )
        {
            timerRing.fillAmount =
                Mathf.Clamp01(
                    remaining /
                    revealDuration
                );
        }

        if (
            timerText != null
        )
        {
            timerText.text =
                Mathf.CeilToInt(
                    remaining
                ).ToString();
        }

        float pulse =
            (
                Mathf.Sin(
                    Time.time *
                    pulseSpeed *
                    Mathf.PI *
                    2f
                ) +
                1f
            ) *
            0.5f;

        if (
            mecchaTargets == null
        )
        {
            return;
        }

        foreach (
            HunterMecchaTarget target
            in mecchaTargets
        )
        {
            if (
                target != null &&
                !target.IsHit()
            )
            {
                target.UpdateRevealPulse(
                    pulse
                );
            }
        }
    }

    private void EndRevealVisuals()
    {
        if (
            mecchaTargets != null
        )
        {
            foreach (
                HunterMecchaTarget target
                in mecchaTargets
            )
            {
                if (
                    target != null &&
                    !target.IsHit()
                )
                {
                    target.EndRevealPulse();
                }
            }
        }

        if (
            revealPhasePanel != null
        )
        {
            revealPhasePanel.SetActive(
                false
            );
        }

        Debug.Log(
            "[MultiplayerReveal] " +
            "Reveal visuals ended."
        );
    }
}