using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;

public class MultiplayerUIController : MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkGameState
        networkState;

    [SerializeField]
    private MultiplayerRoleController
        roleController;

    [SerializeField]
    private SharedAROriginManager
        originManager;

    [Header("Panels")]
    [SerializeField]
    private GameObject roleSelectionPanel;

    [SerializeField]
    private GameObject waitingForPlayerPanel;

    [SerializeField]
    private GameObject scanOriginPanel;

    [SerializeField]
    private GameObject hiderGamePanel;

    [SerializeField]
    private GameObject seekerWaitingPanel;

    [SerializeField]
    private GameObject hidingEndedPanel;

    [SerializeField]
    private GameObject seekerGamePanel;

    [SerializeField]
    private GameObject revealPanel;

    [SerializeField]
    private GameObject resultPanel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text
        seekerWaitingTimerText;

    [SerializeField]
    private TMP_Text
        hiderTimerText;

    [SerializeField]
    private TMP_Text
        seekerTimerText;

    [SerializeField]
    private TMP_Text
        remainingMecchaText;

    [SerializeField]
    private TMP_Text
        resultText;

    private MultiplayerPhase
        previousPhase;

    private void Start()
    {
        if (
            hidingEndedPanel != null
        )
        {
            hidingEndedPanel
                .SetActive(false);
        }
    }

    private void Update()
    {
        if (
            networkState == null ||
            !networkState.IsNetworkReady ||
            networkState.Runner == null
        )
        {
            return;
        }

        MultiplayerRole role =
            roleController.GetLocalRole();

        MultiplayerPhase phase =
            networkState.Phase;

        UpdatePanels(
            role,
            phase
        );

        UpdateTimers();

        if (
            phase != previousPhase
        )
        {
            HandlePhaseChanged(
                role,
                previousPhase,
                phase
            );

            previousPhase =
                phase;
        }
    }

    private void UpdatePanels(
        MultiplayerRole role,
        MultiplayerPhase phase
    )
    {
        SetPanel(
            roleSelectionPanel,
            phase ==
                MultiplayerPhase
                    .WaitingForPlayers &&
            role ==
                MultiplayerRole.None
        );

        bool waitingForOtherPlayer =
            phase ==
                MultiplayerPhase
                    .WaitingForPlayers &&
            role !=
                MultiplayerRole.None;

        SetPanel(
            waitingForPlayerPanel,
            waitingForOtherPlayer
        );

        bool scanOrigin =
            phase ==
                MultiplayerPhase
                    .WaitingForOrigins &&
            !originManager.HasOrigin;

        SetPanel(
            scanOriginPanel,
            scanOrigin
        );

        bool hiderPlaying =
            phase ==
                MultiplayerPhase
                    .HiderHiding &&
            role ==
                MultiplayerRole.Hider;

        SetPanel(
            hiderGamePanel,
            hiderPlaying
        );

        bool seekerWaiting =
            phase ==
                MultiplayerPhase
                    .HiderHiding &&
            role ==
                MultiplayerRole.Seeker;

        SetPanel(
            seekerWaitingPanel,
            seekerWaiting
        );

        bool seekerPanelRequired =
            role ==
            MultiplayerRole.Seeker &&
        (
        phase ==
            MultiplayerPhase.SeekerSearching ||
        phase ==
            MultiplayerPhase.Reveal ||
        phase ==
            MultiplayerPhase.Result
        );

        SetPanel(
            seekerGamePanel,
            seekerPanelRequired
        );

        bool reveal =
            phase ==
                MultiplayerPhase.Reveal &&
            role ==
                MultiplayerRole.Seeker;

        SetPanel(
            revealPanel,
            reveal
        );

        bool result =
            phase ==
                MultiplayerPhase.Result &&
            role ==
                MultiplayerRole.Seeker;

        SetPanel(
            resultPanel,
            result
        );

        if (
            result &&
            resultText != null
        )
        {
            switch (
                networkState.Result
            )
            {
                case MultiplayerResult
                    .SeekerWin:

                    resultText.text =
                        "Seekers WONN!!!";

                    break;

                case MultiplayerResult
                    .HiderWin:

                    resultText.text =
                        "Hiders WONN!!";

                    break;

                case MultiplayerResult
                    .Neither:

                    resultText.text =
                        "Neither Won - " +
                        "No Meccha Placed :(";

                    break;
            }
        }

        if (
            remainingMecchaText !=
            null
        )
        {
            remainingMecchaText.text =
                "Meccha Remaining : " +
                networkState
                    .RemainingMecchas;
        }
    }

    private void UpdateTimers()
    {
        float hidingTime =
            networkState
                .GetHiderRemainingTime();

        string hidingText =
            FormatTime(
                hidingTime
            );

        if (
            hiderTimerText != null
        )
        {
            hiderTimerText.text =
                hidingText;
        }

        if (
            seekerWaitingTimerText !=
            null
        )
        {
            seekerWaitingTimerText.text =
                hidingText;
        }

        if (
            seekerTimerText != null
        )
        {
            seekerTimerText.text =
                FormatTime(
                    networkState
                        .GetSeekerRemainingTime()
                );
        }
    }

    private void HandlePhaseChanged(
        MultiplayerRole role,
        MultiplayerPhase oldPhase,
        MultiplayerPhase newPhase
    )
    {
        if (
            role ==
                MultiplayerRole.Hider &&
            oldPhase ==
                MultiplayerPhase
                    .HiderHiding &&
            newPhase ==
                MultiplayerPhase
                    .SeekerSearching
        )
        {
            StartCoroutine(
                ShowHidingEndedPanel()
            );
        }
    }

    private IEnumerator
        ShowHidingEndedPanel()
    {
        if (
            hidingEndedPanel == null
        )
        {
            yield break;
        }

        hidingEndedPanel
            .SetActive(true);

        yield return
            new WaitForSeconds(
                10f
            );

        hidingEndedPanel
            .SetActive(false);
    }

    private string FormatTime(
        float seconds
    )
    {
        int totalSeconds =
            Mathf.CeilToInt(
                seconds
            );

        int minutes =
            totalSeconds / 60;

        int remainingSeconds =
            totalSeconds % 60;

        return
            minutes.ToString("00") +
            ":" +
            remainingSeconds
                .ToString("00");
    }

    private void SetPanel(
        GameObject panel,
        bool active
    )
    {
        if (
            panel != null &&
            panel.activeSelf != active
        )
        {
            panel.SetActive(
                active
            );
        }
    }
}