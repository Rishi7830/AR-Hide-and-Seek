using UnityEngine;

public class BrushTipController : MonoBehaviour
{
    [Header("Brush State")]
    public Color currentColor = Color.red;
    [SerializeField] private Renderer brushTipRenderer; // Visual indicator on brush

    [Header("Painting Settings")]
    [SerializeField] private float paintDistanceThreshold = 0.05f; // 5cm proximity to paint
    [SerializeField] private LayerMask paintableLayer;

    private void OnTriggerEnter(Collider other)
    {
        // Sample color when hovering over a palette sphere
        ColorSphere sphere = other.GetComponent<ColorSphere>();
        if (sphere != null)
        {
            currentColor = sphere.sphereColor;

            // Update physical brush tip material
            if (brushTipRenderer != null)
            {
                brushTipRenderer.material.color = currentColor;
            }

            // Update UI Preview Box via Singleton
            if (PaintUIController.Instance != null)
            {
                PaintUIController.Instance.UpdateChosenColor(currentColor);
            }

            Debug.Log($"[Brush] Color Changed to: {currentColor}");
        }
    }

    private void Update()
    {
        // Only allow painting when Paint Mode is enabled via UI
        if (PaintUIController.Instance != null && !PaintUIController.Instance.isPaintModeActive)
        {
            return;
        }

        // Paint Meccha when brush tip gets close
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, paintDistanceThreshold, paintableLayer))
        {
            Renderer targetRenderer = hit.collider.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                targetRenderer.material.color = currentColor;
            }
        }
    }
}