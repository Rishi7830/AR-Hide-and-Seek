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

    [Header("Initial Color")]
    [SerializeField] private Color startingColor = Color.white;

    public bool IsPaintModeActive { get; private set; }

    public Color CurrentChosenColor { get; private set; }

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
        CurrentChosenColor = startingColor;

        IsPaintModeActive = startInPaintMode;

        UpdateUI();

        UpdateChosenColor(CurrentChosenColor);

        Debug.Log(
            "[PaintUI] Paint UI Controller initialized."
        );
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
        else
        {
            Debug.LogWarning(
                "[PaintUI] ColorPreviewPanel is not assigned."
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
        else
        {
            Debug.LogWarning(
                "[PaintUI] ChosenColorBox is not assigned."
            );
        }

        Debug.Log(
            "[PaintUI] Chosen Color = " +
            newColor
        );
    }
}