using UnityEngine;

public class MecchaTexturePainter : MonoBehaviour
{
    [Header("Meccha Renderer")]
    [SerializeField] private SkinnedMeshRenderer targetRenderer;

    [Header("Paint Texture")]
    [SerializeField] private int textureWidth = 1024;
    [SerializeField] private int textureHeight = 1024;

    [Header("Brush")]
    [Tooltip("Brush radius in texture pixels.")]
    [SerializeField] private int brushRadius = 12;

    [Header("Debug")]
    [SerializeField] private bool logPaintInfo = true;

    private Texture2D paintTexture;
    private Material runtimeMaterial;

    private Color currentPaintColor = Color.red;

    private Vector2? previousUV = null;

    public Texture2D PaintTexture => paintTexture;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer =
                GetComponent<SkinnedMeshRenderer>();
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

    // MATERIAL

    private void InitializeRuntimeMaterial()
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (targetRenderer.sharedMaterial == null)
        {
            Debug.LogError(
                "[MecchaPainter] " +
                "Target renderer has no material."
            );

            return;
        }
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

        // CREATE WHITE CANVAS

        Color[] pixels =
            new Color[
                textureWidth *
                textureHeight
            ];

        for (
            int i = 0;
            i < pixels.Length;
            i++
        )
        {
            pixels[i] = Color.white;
        }

        paintTexture.SetPixels(pixels);

        paintTexture.Apply();

        // ASSIGN TO URP LIT BASE MAP

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

    // COLOR

    public void SetPaintColor(Color newColor)
    {
        currentPaintColor =
            newColor;

        Debug.Log(
            "[MecchaPainter] Paint color changed to: " +
            currentPaintColor
        );
    }

    // SINGLE DOT

    public void PaintAtUV(Vector2 uv)
    {
        if (paintTexture == null)
        {
            return;
        }

        uv.x =
            Mathf.Clamp01(uv.x);

        uv.y =
            Mathf.Clamp01(uv.y);

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

        paintTexture.Apply();

        if (logPaintInfo)
        {
            Debug.Log(
                "[MecchaPainter] " +
                "Painted UV = " +
                uv
            );
        }
    }

    // DRAW BRUSH

    private void DrawBrush(
        int centerX,
        int centerY
    )
    {
        int radius =
            Mathf.Max(
                1,
                brushRadius
            );

        int radiusSquared =
            radius * radius;

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

        for (
            int x = minX;
            x <= maxX;
            x++
        )
        {
            for (
                int y = minY;
                y <= maxY;
                y++
            )
            {
                int dx =
                    x - centerX;

                int dy =
                    y - centerY;

                int distanceSquared =
                    dx * dx +
                    dy * dy;

                if (
                    distanceSquared <=
                    radiusSquared
                )
                {
                    paintTexture.SetPixel(
                        x,
                        y,
                        currentPaintColor
                    );
                }
            }
        }
    }

    // DRAW LINE

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
                Mathf.Clamp01(uv.x);

            uv.y =
                Mathf.Clamp01(uv.y);

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

        paintTexture.Apply();
    }

    // STROKE

    public void BeginStroke(Vector2 uv)
    {
        previousUV =
            uv;

        PaintAtUV(
            uv
        );
    }

    public void ContinueStroke(Vector2 uv)
    {
        if (!previousUV.HasValue)
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

    public void EndStroke()
    {
        previousUV =
            null;
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