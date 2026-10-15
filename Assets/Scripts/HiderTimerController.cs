using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HiderTimerController : MonoBehaviour
{
    public enum TimerMode
    {
        Local,
        NetworkHider,
        NetworkSeeker
    }

    [Header("Timer Settings")]
    [Tooltip(
        "Total gameplay duration in seconds."
    )]
    public float totalTimeInSeconds = 120f;

    [Header("Timer Mode")]
    [Tooltip(
        "Local = original single-phone timer. " +
        "NetworkHider = Photon synchronized Hider timer. " +
        "NetworkSeeker = Photon synchronized Seeker timer."
    )]
    [SerializeField]
    private TimerMode timerMode =
        TimerMode.Local;

    [Header("Multiplayer")]
    [Tooltip(
        "Assign NetworkGameState only when using a network timer."
    )]
    [SerializeField]
    private NetworkGameState networkGameState;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image timerFillImage;
    public GameObject timeUpPanel;
    public GameObject pausePanel;

    [Header("Hunter Reveal Phase")]
    [Tooltip(
        "Assign this ONLY for Hunter gameplay in single-phone mode. " +
        "Leave empty for Hider gameplay and multiplayer."
    )]
    public HunterRevealPhaseController
        hunterRevealPhaseController;

    [Header("Hunter Score")]
    public HunterMecchaScoreController
        hunterScoreController;

    [Header("UI Buttons")]
    public Button pauseButton;
    public Button resumeButton;

    private float currentTime;
    private bool isTimerRunning = false;
    private bool timerCompleted = false;

    private void Start()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(
                PauseGame
            );
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(
                ResumeGame
            );
        }
    }

    private void OnEnable()
    {
        if (
            timerMode ==
            TimerMode.Local
        )
        {
            StartTimer();
        }
        else
        {
            PrepareNetworkTimerUI();
        }
    }

    private void Update()
    {
        if (
            timerMode ==
            TimerMode.Local
        )
        {
            UpdateLocalTimer();
        }
        else
        {
            UpdateNetworkTimer();
        }
    }

    private void UpdateLocalTimer()
    {
        if (!isTimerRunning)
        {
            return;
        }

        // Count down the original single-phone timer.
        currentTime -= Time.deltaTime;

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

    private void UpdateNetworkTimer()
    {
        if (networkGameState == null || !networkGameState.IsNetworkReady)
        {
            return;
        }

        // Photon owns the actual timer.
        if (
            timerMode ==
            TimerMode.NetworkHider
        )
        {
            currentTime =
                networkGameState
                    .GetHiderRemainingTime();

            isTimerRunning =
                networkGameState.Phase ==
                MultiplayerPhase.HiderHiding &&
                !networkGameState
                    .HiderTimerPaused;
        }
        else if (
            timerMode ==
            TimerMode.NetworkSeeker
        )
        {
            currentTime =
                networkGameState
                    .GetSeekerRemainingTime();

            isTimerRunning =
                networkGameState.Phase ==
                MultiplayerPhase
                    .SeekerSearching;
        }

        // In multiplayer this script only displays time.
        // NetworkGameState decides when the phase finishes.
        UpdateTimerUI();
    }

    public void StartTimer()
    {
        if (
            timerMode !=
            TimerMode.Local
        )
        {
            PrepareNetworkTimerUI();

            return;
        }

        currentTime =
            totalTimeInSeconds;

        isTimerRunning =
            true;

        timerCompleted =
            false;

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
            hunterScoreController
                .ResetScore();
        }

        if (
            hunterRevealPhaseController != null
        )
        {
            hunterRevealPhaseController
                .ResetRevealState();
        }

        UpdateTimerUI();
    }

    private void PrepareNetworkTimerUI()
    {
        currentTime =
            totalTimeInSeconds;

        isTimerRunning =
            false;

        timerCompleted =
            false;

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

        UpdateTimerUI();
    }

    public void StopTimer()
    {
        if (
            timerMode ==
            TimerMode.Local
        )
        {
            isTimerRunning =
                false;

            Debug.Log(
                "[Timer] Local timer stopped early."
            );
        }
        else
        {
            // NetworkGameState owns multiplayer timers.
            Debug.Log(
                "[Timer] StopTimer ignored because " +
                "this timer is controlled by Photon."
            );
        }
    }

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

        if (timerText != null)
        {
            timerText.text =
                string.Format(
                    "{0:00}:{1:00}",
                    minutes,
                    seconds
                );
        }

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

    private void OnTimerComplete()
    {
        // Multiplayer completion is handled by NetworkGameState.
        if (
            timerMode !=
            TimerMode.Local
        )
        {
            return;
        }

        // Single-phone Hunter mode.
        if (
            hunterRevealPhaseController !=
            null
        )
        {
            Debug.Log(
                "[Timer] Hunter timer finished. " +
                "Starting Reveal Phase."
            );

            if (
                timeUpPanel != null
            )
            {
                timeUpPanel.SetActive(
                    false
                );
            }

            hunterRevealPhaseController
                .BeginRevealPhase();

            return;
        }

        // Single-phone Hider mode.
        if (
            timeUpPanel != null
        )
        {
            timeUpPanel.SetActive(
                true
            );
        }

        Debug.Log(
            "[Timer] Hiding time is over!"
        );
    }

    public void PauseGame()
    {
        // Original single-phone pause.
        if (
            timerMode ==
            TimerMode.Local
        )
        {
            isTimerRunning =
                false;

            Time.timeScale =
                0f;

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
                    true
                );
            }

            return;
        }

        // Only the Hider network timer can pause the hiding phase.
        if (
            timerMode ==
                TimerMode.NetworkHider &&
            networkGameState != null
        )
        {
            networkGameState
                .PauseHiderTimer();

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
                    true
                );
            }
        }
    }

    public void ResumeGame()
    {
        // Original single-phone resume.
        if (
            timerMode ==
            TimerMode.Local
        )
        {
            Time.timeScale =
                1f;

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
                    false
                );
            }

            if (
                currentTime > 0f
            )
            {
                isTimerRunning =
                    true;
            }

            return;
        }

        // Resume the synchronized Hider timer.
        if (
            timerMode ==
                TimerMode.NetworkHider &&
            networkGameState != null
        )
        {
            networkGameState
                .ResumeHiderTimer();

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
                    false
                );
            }
        }
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }

    public void ResetTimer()
    {
        if (
            timerMode ==
            TimerMode.Local
        )
        {
            StartTimer();
        }
        else
        {
            PrepareNetworkTimerUI();
        }
    }

    public bool IsUsingNetworkTimer()
    {
        return
            timerMode !=
            TimerMode.Local;
    }

    private void OnDisable()
    {
        // Only local single-phone mode uses Time.timeScale.
        if (
            timerMode ==
            TimerMode.Local
        )
        {
            Time.timeScale =
                1f;
        }
    }

    private void OnDestroy()
    {
        if (
            pauseButton != null
        )
        {
            pauseButton.onClick
                .RemoveListener(
                    PauseGame
                );
        }

        if (
            resumeButton != null
        )
        {
            resumeButton.onClick
                .RemoveListener(
                    ResumeGame
                );
        }
    }
}