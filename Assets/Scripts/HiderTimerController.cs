using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HiderTimerController : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip(
        "Total gameplay duration in seconds."
    )]
    public float totalTimeInSeconds = 120f;

    // UI REFERENCES

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image timerFillImage;
    public GameObject timeUpPanel;
    public GameObject pausePanel;

    // HUNTER REVEAL

    [Header("Hunter Reveal Phase")]
    [Tooltip(
        "Assign this ONLY for Hunter gameplay. " +
        "Leave empty for Hider gameplay."
    )]
    public HunterRevealPhaseController
        hunterRevealPhaseController;

    [Header("Hunter Score")]
    public HunterMecchaScoreController
    hunterScoreController;

    // UI BUTTONS

    [Header("UI Buttons")]
    public Button pauseButton;
    public Button resumeButton;

    // INTERNAL STATE

    private float currentTime;
    private bool isTimerRunning = false;
    private bool timerCompleted = false;

    private void Start()
    {
        // CONNECT PAUSE BUTTON

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(
                PauseGame
            );
        }

        // CONNECT RESUME BUTTON

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(
                ResumeGame
            );
        }
    }

    private void OnEnable()
    {
        StartTimer();
    }

    private void Update()
    {
        if (!isTimerRunning)
        {
            return;
        }

        // COUNT DOWN

        currentTime -= Time.deltaTime;

        // TIMER COMPLETE

        if (
            currentTime <= 0f
        )
        {
            currentTime = 0f;
            isTimerRunning = false;

            if (!timerCompleted)
            {
                timerCompleted = true;
                OnTimerComplete();
            }
        }
        UpdateTimerUI();
    }

    // START TIMER

    public void StartTimer()
    {
        currentTime = totalTimeInSeconds;
        isTimerRunning = true;
        timerCompleted = false;

        // HIDE NORMAL END PANELS

        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(
                false
            );
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(
                false
            );
        }

        if (
            hunterScoreController != null
        )
        {
            hunterScoreController.ResetScore();
        }
         // Added
        if (hunterRevealPhaseController != null)
        {
            hunterRevealPhaseController.ResetRevealState();
        }
        UpdateTimerUI();
    }

    // UPDATE TIMER UI

    private void UpdateTimerUI()
    {
        int minutes =
            Mathf.FloorToInt(
                currentTime / 60f
            );

        int seconds =
            Mathf.FloorToInt(
                currentTime % 60f
            );

        // TIMER TEXT

        if (timerText != null)
        {
            timerText.text =
                string.Format(
                    "{0:00}:{1:00}",
                    minutes,
                    seconds
                );
        }

        // TIMER RING

        if (
            timerFillImage != null &&
            totalTimeInSeconds > 0f
        )
        {
            timerFillImage.fillAmount =
                Mathf.Clamp01(
                    currentTime /
                    totalTimeInSeconds
                );
        }
    }

    // TIMER COMPLETE

    private void OnTimerComplete()
    {
        // HUNTER MODE

        if (
            hunterRevealPhaseController != null
        )
        {
            Debug.Log(
                "[Timer] Hunter timer finished. " +
                "Starting Reveal Phase."
            );

            if (timeUpPanel != null)
            {
                timeUpPanel.SetActive(
                    false
                );
            }

            // START 30 SECOND REVEAL

            hunterRevealPhaseController
                .BeginRevealPhase();

            return;
        }

        // HIDER MODE

        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(
                true
            );
        }

        Debug.Log(
            "[Timer] Hiding time is over!"
        );
    }

    // PAUSE

    public void PauseGame()
    {
        isTimerRunning = false;

        // FREEZE GAME TIME

        Time.timeScale = 0f;

        // SHOW PAUSE PANEL

        if (pausePanel != null)
        {
            pausePanel.SetActive(
                true
            );
        }
    }

    // RESUME

    public void ResumeGame()
    {
        // RESTORE TIME

        Time.timeScale = 1f;

        // HIDE PAUSE PANEL

        if (pausePanel != null)
        {
            pausePanel.SetActive(
                false
            );
        }

        // RESUME TIMER

        if (currentTime > 0f)
        {
            isTimerRunning =
                true;
        }
    }

    // GET CURRENT TIME

    public float GetCurrentTime()
    {
        return currentTime;
    }

    // GET TIMER STATE

    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }

    // RESET

    public void ResetTimer()
    {
        StartTimer();
    }

    // DISABLE

    private void OnDisable()
    {
        Time.timeScale =
            1f;
    }

    // DESTROY

    private void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(
                PauseGame
            );
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(
                ResumeGame
            );
        }
    }
}