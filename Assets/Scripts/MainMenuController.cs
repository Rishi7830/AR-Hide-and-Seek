using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Navigation Mode")]
    [Tooltip("If true, switches UI panels within the same scene. If false, loads a new scene.")]
    public bool usePanelSwapping = true;

    [Header("Panel Swapping References (Option A)")]
    public GameObject mainMenuPanel;
    public GameObject hiderGamePanel;
    public GameObject hunterGamePanel; // Optional placeholder for future implementation

    [Header("Scene Management Names (Option B)")]
    public string hiderSceneName = "HiderScene";
    public string hunterSceneName = "HunterScene";

    [Header("UI Buttons")]
    public Button hiderButton;
    public Button hunterButton;

    private void Start()
    {
        // Bind button listeners dynamically
        if (hiderButton != null)
            hiderButton.onClick.AddListener(OnHiderButtonClicked);

        if (hunterButton != null)
            hunterButton.onClick.AddListener(OnHunterButtonClicked);

        // Ensure proper starting visibility if using panel swapping
        if (usePanelSwapping)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (hiderGamePanel != null) hiderGamePanel.SetActive(false);
            if (hunterGamePanel != null) hunterGamePanel.SetActive(false);
        }
    }

    public void OnHiderButtonClicked()
    {
        if (usePanelSwapping)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hiderGamePanel != null) hiderGamePanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene(hiderSceneName);
        }
    }

    public void OnHunterButtonClicked()
    {
        if (usePanelSwapping)
        {
            // Placeholder feedback for Hunter mode implementation
            Debug.Log("Hunter mode selected.");
        }
        else
        {
            if (!string.IsNullOrEmpty(hunterSceneName))
            {
                SceneManager.LoadScene(hunterSceneName);
            }
        }
    }

    // Call this from a back/exit button inside the Hider game view to return to main menu
    public void ReturnToMainMenu()
    {
        if (usePanelSwapping)
        {
            if (hiderGamePanel != null) hiderGamePanel.SetActive(false);
            if (hunterGamePanel != null) hunterGamePanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}