using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MecchaFineTuneController : MonoBehaviour
{
    [Header("UI Panel Reference")]
    public GameObject fineTunePanel;

    // SENSITIVITY UI

    [Header("Sensitivity UI")]
    [SerializeField]
    private Slider movementSensitivitySlider;
    [SerializeField]
    private Slider rotationSensitivitySlider;
    [SerializeField]
    private TMP_Text movementText;
    [SerializeField]
    private TMP_Text rotationText;

    // SENSITIVITY SETTINGS

    [Header("Sensitivity Settings")]
    [SerializeField]
    private int defaultMovementSensitivity = 10;
    [SerializeField]
    private int defaultRotationSensitivity = 10;
    [SerializeField]
    private int minSensitivity = 1;
    [SerializeField]
    private int maxSensitivity = 20;

    [Header("Default Fine-Tune Values")]
    [Tooltip(
        "Movement distance at sensitivity 10."
    )]
    [SerializeField]
    private float defaultMovementStep = 0.05f;
    [Tooltip(
        "Rotation angle at sensitivity 10."
    )]
    [SerializeField]
    private float defaultRotationAngle = 15f;
    [Header("AR Camera Reference")]
    public Transform arCameraTransform;
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;

    // FEEDBACK MATERIALS

    [Header("Feedback Materials")]
    public Material validMaterial;
    public Material invalidMaterial;

    // PLACEMENT VALIDATION

    [Header("Placement Validation")]
    [Tooltip(
        "Maximum distance between any sampled part of the Meccha " +
        "and an AR plane for placement to be valid."
    )]
    [SerializeField]
    private float validPlacementDistance = 0.05f;
    [Tooltip(
        "Maximum distance to search downward for an AR plane."
    )]
    //[SerializeField]
    //private float planeSearchDistance = 0.20f;

    // TARGET

    [Header("Target & Current Tuning Values")]
    public GameObject currentMeccha;
    public float stepDistance = 0.05f;
    public float rotationAngle = 15f;

    // MATERIAL TRACKING

    private Dictionary<Renderer, Material[]> originalMaterials =
        new Dictionary<Renderer, Material[]>();

    // STATE

    private bool isFineTuningActive = false;
    private bool lastPlacementWasValid = false;
    private bool placementStateInitialized = false;
    private static List<ARRaycastHit> s_Hits =
        new List<ARRaycastHit>();

    private Renderer[] cachedMecchaRenderers;

    private void Start()
    {
        if (fineTunePanel != null)
        {
            fineTunePanel.SetActive(false);
        }

        if (
            arCameraTransform == null &&
            Camera.main != null
        )
        {
            arCameraTransform =
                Camera.main.transform;
        }

        // FIND RAYCAST MANAGER

        if (raycastManager == null)
        {
            raycastManager =
                FindFirstObjectByType<
                    ARRaycastManager>();
        }

        // MOVEMENT SLIDER

        if (movementSensitivitySlider != null)
        {
            movementSensitivitySlider.minValue =
                minSensitivity;

            movementSensitivitySlider.maxValue =
                maxSensitivity;

            movementSensitivitySlider.wholeNumbers =
                true;

            movementSensitivitySlider.value =
                defaultMovementSensitivity;

            movementSensitivitySlider.onValueChanged
                .AddListener(
                    OnMovementSensitivityChanged
                );
        }

        // ROTATION SLIDER

        if (rotationSensitivitySlider != null)
        {
            rotationSensitivitySlider.minValue =
                minSensitivity;

            rotationSensitivitySlider.maxValue =
                maxSensitivity;

            rotationSensitivitySlider.wholeNumbers =
                true;

            rotationSensitivitySlider.value =
                defaultRotationSensitivity;

            rotationSensitivitySlider.onValueChanged
                .AddListener(
                    OnRotationSensitivityChanged
                );
        }

        UpdateMovementStep(
            defaultMovementSensitivity
        );

        UpdateRotationAngle(
            defaultRotationSensitivity
        );

        UpdateMovementText(
            defaultMovementSensitivity
        );

        UpdateRotationText(
            defaultRotationSensitivity
        );
    }

    // UPDATE

    private void Update()
    {
        if (
            !isFineTuningActive ||
            currentMeccha == null
        )
        {
            return;
        }

        UpdatePlacementStatusFeedback();
    }

    // DESTROY

    private void OnDestroy()
    {
        if (movementSensitivitySlider != null)
        {
            movementSensitivitySlider.onValueChanged
                .RemoveListener(
                    OnMovementSensitivityChanged
                );
        }

        if (rotationSensitivitySlider != null)
        {
            rotationSensitivitySlider.onValueChanged
                .RemoveListener(
                    OnRotationSensitivityChanged
                );
        }
    }

    // TARGET MECCHA

    public void SetTargetMeccha(
        GameObject meccha
    )
    {
        if (
            currentMeccha == meccha
        )
        {
            return;
        }

        if (currentMeccha != null)
        {
            RestoreOriginalMaterials();
        }

        currentMeccha =
            meccha;

        cachedMecchaRenderers =
            null;

        placementStateInitialized =
            false;

        if (currentMeccha == null)
        {
            isFineTuningActive =
                false;

            if (fineTunePanel != null)
            {
                fineTunePanel.SetActive(false);
            }

            return;
        }

        if (isFineTuningActive)
        {
            CacheOriginalMaterials();

            CacheMecchaRenderers();

            UpdatePlacementStatusFeedback();
        }
    }

    // FINE TUNE PANEL

    public void ToggleFineTunePanel()
    {
        if (fineTunePanel == null)
        {
            return;
        }

        bool isActive =
            !fineTunePanel.activeSelf;

        if (isActive)
        {
            OpenFineTunePanel();
        }
        else
        {
            CloseFineTunePanel();
        }
    }

    // OPEN

    public void OpenFineTunePanel()
    {
        if (fineTunePanel != null)
        {
            fineTunePanel.SetActive(true);
        }

        isFineTuningActive =
            true;

        placementStateInitialized =
            false;

        if (currentMeccha != null)
        {
            CacheOriginalMaterials();

            CacheMecchaRenderers();

            UpdatePlacementStatusFeedback();
        }
    }

    // CLOSE

    public void CloseFineTunePanel()
    {
        isFineTuningActive =
            false;

        if (fineTunePanel != null)
        {
            fineTunePanel.SetActive(false);
        }

        RestoreOriginalMaterials();

        placementStateInitialized =
            false;

        if (currentMeccha != null)
        {
            Debug.Log(
                "Fine tuning closed. Meccha remains anchored. " +
                "Position: " +
                currentMeccha.transform.position
            );
        }
    }

    // MOVEMENT SENSITIVITY

    private void OnMovementSensitivityChanged(
        float value
    )
    {
        int sensitivity =
            Mathf.RoundToInt(value);

        UpdateMovementStep(
            sensitivity
        );

        UpdateMovementText(
            sensitivity
        );

        Debug.Log(
            "[FineTune] Movement Sensitivity = " +
            sensitivity +
            " | Step Distance = " +
            stepDistance +
            " m"
        );
    }

    // UPDATE MOVEMENT STEP

    private void UpdateMovementStep(
        int sensitivity
    )
    {
        float multiplier =
            (float)sensitivity /
            defaultMovementSensitivity;

        stepDistance =
            defaultMovementStep *
            multiplier;
    }

    // MOVEMENT TEXT

    private void UpdateMovementText(
        int sensitivity
    )
    {
        if (movementText != null)
        {
            movementText.text =
                "Movement Sensitivity: " +
                sensitivity;
        }
    }

    // ROTATION SENSITIVITY

    private void OnRotationSensitivityChanged(
        float value
    )
    {
        int sensitivity =
            Mathf.RoundToInt(value);

        UpdateRotationAngle(
            sensitivity
        );

        UpdateRotationText(
            sensitivity
        );

        Debug.Log(
            "[FineTune] Rotation Sensitivity = " +
            sensitivity +
            " | Rotation Angle = " +
            rotationAngle +
            " degrees"
        );
    }

    // UPDATE ROTATION ANGLE

    private void UpdateRotationAngle(
        int sensitivity
    )
    {
        float multiplier =
            (float)sensitivity /
            defaultRotationSensitivity;

        rotationAngle =
            defaultRotationAngle *
            multiplier;
    }

    // ROTATION TEXT

    private void UpdateRotationText(
        int sensitivity
    )
    {
        if (rotationText != null)
        {
            rotationText.text =
                "Rotation Sensitivity: " +
                sensitivity;
        }
    }

    // CACHE ORIGINAL MATERIALS

    private void CacheOriginalMaterials()
    {
        originalMaterials.Clear();

        if (currentMeccha == null)
        {
            return;
        }

        Renderer[] renderers =
            currentMeccha
                .GetComponentsInChildren<
                    Renderer>(
                    true
                );

        foreach (
            Renderer renderer
            in renderers
        )
        {
            if (renderer != null)
            {
                originalMaterials[renderer] =
                    renderer.sharedMaterials;
            }
        }
    }

    // CACHE RENDERERS

    private void CacheMecchaRenderers()
    {
        if (currentMeccha == null)
        {
            cachedMecchaRenderers =
                null;

            return;
        }

        cachedMecchaRenderers =
            currentMeccha
                .GetComponentsInChildren<
                    Renderer>(
                    true
                );
    }

    // RESTORE ORIGINAL MATERIALS

    private void RestoreOriginalMaterials()
    {
        foreach (
            KeyValuePair<
                Renderer,
                Material[]
            > entry
            in originalMaterials
        )
        {
            if (entry.Key != null)
            {
                entry.Key.materials =
                    entry.Value;
            }
        }

        originalMaterials.Clear();
    }

    // APPLY FEEDBACK MATERIAL

    private void ApplySingleMaterialToTarget(
        Material targetMat
    )
    {
        if (
            currentMeccha == null ||
            targetMat == null
        )
        {
            return;
        }

        if (cachedMecchaRenderers == null)
        {
            CacheMecchaRenderers();
        }

        foreach (
            Renderer renderer
            in cachedMecchaRenderers
        )
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials =
                renderer.materials;

            for (
                int i = 0;
                i < materials.Length;
                i++
            )
            {
                materials[i] =
                    targetMat;
            }

            renderer.materials =
                materials;
        }
    }

    // UPDATE PLACEMENT FEEDBACK

    private void UpdatePlacementStatusFeedback()
    {
        bool isValid =
            CheckIfMecchaIsOnPlane();

        // ONLY CHANGE MATERIAL WHEN STATE CHANGES

        if (
            placementStateInitialized &&
            isValid ==
            lastPlacementWasValid
        )
        {
            return;
        }

        placementStateInitialized =
            true;

        lastPlacementWasValid =
            isValid;

        // APPLY FEEDBACK

        if (isValid)
        {
            ApplySingleMaterialToTarget(
                validMaterial
            );
        }
        else
        {
            ApplySingleMaterialToTarget(
                invalidMaterial
            );
        }

        Debug.Log(
            "[FineTune] Placement = " +
            (isValid ? "VALID" : "INVALID")
        );
    }

    // CHECK MECCHA PLACEMENT

    private bool CheckIfMecchaIsOnPlane()
    {
        if (
            currentMeccha == null ||
            raycastManager == null
        )
        {
            return false;
        }

        if (cachedMecchaRenderers == null)
        {
            CacheMecchaRenderers();
        }

        if (
            cachedMecchaRenderers == null ||
            cachedMecchaRenderers.Length == 0
        )
        {
            return false;
        }

        bool hasBounds = false;

        Bounds mecchaBounds =
            new Bounds(
                currentMeccha.transform.position,
                Vector3.zero
            );

        foreach (
            Renderer renderer
            in cachedMecchaRenderers
        )
        {
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                mecchaBounds =
                    renderer.bounds;

                hasBounds = true;
            }
            else
            {
                mecchaBounds.Encapsulate(
                    renderer.bounds
                );
            }
        }

        if (!hasBounds)
        {
            return false;
        }

        // Sample important points around the Meccha.
        Vector3[] samplePoints =
        {
        mecchaBounds.center,

        new Vector3(
            mecchaBounds.min.x,
            mecchaBounds.min.y,
            mecchaBounds.min.z
        ),

        new Vector3(
            mecchaBounds.min.x,
            mecchaBounds.min.y,
            mecchaBounds.max.z
        ),

        new Vector3(
            mecchaBounds.max.x,
            mecchaBounds.min.y,
            mecchaBounds.min.z
        ),

        new Vector3(
            mecchaBounds.max.x,
            mecchaBounds.min.y,
            mecchaBounds.max.z
        ),

        new Vector3(
            mecchaBounds.center.x,
            mecchaBounds.min.y,
            mecchaBounds.center.z
        ),

        new Vector3(
            mecchaBounds.center.x,
            mecchaBounds.max.y,
            mecchaBounds.center.z
        ),

        new Vector3(
            mecchaBounds.min.x,
            mecchaBounds.center.y,
            mecchaBounds.center.z
        ),

        new Vector3(
            mecchaBounds.max.x,
            mecchaBounds.center.y,
            mecchaBounds.center.z
        ),

        new Vector3(
            mecchaBounds.center.x,
            mecchaBounds.center.y,
            mecchaBounds.min.z
        ),

        new Vector3(
            mecchaBounds.center.x,
            mecchaBounds.center.y,
            mecchaBounds.max.z
        )
    };

        // Test from each sample point in all 6 world directions.
        Vector3[] directions =
        {
        Vector3.up,
        Vector3.down,
        Vector3.left,
        Vector3.right,
        Vector3.forward,
        Vector3.back
    };

        foreach (
            Vector3 point
            in samplePoints
        )
        {
            foreach (
                Vector3 direction
                in directions
            )
            {
                Ray ray =
                    new Ray(
                        point,
                        direction
                    );

                s_Hits.Clear();

                if (
                    !raycastManager.Raycast(
                        ray,
                        s_Hits,
                        TrackableType.Planes
                    )
                )
                {
                    continue;
                }

                foreach (
                    ARRaycastHit hit
                    in s_Hits
                )
                {
                    float distance =
                        Vector3.Distance(
                            point,
                            hit.pose.position
                        );

                    if (
                        distance <=
                        validPlacementDistance
                    )
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // MOVEMENT CONTROLS

    public void MoveUp()
    {
        if (currentMeccha == null)
        {
            return;
        }

        currentMeccha.transform.localPosition +=
            Vector3.up *
            stepDistance;
    }

    public void MoveDown()
    {
        if (currentMeccha == null)
        {
            return;
        }

        currentMeccha.transform.localPosition +=
            Vector3.down *
            stepDistance;
    }

    public void MoveLeft()
    {
        if (
            currentMeccha == null ||
            arCameraTransform == null
        )
        {
            return;
        }

        Vector3 cameraRight =
            arCameraTransform.right;

        currentMeccha.transform.position -=
            cameraRight *
            stepDistance;
    }

    public void MoveRight()
    {
        if (
            currentMeccha == null ||
            arCameraTransform == null
        )
        {
            return;
        }

        Vector3 cameraRight =
            arCameraTransform.right;

        currentMeccha.transform.position +=
            cameraRight *
            stepDistance;
    }

    public void MoveFront()
    {
        if (
            currentMeccha == null ||
            arCameraTransform == null
        )
        {
            return;
        }

        Vector3 camForward =
            arCameraTransform.forward;

        camForward.y = 0f;

        if (
            camForward.sqrMagnitude >
            0.001f
        )
        {
            currentMeccha.transform.position +=
                camForward.normalized *
                stepDistance;
        }
    }

    public void MoveBack()
    {
        if (
            currentMeccha == null ||
            arCameraTransform == null
        )
        {
            return;
        }

        Vector3 camForward =
            arCameraTransform.forward;

        camForward.y = 0f;

        if (
            camForward.sqrMagnitude >
            0.001f
        )
        {
            currentMeccha.transform.position -=
                camForward.normalized *
                stepDistance;
        }
    }

    // ROTATION CONTROLS

    public void RotateHorizontal()
    {
        if (currentMeccha == null)
        {
            return;
        }

        currentMeccha.transform.Rotate(
            Vector3.up,
            rotationAngle,
            Space.Self
        );
    }

    public void RotateVertical()
    {
        if (
            currentMeccha == null ||
            arCameraTransform == null
        )
        {
            return;
        }

        currentMeccha.transform.Rotate(
            arCameraTransform.right,
            rotationAngle,
            Space.World
        );
    }

    // RE-ANCHORING

    public void ReAnchorCurrentMeccha()
    {
        Debug.Log(
            "ReAnchorCurrentMeccha() called - ignored. " +
            "Existing ARAnchor is being preserved."
        );
    }
}