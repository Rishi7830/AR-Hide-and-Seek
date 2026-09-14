using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HideControlsToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_Text offOnText;

    [Header("Text Values")]
    [SerializeField] private string offText = "OFF";
    [SerializeField] private string onText = "ON";

    [Header("Starting State")]
    [SerializeField] private bool controlsHiddenAtStart = false;

    private bool controlsHidden;

    private void Awake()
    {
        controlsHidden = controlsHiddenAtStart;
    }

    private void Start()
    {
        ApplyState();
    }

    public void ToggleControls()
    {
        controlsHidden = !controlsHidden;

        ApplyState();

        Debug.Log(
            "[HideControls] Controls are now " +
            (controlsHidden ? "HIDDEN" : "VISIBLE")
        );
    }

    private void ApplyState()
    {
        // SHOW / HIDE MENU PANEL

        if (menuPanel != null)
        {
            menuPanel.SetActive(!controlsHidden);
        }
        else
        {
            Debug.LogWarning(
                "[HideControls] Menu Panel is not assigned."
            );
        }

        // UPDATE ON/OFF TEXT

        if (offOnText != null)
        {
            offOnText.text =
                controlsHidden
                    ? onText
                    : offText;
        }
        else
        {
            Debug.LogWarning(
                "[HideControls] OffOnText is not assigned."
            );
        }
    }
}