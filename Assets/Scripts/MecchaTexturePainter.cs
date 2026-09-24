using UnityEngine;

public class MecchaTexturePainter : MonoBehaviour
{
    [Header("Meccha Renderer")]
    [SerializeField]
    private SkinnedMeshRenderer targetRenderer;

    // PAINT TEXTURE

    [Header("Paint Texture")]
    [SerializeField]
    private int textureWidth = 1024;

    [SerializeField]
    private int textureHeight = 1024;

    // BRUSH

    [Header("Brush")]
    [SerializeField]
    private int brushRadius = 20;

    // PERFORMANCE

    [Header("Painting Performance")]

    [Tooltip(
        "How often the modified texture is uploaded to the GPU."
    )]
    [SerializeField]
    private float textureApplyInterval = 0.05f;

    [Tooltip(
        "Automatically apply the texture when a stroke ends."
    )]
    [SerializeField]
    private bool applyTextureWhenStrokeEnds = true;

    [Header("Debug")]
    [SerializeField]
    private bool logPaintInfo = false;

    // INTERNAL STATE

    private Texture2D paintTexture;
    private Material runtimeMaterial;
    private Color currentPaintColor = Color.red;
    private Vector2? previousUV = null;

    // TEXTURE APPLY STATE

    private float timeSinceLastApply = 0f;
    private bool textureDirty = false;

    private void Awake()
    {
        // FIND RENDERER

        if (targetRenderer == null)
        {
            targetRenderer =
                GetComponent<
                    SkinnedMeshRenderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogError(
                "[MecchaPainter] " +
                "No SkinnedMeshRenderer found on " +
                gameObject.name
            );

            return;
        }

        InitializeRuntimeMaterial();

        InitializePaintTexture();
    }

    // UPDATE

    private void Update()
    {
        if (!textureDirty)
        {
            return;
        }

        if (textureApplyInterval <= 0f)
        {
            ApplyPaintTexture();

            return;
        }

        timeSinceLastApply +=
            Time.deltaTime;

        if (
            timeSinceLastApply >=
            textureApplyInterval
        )
        {
            ApplyPaintTexture();
        }
    }

    // MATERIAL

    private void InitializeRuntimeMaterial()
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (
            targetRenderer.sharedMaterial ==
            null
        )
        {
            Debug.LogError(
                "[MecchaPainter] " +
                "Target renderer has no material."
            );

            return;
        }

        // CREATE RUNTIME MATERIAL

        runtimeMaterial =
            new Material(
                targetRenderer.sharedMaterial
            );

        runtimeMaterial.name =
            targetRenderer.sharedMaterial.name +
            "_RuntimePaint";

