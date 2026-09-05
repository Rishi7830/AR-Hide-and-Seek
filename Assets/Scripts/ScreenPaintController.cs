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

    private MecchaTexturePainter currentPainter;

    private void Awake()
    {
        // Enable New Input System Enhanced Touch.
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
            EnhancedTouch touch =
                EnhancedTouch.activeTouches[0];

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
        Vector2 screenPosition =
            touch.screenPosition;

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

    // BEGIN PAINT / COLOR SELECTION

    private void BeginPaint(
        Vector2 screenPosition
    )
    {
        if (arCamera == null)
        {
            return;
        }

        // IGNORE UI

        if (IsPointerOverUI())
        {
            return;
        }

        // CREATE CAMERA RAY

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance
            ))
        {
            Debug.Log(
                "[ScreenPaint] Nothing was hit."
            );

            return;
        }

        Debug.Log(
            "[ScreenPaint] Hit: " +
            hit.collider.name
        );

        // COLOR SPHERE

        ColorSphere colorSphere =
            hit.collider.GetComponent<ColorSphere>();

        if (colorSphere == null)
        {
            colorSphere =
                hit.collider.GetComponentInParent<ColorSphere>();
        }

        if (colorSphere != null)
        {
            SelectColor(
                colorSphere
            );

            return;
        }

        // MECCHA

        MecchaTexturePainter painter =
            hit.collider.GetComponent<
                MecchaTexturePainter>();

        if (painter == null)
        {
            painter =
                hit.collider.GetComponentInParent<
                    MecchaTexturePainter>();
        }

        if (painter != null)
        {
            // IMPORTANT: calculate the UV manually instead.

            if (!TryCalculateHitUV(
                    hit,
                    out Vector2 uv
                ))
            {
                Debug.LogWarning(
                    "[ScreenPaint] Could not calculate UV " +
                    "for Meccha hit."
                );

                return;
            }

            StartMecchaStroke(
                painter,
                uv
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

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance
            ))
        {
            return;
        }

        MecchaTexturePainter painter =
            hit.collider.GetComponent<
                MecchaTexturePainter>();

        if (painter == null)
        {
            painter =
                hit.collider.GetComponentInParent<
                    MecchaTexturePainter>();
        }

        if (painter != currentPainter)
        {
            return;
        }

        // MANUAL UV CALCULATION

        if (!TryCalculateHitUV(
                hit,
                out Vector2 uv
            ))
        {
            return;
        }

        currentPainter.ContinueStroke(
            uv
        );
    }

    // CALCULATE UV FROM TRIANGLE + BARYCENTRIC COORDINATES

    private bool TryCalculateHitUV(
        RaycastHit hit,
        out Vector2 uv
    )
    {
        uv = Vector2.zero;

        // We specifically need a MeshCollider.
        MeshCollider meshCollider =
            hit.collider as MeshCollider;

        if (meshCollider == null)
        {
            Debug.LogWarning(
                "[ScreenPaint] Collider is not a MeshCollider."
            );

            return false;
        }

        Mesh mesh =
            meshCollider.sharedMesh;

        if (mesh == null)
        {
            Debug.LogWarning(
                "[ScreenPaint] MeshCollider has no mesh."
            );

            return false;
        }

        // CHECK TRIANGLE INDEX

        int triangleIndex =
            hit.triangleIndex;

        if (triangleIndex < 0)
        {
            Debug.LogWarning(
                "[ScreenPaint] Invalid triangle index."
            );

            return false;
        }

        // Each triangle contains 3 vertex indices.
        int triangleStart =
            triangleIndex * 3;

        if (
            triangleStart + 2 >=
            mesh.triangles.Length
        )
        {
            Debug.LogWarning(
                "[ScreenPaint] Triangle index is outside mesh."
            );

            return false;
        }

        // GET TRIANGLE VERTEX INDICES

        int[] triangles =
            mesh.triangles;

        int vertexIndexA =
            triangles[triangleStart];

        int vertexIndexB =
            triangles[triangleStart + 1];

        int vertexIndexC =
            triangles[triangleStart + 2];

        // GET UV0

        Vector2[] uvs =
            mesh.uv;

        if (
            uvs == null ||
            uvs.Length == 0
        )
        {
            Debug.LogWarning(
                "[ScreenPaint] Mesh has no UV0 coordinates."
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
                "[ScreenPaint] UV array does not contain " +
                "the triangle vertices."
            );

            return false;
        }

        Vector2 uvA =
            uvs[vertexIndexA];

        Vector2 uvB =
            uvs[vertexIndexB];

        Vector2 uvC =
            uvs[vertexIndexC];

        // INTERPOLATE USING BARYCENTRIC COORDINATES

        Vector3 bary =
            hit.barycentricCoordinate;

        uv =
            uvA * bary.x +
            uvB * bary.y +
            uvC * bary.z;

        // KEEP UV IN NORMAL RANGE

        uv.x =
            Mathf.Clamp01(uv.x);

        uv.y =
            Mathf.Clamp01(uv.y);

        return true;
    }

    // END PAINT

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

        Color selectedColor =
            colorSphere.sphereColor;

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

        currentPainter =
            painter;

        Color selectedColor =
            PaintUIController.Instance
                .CurrentChosenColor;

        currentPainter.SetPaintColor(
            selectedColor
        );

        currentPainter.BeginStroke(
            uv
        );

        Debug.Log(
            "[ScreenPaint] Started Meccha paint stroke."
        );
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