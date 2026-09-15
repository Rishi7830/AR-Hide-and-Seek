using UnityEngine;
using UnityEngine.UI;

public class ARHandLandmarkVisualizer : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField]
    private RectTransform overlay;

    [Header("Appearance")]

    [Tooltip("Size of each hand landmark point in pixels.")]
    [SerializeField]
    private float jointSize = 14f;

    [Tooltip("Thickness of the connecting hand lines in pixels.")]
    [SerializeField]
    private float lineThickness = 6f;

    [Tooltip("Color of the 21 landmark points.")]
    [SerializeField]
    private Color jointColor =
        new Color(
            0.6f,
            0.2f,
            1f,
            1f
        );

    [Tooltip("Color of the connecting hand lines.")]
    [SerializeField]
    private Color lineColor =
        Color.white;

    [Header("Camera Mapping")]

    [Tooltip(
        "Mirror the landmark positions horizontally. " +
        "Keep this ON if the hand appears left/right reversed."
    )]
    [SerializeField]
    private bool mirrorX = true;

    [Tooltip(
        "Mirror the landmark positions vertically."
    )]
    [SerializeField]
    private bool mirrorY = true;

    [Tooltip(
        "Rotate the landmark coordinates to match the camera."
    )]
    [SerializeField]
    private RotationMode rotation =
        RotationMode.None;

    public enum RotationMode
    {
        None,
        Rotate90,
        Rotate180,
        Rotate270
    }

    // INTERNAL OBJECTS
    private RectTransform[] joints =
        new RectTransform[21];

    private RectTransform[] lines;

    // MEDIAPIPE HAND CONNECTIONS
    private readonly int[,] connections =
    {
        // THUMB
        { 0, 1 },
        { 1, 2 },
        { 2, 3 },
        { 3, 4 },

        // INDEX
        { 0, 5 },
        { 5, 6 },
        { 6, 7 },
        { 7, 8 },

        // MIDDLE
        { 5, 9 },
        { 9, 10 },
        { 10, 11 },
        { 11, 12 },

        // RING
        { 9, 13 },
        { 13, 14 },
        { 14, 15 },
        { 15, 16 },

        // PINKY
        { 13, 17 },
        { 17, 18 },
        { 18, 19 },
        { 19, 20 },

        // PALM
        { 0, 17 }
    };

    private void Awake()
    {
        if (overlay == null)
        {
            overlay =
                GetComponent<RectTransform>();
        }

        if (overlay == null)
        {
            Debug.LogError(
                "[HandVisualizer] " +
                "No overlay RectTransform found."
            );

            enabled = false;
            return;
        }

        CreateHandVisuals();

        HideHand();
    }

    // CREATE HAND VISUAL OBJECTS
    private void CreateHandVisuals()
    {
        // CREATE 21 LANDMARK JOINTS
        for (int i = 0; i < 21; i++)
        {
            GameObject joint =
                new GameObject(
                    "HandJoint_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            joint.transform.SetParent(
                overlay,
                false
            );

            RectTransform rect =
                joint.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.sizeDelta =
                new Vector2(
                    jointSize,
                    jointSize
                );

            Image image =
                joint.GetComponent<Image>();

            image.color =
                jointColor;

            image.raycastTarget =
                false;

            joints[i] =
                rect;
        }

        // CREATE CONNECTION LINES
        int connectionCount =
            connections.GetLength(0);

        lines =
            new RectTransform[
                connectionCount
            ];

        for (
            int i = 0;
            i < connectionCount;
            i++
        )
        {
            GameObject line =
                new GameObject(
                    "HandLine_" + i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            line.transform.SetParent(
                overlay,
                false
            );

            // Lines go behind the landmark points.
            line.transform.SetAsFirstSibling();

            RectTransform rect =
                line.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            Image image =
                line.GetComponent<Image>();

            image.color =
                lineColor;

            // Never block UI interaction.
            image.raycastTarget =
                false;

            lines[i] =
                rect;
        }
    }

    // UPDATE HAND
    public void UpdateHand(
        Vector2[] normalizedLandmarks,
        int imageWidth,
        int imageHeight
    )
    {
        // SAFETY CHECK
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (
            normalizedLandmarks == null ||
            normalizedLandmarks.Length < 21
        )
        {
            HideHand();
            return;
        }

        Vector2[] screenPositions =
            new Vector2[21];

        // CONVERT ALL 21 LANDMARKS
        for (int i = 0; i < 21; i++)
        {
            Vector2 normalized =
                normalizedLandmarks[i];

            if (mirrorX)
            {
                normalized.x =
                    1f -
                    normalized.x;
            }

            if (mirrorY)
            {
                normalized.y =
                    1f -
                    normalized.y;
            }

            // ROTATION
            normalized =
                ApplyRotation(
                    normalized
                );

            // CAMERA IMAGE -> SCREEN
            Vector2 mapped =
                ConvertImageToScreen(
                    normalized,
                    imageWidth,
                    imageHeight
                );

            screenPositions[i] =
                mapped;

            joints[i]
                .anchoredPosition =
                mapped;

            joints[i]
                .gameObject
                .SetActive(true);
        }

        // UPDATE HAND CONNECTION LINES
        for (
            int i = 0;
            i < connections.GetLength(0);
            i++
        )
        {
            int startIndex =
                connections[i, 0];

            int endIndex =
                connections[i, 1];

            DrawLine(
                lines[i],
                screenPositions[startIndex],
                screenPositions[endIndex]
            );

            lines[i]
                .gameObject
                .SetActive(true);
        }
    }

    // APPLY ROTATION
    private Vector2 ApplyRotation(
        Vector2 point
    )
    {
        switch (rotation)
        {
            case RotationMode.Rotate90:

                return new Vector2(
                    1f - point.y,
                    point.x
                );

            case RotationMode.Rotate180:

                return new Vector2(
                    1f - point.x,
                    1f - point.y
                );

            case RotationMode.Rotate270:

                return new Vector2(
                    point.y,
                    1f - point.x
                );

            default:

                return point;
        }
    }

    // CAMERA IMAGE -> UI SCREEN
    private Vector2 ConvertImageToScreen(
        Vector2 normalized,
        int imageWidth,
        int imageHeight
    )
    {
        // CALCULATE CAMERA IMAGE ASPECT
        float imageAspect =
            (float)imageWidth /
            Mathf.Max(
                1,
                imageHeight
            );

        // CALCULATE SCREEN ASPECT
        Rect screenRect =
            overlay.rect;

        float screenWidth =
            screenRect.width;

        float screenHeight =
            screenRect.height;

        float screenAspect =
            screenWidth /
            Mathf.Max(
                1f,
                screenHeight
            );

        float x =
            normalized.x;

        float y =
            normalized.y;

        // ASPECT-FILL MAPPING
        if (
            imageAspect >
            screenAspect
        )
        {
            // CAMERA IMAGE IS WIDER THAN SCREEN
            float visibleWidth =
                screenAspect /
                imageAspect;

            float crop =
                (
                    1f -
                    visibleWidth
                ) *
                0.5f;

            x =
                (
                    x -
                    crop
                ) /
                visibleWidth;
        }
        else
        {
            float visibleHeight =
                imageAspect /
                screenAspect;

            float crop =
                (
                    1f -
                    visibleHeight
                ) *
                0.5f;

            y =
                (
                    y -
                    crop
                ) /
                visibleHeight;
        }
        x =
            Mathf.Clamp01(
                x
            );

        y =
            Mathf.Clamp01(
                y
            );

        float uiX =
            (
                x -
                0.5f
            ) *
            screenWidth;

        float uiY =
            (
                y -
                0.5f
            ) *
            screenHeight;

        return new Vector2(
            uiX,
            uiY
        );
    }

    // DRAW CONNECTION LINE
    private void DrawLine(
        RectTransform line,
        Vector2 start,
        Vector2 end
    )
    {
        Vector2 difference =
            end -
            start;

        float distance =
            difference.magnitude;

        Vector2 midpoint =
            (
                start +
                end
            ) *
            0.5f;

        line.anchoredPosition =
            midpoint;

        line.sizeDelta =
            new Vector2(
                distance,
                lineThickness
            );

        float angle =
            Mathf.Atan2(
                difference.y,
                difference.x
            ) *
            Mathf.Rad2Deg;

        line.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    // HIDE HAND
    public void HideHand()
    {
        // HIDE ALL LANDMARK POINTS
        foreach (
            RectTransform joint
            in joints
        )
        {
            if (joint != null)
            {
                joint.gameObject.SetActive(
                    false
                );
            }
        }

        // HIDE ALL CONNECTION LINES
        if (lines != null)
        {
            foreach (
                RectTransform line
                in lines
            )
            {
                if (line != null)
                {
                    line.gameObject.SetActive(
                        false
                    );
                }
            }
        }
    }

    // GET LANDMARK UI POSITION
    public Vector2 GetLandmarkUIPosition(
        int landmarkIndex
    )
    {
        if (
            landmarkIndex < 0 ||
            landmarkIndex >= joints.Length ||
            joints[landmarkIndex] == null
        )
        {
            return Vector2.zero;
        }

        return joints[
            landmarkIndex
        ].anchoredPosition;
    }
}