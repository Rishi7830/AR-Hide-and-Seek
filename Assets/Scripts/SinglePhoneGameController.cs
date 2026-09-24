using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SinglePhoneGameController : MonoBehaviour
{
    public static SinglePhoneGameController Instance;

    public enum GamePhase
    {
        MainMenu,
        HiderHiding,
        SwitchingToHunter,
        HunterSearching
    }

    [Header("Timing")]
    [SerializeField]
    private float hidingDuration = 120f;

    [SerializeField]
    private float switchDuration = 20f;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject hiderGamePanel;
    public GameObject hunterGamePanel;
    public GameObject hunterSwitchPanel;

    [Header("Switch Panel UI")]
    public TMP_Text switchMessageText;
    public TMP_Text switchCountdownText;

    [Header("Hunter UI")]
    public TMP_Text remainingMecchaText;

    [Header("Existing Managers")]
    public SpawnMeccha spawnMeccha;
    public MecchaFineTuneController fineTuneController;
    public MecchaCounterManager mecchaCounterManager;

    [Header("AR References")]
    public ARPlaneManager arPlaneManager;
    public GameObject handTrackingCanvas;

    [Header("MediaPipe Hand Tracking")]
    [SerializeField]
    private GameObject mediaPipeHandManager;

    public GamePhase CurrentPhase { get; private set; }

    private Coroutine gameCoroutine;

    private void Awake()
    {
        Instance = this;
        CurrentPhase = GamePhase.MainMenu;
    }

    private void Start()
    {
        ResetToMainMenu();
    }

    public void BeginHiderGame()
    {
        if (gameCoroutine != null)
        {
            StopCoroutine(gameCoroutine);
        }

        gameCoroutine =
            StartCoroutine(HiderGameRoutine());
    }

    private IEnumerator HiderGameRoutine()
    {
        CurrentPhase = GamePhase.HiderHiding;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (hiderGamePanel != null)
            hiderGamePanel.SetActive(true);

        if (hunterGamePanel != null)
            hunterGamePanel.SetActive(false);

        if (hunterSwitchPanel != null)
            hunterSwitchPanel.SetActive(false);

        if (arPlaneManager != null)
            arPlaneManager.enabled = true;

        if (mediaPipeHandManager != null)
            mediaPipeHandManager.SetActive(false);

        if (handTrackingCanvas != null)
            handTrackingCanvas.SetActive(false);

        SetHiderGameplayEnabled(true);

        // Your existing TimerRingBackground handles
        // the visible 120-second ring.
        yield return new WaitForSeconds(
            hidingDuration
        );

        BeginHunterSwitchPhase();
    }

    private void BeginHunterSwitchPhase()
    {
        if (gameCoroutine != null)
        {
            StopCoroutine(gameCoroutine);
        }

        gameCoroutine =
            StartCoroutine(
                HunterSwitchRoutine()
            );
    }

    private IEnumerator HunterSwitchRoutine()
    {
        CurrentPhase =
            GamePhase.SwitchingToHunter;

        SetHiderGameplayEnabled(false);

        if (hiderGamePanel != null)
            hiderGamePanel.SetActive(false);

        if (hunterGamePanel != null)
            hunterGamePanel.SetActive(false);

        if (hunterSwitchPanel != null)
            hunterSwitchPanel.SetActive(true);

        if (switchMessageText != null)
        {
            switchMessageText.text =
                "Hiding time is over\n" +
                "Now switch to Hunter";
        }

        float remainingTime = switchDuration;

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            if (switchCountdownText != null)
            {
                switchCountdownText.text =
                    Mathf.CeilToInt(
                        remainingTime
                    ).ToString();
            }

            yield return null;
        }

        StartHunterPhase();
    }

    private void StartHunterPhase()
    {
        CurrentPhase =
            GamePhase.HunterSearching;

        if (hunterSwitchPanel != null)
            hunterSwitchPanel.SetActive(false);

        if (hiderGamePanel != null)
            hiderGamePanel.SetActive(false);

        if (hunterGamePanel != null)
            hunterGamePanel.SetActive(true);

        if (arPlaneManager != null)
            arPlaneManager.enabled = false;

        if (mediaPipeHandManager != null)
            mediaPipeHandManager.SetActive(true);

        if (handTrackingCanvas != null)
            handTrackingCanvas.SetActive(true);

        SetHiderGameplayEnabled(false);

        UpdateHunterMecchaCount();

        Debug.Log(
            "Hunter phase started. Meccha count: " +
            GetCurrentMecchaCount()
        );
    }

    private void SetHiderGameplayEnabled(
        bool enabled
    )
    {
        if (!enabled &&
            fineTuneController != null)
        {
            fineTuneController.CloseFineTunePanel();
        }

        if (spawnMeccha != null)
        {
            spawnMeccha.enabled = enabled;
        }

        if (fineTuneController != null)
        {
            fineTuneController.enabled = enabled;
        }
    }

    private int GetCurrentMecchaCount()
    {
        if (mecchaCounterManager == null)
            return 0;

        return mecchaCounterManager.CurrentCount;
    }

    private void UpdateHunterMecchaCount()
    {
        if (remainingMecchaText == null)
            return;

        int count =
            GetCurrentMecchaCount();

        remainingMecchaText.text =
            $"Meccha Remaining: {count}";
    }

    public void ResetToMainMenu()
    {
        CurrentPhase =
            GamePhase.MainMenu;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (hiderGamePanel != null)
            hiderGamePanel.SetActive(false);

        if (hunterGamePanel != null)
            hunterGamePanel.SetActive(false);

        if (hunterSwitchPanel != null)
            hunterSwitchPanel.SetActive(false);

        if (mediaPipeHandManager != null)
            mediaPipeHandManager.SetActive(false);

        if (handTrackingCanvas != null)
            handTrackingCanvas.SetActive(false);

        if (arPlaneManager != null)
            arPlaneManager.enabled = false;

        if (spawnMeccha != null)
            spawnMeccha.enabled = false;

        if (fineTuneController != null)
            fineTuneController.enabled = false;
    }
}