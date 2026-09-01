using UnityEngine;
using UnityEngine.UI;

public class PaintUIController : MonoBehaviour
{
    public static PaintUIController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject colorPreviewPanel;
    [SerializeField] private RawImage chosenColorBox; // RawImage

    [Header("State")]
    public bool isPaintModeActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (colorPreviewPanel != null)
        {
            colorPreviewPanel.SetActive(false);
        }
    }

    public void TogglePaintMode()
    {
        isPaintModeActive = !isPaintModeActive;

        if (colorPreviewPanel != null)
        {
            colorPreviewPanel.SetActive(isPaintModeActive);
        }
    }

    public void UpdateChosenColor(Color newColor)
    {
        if (chosenColorBox != null)
        {
            chosenColorBox.color = newColor;
        }
    }
}