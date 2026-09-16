using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Navigation Mode")]
    [Tooltip("If true, switches UI panels within the same scene. If false, loads a new scene.")]
    public bool usePanelSwapping = true;

    [Header("Panel Swapping References (Option A)")]
    public GameObject mainMenuPanel;
    public GameObject hiderGamePanel;
    public GameObject hunterGamePanel; // Optional placeholder for future implementation
    // public GameObject seekerGamePanel;

    [Header("Hunter Hand Tracking")]
    public GameObject handTrackingCanvas;

    [Header("AR Plane Detection")]
    public ARPlaneManager arPlaneManager;

    [Header("Scene Management Names (Option B)")]
    public string hiderSceneName = "HiderScene";
    public string hunterSceneName = "HunterScene";

    [Header("UI Buttons")]
    public Button hiderButton;
    public Button hunterButton;
    // public Button seekerButton;

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
            if (handTrackingCanvas != null)
                handTrackingCanvas.SetActive(false);
        }
        if (arPlaneManager != null)
        {
            arPlaneManager.enabled = false;
        }
    }

    private void ShowAllTrackedPlanes()
    {
        if (arPlaneManager == null)
            return;

        foreach (
            ARPlane plane
            in arPlaneManager.trackables
        )
        {
            if (plane != null)
            {
                plane.gameObject.SetActive(true);
            }
        }
    }

    public void OnHiderButtonClicked()
    {
        if (usePanelSwapping)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (hiderGamePanel != null) hiderGamePanel.SetActive(true);
            if (handTrackingCanvas != null)
                handTrackingCanvas.SetActive(false);
            if (arPlaneManager != null)
            {
                arPlaneManager.enabled = true;
                ShowAllTrackedPlanes();
            }

            Debug.Log(
            "[GameMode] Hider mode. " +
            "Plane detection ON, hand visualization OFF."
            );
        }
        else
        {
            SceneManager.LoadScene(hiderSceneName);
        }
    }

    private void HideAllTrackedPlanes()
    {
        if (arPlaneManager == null)
            return;

        foreach (
            ARPlane plane
            in arPlaneManager.trackables
        )
        {
            if (plane != null)
            {
                plane.gameObject.SetActive(false);
            }
        }
    }

    public void OnHunterButtonClicked()
    {
        if (usePanelSwapping)
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            if (hiderGamePanel != null)
                hiderGamePanel.SetActive(false);

            if (hunterGamePanel != null)
                hunterGamePanel.SetActive(true);

            // Hunter DOES use visible hand landmarks.
            if (handTrackingCanvas != null)
                handTrackingCanvas.SetActive(true);

            if (arPlaneManager != null)
            {
                arPlaneManager.enabled = false;
                HideAllTrackedPlanes();
            }

            Debug.Log(
                "Hunter mode selected. " +
                "Hand landmarks enabled."
            );
        }
        else
        {
            if (!string.IsNullOrEmpty(hunterSceneName))
            {
                SceneManager.LoadScene(
                    hunterSceneName
                );
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
            if (handTrackingCanvas != null)
                handTrackingCanvas.SetActive(false);
        }
        else
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}