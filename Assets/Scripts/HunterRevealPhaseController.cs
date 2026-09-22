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

    [SerializeField]
    private float revealDuration = 30f;

    // =============================================================
    // PULSE SETTINGS
    // =============================================================

    [Header("Meccha Pulse")]

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
        // INITIAL STATE
        // ---------------------------------------------------------

        ResetRevealState();
    }

    // =============================================================
    // RESET REVEAL STATE
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
        // ENABLE SHOOTING FOR NEW HUNTER ROUND
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = true;
        }

        // ---------------------------------------------------------
        // RESET REVEAL TIMER UI
        // ---------------------------------------------------------

        if (timerRing != null)
        {
            timerRing.fillAmount = 1f;
        }

        if (timerText != null)
        {
            timerText.text =
                Mathf.CeilToInt(
                    revealDuration
                ).ToString();
        }
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
        // PREVENT DUPLICATE RESULT
        // ---------------------------------------------------------

        if (finalResultShown)
        {
            return;
        }

        // ---------------------------------------------------------
        // FIND SCORE CONTROLLER
        // ---------------------------------------------------------

        if (scoreController == null)
        {
            scoreController =
                FindFirstObjectByType<
                    HunterMecchaScoreController>();
        }

        // =========================================================
        // CASE 1:
        // NO MECCHAS WERE PLACED
        // =========================================================

        if (
            scoreController != null &&
            scoreController.HasNoMecchas()
        )
        {
            Debug.Log(
                "[HunterReveal] " +
                "No Mecchas were placed. " +
                "Skipping Reveal Phase."
            );

            ShowNoMecchaResult();

            return;
        }

        // =========================================================
        // CASE 2:
        // ALL MECCHAS WERE FOUND
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
        // CASE 3:
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
        // HIDE RESULT PANEL
        // ---------------------------------------------------------

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // RESET TIMER
        // ---------------------------------------------------------

        if (timerRing != null)
        {
            timerRing.fillAmount =
                1f;
        }

        UpdateTimerUI();

        // ---------------------------------------------------------
        // START REMAINING MECCHA PULSE
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
            "Reveal Phase started."
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
        // PULSE
        // ---------------------------------------------------------

        UpdateMecchaPulse();

        // ---------------------------------------------------------
        // TIMER UI
        // ---------------------------------------------------------

        UpdateTimerUI();

        // ---------------------------------------------------------
        // REVEAL ENDS
        // ---------------------------------------------------------

        if (
            remainingTime <= 0f
        )
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

        foreach (
            HunterMecchaTarget target
            in mecchaTargets
        )
        {
            if (target == null)
            {
                continue;
            }

            // -----------------------------------------------------
            // ONLY UNFOUND MECCHAS PULSE
            // -----------------------------------------------------

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
        // RING
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
        // TEXT
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

        revealActive = false;

        remainingTime = 0f;

        // ---------------------------------------------------------
        // STOP PULSE
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
        // SHOOTING STAYS DISABLED
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        // ---------------------------------------------------------
        // MECCHAS REMAIN
        // ---------------------------------------------------------

        ShowHidersWin();
    }

    // =============================================================
    // NO MECCHA RESULT
    // =============================================================

    private void ShowNoMecchaResult()
    {
        // ---------------------------------------------------------
        // MAKE SURE EVERYTHING IS STOPPED
        // ---------------------------------------------------------

        revealActive = false;

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        if (revealPhasePanel != null)
        {
            revealPhasePanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // SHOW RESULT PANEL
        // ---------------------------------------------------------

        ShowFinalResult(
            "Neither Won - No Meccha Placed :("
        );

        Debug.Log(
            "[HunterGame] " +
            "Neither team won because no Meccha was placed."
        );
    }

    // =============================================================
    // SEEKERS WIN
    // =============================================================

    private void ShowSeekersWin()
    {
        // ---------------------------------------------------------
        // DISABLE SHOOTING
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        // ---------------------------------------------------------
        // STOP REVEAL
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
        // DISABLE SHOOTING
        // ---------------------------------------------------------

        if (shootButton != null)
        {
            shootButton.interactable = false;
        }

        // ---------------------------------------------------------
        // STOP REVEAL
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
        // STOP PULSING
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
        // WAIT FOR RESULT DURATION
        // ---------------------------------------------------------

        yield return new WaitForSeconds(
            resultDuration
        );

        // ---------------------------------------------------------
        // HIDE RESULT
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
        // REMOVE GUN RACK
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
        // RESET INTERNAL STATE
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