        targetRenderer.material =
            runtimeMaterial;
    }

    // PAINT TEXTURE

    private void InitializePaintTexture()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        // CREATE TEXTURE

        paintTexture =
            new Texture2D(
                textureWidth,
                textureHeight,
                TextureFormat.RGBA32,
                false
            );

        paintTexture.name =
            "MecchaRuntimePaintTexture";

        paintTexture.wrapMode =
            TextureWrapMode.Clamp;

        paintTexture.filterMode =
            FilterMode.Bilinear;

        // CREATE WHITE PIXEL ARRAY

        Color32[] pixels =
            new Color32[
                textureWidth *
                textureHeight
            ];

        Color32 white =
            new Color32(
                255,
                255,
                255,
                255
            );

        for (
            int i = 0;
            i < pixels.Length;
            i++
        )
        {
            pixels[i] =
                white;
        }

        // INITIALISE

        paintTexture.SetPixels32(
            pixels
        );

        paintTexture.Apply(
            false
        );

        // ASSIGN TO MATERIAL

        runtimeMaterial.SetTexture(
            "_BaseMap",
            paintTexture
        );

        Debug.Log(
            "[MecchaPainter] " +
            "Paint texture initialized: " +
            textureWidth +
            "x" +
            textureHeight
        );
    }

    // PAINT COLOR

    public void SetPaintColor(
        Color newColor
    )
    {
        currentPaintColor =
            newColor;

        if (logPaintInfo)
        {
            Debug.Log(
                "[MecchaPainter] " +
                "Paint color = " +
                currentPaintColor
            );
        }
    }

    // BRUSH RADIUS

    public void SetBrushRadius(
        int newRadius
    )
    {
        brushRadius =
            Mathf.Max(
                1,
                newRadius
            );

        if (logPaintInfo)
        {
            Debug.Log(
                "[MecchaPainter] " +
                "Brush radius = " +
                brushRadius
            );
        }
    }

    // PAINT SINGLE POINT

    public void PaintAtUV(
        Vector2 uv
    )
    {
        if (paintTexture == null)
        {
            return;
        }

        uv.x =
            Mathf.Clamp01(
                uv.x
            );

        uv.y =
            Mathf.Clamp01(
                uv.y
            );

        int pixelX =
            Mathf.RoundToInt(
                uv.x *
                (textureWidth - 1)
            );

        int pixelY =
            Mathf.RoundToInt(
                uv.y *
                (textureHeight - 1)
            );

        DrawBrush(
            pixelX,
            pixelY
        );
        textureDirty = true;
    }

    // DRAW BRUSH

    private void DrawBrush(
        int centerX,
        int centerY
    )
    {
        if (paintTexture == null)
        {
            return;
        }

        int radius =
            Mathf.Max(
                1,
                brushRadius
            );

        int radiusSquared =
            radius *
            radius;

        int minX =
            Mathf.Max(
                0,
                centerX - radius
            );

        int maxX =
            Mathf.Min(
                textureWidth - 1,
                centerX + radius
            );

        int minY =
            Mathf.Max(
                0,
                centerY - radius
            );

        int maxY =
            Mathf.Min(
                textureHeight - 1,
                centerY + radius
            );

        Color32 paintColor =
            currentPaintColor;

        // DRAW

        for (
            int y = minY;
            y <= maxY;
            y++
        )
        {
            int dy =
                y - centerY;

            int dySquared =
                dy * dy;

            for (
                int x = minX;
                x <= maxX;
                x++
            )
            {
                int dx =
                    x - centerX;

                if (
                    dx * dx +
                    dySquared <=
                    radiusSquared
                )
                {
                    paintTexture.SetPixel(
                        x,
                        y,
                        paintColor
                    );
                }
            }
        }
    }

    // PAINT LINE

    public void PaintLine(
        Vector2 startUV,
        Vector2 endUV
    )
    {
        if (paintTexture == null)
        {
            return;
        }

        float distance =
            Vector2.Distance(
                startUV,
                endUV
            );

        int steps =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    distance *
                    textureWidth
                )
            );

        for (
            int i = 0;
            i <= steps;
            i++
        )
        {
            float t =
                (float)i /
                steps;

            Vector2 uv =
                Vector2.Lerp(
                    startUV,
                    endUV,
                    t
                );

            uv.x =
                Mathf.Clamp01(
                    uv.x
                );

            uv.y =
                Mathf.Clamp01(
                    uv.y
                );

            int pixelX =
                Mathf.RoundToInt(
                    uv.x *
                    (textureWidth - 1)
                );

            int pixelY =
                Mathf.RoundToInt(
                    uv.y *
                    (textureHeight - 1)
                );

            DrawBrush(
                pixelX,
                pixelY
            );
        }
        textureDirty = true;
    }

    // BEGIN STROKE

    public void BeginStroke(
        Vector2 uv
    )
    {
        previousUV =
            uv;
        PaintAtUV(
            uv
        );
        timeSinceLastApply =
            0f;
    }

    // CONTINUE STROKE

    public void ContinueStroke(
        Vector2 uv
    )
    {
        if (
            !previousUV.HasValue
        )
        {
            BeginStroke(
                uv
            );

            return;
        }

        PaintLine(
            previousUV.Value,
            uv
        );

        previousUV =
            uv;
    }

    // END STROKE

    public void EndStroke()
    {
        previousUV =
            null;

        // APPLY FINAL PAINT IMMEDIATELY

        if (
            applyTextureWhenStrokeEnds &&
            textureDirty
        )
        {
            ApplyPaintTexture();
        }
    }

    // APPLY PAINT TEXTURE

    private void ApplyPaintTexture()
    {
        if (
            paintTexture == null ||
            !textureDirty
        )
        {
            return;
        }

        paintTexture.Apply(
            false
        );

        textureDirty =
            false;

        timeSinceLastApply =
            0f;
    }

    // CLEANUP

    private void OnDestroy()
    {
        if (paintTexture != null)
        {
            Destroy(
                paintTexture
            );
        }

        if (runtimeMaterial != null)
        {
            Destroy(
                runtimeMaterial
            );
        }
    }
}