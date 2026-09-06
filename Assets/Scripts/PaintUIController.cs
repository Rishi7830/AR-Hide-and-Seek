using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PaintUIController : MonoBehaviour
{
    public static PaintUIController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject colorPreviewPanel;
    [SerializeField] private RawImage chosenColorBox;

    [Header("Brush Thickness UI")]
    [SerializeField] private Slider brushThicknessSlider;

    [SerializeField] private TMP_Text brushThicknessLabel;

    [Header("Paint Mode")]
    [SerializeField] private bool startInPaintMode = false;

    [Header("Initial Color")]
    [SerializeField] private Color startingColor = Color.white;

    [Header("Brush Thickness")]
    [SerializeField] private int defaultBrushRadius = 20;

    public bool IsPaintModeActive { get; private set; }

    public Color CurrentChosenColor { get; private set; }

    public int CurrentBrushRadius { get; private set; }

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
        // INITIAL STATE

        CurrentChosenColor =
            startingColor;

        IsPaintModeActive =
            startInPaintMode;

        CurrentBrushRadius =
            defaultBrushRadius;

        // CONFIGURE SLIDER

        if (brushThicknessSlider != null)
        {
            brushThicknessSlider.minValue = 5f;
            brushThicknessSlider.maxValue = 80f;
            brushThicknessSlider.wholeNumbers = true;

            brushThicknessSlider.value =
                defaultBrushRadius;

            brushThicknessSlider.onValueChanged
                .AddListener(OnBrushThicknessChanged);
        }

        UpdateUI();

        UpdateChosenColor(
            CurrentChosenColor
        );

        UpdateBrushThicknessLabel();

        Debug.Log(
            "[PaintUI] Paint UI Controller initialized."
        );

        Debug.Log(
            "[PaintUI] Initial Brush Radius = " +
            CurrentBrushRadius
        );
    }

    // PAINT MODE

    public void TogglePaintMode()
    {
        IsPaintModeActive =
            !IsPaintModeActive;

        UpdateUI();

        Debug.Log(
            "[PaintUI] Paint Mode = " +
            (
                IsPaintModeActive
                    ? "ON"
                    : "OFF"
            )
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
                "[PaintUI] ColorPreviewPanel " +
                "is not assigned."
            );
        }
    }

    // COLOR

    public void UpdateChosenColor(
        Color newColor
    )
    {
        CurrentChosenColor =
            newColor;

        if (chosenColorBox != null)
        {
            chosenColorBox.color =
                newColor;
        }
        else
        {
            Debug.LogWarning(
                "[PaintUI] ChosenColorBox " +
                "is not assigned."
            );
        }

        Debug.Log(
            "[PaintUI] Chosen Color = " +
            newColor
        );
    }

    // BRUSH THICKNESS

    private void OnBrushThicknessChanged(
        float value
    )
    {
        CurrentBrushRadius =
            Mathf.RoundToInt(value);

        UpdateBrushThicknessLabel();

        Debug.Log(
            "[PaintUI] Brush Thickness = " +
            CurrentBrushRadius
        );
    }

    private void UpdateBrushThicknessLabel()
    {
        if (brushThicknessLabel != null)
        {
            brushThicknessLabel.text =
                "Brush Thickness: " +
                CurrentBrushRadius;
        }
    }

    public int GetBrushRadius()
    {
        return CurrentBrushRadius;
    }
}