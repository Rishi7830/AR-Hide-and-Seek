using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

using EnhancedTouch =
    UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ScreenPaintController : MonoBehaviour
{
    // CAMERA

    [Header("Camera")]
    [SerializeField]
    private Camera arCamera;

    // RAYCAST

    [Header("Raycast")]
    [SerializeField]
    private float rayDistance = 100f;

    [Tooltip(
        "Maximum number of physics hits stored for one ray."
    )]
    [SerializeField]
    private int maxRaycastHits = 16;

    // PAINT SMOOTHING / THROTTLING

    [Header("Painting Performance")]
    [Tooltip(
        "Minimum screen movement in pixels before another paint " +
        "sample is calculated."
    )]
    [SerializeField]
    private float minimumPaintPixelMovement = 4f;

    [Tooltip(
        "If enabled, painting only processes when the touch " +
        "has actually moved."
    )]
    [SerializeField]
    private bool onlyPaintWhenMoving = true;

    // EDITOR

    [Header("Editor Testing")]
    [SerializeField]
    private bool allowMouseInEditor = true;

    [Header("Debug")]
    [SerializeField]
    private bool logRaycastHits = false;

    // INTERNAL STATE

    private MecchaTexturePainter currentPainter;
    private Vector2 lastPaintScreenPosition;
    private bool hasPaintScreenPosition = false;

    // RAYCAST BUFFER

    private RaycastHit[] raycastHits;
    private class MeshUVData
    {
        public int[] triangles;
        public Vector2[] uvs;
    }

    private readonly Dictionary<
        Mesh,
        MeshUVData
    > meshUVCache =
        new Dictionary<
            Mesh,
            MeshUVData
        >();

    // STATIC COLORS

    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        EnhancedTouchSupport.Enable();
        raycastHits =
            new RaycastHit[
                Mathf.Max(
                    maxRaycastHits,
                    1
                )
            ];
    }

    private void Start()
    {
        if (arCamera == null)
        {
            arCamera =
                Camera.main;
        }

        if (arCamera == null)
        {
            Debug.LogError(
                "[ScreenPaint] AR Camera not found."
            );
        }
    }

    private void OnDestroy()
    {
        EnhancedTouchSupport.Disable();

        meshUVCache.Clear();
    }

    private void Update()
    {
        if (
            PaintUIController.Instance == null
        )
        {
            return;
        }

        if (
            !PaintUIController
                .Instance
                .IsPaintModeActive
        )
        {
            if (currentPainter != null)
            {
                currentPainter.EndStroke();

                currentPainter = null;
            }

            hasPaintScreenPosition = false;

            return;
        }

        // TOUCH

        if (
            EnhancedTouch.activeTouches.Count > 0
        )
        {
            EnhancedTouch touch =
                EnhancedTouch
                    .activeTouches[0];

            ProcessTouch(touch);

            return;
        }

        // EDITOR MOUSE

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
                    Mouse.current.position
                        .ReadValue()
                );
            }

            if (
                Mouse.current.leftButton
                    .isPressed
            )
            {
                ContinuePaint(
                    Mouse.current.position
                        .ReadValue()
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
            // TOUCH BEGAN

            case UnityEngine.InputSystem.TouchPhase.Began:
                BeginPaint(
                    screenPosition
                );
                break;

            // TOUCH MOVED

            case UnityEngine.InputSystem.TouchPhase.Moved:
                ContinuePaint(
                    screenPosition
                );
                break;

            case UnityEngine.InputSystem.TouchPhase.Stationary:
                break;

            // TOUCH ENDED

            case UnityEngine.InputSystem.TouchPhase.Ended:
                EndPaint();
                break;

            case UnityEngine.InputSystem.TouchPhase.Canceled:
                EndPaint();
                break;
        }
    }

    // BEGIN PAINT

    private void BeginPaint(
        Vector2 screenPosition
    )
    {
        if (arCamera == null)
        {
            return;
        }

        if (
            IsPointerOverUI()
        )
        {
            return;
        }

        hasPaintScreenPosition = true;

        lastPaintScreenPosition =
            screenPosition;

        // CREATE RAY

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        // RAYCAST

        int hitCount =
            Physics.RaycastNonAlloc(
                ray,
                raycastHits,
                rayDistance
            );

        if (hitCount <= 0)
        {
            if (logRaycastHits)
            {
                Debug.Log(
                    "[ScreenPaint] " +
                    "Nothing was hit."
                );
            }

            return;
        }

        // FIND CLOSEST RELEVANT HIT

        RaycastHit closestHit;

        if (
            !TryFindPaintableHit(
                hitCount,
                out closestHit
            )
        )
        {
            return;
        }

        // COLOR SPHERE

        ColorSphere colorSphere =
            closestHit.collider
                .GetComponent<ColorSphere>();

        if (colorSphere == null)
        {
            colorSphere =
                closestHit.collider
                    .GetComponentInParent<
                        ColorSphere>();
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
            closestHit.collider
                .GetComponent<
                    MecchaTexturePainter>();

        if (painter == null)
        {
            painter =
                closestHit.collider
                    .GetComponentInParent<
                        MecchaTexturePainter>();
        }

        if (painter == null)
        {
            return;
        }

        if (
            !TryCalculateHitUV(
                closestHit,
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
    }

    // CONTINUE PAINT

    private void ContinuePaint(
        Vector2 screenPosition
    )
    {
        if (
            currentPainter == null ||
            arCamera == null
        )
        {
            return;
        }

        if (
            IsPointerOverUI()
        )
        {
            return;
        }

        if (
            onlyPaintWhenMoving &&
            hasPaintScreenPosition
        )
        {
            float movement =
                Vector2.Distance(
                    screenPosition,
                    lastPaintScreenPosition
                );

            if (
                movement <
                minimumPaintPixelMovement
            )
            {
                return;
            }
        }

        // STORE NEW POSITION

        lastPaintScreenPosition =
            screenPosition;

        hasPaintScreenPosition =
            true;

        // CREATE RAY

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        // RAYCAST

        int hitCount =
            Physics.RaycastNonAlloc(
                ray,
                raycastHits,
                rayDistance
            );

        if (hitCount <= 0)
        {
            return;
        }

        // FIND CLOSEST HIT BELONGING TO CURRENT MECCHA

        RaycastHit closestHit;

        if (
            !TryFindCurrentPainterHit(
                hitCount,
                out closestHit
            )
        )
        {
            return;
        }

        if (
            !TryCalculateHitUV(
                closestHit,
                out Vector2 uv
            )
        )
        {
            return;
        }

        // CONTINUE PAINT

        currentPainter.ContinueStroke(
            uv
        );
    }

    // FIND PAINTABLE HIT

    private bool TryFindPaintableHit(
        int hitCount,
        out RaycastHit closestHit
    )
    {
        closestHit =
            default;

        bool foundHit =
            false;

        float closestDistance =
            float.MaxValue;

        for (
            int i = 0;
            i < hitCount;
            i++
        )
        {
            RaycastHit hit =
                raycastHits[i];

            if (
                hit.collider == null
            )
            {
                continue;
            }

            // CHECK COLOR SPHERE

            ColorSphere colorSphere =
                hit.collider
                    .GetComponent<ColorSphere>();

            if (colorSphere == null)
            {
                colorSphere =
                    hit.collider
                        .GetComponentInParent<
                            ColorSphere>();
            }

            if (colorSphere != null)
            {
                if (
                    hit.distance <
                    closestDistance
                )
                {
                    closestHit =
                        hit;

                    closestDistance =
                        hit.distance;

                    foundHit =
                        true;
                }

                continue;
            }

            // CHECK MECCHA

            MecchaTexturePainter painter =
                hit.collider
                    .GetComponent<
                        MecchaTexturePainter>();

            if (painter == null)
            {
                painter =
                    hit.collider
                        .GetComponentInParent<
                            MecchaTexturePainter>();
            }

            if (painter == null)
            {
                continue;
            }

            if (
                hit.distance <
                closestDistance
            )
            {
                closestHit =
                    hit;

                closestDistance =
                    hit.distance;

                foundHit =
                    true;
            }
        }

        if (logRaycastHits)
        {
            Debug.Log(
                "[ScreenPaint] " +
                "Raycast hits = " +
                hitCount +
                " | Paintable hit = " +
                foundHit
            );
        }

        return foundHit;
    }

    // FIND CURRENT PAINTER HIT

    private bool TryFindCurrentPainterHit(
        int hitCount,
        out RaycastHit closestHit
    )
    {
        closestHit =
            default;

        bool foundHit =
            false;

        float closestDistance =
            float.MaxValue;

        for (
            int i = 0;
            i < hitCount;
            i++
        )
        {
            RaycastHit hit =
                raycastHits[i];

            if (
                hit.collider == null
            )
            {
                continue;
            }

            MecchaTexturePainter painter =
                hit.collider
                    .GetComponent<
                        MecchaTexturePainter>();

            if (painter == null)
            {
                painter =
                    hit.collider
                        .GetComponentInParent<
                            MecchaTexturePainter>();
            }

            // ONLY ACCEPT CURRENT MECCHA

            if (
                painter != currentPainter
            )
            {
                continue;
            }

            if (
                hit.distance <
                closestDistance
            )
            {
                closestHit =
                    hit;

                closestDistance =
                    hit.distance;

                foundHit =
                    true;
            }
        }

        return foundHit;
    }

    // END PAINT

    private void EndPaint()
    {
        if (currentPainter != null)
        {
            currentPainter.EndStroke();

            currentPainter = null;
        }

        hasPaintScreenPosition =
            false;
    }

    // COLOR SELECTION

    private void SelectColor(
        ColorSphere colorSphere
    )
    {
        if (
            colorSphere == null
        )
        {
            return;
        }

        Color selectedColor =
            colorSphere.sphereColor;

        if (
            PaintUIController.Instance != null
        )
        {
            PaintUIController
                .Instance
                .UpdateChosenColor(
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
        if (
            painter == null
        )
        {
            return;
        }

        if (
            PaintUIController.Instance == null
        )
        {
            return;
        }

        currentPainter =
            painter;

        // SELECTED COLOR

        Color selectedColor =
            PaintUIController
                .Instance
                .CurrentChosenColor;

        // BRUSH RADIUS

        int selectedBrushRadius =
            PaintUIController
                .Instance
                .CurrentBrushRadius;

        currentPainter.SetPaintColor(
            selectedColor
        );

        currentPainter.SetBrushRadius(
            selectedBrushRadius
        );

        // BEGIN STROKE

        currentPainter.BeginStroke(
            uv
        );

        if (logRaycastHits)
        {
            Debug.Log(
                "[ScreenPaint] Started Meccha stroke."
            );
        }
    }

    // UV CACHE

    private bool TryCalculateHitUV(
        RaycastHit hit,
        out Vector2 uv
    )
    {
        uv =
            Vector2.zero;

        // MESH COLLIDER

        MeshCollider meshCollider =
            hit.collider as MeshCollider;

        if (
            meshCollider == null
        )
        {
            return false;
        }

        Mesh mesh =
            meshCollider.sharedMesh;

        if (
            mesh == null
        )
        {
            return false;
        }

        // GET CACHED MESH DATA

        if (
            !meshUVCache.TryGetValue(
                mesh,
                out MeshUVData data
            )
        )
        {
            data =
                new MeshUVData();

            data.triangles =
                mesh.triangles;

            data.uvs =
                mesh.uv;

            if (
                data.triangles == null ||
                data.triangles.Length == 0 ||
                data.uvs == null ||
                data.uvs.Length == 0
            )
            {
                return false;
            }

            meshUVCache.Add(
                mesh,
                data
            );
        }

        // TRIANGLE INDEX

        int triangleIndex =
            hit.triangleIndex;

        if (
            triangleIndex < 0
        )
        {
            return false;
        }

        int triangleStart =
            triangleIndex * 3;

        if (
            triangleStart + 2 >=
            data.triangles.Length
        )
        {
            return false;
        }

        // VERTICES

        int vertexIndexA =
            data.triangles[
                triangleStart
            ];

        int vertexIndexB =
            data.triangles[
                triangleStart + 1
            ];

        int vertexIndexC =
            data.triangles[
                triangleStart + 2
            ];

        // UV INDEX CHECK

        if (
            vertexIndexA >= data.uvs.Length ||
            vertexIndexB >= data.uvs.Length ||
            vertexIndexC >= data.uvs.Length
        )
        {
            return false;
        }

        // UV VALUES

        Vector2 uvA =
            data.uvs[
                vertexIndexA
            ];

        Vector2 uvB =
            data.uvs[
                vertexIndexB
            ];

        Vector2 uvC =
            data.uvs[
                vertexIndexC
            ];

        // BARYCENTRIC COORDINATES

        Vector3 bary =
            hit.barycentricCoordinate;

        uv =
            uvA * bary.x +
            uvB * bary.y +
            uvC * bary.z;

        uv.x =
            Mathf.Clamp01(
                uv.x
            );

        uv.y =
            Mathf.Clamp01(
                uv.y
            );

        return true;
    }

    // UI CHECK

    private bool IsPointerOverUI()
    {
        if (
            EventSystem.current == null
        )
        {
            return false;
        }

        return EventSystem
            .current
            .IsPointerOverGameObject();
    }
}