using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;

public class StereoMultiplayerUIController :
    MonoBehaviour
{
    // ============================================================
    // NETWORK
    // ============================================================

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

    // ============================================================
    // ORIGINAL / HIDER PANELS
    // ============================================================

    [Header("Original / Hider Panels")]

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

    // ============================================================
    // ORIGINAL SINGLE SEEKER PANELS
    // ============================================================
    //
    // These can remain assigned or be left empty.
    // Stereo panels take priority when configured.
    //

    [Header("Original Single Seeker Panels")]

    [SerializeField]
    private GameObject seekerGamePanel;

    [SerializeField]
    private GameObject revealPanel;

    [SerializeField]
    private GameObject resultPanel;

    // ============================================================
    // STEREO SEEKER SETUP PANELS
    // ============================================================

    [Header("Stereo Seeker Setup Panels")]

    // ------------------------------------------------------------
    // SCAN ORIGIN
    // ------------------------------------------------------------

    [Tooltip(
        "Left-eye Initial AprilTag scan panel."
    )]
    [SerializeField]
    private GameObject
        seekerLeftScanOriginPanel;

    [Tooltip(
        "Right-eye Initial AprilTag scan panel."
    )]
    [SerializeField]
    private GameObject
        seekerRightScanOriginPanel;

    // ------------------------------------------------------------
    // COORDINATES SET
    // ------------------------------------------------------------

    [Tooltip(
        "Left-eye Coordinates Set panel."
    )]
    [SerializeField]
    private GameObject
        seekerLeftCoordinatesSetPanel;

    [Tooltip(
        "Right-eye Coordinates Set panel."
    )]
    [SerializeField]
    private GameObject
        seekerRightCoordinatesSetPanel;

    // ------------------------------------------------------------
    // COORDINATES UPDATED
    // ------------------------------------------------------------

    [Tooltip(
        "Left-eye Coordinates Updated panel."
    )]
    [SerializeField]
    private GameObject
        seekerLeftCoordinatesUpdatedPanel;

    [Tooltip(
        "Right-eye Coordinates Updated panel."
    )]
    [SerializeField]
    private GameObject
        seekerRightCoordinatesUpdatedPanel;

    // ------------------------------------------------------------
    // SEEKER WAITING
    // ------------------------------------------------------------

    [Tooltip(
        "Left-eye Seeker Waiting panel."
    )]
    [SerializeField]
    private GameObject
        seekerLeftWaitingPanel;

    [Tooltip(
        "Right-eye Seeker Waiting panel."
    )]
    [SerializeField]
    private GameObject
        seekerRightWaitingPanel;

    // ------------------------------------------------------------
    // HIDING ENDED
    // ------------------------------------------------------------

    [Tooltip(
        "Left-eye Hiding Ended panel."
    )]
    [SerializeField]
    private GameObject
        seekerLeftHidingEndedPanel;

    [Tooltip(
        "Right-eye Hiding Ended panel."
    )]
    [SerializeField]
    private GameObject
        seekerRightHidingEndedPanel;

    // ============================================================
    // STEREO SEEKER GAMEPLAY PANELS
    // ============================================================

    [Header("Stereo Seeker Gameplay Panels")]

    [SerializeField]
    private GameObject
        seekerLeftGamePanel;

    [SerializeField]
    private GameObject
        seekerRightGamePanel;

    [SerializeField]
    private GameObject
        seekerLeftRevealPanel;

    [SerializeField]
    private GameObject
        seekerRightRevealPanel;

    [SerializeField]
    private GameObject
        seekerLeftResultPanel;

    [SerializeField]
    private GameObject
        seekerRightResultPanel;

    // ============================================================
    // HIDER END-GAME PANELS
    // ============================================================

    [Header("Hider End-Game Panels")]

    [SerializeField]
    private GameObject
        hiderRevealPanel;

    [SerializeField]
    private GameObject
        hiderResultPanel;

    // ============================================================
    // MECCHA FOUND POPUPS
    // ============================================================

    [Header("Meccha Found Popups")]

    // Hider popup.
    [SerializeField]
    private GameObject
        hiderFoundPanel;

    // Old single-screen Seeker popup.
    [SerializeField]
    private GameObject
        seekerFoundPanel;

    // Stereo Seeker popups.
    [SerializeField]
    private GameObject
        seekerLeftFoundPanel;

    [SerializeField]
    private GameObject
        seekerRightFoundPanel;

    [SerializeField]
    private float foundPopupDuration =
        2f;

    // ============================================================
    // ORIGINAL TEXTS
    // ============================================================

    [Header("Original Texts")]

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

    // ============================================================
    // ORIGINAL FOUND TEXTS
    // ============================================================

    [Header("Found Popup Text")]

    [SerializeField]
    private TMP_Text
        hiderFoundText;

    [SerializeField]
    private TMP_Text
        seekerFoundText;

    // ============================================================
    // HIDER REVEAL / RESULT TEXT
    // ============================================================

    [Header("Hider Reveal UI")]

    [SerializeField]
    private TMP_Text
        hiderRevealTimerText;

    [Header("Hider Result UI")]

    [SerializeField]
    private TMP_Text
        hiderResultText;

    // ============================================================
    // STEREO SEEKER TEXTS
    // ============================================================

    [Header("Stereo Seeker Texts")]

    // ------------------------------------------------------------
    // SEEKER WAITING
    // ------------------------------------------------------------

    [SerializeField]
    private TMP_Text
        seekerLeftWaitingTimerText;

    [SerializeField]
    private TMP_Text
        seekerRightWaitingTimerText;

    // ------------------------------------------------------------
    // SEEKER SEARCHING
    // ------------------------------------------------------------

    [SerializeField]
    private TMP_Text
        seekerLeftTimerText;

    [SerializeField]
    private TMP_Text
        seekerRightTimerText;

    // ------------------------------------------------------------
    // REMAINING MECCHA
    // ------------------------------------------------------------

    [SerializeField]
    private TMP_Text
        seekerLeftRemainingText;

    [SerializeField]
    private TMP_Text
        seekerRightRemainingText;

    // ------------------------------------------------------------
    // FOUND POPUP
    // ------------------------------------------------------------

    [SerializeField]
    private TMP_Text
        seekerLeftFoundText;

    [SerializeField]
    private TMP_Text
        seekerRightFoundText;

    // ------------------------------------------------------------
    // REVEAL
    // ------------------------------------------------------------

    [SerializeField]
    private TMP_Text
        seekerLeftRevealTimerText;

    [SerializeField]
    private TMP_Text
        seekerRightRevealTimerText;

    // ------------------------------------------------------------
    // RESULT
    // ------------------------------------------------------------

    [SerializeField]
    private TMP_Text
        seekerLeftResultText;

    [SerializeField]
    private TMP_Text
        seekerRightResultText;

    // ============================================================
    // STATE
    // ============================================================

    private MultiplayerPhase
        previousPhase;

    private int previousMecchasFound =
        0;

    private float foundPopupEndTime =
        -1f;

    // ============================================================
    // UNITY START
    // ============================================================

    private void Start()
    {
        // --------------------------------------------------------
        // Existing Hider panel
        // --------------------------------------------------------

        SetPanel(
            hidingEndedPanel,
            false
        );

        // --------------------------------------------------------
        // Hide found popups
        // --------------------------------------------------------

        HideFoundPopup();

        // --------------------------------------------------------
        // Hide Hider end-game panels
        // --------------------------------------------------------

        SetPanel(
            hiderRevealPanel,
            false
        );

        SetPanel(
            hiderResultPanel,
            false
        );

        // --------------------------------------------------------
        // Hide all stereo setup panels
        // --------------------------------------------------------

        HideStereoSetupPanels();

        // --------------------------------------------------------
        // Hide all stereo gameplay panels
        // --------------------------------------------------------

        HideStereoGameplayPanels();
    }

    // ============================================================
    // UPDATE
    // ============================================================

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

        // --------------------------------------------------------
        // UPDATE PANELS
        // --------------------------------------------------------

        UpdatePanels(
            role,
            phase
        );

        // --------------------------------------------------------
        // UPDATE TIMERS
        // --------------------------------------------------------

        UpdateTimers();

        // --------------------------------------------------------
        // CHECK MECCHA FOUND
        // --------------------------------------------------------

        CheckMecchaFound();

        // --------------------------------------------------------
        // POPUP TIMEOUT
        // --------------------------------------------------------

        if (
            Time.time >
            foundPopupEndTime
        )
        {
            HideFoundPopup();
        }

        // --------------------------------------------------------
        // PHASE CHANGE
        // --------------------------------------------------------

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

    // ============================================================
    // MAIN PANEL CONTROL
    // ============================================================

    private void UpdatePanels(
        MultiplayerRole role,
        MultiplayerPhase phase
    )
    {
        // ========================================================
        // ROLE SELECTION
        // ========================================================

        bool roleSelection =
            phase ==
                MultiplayerPhase
                    .WaitingForPlayers &&
            role ==
                MultiplayerRole.None;

        SetPanel(
            roleSelectionPanel,
            roleSelection
        );

        // ========================================================
        // WAITING FOR OTHER PLAYER
        // ========================================================

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

        // ========================================================
        // SCAN ORIGIN
        // ========================================================

        bool scanOrigin =
            phase ==
                MultiplayerPhase
                    .WaitingForOrigins &&
            !originManager.HasOrigin;

        if (
            role ==
            MultiplayerRole.Seeker
        )
        {
            // ----------------------------------------------------
            // Seeker gets stereo scan panels.
            // ----------------------------------------------------

            SetPanel(
                scanOriginPanel,
                false
            );

            SetPanel(
                seekerLeftScanOriginPanel,
                scanOrigin
            );

            SetPanel(
                seekerRightScanOriginPanel,
                scanOrigin
            );
        }
        else
        {
            // ----------------------------------------------------
            // Hider keeps original full-screen scan panel.
            // ----------------------------------------------------

            SetPanel(
                scanOriginPanel,
                scanOrigin
            );

            SetPanel(
                seekerLeftScanOriginPanel,
                false
            );

            SetPanel(
                seekerRightScanOriginPanel,
                false
            );
        }

        // ========================================================
        // HIDER GAMEPLAY
        // ========================================================

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

        // ========================================================
        // SEEKER WAITING
        // ========================================================

        bool seekerWaiting =
            phase ==
                MultiplayerPhase
                    .HiderHiding &&
            role ==
                MultiplayerRole.Seeker;

        if (
            role ==
            MultiplayerRole.Seeker
        )
        {
            // ----------------------------------------------------
            // Hide original full-screen waiting panel.
            // ----------------------------------------------------

            SetPanel(
                seekerWaitingPanel,
                false
            );

            // ----------------------------------------------------
            // Show stereo waiting panels.
            // ----------------------------------------------------

            SetPanel(
                seekerLeftWaitingPanel,
                seekerWaiting
            );

            SetPanel(
                seekerRightWaitingPanel,
                seekerWaiting
            );
        }
        else
        {
            // Hider never uses Seeker waiting panels.
            SetPanel(
                seekerWaitingPanel,
                false
            );

            SetPanel(
                seekerLeftWaitingPanel,
                false
            );

            SetPanel(
                seekerRightWaitingPanel,
                false
            );
        }

        // ========================================================
        // SEEKER SEARCHING
        // ========================================================

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

        // Disable old single Seeker panel.
        SetPanel(
            seekerGamePanel,
            false
        );

        // Enable stereo game panels.
        SetPanel(
            seekerLeftGamePanel,
            seekerPanelRequired
        );

        SetPanel(
            seekerRightGamePanel,
            seekerPanelRequired
        );

        // ========================================================
        // SEEKER REVEAL
        // ========================================================

        bool seekerReveal =
            phase ==
                MultiplayerPhase.Reveal &&
            role ==
                MultiplayerRole.Seeker;

        // Old panel off.
        SetPanel(
            revealPanel,
            false
        );

        // Stereo panels.
        SetPanel(
            seekerLeftRevealPanel,
            seekerReveal
        );

        SetPanel(
            seekerRightRevealPanel,
            seekerReveal
        );

        // ========================================================
        // SEEKER RESULT
        // ========================================================

        bool seekerResult =
            phase ==
                MultiplayerPhase.Result &&
            role ==
                MultiplayerRole.Seeker;

        // Old panel off.
        SetPanel(
            resultPanel,
            false
        );

        // Stereo panels.
        SetPanel(
            seekerLeftResultPanel,
            seekerResult
        );

        SetPanel(
            seekerRightResultPanel,
            seekerResult
        );

        // ========================================================
        // HIDER REVEAL
        // ========================================================

        bool hiderReveal =
            phase ==
                MultiplayerPhase.Reveal &&
            role ==
                MultiplayerRole.Hider;

        SetPanel(
            hiderRevealPanel,
            hiderReveal
        );

        // ========================================================
        // HIDER RESULT
        // ========================================================

        bool hiderResult =
            phase ==
                MultiplayerPhase.Result &&
            role ==
                MultiplayerRole.Hider;

        SetPanel(
            hiderResultPanel,
            hiderResult
        );

        // ========================================================
        // RESULT TEXT - SEEKER
        // ========================================================

        if (
            seekerResult
        )
        {
            string resultMessage =
                GetResultMessage(
                    networkState.Result
                );

            if (
                seekerLeftResultText !=
                null
            )
            {
                seekerLeftResultText.text =
                    resultMessage;
            }

            if (
                seekerRightResultText !=
                null
            )
            {
                seekerRightResultText.text =
                    resultMessage;
            }
        }

        // ========================================================
        // RESULT TEXT - HIDER
        // ========================================================

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

        // ========================================================
        // REMAINING MECCHAS
        // ========================================================

        string remainingText =
            "Meccha Remaining : " +
            networkState.RemainingMecchas;

        if (
            remainingMecchaText !=
            null
        )
        {
            remainingMecchaText.text =
                remainingText;
        }

        if (
            seekerLeftRemainingText !=
            null
        )
        {
            seekerLeftRemainingText.text =
                remainingText;
        }

        if (
            seekerRightRemainingText !=
            null
        )
        {
            seekerRightRemainingText.text =
                remainingText;
        }
    }

    // ============================================================
    // TIMER CONTROL
    // ============================================================

    private void UpdateTimers()
    {
        // ========================================================
        // HIDER TIMER
        // ========================================================

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

        // ========================================================
        // ORIGINAL SEEKER WAITING TIMER
        // ========================================================

        if (
            seekerWaitingTimerText !=
            null
        )
        {
            seekerWaitingTimerText.text =
                hidingText;
        }

        // ========================================================
        // STEREO SEEKER WAITING TIMERS
        // ========================================================

        if (
            seekerLeftWaitingTimerText !=
            null
        )
        {
            seekerLeftWaitingTimerText.text =
                hidingText;
        }

        if (
            seekerRightWaitingTimerText !=
            null
        )
        {
            seekerRightWaitingTimerText.text =
                hidingText;
        }

        // ========================================================
        // SEEKER SEARCH TIMER
        // ========================================================

        string seekerTimeText =
            FormatTime(
                networkState
                    .GetSeekerRemainingTime()
            );

        if (
            seekerTimerText != null
        )
        {
            seekerTimerText.text =
                seekerTimeText;
        }

        if (
            seekerLeftTimerText !=
            null
        )
        {
            seekerLeftTimerText.text =
                seekerTimeText;
        }

        if (
            seekerRightTimerText !=
            null
        )
        {
            seekerRightTimerText.text =
                seekerTimeText;
        }

        // ========================================================
        // HIDER REVEAL TIMER
        // ========================================================

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

        // ========================================================
        // STEREO SEEKER REVEAL TIMER
        // ========================================================

        if (
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

            string revealText =
                FormatTime(
                    revealTime
                );

            if (
                seekerLeftRevealTimerText !=
                null
            )
            {
                seekerLeftRevealTimerText.text =
                    revealText;
            }

            if (
                seekerRightRevealTimerText !=
                null
            )
            {
                seekerRightRevealTimerText.text =
                    revealText;
            }
        }
    }

    // ============================================================
    // MECCHA FOUND
    // ============================================================

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

    // ============================================================
    // SHOW MECCHA FOUND POPUP
    // ============================================================

    private void ShowMecchaFoundPopup()
    {
        string message =
            networkState.MecchasFound +
            " Meccha Found, " +
            networkState.RemainingMecchas +
            " Meccha Remaining";

        MultiplayerRole role =
            roleController.GetLocalRole();

        // --------------------------------------------------------
        // HIDER
        // --------------------------------------------------------

        if (
            role ==
            MultiplayerRole.Hider
        )
        {
            if (
                hiderFoundPanel !=
                null
            )
            {
                hiderFoundPanel
                    .SetActive(true);
            }

            if (
                hiderFoundText !=
                null
            )
            {
                hiderFoundText.text =
                    message;
            }
        }

        // --------------------------------------------------------
        // SEEKER
        // --------------------------------------------------------

        if (
            role ==
            MultiplayerRole.Seeker
        )
        {
            // Old single-screen popup OFF.
            SetPanel(
                seekerFoundPanel,
                false
            );

            // Left eye.
            if (
                seekerLeftFoundPanel !=
                null
            )
            {
                seekerLeftFoundPanel
                    .SetActive(true);
            }

            if (
                seekerLeftFoundText !=
                null
            )
            {
                seekerLeftFoundText.text =
                    message;
            }

            // Right eye.
            if (
                seekerRightFoundPanel !=
                null
            )
            {
                seekerRightFoundPanel
                    .SetActive(true);
            }

            if (
                seekerRightFoundText !=
                null
            )
            {
                seekerRightFoundText.text =
                    message;
            }
        }

        foundPopupEndTime =
            Time.time +
            foundPopupDuration;
    }

    // ============================================================
    // HIDE FOUND POPUP
    // ============================================================

    private void HideFoundPopup()
    {
        if (
            hiderFoundPanel !=
            null
        )
        {
            hiderFoundPanel
                .SetActive(false);
        }

        if (
            seekerFoundPanel !=
            null
        )
        {
            seekerFoundPanel
                .SetActive(false);
        }

        if (
            seekerLeftFoundPanel !=
            null
        )
        {
            seekerLeftFoundPanel
                .SetActive(false);
        }

        if (
            seekerRightFoundPanel !=
            null
        )
        {
            seekerRightFoundPanel
                .SetActive(false);
        }
    }

    // ============================================================
    // PHASE CHANGED
    // ============================================================

    private void HandlePhaseChanged(
        MultiplayerRole role,
        MultiplayerPhase oldPhase,
        MultiplayerPhase newPhase
    )
    {
        // --------------------------------------------------------
        // HIDING -> SEARCHING
        // --------------------------------------------------------

        if (
            oldPhase ==
                MultiplayerPhase.HiderHiding &&
            newPhase ==
                MultiplayerPhase.SeekerSearching
        )
        {
            StartCoroutine(
                ShowHidingEndedPanel(
                    role
                )
            );
        }

        // --------------------------------------------------------
        // ENTER REVEAL
        // --------------------------------------------------------

        if (
            newPhase ==
            MultiplayerPhase.Reveal
        )
        {
            HideFoundPopup();

            Debug.Log(
                "[StereoUI] " +
                "Network-wide Reveal Phase started."
            );
        }

        // --------------------------------------------------------
        // ENTER RESULT
        // --------------------------------------------------------

        if (
            newPhase ==
            MultiplayerPhase.Result
        )
        {
            HideFoundPopup();

            Debug.Log(
                "[StereoUI] " +
                "Network-wide Result Phase started. " +
                "Result = " +
                networkState.Result
            );
        }

        // --------------------------------------------------------
        // NEW ROUND
        // --------------------------------------------------------

        if (
            newPhase ==
            MultiplayerPhase.WaitingForPlayers
        )
        {
            previousMecchasFound =
                0;

            HideFoundPopup();

            HideStereoSetupPanels();

            HideStereoGameplayPanels();

            SetPanel(
                hiderRevealPanel,
                false
            );

            SetPanel(
                hiderResultPanel,
                false
            );
        }
    }

    // ============================================================
    // HIDING ENDED PANEL
    // ============================================================

    private IEnumerator
        ShowHidingEndedPanel(
            MultiplayerRole role
        )
    {
        if (
            role ==
            MultiplayerRole.Hider
        )
        {
            // Hider stays full-screen.
            if (
                hidingEndedPanel !=
                null
            )
            {
                hidingEndedPanel
                    .SetActive(true);
            }

            yield return
                new WaitForSeconds(
                    10f
                );

            SetPanel(
                hidingEndedPanel,
                false
            );

            yield break;
        }

        if (
            role ==
            MultiplayerRole.Seeker
        )
        {
            // Left eye.
            SetPanel(
                seekerLeftHidingEndedPanel,
                true
            );

            // Right eye.
            SetPanel(
                seekerRightHidingEndedPanel,
                true
            );

            yield return
                new WaitForSeconds(
                    10f
                );

            SetPanel(
                seekerLeftHidingEndedPanel,
                false
            );

            SetPanel(
                seekerRightHidingEndedPanel,
                false
            );
        }
    }

    // ============================================================
    // STEREO COORDINATES SET
    // ============================================================
    //
    // Called by SharedAROriginManager when the Seeker's
    // InitialAprilTag coordinates have been established.
    //

    public void ShowSeekerCoordinatesSet(
        float duration
    )
    {
        if (
            roleController == null ||
            roleController.GetLocalRole() !=
                MultiplayerRole.Seeker
        )
        {
            return;
        }

        StartCoroutine(
            ShowSeekerCoordinatesSetRoutine(
                duration
            )
        );
    }

    private IEnumerator
        ShowSeekerCoordinatesSetRoutine(
            float duration
        )
    {
        SetPanel(
            seekerLeftCoordinatesSetPanel,
            true
        );

        SetPanel(
            seekerRightCoordinatesSetPanel,
            true
        );

        yield return
            new WaitForSeconds(
                duration
            );

        SetPanel(
            seekerLeftCoordinatesSetPanel,
            false
        );

        SetPanel(
            seekerRightCoordinatesSetPanel,
            false
        );
    }

    // ============================================================
    // STEREO COORDINATES UPDATED
    // ============================================================
    //
    // Called by SharedAROriginManager after an origin RESCAN.
    //

    public void ShowSeekerCoordinatesUpdated(
        float duration
    )
    {
        if (
            roleController == null ||
            roleController.GetLocalRole() !=
                MultiplayerRole.Seeker
        )
        {
            return;
        }

        StartCoroutine(
            ShowSeekerCoordinatesUpdatedRoutine(
                duration
            )
        );
    }

    private IEnumerator
        ShowSeekerCoordinatesUpdatedRoutine(
            float duration
        )
    {
        SetPanel(
            seekerLeftCoordinatesUpdatedPanel,
            true
        );

        SetPanel(
            seekerRightCoordinatesUpdatedPanel,
            true
        );

        yield return
            new WaitForSeconds(
                duration
            );

        SetPanel(
            seekerLeftCoordinatesUpdatedPanel,
            false
        );

        SetPanel(
            seekerRightCoordinatesUpdatedPanel,
            false
        );
    }

    // ============================================================
    // HIDE ALL STEREO SETUP PANELS
    // ============================================================

    private void HideStereoSetupPanels()
    {
        SetPanel(
            seekerLeftScanOriginPanel,
            false
        );

        SetPanel(
            seekerRightScanOriginPanel,
            false
        );

        SetPanel(
            seekerLeftCoordinatesSetPanel,
            false
        );

        SetPanel(
            seekerRightCoordinatesSetPanel,
            false
        );

        SetPanel(
            seekerLeftCoordinatesUpdatedPanel,
            false
        );

        SetPanel(
            seekerRightCoordinatesUpdatedPanel,
            false
        );

        SetPanel(
            seekerLeftWaitingPanel,
            false
        );

        SetPanel(
            seekerRightWaitingPanel,
            false
        );

        SetPanel(
            seekerLeftHidingEndedPanel,
            false
        );

        SetPanel(
            seekerRightHidingEndedPanel,
            false
        );
    }

    // ============================================================
    // HIDE ALL STEREO GAMEPLAY PANELS
    // ============================================================

    private void HideStereoGameplayPanels()
    {
        SetPanel(
            seekerLeftGamePanel,
            false
        );

        SetPanel(
            seekerRightGamePanel,
            false
        );

        SetPanel(
            seekerLeftRevealPanel,
            false
        );

        SetPanel(
            seekerRightRevealPanel,
            false
        );

        SetPanel(
            seekerLeftResultPanel,
            false
        );

        SetPanel(
            seekerRightResultPanel,
            false
        );

        SetPanel(
            seekerLeftFoundPanel,
            false
        );

        SetPanel(
            seekerRightFoundPanel,
            false
        );
    }

    // ============================================================
    // RESULT MESSAGE
    // ============================================================

    private string GetResultMessage(
        MultiplayerResult result
    )
    {
        switch (
            result
        )
        {
            case MultiplayerResult.SeekerWin:

                return
                    "Seekers WONN!!!";

            case MultiplayerResult.HiderWin:

                return
                    "Hiders WONN!!";

            case MultiplayerResult.Neither:

                return
                    "Neither Won - " +
                    "No Meccha Placed :(";

            default:

                return "";
        }
    }

    // ============================================================
    // FORMAT TIME
    // ============================================================

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

    // ============================================================
    // PANEL HELPER
    // ============================================================

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