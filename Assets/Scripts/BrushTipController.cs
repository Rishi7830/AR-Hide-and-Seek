using UnityEngine;

public class BrushTipController : MonoBehaviour
{
    [Header("Brush State")]
    [SerializeField] private Color currentColor = Color.red;
    [SerializeField] private Renderer brushTipRenderer;

    [Header("Painting Settings")]
    [SerializeField] private float paintDistanceThreshold = 0.05f;
    [SerializeField] private LayerMask paintableLayer;

    public Color CurrentColor => currentColor;

    private void Start()
    {
        // Make sure the brush starts with its current color.
        UpdateBrushVisual();

        // Update the UI if Paint Mode is already active.
        if (PaintUIController.Instance != null)
        {
            PaintUIController.Instance.UpdateChosenColor(
                currentColor
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ONLY SAMPLE COLORS WHILE PAINT MODE IS ACTIVE

        if (PaintUIController.Instance != null &&
            !PaintUIController.Instance.IsPaintModeActive)
        {
            return;
        }

        // CHECK FOR COLOR SPHERE

        ColorSphere sphere =
            other.GetComponent<ColorSphere>();

        if (sphere == null)
        {
            // Sometimes the collider may be on a child object.
            sphere =
                other.GetComponentInParent<ColorSphere>();
        }

        if (sphere == null)
        {
            return;
        }

        // GET NEW COLOR

        currentColor = sphere.sphereColor;

        // UPDATE PHYSICAL BRUSH TIP

        UpdateBrushVisual();

        // UPDATE UI COLOR BOX

        if (PaintUIController.Instance != null)
        {
            PaintUIController.Instance.UpdateChosenColor(
                currentColor
            );
        }

        Debug.Log(
            "[Brush] Color Chosen: " +
            currentColor
        );
    }

    private void UpdateBrushVisual()
    {
        if (brushTipRenderer == null)
        {
            return;
        }

        brushTipRenderer.material.color =
            currentColor;
    }

    // PAINT MECCHA

    private void Update()
    {
        // Only paint while Paint Mode is active.
        if (PaintUIController.Instance != null &&
            !PaintUIController.Instance.IsPaintModeActive)
        {
            return;
        }

        PaintMeccha();
    }

    private void PaintMeccha()
    {
        RaycastHit hit;

        if (!Physics.Raycast(
                transform.position,
                transform.forward,
                out hit,
                paintDistanceThreshold,
                paintableLayer))
        {
            return;
        }

        // First try the collider's own Renderer.
        Renderer targetRenderer =
            hit.collider.GetComponent<Renderer>();

        // If the collider belongs to a child of a Skinned Mesh Renderer or another model hierarchy,
        // search the parent too.
        if (targetRenderer == null)
        {
            targetRenderer =
                hit.collider.GetComponentInParent<Renderer>();
        }

        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.material.color =
            currentColor;
    }
}