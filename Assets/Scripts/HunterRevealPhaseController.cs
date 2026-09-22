using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    // =============================================================
    // REVEAL SETTINGS
    // =============================================================

    [Header("Reveal Settings")]

    [Tooltip(
        "Duration of the reveal phase in seconds."
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

    private float remainingTime;

    private bool revealActive =
        false;

    private HunterMecchaTarget[] mecchaTargets;

    // =============================================================
    // START
    // =============================================================

    private void Start()
    {
        // ---------------------------------------------------------
        // HIDE REVEAL PANEL
        // ---------------------------------------------------------

        if (revealPhasePanel != null)
        {
            revealPhasePanel.SetActive(
                false
            );
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
        // PREVENT DUPLICATES
        // ---------------------------------------------------------

        if (revealActive)
        {
            return;
        }

        // ---------------------------------------------------------
        // FIND CURRENT MECCHAS
        // ---------------------------------------------------------

        FindMecchaTargets();

        // ---------------------------------------------------------
        // INITIALISE REVEAL
        // ---------------------------------------------------------

        revealActive =
            true;

        remainingTime =
            revealDuration;

        // ---------------------------------------------------------
        // SHOW PANEL
        // ---------------------------------------------------------

        if (revealPhasePanel != null)
        {
            revealPhasePanel.SetActive(
                true
            );
        }

        // ---------------------------------------------------------
        // RESET TIMER RING
        // ---------------------------------------------------------

        if (timerRing != null)
        {
            timerRing.fillAmount =
                1f;
        }

        // ---------------------------------------------------------
        // UPDATE TEXT
        // ---------------------------------------------------------

        UpdateTimerUI();

        // ---------------------------------------------------------
        // START PULSE
        // ONLY MECCHAS THAT HAVE NOT BEEN SHOT
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
        // COUNT DOWN
        // ---------------------------------------------------------

        remainingTime -=
            Time.deltaTime;

        // ---------------------------------------------------------
        // PULSE
        // ---------------------------------------------------------

        UpdateMecchaPulse();

        // ---------------------------------------------------------
        // UI
        // ---------------------------------------------------------

        UpdateTimerUI();

        // ---------------------------------------------------------
        // END
        // ---------------------------------------------------------

        if (
            remainingTime <= 0f
        )
        {
            remainingTime =
                0f;

            EndRevealPhase();
        }
    }

    // =============================================================
    // MECCHA PULSE
    // =============================================================

    private void UpdateMecchaPulse()
    {
        if (
            mecchaTargets == null
        )
        {
            return;
        }

        // ---------------------------------------------------------
        // CREATE 0 ? 1 ? 0 WAVE
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
        // UPDATE EVERY UNHIT MECCHA
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
    // TIMER UI
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

        revealActive =
            false;

        // ---------------------------------------------------------
        // STOP PULSING
        // ---------------------------------------------------------

        if (
            mecchaTargets != null
        )
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

                // -------------------------------------------------
                // ONLY RESET MECCHAS THAT WERE NOT SHOT
                // -------------------------------------------------

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
            revealPhasePanel.SetActive(
                false
            );
        }

        Debug.Log(
            "[HunterReveal] " +
            "Reveal Phase finished."
        );
    }

    // =============================================================
    // CHECK REVEAL STATE
    // =============================================================

    public bool IsRevealActive()
    {
        return revealActive;
    }

    // =============================================================
    // GET REMAINING TIME
    // =============================================================

    public float GetRemainingTime()
    {
        return remainingTime;
    }
}