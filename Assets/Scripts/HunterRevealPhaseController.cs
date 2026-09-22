using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class HunterRevealPhaseController : MonoBehaviour
{
    // =============================================================
    // REVEAL UI
    // =============================================================

    [Header("Reveal UI")]

    [SerializeField]
    private GameObject revealPhasePanel;

    [SerializeField]
    private Image timerRing;

    [SerializeField]
    private TMP_Text timerText;

    [SerializeField]
    private Button shootButton;

    // =============================================================
    // FINAL RESULT UI
    // =============================================================

    [Header("Final Result UI")]

    [SerializeField]
    private GameObject resultPanel;

    [SerializeField]
    private TMP_Text resultText;

    [Tooltip(
        "How long the final result panel stays visible."
    )]
    [SerializeField]
    private float resultDuration = 10f;

    // =============================================================
    // RETURN TO MAIN MENU
    // =============================================================

    [Header("Return To Main Menu")]

    [SerializeField]
    private GameObject hunterGamePanel;

    [SerializeField]
    private GameObject mainMenuPanel;

    [Tooltip(
        "Optional. Disables the hand tracking visualizer "
        + "when returning to the Main Menu."
    )]
    [SerializeField]
    private GameObject handTrackingCanvas;

    [Tooltip(
        "Optional. Removes the spawned Hunter gun rack "
        + "when the Hunter game ends."
    )]
    [SerializeField]
    private HunterGunManager gunManager;

    // =============================================================
    // SCORE
    // =============================================================

    [Header("Hunter Score")]

    [SerializeField]
    private HunterMecchaScoreController scoreController;

    // =============================================================
    // REVEAL SETTINGS
    // =============================================================

    [Header("Reveal Settings")]

    [Tooltip(
        "Duration of the Reveal Phase in seconds."
    )]
    [SerializeField]
    private float revealDuration = 30f;

    // =============================================================
    // PULSE SETTINGS
    // =============================================================

    [Header("Meccha Pulse")]

    [Tooltip(
        "Number of red-white pulses per second."
    )]
    [SerializeField]
    private float pulseSpeed = 2f;

    // =============================================================
    // STATE
    // =============================================================

    private float remainingTime = 0f;

    private bool revealActive = false;

    private bool finalResultShown = false;

    private HunterMecchaTarget[] mecchaTargets;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        // ---------------------------------------------------------
        // FIND SCORE CONTROLLER
        // ---------------------------------------------------------

        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<
                    HunterMecchaScoreController>();
        }

        // ---------------------------------------------------------
        // FIND GUN MANAGER
        // ---------------------------------------------------------

        if (gunManager == null)
        {
            gunManager =
                FindFirstObjectByType<
                    HunterGunManager>();
        }

        // ---------------------------------------------------------
        // INITIAL UI STATE
        // ---------------------------------------------------------

        ResetRevealState();
    }

    // =============================================================
    // RESET REVEAL STATE
    //
    // Call this when a NEW Hunter round starts.
    // =============================================================

    public void ResetRevealState()
    {
        // ---------------------------------------------------------
        // RESET INTERNAL STATE
        // ---------------------------------------------------------

        remainingTime = 0f;

        revealActive = false;

        finalResultShown = false;

        // ---------------------------------------------------------
        // HIDE REVEAL PANEL
        // ---------------------------------------------------------

        if (revealPhasePanel != null)
        {
            revealPhasePanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // HIDE RESULT PANEL
        // ---------------------------------------------------------

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // ENABLE SHOOTING FOR A NEW HUNTER ROUND
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = true;
        }

        // ---------------------------------------------------------
        // RESET TIMER UI
        // ---------------------------------------------------------

        if (timerRing != null)
        {
            timerRing.fillAmount = 1f;
        }

        if (timerText != null)
        {
            timerText.text = "30";
        }

        Debug.Log(
            "[HunterReveal] Reveal state reset."
        );
    }

    // =============================================================
    // FIND MECCHAS
    // =============================================================

    private void FindMecchaTargets()
    {
        mecchaTargets =
            FindObjectsByType<
                HunterMecchaTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
    }

    // =============================================================
    // BEGIN REVEAL PHASE
    // =============================================================

    public void BeginRevealPhase()
    {
        // ---------------------------------------------------------
        // PREVENT DUPLICATE END STATES
        // ---------------------------------------------------------

        if (finalResultShown)
        {
            return;
        }

        // ---------------------------------------------------------
        // FIND SCORE CONTROLLER IF NECESSARY
        // ---------------------------------------------------------

        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<
                    HunterMecchaScoreController>();
        }

        // =========================================================
        // CASE 1:
        // ALL MECCHAS WERE ALREADY FOUND
        //
        // Therefore there is NO Reveal Phase.
        // =========================================================

        if (
            scoreController != null &&
            scoreController.AllMecchasFound()
        )
        {
            Debug.Log(
                "[HunterReveal] " +
                "All Mecchas found. " +
                "Skipping Reveal Phase."
            );

            ShowSeekersWin();

            return;
        }

        // =========================================================
        // CASE 2:
        // MECCHAS ARE STILL REMAINING
        // =========================================================

        FindMecchaTargets();

        revealActive = true;

        remainingTime =
            revealDuration;

        // ---------------------------------------------------------
        // DISABLE SHOOTING
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        // ---------------------------------------------------------
        // SHOW REVEAL PANEL
        // ---------------------------------------------------------

        if (revealPhasePanel != null)
        {
            revealPhasePanel.SetActive(true);
        }

        // ---------------------------------------------------------
        // HIDE FINAL RESULT PANEL
        // ---------------------------------------------------------

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // RESET REVEAL TIMER
        // ---------------------------------------------------------

        if (timerRing != null)
        {
            timerRing.fillAmount = 1f;
        }

        UpdateTimerUI();

        // ---------------------------------------------------------
        // START PULSE ON UNFOUND MECCHAS
        // ---------------------------------------------------------

        if (mecchaTargets != null)
        {
            foreach (
                HunterMecchaTarget target
                in mecchaTargets
            )
            {
                if (target == null)
                {
                    continue;
                }

                if (!target.IsHit())
                {
                    target.StartRevealPulse();
                }
            }
        }

        Debug.Log(
            "[HunterReveal] " +
            "Reveal Phase started. " +
            "Shooting disabled."
        );
    }

    // =============================================================
    // UPDATE
    // =============================================================

    private void Update()
    {
        if (!revealActive)
        {
            return;
        }

        // ---------------------------------------------------------
        // COUNTDOWN
        // ---------------------------------------------------------

        remainingTime -=
            Time.deltaTime;

        if (remainingTime < 0f)
        {
            remainingTime = 0f;
        }

        // ---------------------------------------------------------
        // UPDATE PULSE
        // ---------------------------------------------------------

        UpdateMecchaPulse();

        // ---------------------------------------------------------
        // UPDATE TIMER UI
        // ---------------------------------------------------------

        UpdateTimerUI();

        // ---------------------------------------------------------
        // END REVEAL WHEN TIMER FINISHES
        //
        // IMPORTANT:
        // We do NOT check whether all Mecchas were found here,
        // because shooting is disabled during Reveal.
        // ---------------------------------------------------------

        if (remainingTime <= 0f)
        {
            EndRevealPhase();
        }
    }

    // =============================================================
    // UPDATE MECCHA PULSE
    // =============================================================

    private void UpdateMecchaPulse()
    {
        if (mecchaTargets == null)
        {
            return;
        }

        // ---------------------------------------------------------
        // CREATE SMOOTH 0 -> 1 -> 0 PULSE
        // ---------------------------------------------------------

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

        // ---------------------------------------------------------
        // APPLY TO UNFOUND MECCHAS ONLY
        // ---------------------------------------------------------

        foreach (
            HunterMecchaTarget target
            in mecchaTargets
        )
        {
            if (target == null)
            {
                continue;
            }

            if (!target.IsHit())
            {
                target.UpdateRevealPulse(
                    pulse
                );
            }
        }
    }

    // =============================================================
    // UPDATE TIMER UI
    // =============================================================

    private void UpdateTimerUI()
    {
        // ---------------------------------------------------------
        // TIMER RING
        // ---------------------------------------------------------

        if (
            timerRing != null &&
            revealDuration > 0f
        )
        {
            timerRing.fillAmount =
                Mathf.Clamp01(
                    remainingTime /
                    revealDuration
                );
        }

        // ---------------------------------------------------------
        // TIMER TEXT
        // ---------------------------------------------------------

        if (timerText != null)
        {
            int seconds =
                Mathf.CeilToInt(
                    Mathf.Max(
                        remainingTime,
                        0f
                    )
                );

            timerText.text =
                seconds.ToString();
        }
    }

    // =============================================================
    // END REVEAL
    // =============================================================

    public void EndRevealPhase()
    {
        if (!revealActive)
        {
            return;
        }

        // ---------------------------------------------------------
        // STOP REVEAL STATE
        // ---------------------------------------------------------

        revealActive = false;

        remainingTime = 0f;

        // ---------------------------------------------------------
        // STOP MECCHA PULSES
        // ---------------------------------------------------------

        if (mecchaTargets != null)
        {
            foreach (
                HunterMecchaTarget target
                in mecchaTargets
            )
            {
                if (target == null)
                {
                    continue;
                }

                if (!target.IsHit())
                {
                    target.EndRevealPulse();
                }
            }
        }

        // ---------------------------------------------------------
        // HIDE REVEAL PANEL
        // ---------------------------------------------------------

        if (revealPhasePanel != null)
        {
            revealPhasePanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // SHOOT REMAINS DISABLED
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        // ---------------------------------------------------------
        // MECCHAS REMAINED UNFOUND
        //
        // Because shooting was disabled throughout Reveal,
        // if we reach here with remaining Mecchas, Hiders win.
        // ---------------------------------------------------------

        ShowHidersWin();
    }

    // =============================================================
    // SEEKERS WIN
    // =============================================================

    private void ShowSeekersWin()
    {
        // ---------------------------------------------------------
        // MAKE SURE SHOOTING IS DISABLED
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        // ---------------------------------------------------------
        // MAKE SURE REVEAL IS NOT ACTIVE
        // ---------------------------------------------------------

        StopReveal();

        // ---------------------------------------------------------
        // SHOW RESULT
        // ---------------------------------------------------------

        ShowFinalResult(
            "Seekers WONN!!"
        );

        Debug.Log(
            "[HunterGame] " +
            "SEEKERS WON!"
        );
    }

    // =============================================================
    // HIDERS WIN
    // =============================================================

    private void ShowHidersWin()
    {
        // ---------------------------------------------------------
        // SHOOTING DISABLED
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        // ---------------------------------------------------------
        // MAKE SURE REVEAL IS STOPPED
        // ---------------------------------------------------------

        StopReveal();

        // ---------------------------------------------------------
        // SHOW RESULT
        // ---------------------------------------------------------

        ShowFinalResult(
            "Hiders WONN!!"
        );

        Debug.Log(
            "[HunterGame] " +
            "HIDERS WON!"
        );
    }

    // =============================================================
    // STOP REVEAL
    // =============================================================

    private void StopReveal()
    {
        revealActive = false;

        // ---------------------------------------------------------
        // STOP ALL REMAINING PULSES
        // ---------------------------------------------------------

        if (mecchaTargets != null)
        {
            foreach (
                HunterMecchaTarget target
                in mecchaTargets
            )
            {
                if (target == null)
                {
                    continue;
                }

                if (!target.IsHit())
                {
                    target.EndRevealPulse();
                }
            }
        }

        // ---------------------------------------------------------
        // HIDE REVEAL PANEL
        // ---------------------------------------------------------

        if (revealPhasePanel != null)
        {
            revealPhasePanel.SetActive(false);
        }
    }

    // =============================================================
    // FINAL RESULT
    // =============================================================

    private void ShowFinalResult(
        string message
    )
    {
        // ---------------------------------------------------------
        // PREVENT DUPLICATES
        // ---------------------------------------------------------

        if (finalResultShown)
        {
            return;
        }

        finalResultShown = true;

        // ---------------------------------------------------------
        // RESET TIME SCALE
        // ---------------------------------------------------------

        Time.timeScale = 1f;

        // ---------------------------------------------------------
        // SET RESULT TEXT
        // ---------------------------------------------------------

        if (resultText != null)
        {
            resultText.text =
                message;
        }

        // ---------------------------------------------------------
        // SHOW RESULT PANEL
        // ---------------------------------------------------------

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        // ---------------------------------------------------------
        // START FINAL RESULT TIMER
        // ---------------------------------------------------------

        StartCoroutine(
            ReturnToMainMenuAfterResult()
        );
    }

    // =============================================================
    // RETURN TO MAIN MENU
    // =============================================================

    private IEnumerator
        ReturnToMainMenuAfterResult()
    {
        // ---------------------------------------------------------
        // WAIT 10 SECONDS
        // ---------------------------------------------------------

        yield return new WaitForSeconds(
            resultDuration
        );

        // ---------------------------------------------------------
        // HIDE RESULT PANEL
        // ---------------------------------------------------------

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // HIDE HUNTER GAME PANEL
        // ---------------------------------------------------------

        if (hunterGamePanel != null)
        {
            hunterGamePanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // DISABLE HAND TRACKING
        // ---------------------------------------------------------

        if (handTrackingCanvas != null)
        {
            handTrackingCanvas.SetActive(false);
        }

        // ---------------------------------------------------------
        // RESET / REMOVE GUN RACK
        // ---------------------------------------------------------

        if (gunManager != null)
        {
            gunManager.ResetGunRack();
        }

        // ---------------------------------------------------------
        // SHOW MAIN MENU
        // ---------------------------------------------------------

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        // ---------------------------------------------------------
        // RESET RESULT STATE
        // ---------------------------------------------------------

        finalResultShown = false;

        Debug.Log(
            "[HunterGame] " +
            "Returned to Main Menu."
        );
    }

    // =============================================================
    // PUBLIC STATE
    // =============================================================

    public bool IsRevealActive()
    {
        return revealActive;
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }
}