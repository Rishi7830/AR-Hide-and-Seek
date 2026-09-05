using UnityEngine;
using UnityEngine.UI;

public class PaintUIController : MonoBehaviour
{
    public static PaintUIController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject colorPreviewPanel;
    [SerializeField] private RawImage chosenColorBox;

    [Header("Paint Mode")]
    [SerializeField] private bool startInPaintMode = false;

    public bool IsPaintModeActive { get; private set; }

    public Color CurrentChosenColor { get; private set; } = Color.white;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        IsPaintModeActive = startInPaintMode;

        CurrentChosenColor = Color.white;

        UpdateUI();
        UpdateChosenColor(CurrentChosenColor);

        Debug.Log("[PaintUI] Paint UI Controller started.");
    }

    public void TogglePaintMode()
    {
        IsPaintModeActive = !IsPaintModeActive;

        UpdateUI();

        Debug.Log(
            "[PaintUI] Paint Mode = " +
            (IsPaintModeActive ? "ON" : "OFF")
        );
    }

    private void UpdateUI()
    {
        if (colorPreviewPanel != null)
        {
            colorPreviewPanel.SetActive(
                IsPaintModeActive
            );
        }
    }

    public void UpdateChosenColor(Color newColor)
    {
        CurrentChosenColor = newColor;

        if (chosenColorBox != null)
        {
            chosenColorBox.color = newColor;
        }

        Debug.Log(
            "[PaintUI] Chosen color updated to: " +
            newColor
        );
    }
}