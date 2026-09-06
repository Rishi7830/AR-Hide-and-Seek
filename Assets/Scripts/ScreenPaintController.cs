using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

using EnhancedTouch =
    UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ScreenPaintController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera arCamera;

    [Header("Raycast")]
    [SerializeField] private float rayDistance = 100f;

    [Header("Editor Testing")]
    [SerializeField] private bool allowMouseInEditor = true;

    [Header("Debug")]
    [SerializeField] private bool logRaycastHits = true;

    private MecchaTexturePainter currentPainter;

    private void Awake()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDestroy()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
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
    }

    private void Update()
    {
        // PAINT MODE CHECK

        if (PaintUIController.Instance == null)
        {
            return;
        }

        if (!PaintUIController.Instance.IsPaintModeActive)
        {
            if (currentPainter != null)
            {
                currentPainter.EndStroke();
                currentPainter = null;
            }

            return;
        }

        // NEW INPUT SYSTEM TOUCH

        if (EnhancedTouch.activeTouches.Count > 0)
        {
            EnhancedTouch touch = EnhancedTouch.activeTouches[0];
            ProcessTouch(touch);

            return;
        }

        // UNITY EDITOR MOUSE

        if (
            allowMouseInEditor &&
            Mouse.current != null
        )
        {
            if (
                Mouse.current.leftButton
                    .wasPressedThisFrame
            )
            {
                BeginPaint(
                    Mouse.current.position.ReadValue()
                );
            }

            if (
                Mouse.current.leftButton.isPressed
            )
            {
                ContinuePaint(
                    Mouse.current.position.ReadValue()
                );
            }

            if (
                Mouse.current.leftButton
                    .wasReleasedThisFrame
            )
            {
                EndPaint();
            }
        }
    }

    // TOUCH PROCESSING

    private void ProcessTouch(
        EnhancedTouch touch
    )
    {
        Vector2 screenPosition = touch.screenPosition;

        switch (touch.phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:

                BeginPaint(screenPosition);
                break;

            case UnityEngine.InputSystem.TouchPhase.Moved:

                ContinuePaint(screenPosition);
                break;

            case UnityEngine.InputSystem.TouchPhase.Stationary:

                ContinuePaint(screenPosition);
                break;

            case UnityEngine.InputSystem.TouchPhase.Ended:

                EndPaint();
                break;

            case UnityEngine.InputSystem.TouchPhase.Canceled:

                EndPaint();
                break;
        }
    }

    // BEGIN TOUCH

    private void BeginPaint(
        Vector2 screenPosition
    )
    {
        if (arCamera == null)
        {
            return;
        }

        if (IsPointerOverUI())
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(
                screenPosition
            );

        RaycastHit[] hits = Physics.RaycastAll(
                ray,
                rayDistance
            );

        if (hits == null || hits.Length == 0)
        {
            Debug.Log(
                "[ScreenPaint] Nothing was hit."
            );

            return;
        }

        // SORT HITS BY DISTANCE

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        if (logRaycastHits)
        {
            Debug.Log(
                "[ScreenPaint] Number of raycast hits: " +
                hits.Length
            );

            foreach (RaycastHit debugHit in hits)
            {
                Debug.Log(
                    "[ScreenPaint] Hit: " +
                    debugHit.collider.name +
                    " | Distance: " +
                    debugHit.distance
                );
            }
        }

        // SEARCH FOR COLOR SPHERE OR MECCHA

        foreach (RaycastHit hit in hits)
        {
            // COLOR SPHERE

            ColorSphere colorSphere = hit.collider.GetComponent<ColorSphere>();

            if (colorSphere == null)
            {
                colorSphere = hit.collider.GetComponentInParent<ColorSphere>();
            }

            if (colorSphere != null)
            {
                SelectColor(
                    colorSphere
                );

                return;
            }

            // MECCHA

            MecchaTexturePainter painter = hit.collider.GetComponent < MecchaTexturePainter>();

            if (painter == null)
            {
                painter =
                    hit.collider
                        .GetComponentInParent<
                            MecchaTexturePainter>();
            }

            if (painter != null)
            {
                if (
                    !TryCalculateHitUV(
                        hit,
                        out Vector2 uv
                    )
                )
                {
                    Debug.LogWarning(
                        "[ScreenPaint] " +
                        "Could not calculate Meccha UV."
                    );

                    return;
                }

                StartMecchaStroke(
                    painter,
                    uv
                );

                return;
            }

            // IGNORE EVERYTHING ELSE

            Debug.Log(
                "[ScreenPaint] Ignoring non-paintable hit: " +
                hit.collider.name
            );
        }
    }

    // CONTINUE PAINT

    private void ContinuePaint(
        Vector2 screenPosition
    )
    {
        if (currentPainter == null)
        {
            return;
        }

        if (arCamera == null)
        {
            return;
        }

        if (IsPointerOverUI())
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(
                screenPosition
            );

        RaycastHit[] hits = Physics.RaycastAll(
                ray,
                rayDistance
            );

        if (hits == null || hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            MecchaTexturePainter painter =
                hit.collider.GetComponent<
                    MecchaTexturePainter>();

            if (painter == null)
            {
                painter =
                    hit.collider
                        .GetComponentInParent<
                            MecchaTexturePainter>();
            }

            if (painter != currentPainter)
            {
                continue;
            }

            if (
                !TryCalculateHitUV(
                    hit,
                    out Vector2 uv
                )
            )
            {
                return;
            }

            currentPainter.ContinueStroke(
                uv
            );

            return;
        }
    }

    // END TOUCH

    private void EndPaint()
    {
        if (currentPainter != null)
        {
            currentPainter.EndStroke();

            currentPainter = null;
        }
    }

    // COLOR SELECTION

    private void SelectColor(
        ColorSphere colorSphere
    )
    {
        if (colorSphere == null)
        {
            return;
        }

        Color selectedColor = colorSphere.sphereColor;

        if (PaintUIController.Instance != null)
        {
            PaintUIController.Instance.UpdateChosenColor(
                selectedColor
            );
        }

        Debug.Log(
            "[ScreenPaint] COLOR SELECTED: " +
            selectedColor
        );
    }

    // private void StartMecchaStroke(
    // MecchaTexturePainter painter,
    // Vector2 uv
    // )

    // START MECCHA STROKE
    private void StartMecchaStroke(
        MecchaTexturePainter painter,
        Vector2 uv
    )
    {
        if (painter == null)
        {
            return;
        }

        if (PaintUIController.Instance == null)
        {
            return;
        }

        currentPainter = painter;

        // GET SELECTED COLOR
        Color selectedColor =
            PaintUIController.Instance.CurrentChosenColor;

        // GET SELECTED BRUSH THICKNESS
        int selectedBrushRadius =
            PaintUIController.Instance.CurrentBrushRadius;

        // APPLY SETTINGS TO CURRENT MECCHA
        currentPainter.SetPaintColor(
            selectedColor
        );

        currentPainter.SetBrushRadius(
            selectedBrushRadius
        );

        // START STROKE
        currentPainter.BeginStroke(
            uv
        );

        Debug.Log(
            "[ScreenPaint] Started Meccha stroke. " +
            "Color = " +
            selectedColor +
            " | Brush Radius = " +
            selectedBrushRadius
        );
    }

    // MANUAL UV CALCULATION

    private bool TryCalculateHitUV(
        RaycastHit hit,
        out Vector2 uv
    )
    {
        uv = Vector2.zero;

        MeshCollider meshCollider = hit.collider as MeshCollider;

        if (meshCollider == null)
        {
            Debug.LogWarning(
                "[ScreenPaint] " +
                "Collider is not a MeshCollider."
            );

            return false;
        }

        Mesh mesh =
            meshCollider.sharedMesh;

        if (mesh == null)
        {
            Debug.LogWarning(
                "[ScreenPaint] " +
                "MeshCollider has no mesh."
            );

            return false;
        }

        int triangleIndex = hit.triangleIndex;

        if (triangleIndex < 0)
        {
            Debug.LogWarning(
                "[ScreenPaint] " +
                "Invalid triangle index."
            );

            return false;
        }

        int[] triangles = mesh.triangles;

        int triangleStart = triangleIndex * 3;

        if (
            triangleStart + 2 >= triangles.Length
        )
        {
            Debug.LogWarning(
                "[ScreenPaint] " +
                "Triangle index is outside mesh."
            );

            return false;
        }

        int vertexIndexA = triangles[triangleStart];
        int vertexIndexB = triangles[triangleStart + 1];
        int vertexIndexC = triangles[triangleStart + 2];

        Vector2[] uvs = mesh.uv;

        if (
            uvs == null || uvs.Length == 0
        )
        {
            Debug.LogWarning(
                "[ScreenPaint] " +
                "Mesh has no UV0 coordinates."
            );

            return false;
        }

        if (
            vertexIndexA >= uvs.Length ||
            vertexIndexB >= uvs.Length ||
            vertexIndexC >= uvs.Length
        )
        {
            Debug.LogWarning(
                "[ScreenPaint] " +
                "UV array does not contain " +
                "triangle vertices."
            );

            return false;
        }

        Vector2 uvA = uvs[vertexIndexA];
        Vector2 uvB = uvs[vertexIndexB];
        Vector2 uvC = uvs[vertexIndexC];
        Vector3 bary = hit.barycentricCoordinate;

        uv = uvA * bary.x + uvB * bary.y + uvC * bary.z;
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);

        return true;
    }

    // UI CHECK

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current
            .IsPointerOverGameObject();
    }
}