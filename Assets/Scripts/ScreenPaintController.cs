using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ScreenPaintController : MonoBehaviour
{
    [Header("AR Camera")]
    [SerializeField] private Camera arCamera;

    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 100f;

    [Header("Input Settings")]
    [SerializeField] private bool allowMouseInEditor = true;

    private void Start()
    {
        // Automatically find the AR camera if one wasn't assigned.
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
            Debug.LogError(
                "[ScreenPaint] AR Camera not found."
            );
        }
        else
        {
            Debug.Log(
                "[ScreenPaint] AR Camera found: " +
                arCamera.name
            );
        }
    }

    private void Update()
    {
        // ---------------------------------------------------------
        // CHECK PAINT MODE
        // ---------------------------------------------------------

        if (PaintUIController.Instance == null)
        {
            return;
        }

        if (!PaintUIController.Instance.IsPaintModeActive)
        {
            return;
        }

        // ---------------------------------------------------------
        // NEW INPUT SYSTEM - ANDROID TOUCH
        // ---------------------------------------------------------

        if (Touchscreen.current != null)
        {
            var primaryTouch =
                Touchscreen.current.primaryTouch;

            if (primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 screenPosition =
                    primaryTouch.position.ReadValue();

                int touchId =
                    primaryTouch.touchId.ReadValue();

                ProcessScreenTap(
                    screenPosition,
                    touchId
                );
            }
        }

        // ---------------------------------------------------------
        // NEW INPUT SYSTEM - MOUSE
        // ---------------------------------------------------------

        if (
            allowMouseInEditor &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame
        )
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            ProcessMouseClick(mousePosition);
        }
    }

    // =============================================================
    // ANDROID TOUCH
    // =============================================================

    private void ProcessScreenTap(
        Vector2 screenPosition,
        int touchId
    )
    {
        // Don't paint/select a sphere if the finger
        // is currently pressing a UI button/panel.
        if (IsTouchOverUI(touchId))
        {
            Debug.Log(
                "[ScreenPaint] Touch ignored because it is over UI."
            );

            return;
        }

        PerformRaycast(screenPosition);
    }

    // =============================================================
    // MOUSE / UNITY EDITOR
    // =============================================================

    private void ProcessMouseClick(
        Vector2 screenPosition
    )
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log(
                "[ScreenPaint] Mouse click ignored because it is over UI."
            );

            return;
        }

        PerformRaycast(screenPosition);
    }

    // =============================================================
    // RAYCAST
    // =============================================================

    private void PerformRaycast(
        Vector2 screenPosition
    )
    {
        if (arCamera == null)
        {
            return;
        }

        // Create a ray from the AR camera through
        // the touched screen pixel.
        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        RaycastHit hit;

        bool didHit =
            Physics.Raycast(
                ray,
                out hit,
                rayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            );

        if (!didHit)
        {
            Debug.Log(
                "[ScreenPaint] Tap did not hit any 3D collider."
            );

            return;
        }

        Debug.Log(
            "[ScreenPaint] Ray hit: " +
            hit.collider.name
        );

        // ---------------------------------------------------------
        // CHECK FOR COLOR SPHERE
        // ---------------------------------------------------------

        ColorSphere colorSphere =
            hit.collider.GetComponent<ColorSphere>();

        if (colorSphere == null)
        {
            colorSphere =
                hit.collider.GetComponentInParent<ColorSphere>();
        }

        if (colorSphere != null)
        {
            SelectColor(colorSphere);

            return;
        }

        // ---------------------------------------------------------
        // SOMETHING ELSE WAS HIT
        // ---------------------------------------------------------

        Debug.Log(
            "[ScreenPaint] Hit object is not a ColorSphere: " +
            hit.collider.name
        );
    }

    // =============================================================
    // SELECT COLOR
    // =============================================================

    private void SelectColor(
        ColorSphere colorSphere
    )
    {
        if (colorSphere == null)
        {
            return;
        }

        Color selectedColor =
            colorSphere.sphereColor;

        // Update the selected color.
        if (PaintUIController.Instance != null)
        {
            PaintUIController.Instance.UpdateChosenColor(
                selectedColor
            );
        }

        Debug.Log(
            "[ScreenPaint] COLOR SELECTED: " +
            colorSphere.name +
            " | Color: " +
            selectedColor
        );
    }

    // =============================================================
    // UI TOUCH CHECK
    // =============================================================

    private bool IsTouchOverUI(
        int touchId
    )
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject(
            touchId
        );
    }
}