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

    [Header("Sensitivity UI")]
    [SerializeField] private Slider movementSensitivitySlider;
    [SerializeField] private Slider rotationSensitivitySlider;

    [SerializeField] private TMP_Text movementText;
    [SerializeField] private TMP_Text rotationText;

    [Header("Sensitivity Settings")]
    [SerializeField] private int defaultMovementSensitivity = 10;
    [SerializeField] private int defaultRotationSensitivity = 10;

    [SerializeField] private int minSensitivity = 1;
    [SerializeField] private int maxSensitivity = 20;

    [Header("Default Fine-Tune Values")]
    [Tooltip(
        "Movement distance at the default sensitivity of 10."
    )]
    [SerializeField] private float defaultMovementStep = 0.05f;

    [Tooltip(
        "Rotation angle at the default sensitivity of 10."
    )]
    [SerializeField] private float defaultRotationAngle = 15f;

    [Header("AR Camera Reference")]
    public Transform arCameraTransform;
    public ARRaycastManager raycastManager;
    public ARAnchorManager anchorManager;

    [Header("Feedback Materials")]
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Target & Current Tuning Values")]
    public GameObject currentMeccha;

    public float stepDistance = 0.05f;
    public float rotationAngle = 15f;

    // MATERIAL TRACKING

    private Dictionary<Renderer, Material[]> originalMaterials =
        new Dictionary<Renderer, Material[]>();

    private bool isFineTuningActive = false;

    private static List<ARRaycastHit> s_Hits =
        new List<ARRaycastHit>();

    private void Start()
    {
        // Fine tune panel starts hidden.
        if (fineTunePanel != null)
        {
            fineTunePanel.SetActive(false);
        }

        // Find camera automatically if not assigned.
        if (
            arCameraTransform == null &&
            Camera.main != null
        )
        {
            arCameraTransform =
                Camera.main.transform;
        }

        // Find raycast manager automatically if not assigned.
        if (raycastManager == null)
        {
            raycastManager =
                FindFirstObjectByType<ARRaycastManager>();
        }

        // SETUP MOVEMENT SLIDER

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

        // SETUP ROTATION SLIDER

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

        // APPLY DEFAULT VALUES

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

    // CLEANUP LISTENERS

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

    public void SetTargetMeccha(GameObject meccha)
    {
        if (currentMeccha == meccha)
            return;

        // Reset materials on previous target.
        if (currentMeccha != null)
        {
            RestoreOriginalMaterials();
        }

        currentMeccha = meccha;

        if (currentMeccha == null)
        {
            isFineTuningActive = false;

            if (fineTunePanel != null)
                fineTunePanel.SetActive(false);

            return;
        }

        if (isFineTuningActive)
        {
            CacheOriginalMaterials();
            UpdatePlacementStatusFeedback();
        }
    }

    // FINE TUNE PANEL

    public void ToggleFineTunePanel()
    {
        if (fineTunePanel != null)
        {
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
    }

    public void OpenFineTunePanel()
    {
        if (fineTunePanel != null)
        {
            fineTunePanel.SetActive(true);
        }

        isFineTuningActive = true;

        if (currentMeccha != null)
        {
            CacheOriginalMaterials();
            UpdatePlacementStatusFeedback();
        }
    }

    public void CloseFineTunePanel()
    {
        isFineTuningActive = false;

        if (fineTunePanel != null)
        {
            fineTunePanel.SetActive(false);
        }

        RestoreOriginalMaterials();

        // IMPORTANT: Do NOT destroy or recreate the ARAnchor. The Meccha remains anchored.

        if (currentMeccha != null)
        {
            Debug.Log(
                "Fine tuning closed. Meccha remains anchored. " +
                "Position: " +
                currentMeccha.transform.position +
                " | Local Position: " +
                currentMeccha.transform.localPosition
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

    // MATERIAL CACHE

    private void CacheOriginalMaterials()
    {
        originalMaterials.Clear();

        if (currentMeccha == null)
            return;

        Renderer[] renderers =
            currentMeccha
                .GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                originalMaterials[r] =
                    r.sharedMaterials;
            }
        }
    }

    // RESTORE MATERIALS

    private void RestoreOriginalMaterials()
    {
        if (currentMeccha == null)
            return;

        foreach (
            KeyValuePair<Renderer, Material[]> entry
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
            return;

        Renderer[] renderers =
            currentMeccha
                .GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                Material[] matArray =
                    new Material[
                        r.sharedMaterials.Length
                    ];

                for (
                    int i = 0;
                    i < matArray.Length;
                    i++
                )
                {
                    matArray[i] =
                        targetMat;
                }

                r.materials =
                    matArray;
            }
        }
    }

    // PLACEMENT FEEDBACK

    private void UpdatePlacementStatusFeedback()
    {
        bool isOnPlane =
            CheckIfMecchaIsOnPlane();

        if (isOnPlane)
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
    }

    // CHECK PLANE

    private bool CheckIfMecchaIsOnPlane()
    {
        if (
            raycastManager == null ||
            currentMeccha == null
        )
            return false;

        Vector3 rayOrigin =
            currentMeccha.transform.position +
            Vector3.up * 0.1f;

        Ray ray = new Ray(
            rayOrigin,
            Vector3.down
        );

        float rayLength =
            0.15f;

        if (
            raycastManager.Raycast(
                ray,
                s_Hits,
                TrackableType.Planes
            )
        )
        {
            if (
                s_Hits[0].distance <=
                rayLength
            )
            {
                float distanceToPlane =
                    Mathf.Abs(
                        currentMeccha
                            .transform.position.y -
                        s_Hits[0].pose.position.y
                    );

                if (
                    distanceToPlane <=
                    0.05f
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    // MOVEMENT CONTROLS

    public void MoveUp()
    {
        if (currentMeccha == null)
            return;

        currentMeccha.transform.localPosition +=
            Vector3.up *
            stepDistance;
    }

    public void MoveDown()
    {
        if (currentMeccha == null)
            return;

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
            return;

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
            return;

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
            return;

        Vector3 camForward =
            arCameraTransform.forward;

        camForward.y = 0;

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
            return;

        Vector3 camForward =
            arCameraTransform.forward;

        camForward.y = 0;

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
            return;

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
            return;

        currentMeccha.transform.Rotate(
            arCameraTransform.right,
            rotationAngle,
            Space.World
        );
    }

    // RE-ANCHORING

    public void ReAnchorCurrentMeccha()
    {
        // Intentionally disabled.

        Debug.Log(
            "ReAnchorCurrentMeccha() called - ignored. " +
            "Existing ARAnchor is being preserved."
        );
    }
}