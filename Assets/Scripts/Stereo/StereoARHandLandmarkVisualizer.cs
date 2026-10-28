using UnityEngine;
using UnityEngine.UI;

public class StereoARHandLandmarkVisualizer :
    MonoBehaviour
{
    [Header("Stereo Overlays")]
    [Tooltip(
        "Left-eye hand landmark overlay."
    )]
    [SerializeField]
    private RectTransform leftOverlay;

    [Tooltip(
        "Right-eye hand landmark overlay."
    )]
    [SerializeField]
    private RectTransform rightOverlay;

    [Header("Legacy Overlay")]
    [Tooltip(
        "Optional original single-screen overlay. " +
        "Leave empty when using stereo overlays."
    )]
    [SerializeField]
    private RectTransform overlay;

    [Header("Appearance")]

    [Tooltip(
        "Size of each hand landmark point in pixels."
    )]
    [SerializeField]
    private float jointSize =
        14f;

    [Tooltip(
        "Thickness of the connecting hand lines in pixels."
    )]
    [SerializeField]
    private float lineThickness =
        6f;

    [Tooltip(
        "Color of the 21 landmark points."
    )]
    [SerializeField]
    private Color jointColor =
        new Color(
            0.6f,
            0.2f,
            1f,
            1f
        );

    [Tooltip(
        "Color of the connecting hand lines."
    )]
    [SerializeField]
    private Color lineColor =
        Color.white;

    [Header("Camera Mapping")]

    [Tooltip(
        "Mirror the landmark positions horizontally. " +
        "Keep this ON if the hand appears left/right reversed."
    )]
    [SerializeField]
    private bool mirrorX =
        true;

    [Tooltip(
        "Mirror the landmark positions vertically."
    )]
    [SerializeField]
    private bool mirrorY =
        true;

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

    // ============================================================
    // LEFT EYE VISUALS
    // ============================================================

    private RectTransform[] leftJoints =
        new RectTransform[21];

    private RectTransform[] leftLines;

    // ============================================================
    // RIGHT EYE VISUALS
    // ============================================================

    private RectTransform[] rightJoints =
        new RectTransform[21];

    private RectTransform[] rightLines;

    // ============================================================
    // LEGACY / PRIMARY VISUALS
    // ============================================================

    private RectTransform[] legacyJoints =
        new RectTransform[21];

    private RectTransform[] legacyLines;

    private bool stereoMode =
        false;

    // ============================================================
    // MEDIAPIPE HAND CONNECTIONS
    // ============================================================

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

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // DETERMINE STEREO MODE
        // --------------------------------------------------------

        stereoMode =
            leftOverlay != null &&
            rightOverlay != null;

        // --------------------------------------------------------
        // STEREO
        // --------------------------------------------------------

        if (stereoMode)
        {
            CreateHandVisuals(
                leftOverlay,
                leftJoints,
                out leftLines
            );

            CreateHandVisuals(
                rightOverlay,
                rightJoints,
                out rightLines
            );

            HideVisuals(
                leftJoints,
                leftLines
            );

            HideVisuals(
                rightJoints,
                rightLines
            );

            Debug.Log(
                "[HandVisualizer] " +
                "Stereo visualisation initialized."
            );

            return;
        }

        // --------------------------------------------------------
        // LEGACY SINGLE OVERLAY
        // --------------------------------------------------------

        if (overlay == null)
        {
            overlay =
                GetComponent<
                    RectTransform
                >();
        }

        if (overlay == null)
        {
            Debug.LogError(
                "[HandVisualizer] " +
                "No overlay RectTransform found."
            );

            enabled =
                false;

            return;
        }

        CreateHandVisuals(
            overlay,
            legacyJoints,
            out legacyLines
        );

        HideVisuals(
            legacyJoints,
            legacyLines
        );

        Debug.Log(
            "[HandVisualizer] " +
            "Single-overlay visualisation initialized."
        );
    }

    // ============================================================
    // CREATE HAND VISUALS
    // ============================================================

    private void CreateHandVisuals(
        RectTransform targetOverlay,
        RectTransform[] targetJoints,
        out RectTransform[] targetLines
    )
    {
        if (
            targetOverlay == null
        )
        {
            targetLines =
                new RectTransform[0];

            return;
        }

        // --------------------------------------------------------
        // 21 LANDMARK JOINTS
        // --------------------------------------------------------

        for (
            int i = 0;
            i < 21;
            i++
        )
        {
            GameObject joint =
                new GameObject(
                    "HandJoint_" +
                    i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            joint.transform.SetParent(
                targetOverlay,
                false
            );

            RectTransform rect =
                joint.GetComponent<
                    RectTransform
                >();

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
                joint.GetComponent<
                    Image
                >();

            image.color =
                jointColor;

            image.raycastTarget =
                false;

            targetJoints[i] =
                rect;
        }

        // --------------------------------------------------------
        // CONNECTION LINES
        // --------------------------------------------------------

        int connectionCount =
            connections.GetLength(
                0
            );

        targetLines =
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
                    "HandLine_" +
                    i,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            line.transform.SetParent(
                targetOverlay,
                false
            );

            // Lines stay behind joints.
            line.transform.SetAsFirstSibling();

            RectTransform rect =
                line.GetComponent<
                    RectTransform
                >();

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
                line.GetComponent<
                    Image
                >();

            image.color =
                lineColor;

            image.raycastTarget =
                false;

            targetLines[i] =
                rect;
        }
    }

    // ============================================================
    // UPDATE HAND
    // ============================================================

    public void UpdateHand(
        Vector2[] normalizedLandmarks,
        int imageWidth,
        int imageHeight
    )
    {
        if (
            !gameObject.activeInHierarchy
        )
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

        // --------------------------------------------------------
        // STEREO MODE
        // --------------------------------------------------------

        if (stereoMode)
        {
            UpdateHandForOverlay(
                normalizedLandmarks,
                imageWidth,
                imageHeight,
                leftOverlay,
                leftJoints,
                leftLines
            );

            UpdateHandForOverlay(
                normalizedLandmarks,
                imageWidth,
                imageHeight,
                rightOverlay,
                rightJoints,
                rightLines
            );

            return;
        }

        // --------------------------------------------------------
        // LEGACY MODE
        // --------------------------------------------------------

        UpdateHandForOverlay(
            normalizedLandmarks,
            imageWidth,
            imageHeight,
            overlay,
            legacyJoints,
            legacyLines
        );
    }

    // ============================================================
    // UPDATE ONE OVERLAY
    // ============================================================

    private void UpdateHandForOverlay(
        Vector2[] normalizedLandmarks,
        int imageWidth,
        int imageHeight,
        RectTransform targetOverlay,
        RectTransform[] targetJoints,
        RectTransform[] targetLines
    )
    {
        if (
            targetOverlay == null ||
            targetJoints == null ||
            targetLines == null
        )
        {
            return;
        }

        Vector2[] screenPositions =
            new Vector2[21];

        // --------------------------------------------------------
        // LANDMARKS
        // --------------------------------------------------------

        for (
            int i = 0;
            i < 21;
            i++
        )
        {
            Vector2 normalized =
                normalizedLandmarks[i];

            // ----------------------------------------------------
            // MIRROR
            // ----------------------------------------------------

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

            // ----------------------------------------------------
            // ROTATION
            // ----------------------------------------------------

            normalized =
                ApplyRotation(
                    normalized
                );

            // ----------------------------------------------------
            // CAMERA IMAGE → THIS OVERLAY
            // ----------------------------------------------------

            Vector2 mapped =
                ConvertImageToScreen(
                    normalized,
                    imageWidth,
                    imageHeight,
                    targetOverlay
                );

            screenPositions[i] =
                mapped;

            targetJoints[i]
                .anchoredPosition =
                mapped;

            targetJoints[i]
                .gameObject
                .SetActive(
                    true
                );
        }

        // --------------------------------------------------------
        // CONNECTION LINES
        // --------------------------------------------------------

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
                targetLines[i],
                screenPositions[startIndex],
                screenPositions[endIndex]
            );

            targetLines[i]
                .gameObject
                .SetActive(
                    true
                );
        }
    }

    // ============================================================
    // APPLY ROTATION
    // ============================================================

    private Vector2 ApplyRotation(
        Vector2 point
    )
    {
        switch (
            rotation
        )
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

    // ============================================================
    // CAMERA IMAGE → UI SCREEN
    // ============================================================

    private Vector2 ConvertImageToScreen(
        Vector2 normalized,
        int imageWidth,
        int imageHeight,
        RectTransform targetOverlay
    )
    {
        // --------------------------------------------------------
        // IMAGE ASPECT
        // --------------------------------------------------------

        float imageAspect =
            (float)imageWidth /
            Mathf.Max(
                1,
                imageHeight
            );

        // --------------------------------------------------------
        // TARGET OVERLAY SIZE
        // --------------------------------------------------------

        Rect screenRect =
            targetOverlay.rect;

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

        // --------------------------------------------------------
        // ASPECT-FILL MAPPING
        // --------------------------------------------------------

        if (
            imageAspect >
            screenAspect
        )
        {
            // Camera image is wider.
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
            // Camera image is taller.
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

        // --------------------------------------------------------
        // NORMALIZED → LOCAL UI
        // --------------------------------------------------------

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

    // ============================================================
    // DRAW CONNECTION LINE
    // ============================================================

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

    // ============================================================
    // HIDE HAND
    // ============================================================

    public void HideHand()
    {
        HideVisuals(
            leftJoints,
            leftLines
        );

        HideVisuals(
            rightJoints,
            rightLines
        );

        HideVisuals(
            legacyJoints,
            legacyLines
        );
    }

    // ============================================================
    // HIDE VISUALS
    // ============================================================

    private void HideVisuals(
        RectTransform[] targetJoints,
        RectTransform[] targetLines
    )
    {
        if (
            targetJoints != null
        )
        {
            foreach (
                RectTransform joint
                in targetJoints
            )
            {
                if (
                    joint != null
                )
                {
                    joint.gameObject
                        .SetActive(
                            false
                        );
                }
            }
        }

        if (
            targetLines != null
        )
        {
            foreach (
                RectTransform line
                in targetLines
            )
            {
                if (
                    line != null
                )
                {
                    line.gameObject
                        .SetActive(
                            false
                        );
                }
            }
        }
    }

    // ============================================================
    // GET LANDMARK UI POSITION
    // ============================================================

    public Vector2 GetLandmarkUIPosition(
        int landmarkIndex
    )
    {
        if (
            landmarkIndex < 0 ||
            landmarkIndex >= 21
        )
        {
            return Vector2.zero;
        }

        // --------------------------------------------------------
        // In stereo mode, return the LEFT-EYE position.
        //
        // This preserves compatibility with any existing code
        // that uses GetLandmarkUIPosition().
        // --------------------------------------------------------

        if (
            stereoMode &&
            leftJoints != null &&
            leftJoints[landmarkIndex] != null
        )
        {
            return leftJoints[
                landmarkIndex
            ].anchoredPosition;
        }

        // --------------------------------------------------------
        // Legacy mode
        // --------------------------------------------------------

        if (
            legacyJoints != null &&
            legacyJoints[landmarkIndex] != null
        )
        {
            return legacyJoints[
                landmarkIndex
            ].anchoredPosition;
        }

        return Vector2.zero;
    }
}