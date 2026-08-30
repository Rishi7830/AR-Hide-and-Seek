using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HiderTimerController : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Total hiding duration in seconds.")]
    public float totalTimeInSeconds = 120f; // 2:00 minutes

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Image timerFillImage;
    public GameObject timeUpPanel;
    public GameObject pausePanel;

    [Header("UI Buttons")]
    public Button pauseButton;
    public Button resumeButton;

    private float currentTime;
    private bool isTimerRunning = false;

    private void Start()
    {
        // Bind dynamic button listeners
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
    }

    private void OnEnable()
    {
        StartTimer();
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isTimerRunning = false;
            OnTimerComplete();
        }

        UpdateTimerUI();
    }

    public void StartTimer()
    {
        currentTime = totalTimeInSeconds;
        isTimerRunning = true;

        if (timeUpPanel != null) timeUpPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = currentTime / totalTimeInSeconds;
        }
    }

    private void OnTimerComplete()
    {
        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(true);
        }

        Debug.Log("Hiding time is over!");
    }

    public void PauseGame()
    {
        isTimerRunning = false;

        // Freezes physics and animation time
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        // Restores game time progression
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (currentTime > 0f)
        {
            isTimerRunning = true;
        }
    }

    private void OnDisable()
    {
        // Ensure timeScale is always reset if switching panels
        Time.timeScale = 1f;
    }
}