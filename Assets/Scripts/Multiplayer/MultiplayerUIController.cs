using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;

public class MultiplayerUIController :
    MonoBehaviour
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

    [Header("Hider End-Game Panels")]
    [SerializeField]
    private GameObject
        hiderRevealPanel;

    [SerializeField]
    private GameObject
        hiderResultPanel;

    [Header("Meccha Found Popups")]
    [SerializeField]
    private GameObject
        hiderFoundPanel;

    [SerializeField]
    private GameObject
        seekerFoundPanel;

    [SerializeField]
    private float foundPopupDuration =
        2f;

    [Header("Found Popup Text")]
    [SerializeField]
    private TMP_Text
        hiderFoundText;

    [SerializeField]
    private TMP_Text
        seekerFoundText;

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

    [Header("Hider Reveal UI")]
    [SerializeField]
    private TMP_Text
        hiderRevealTimerText;

    [Header("Hider Result UI")]
    [SerializeField]
    private TMP_Text
        hiderResultText;

    // STATE

    private MultiplayerPhase
        previousPhase;
    private int previousMecchasFound =
        0;
    private float foundPopupEndTime =
        -1f;

    private void Start()
    {
        if (
            hidingEndedPanel != null
        )
        {
            hidingEndedPanel
                .SetActive(false);
        }

        HideFoundPopup();

        if (
            hiderRevealPanel != null
        )
        {
            hiderRevealPanel
                .SetActive(false);
        }

        if (
            hiderResultPanel != null
        )
        {
            hiderResultPanel
                .SetActive(false);
        }
    }

    private void Update()
    {
        if (
            networkState == null ||
            roleController == null ||
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

        // MAIN GAME PANELS

        UpdatePanels(
            role,
            phase
        );

        // TIMERS

        UpdateTimers();

        // MECCHA FOUND POPUP

        CheckMecchaFound();

        // POPUP TIMER

        if (
            Time.time >
            foundPopupEndTime
        )
        {
            HideFoundPopup();
        }

        // PHASE CHANGES

        if (
            phase !=
            previousPhase
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

    // MAIN PANELS

    private void UpdatePanels(
        MultiplayerRole role,
        MultiplayerPhase phase
    )
    {
        // ROLE SELECTION

        SetPanel(
            roleSelectionPanel,
            phase ==
                MultiplayerPhase
                    .WaitingForPlayers &&
            role ==
                MultiplayerRole.None
        );

        // WAITING FOR OTHER PLAYER

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

        // SCAN ORIGIN

        bool scanOrigin =
            phase ==
                MultiplayerPhase
                    .WaitingForOrigins &&
            !originManager.HasOrigin;

        SetPanel(
            scanOriginPanel,
            scanOrigin
        );

        // HIDER GAMEPLAY

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

        // SEEKER WAITING

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

        // SEEKER MAIN GAME PANEL

        bool seekerPanelRequired =
            role ==
                MultiplayerRole.Seeker &&
            (
                phase ==
                    MultiplayerPhase
                        .SeekerSearching ||
                phase ==
                    MultiplayerPhase
                        .Reveal ||
                phase ==
                    MultiplayerPhase
                        .Result
            );

        SetPanel(
            seekerGamePanel,
            seekerPanelRequired
        );

        // SEEKER REVEAL

        bool seekerReveal =
            phase ==
                MultiplayerPhase.Reveal &&
            role ==
                MultiplayerRole.Seeker;

        SetPanel(
            revealPanel,
            seekerReveal
        );

        // SEEKER RESULT

        bool seekerResult =
            phase ==
                MultiplayerPhase.Result &&
            role ==
                MultiplayerRole.Seeker;

        SetPanel(
            resultPanel,
            seekerResult
        );

        // HIDER REVEAL

        bool hiderReveal =
            phase ==
                MultiplayerPhase.Reveal &&
            role ==
                MultiplayerRole.Hider;

        SetPanel(
            hiderRevealPanel,
            hiderReveal
        );

        // HIDER RESULT

        bool hiderResult =
            phase ==
                MultiplayerPhase.Result &&
            role ==
                MultiplayerRole.Hider;

        SetPanel(
            hiderResultPanel,
            hiderResult
        );

        // RESULT TEXT - SEEKER

        if (
            seekerResult &&
            resultText != null
        )
        {
            resultText.text =
                GetResultMessage(
                    networkState.Result
                );
        }

        // RESULT TEXT - HIDER

        if (
            hiderResult &&
            hiderResultText != null
        )
        {
            hiderResultText.text =
                GetResultMessage(
                    networkState.Result
                );
        }

        // REMAINING MECCHA

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

    // TIMERS

    private void UpdateTimers()
    {
        // HIDER TIMER

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

        // SEEKER WAITING TIMER

        if (
            seekerWaitingTimerText !=
            null
        )
        {
            seekerWaitingTimerText.text =
                hidingText;
        }

        // SEEKER SEARCH TIMER

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

        // HIDER REVEAL TIMER

        if (
            hiderRevealTimerText !=
            null &&
            networkState.Phase ==
                MultiplayerPhase.Reveal
        )
        {
            float revealTime =
                networkState
                    .RevealTimer
                    .RemainingTime(
                        networkState.Runner
                    )
                    ?? 0f;

            hiderRevealTimerText.text =
                FormatTime(
                    revealTime
                );
        }
        // EXISTING SEEKER REVEAL TIMER
    }

    // MECCHA FOUND

    private void CheckMecchaFound()
    {
        if (
            networkState.Phase !=
            MultiplayerPhase.SeekerSearching
        )
        {
            previousMecchasFound =
                networkState.MecchasFound;

            return;
        }

        if (
            networkState.MecchasFound >
            previousMecchasFound
        )
        {
            ShowMecchaFoundPopup();
        }

        previousMecchasFound =
            networkState.MecchasFound;
    }

    // SHOW MECCHA FOUND POPUP

    private void ShowMecchaFoundPopup()
    {
        string message =
            networkState.MecchasFound +
            " Meccha Found, " +
            networkState.RemainingMecchas +
            " Meccha Remaining";

        MultiplayerRole role =
            roleController.GetLocalRole();

        // HIDER PHONE

        if (
            role ==
            MultiplayerRole.Hider
        )
        {
            if (
                hiderFoundPanel != null
            )
            {
                hiderFoundPanel.SetActive(
                    true
                );
            }

            if (
                hiderFoundText != null
            )
            {
                hiderFoundText.text =
                    message;
            }
        }

        // SEEKER PHONE

        if (
            role ==
            MultiplayerRole.Seeker
        )
        {
            if (
                seekerFoundPanel != null
            )
            {
                seekerFoundPanel.SetActive(
                    true
                );
            }

            if (
                seekerFoundText != null
            )
            {
                seekerFoundText.text =
                    message;
            }
        }

        foundPopupEndTime =
            Time.time +
            foundPopupDuration;
    }

    // HIDE FOUND POPUP

    private void HideFoundPopup()
    {
        if (
            hiderFoundPanel != null
        )
        {
            hiderFoundPanel.SetActive(
                false
            );
        }

        if (
            seekerFoundPanel != null
        )
        {
            seekerFoundPanel.SetActive(
                false
            );
        }
    }

    // PHASE CHANGED

    private void HandlePhaseChanged(
        MultiplayerRole role,
        MultiplayerPhase oldPhase,
        MultiplayerPhase newPhase
    )
    {
        // HIDER HIDING -> SEEKER SEARCHING

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

        // ENTER REVEAL

        if (
            newPhase ==
            MultiplayerPhase.Reveal
        )
        {
            HideFoundPopup();

            Debug.Log(
                "[MultiplayerUI] " +
                "Network-wide Reveal Phase started."
            );
        }

        // ENTER RESULT

        if (
            newPhase ==
            MultiplayerPhase.Result
        )
        {
            HideFoundPopup();

            Debug.Log(
                "[MultiplayerUI] " +
                "Network-wide Result Phase started. " +
                "Result = " +
                networkState.Result
            );
        }

        // NEW ROUND

        if (
            newPhase ==
            MultiplayerPhase.WaitingForPlayers
        )
        {
            previousMecchasFound =
                0;

            HideFoundPopup();

            if (
                hiderRevealPanel !=
                null
            )
            {
                hiderRevealPanel
                    .SetActive(false);
            }

            if (
                hiderResultPanel !=
                null
            )
            {
                hiderResultPanel
                    .SetActive(false);
            }
        }
    }

    // HIDING ENDED PANEL

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

    // RESULT MESSAGE

    private string GetResultMessage(
        MultiplayerResult result
    )
    {
        switch (
            result
        )
        {
            case MultiplayerResult
                .SeekerWin:

                return
                    "Seekers WONN!!!";

            case MultiplayerResult
                .HiderWin:

                return
                    "Hiders WONN!!";

            case MultiplayerResult
                .Neither:

                return
                    "Neither Won - " +
                    "No Meccha Placed :(";

            default:

                return "";
        }
    }

    // FORMAT TIMER

    private string FormatTime(
        float seconds
    )
    {
        int totalSeconds =
            Mathf.CeilToInt(
                Mathf.Max(
                    0f,
                    seconds
                )
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

    // PANEL HELPER

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