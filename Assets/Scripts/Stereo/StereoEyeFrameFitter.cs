using UnityEngine;

public class StereoEyeFrameFitter :
    MonoBehaviour
{
    public enum Eye
    {
        Left,
        Right
    }

    [Header("Eye")]

    [SerializeField]
    private Eye eye =
        Eye.Left;

    [Header("Aspect")]

    [SerializeField]
    private float targetAspect =
        16f / 9f;

    [Header("Display Size")]

    [Tooltip(
        "1.0 = full width of each eye half. " +
        "0.95 = 95% width. " +
        "Smaller values create more horizontal breathing room " +
        "for the XR goggles."
    )]
    [Range(0.50f, 1.00f)]
    [SerializeField]
    private float eyeWidthScale =
        0.95f;

    [Tooltip(
        "Additional vertical scale. " +
        "Keep this at 1.0 so the 16:9 aspect ratio is preserved."
    )]
    [Range(0.50f, 1.00f)]
    [SerializeField]
    private float eyeHeightScale =
        1.00f;

    private RectTransform
        rectTransform;

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();
    }

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        UpdateLayout();
    }

    // ============================================================
    // RECT SIZE CHANGE
    // ============================================================

    private void OnRectTransformDimensionsChange()
    {
        UpdateLayout();
    }

    // ============================================================
    // UPDATE LAYOUT
    // ============================================================

    private void UpdateLayout()
    {
        if (
            rectTransform == null ||
            Screen.width <= 0 ||
            Screen.height <= 0
        )
        {
            return;
        }

        // --------------------------------------------------------
        // EACH EYE OWNS HALF OF THE SCREEN WIDTH
        // --------------------------------------------------------

        float halfScreenWidth =
            Screen.width *
            0.5f;

        // --------------------------------------------------------
        // APPLY HORIZONTAL SCALE
        // --------------------------------------------------------

        float eyeWidth =
            halfScreenWidth *
            eyeWidthScale;

        // --------------------------------------------------------
        // MAINTAIN 16:9
        // --------------------------------------------------------

        float eyeHeight =
            eyeWidth /
            Mathf.Max(
                0.01f,
                targetAspect
            );

        // --------------------------------------------------------
        // APPLY OPTIONAL VERTICAL SCALE
        // --------------------------------------------------------

        eyeHeight *=
            eyeHeightScale;

        // --------------------------------------------------------
        // IF TOO TALL, FIT TO SCREEN HEIGHT
        // --------------------------------------------------------

        if (
            eyeHeight >
            Screen.height
        )
        {
            eyeHeight =
                Screen.height;

            eyeWidth =
                eyeHeight *
                targetAspect *
                (
                    eyeWidthScale
                    /
                    Mathf.Max(
                        0.01f,
                        eyeHeightScale
                    )
                );
        }

        // --------------------------------------------------------
        // CENTER VERTICALLY
        // --------------------------------------------------------

        float verticalMargin =
            (
                Screen.height -
                eyeHeight
            ) *
            0.5f;

        // --------------------------------------------------------
        // CENTER EACH EYE INSIDE ITS HALF
        // --------------------------------------------------------

        float horizontalMargin =
            (
                halfScreenWidth -
                eyeWidth
            ) *
            0.5f;

        float xPosition;

        if (
            eye ==
            Eye.Left
        )
        {
            xPosition =
                horizontalMargin;
        }
        else
        {
            xPosition =
                halfScreenWidth +
                horizontalMargin;
        }

        // --------------------------------------------------------
        // APPLY PIXEL-BASED RECT
        // --------------------------------------------------------

        rectTransform.anchorMin =
            new Vector2(
                0f,
                0f
            );

        rectTransform.anchorMax =
            new Vector2(
                0f,
                0f
            );

        rectTransform.pivot =
            new Vector2(
                0f,
                0f
            );

        rectTransform.anchoredPosition =
            new Vector2(
                xPosition,
                verticalMargin
            );

        rectTransform.sizeDelta =
            new Vector2(
                eyeWidth,
                eyeHeight
            );
    }
}