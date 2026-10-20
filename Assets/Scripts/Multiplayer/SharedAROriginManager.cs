using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SharedAROriginManager :
    MonoBehaviour
{
    [Header("AR")]
    [SerializeField]
    private ARTrackedImageManager trackedImageManager;

    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkState;

    [SerializeField]
    private MultiplayerRoleController roleController;

    [Header("Reference Image")]
    [SerializeField]
    private string originImageName =
        "InitialAprilTag";

    [Header("Calibration")]
    [SerializeField]
    private float calibrationTime =
        1.0f;

    [Header("UI")]
    [SerializeField]
    private GameObject coordinateSetPanel;

    [SerializeField]
    private float panelDuration =
        10f;

    [Header("Debug")]
    [SerializeField]
    private GameObject originDebugPrefab;

    private GameObject originObject;

    private GameObject originDebugObject;

    private ARTrackedImage calibrationImage;

    private bool isCalibrating =
        false;

    private bool originLocked =
        false;

    private bool forceOriginLock =
        false;

    private Vector3 positionSum =
        Vector3.zero;

    private Quaternion averagedRotation =
        Quaternion.identity;

    private Vector3 lockedOriginPosition =
        Vector3.zero;

    private Quaternion lockedOriginRotation =
        Quaternion.identity;

    private int sampleCount =
        0;

    public Transform SharedOrigin
    {
        get;
        private set;
    }

    public bool HasOrigin
    {
        get
        {
            return
                SharedOrigin != null &&
                originLocked;
        }
    }

    private void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager
                .trackablesChanged
                .AddListener(
                    OnTrackablesChanged
                );
        }
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager
                .trackablesChanged
                .RemoveListener(
                    OnTrackablesChanged
                );
        }
    }

    private void LateUpdate()
    {
        if (
            !forceOriginLock ||
            SharedOrigin == null
        )
        {
            return;
        }

        // Keep the shared origin at the exact
        // pose captured during initial calibration.
        SharedOrigin.SetPositionAndRotation(
            lockedOriginPosition,
            lockedOriginRotation
        );
    }

    private void OnTrackablesChanged(
        ARTrackablesChangedEventArgs<
            ARTrackedImage
        > eventArgs
    )
    {
        if (originLocked)
        {
            return;
        }

        foreach (
            ARTrackedImage image
            in eventArgs.added
        )
        {
            TryStartCalibration(
                image
            );
        }

        foreach (
            ARTrackedImage image
            in eventArgs.updated
        )
        {
            TryStartCalibration(
                image
            );
        }
    }

    private void TryStartCalibration(
        ARTrackedImage trackedImage
    )
    {
        if (
            originLocked ||
            isCalibrating
        )
        {
            return;
        }

        if (
            trackedImage == null ||
            trackedImage.trackingState !=
                TrackingState.Tracking
        )
        {
            return;
        }

        if (
            trackedImage.referenceImage.name !=
            originImageName
        )
        {
            return;
        }

        if (
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        if (
            networkState.Phase !=
            MultiplayerPhase.WaitingForOrigins
        )
        {
            return;
        }

        if (
            roleController == null ||
            roleController.GetLocalRole() ==
                MultiplayerRole.None
        )
        {
            return;
        }

        calibrationImage =
            trackedImage;

        StartCoroutine(
            CalibrateAndLock()
        );
    }

    private IEnumerator
        CalibrateAndLock()
    {
        isCalibrating =
            true;

        positionSum =
            Vector3.zero;

        sampleCount =
            0;

        bool rotationInitialized =
            false;

        float elapsed =
            0f;

        while (
            elapsed <
            calibrationTime
        )
        {
            if (
                calibrationImage != null &&
                calibrationImage.trackingState ==
                    TrackingState.Tracking
            )
            {
                Vector3 currentPosition =
                    calibrationImage
                        .transform.position;

                Quaternion currentRotation =
                    calibrationImage
                        .transform.rotation;

                positionSum +=
                    currentPosition;

                if (!rotationInitialized)
                {
                    averagedRotation =
                        currentRotation;

                    rotationInitialized =
                        true;
                }
                else
                {
                    float blend =
                        1f /
                        (sampleCount + 1f);

                    averagedRotation =
                        Quaternion.Slerp(
                            averagedRotation,
                            currentRotation,
                            blend
                        );
                }

                sampleCount++;

                elapsed +=
                    Time.deltaTime;
            }

            yield return null;
        }

        if (sampleCount == 0)
        {
            isCalibrating =
                false;

            yield break;
        }

        Vector3 averagePosition =
            positionSum /
            sampleCount;

        CreateLockedOrigin(
            averagePosition,
            averagedRotation
        );

        isCalibrating =
            false;
    }

    private void CreateLockedOrigin(
        Vector3 position,
        Quaternion rotation
    )
    {
        originObject =
            new GameObject(
                "SharedGameOrigin"
            );

        // Keep SharedGameOrigin independent
        // from AR trackables and AR anchors.
        originObject.transform.SetParent(
            null
        );

        originObject.transform
            .SetPositionAndRotation(
                position,
                rotation
            );

        originObject.transform.localScale =
            Vector3.one;

        // Save the calibrated pose permanently.
        lockedOriginPosition =
            position;

        lockedOriginRotation =
            rotation;

        SharedOrigin =
            originObject.transform;

        originLocked =
            true;

        forceOriginLock =
            true;

        if (
            originDebugPrefab != null
        )
        {
            originDebugObject =
                Instantiate(
                    originDebugPrefab,
                    SharedOrigin
                );

            // Sphere sits exactly at
            // the SharedOrigin position.
            originDebugObject.transform
                .localPosition =
                Vector3.zero;

            originDebugObject.transform
                .localRotation =
                Quaternion.identity;
        }

        Debug.Log(
            "[SharedOrigin] Origin permanently FROZEN. " +
            "Position = " +
            lockedOriginPosition +
            " | Rotation = " +
            lockedOriginRotation.eulerAngles +
            " | Samples = " +
            sampleCount
        );

        networkState
            .RequestOriginReady();

        if (
            coordinateSetPanel != null
        )
        {
            StartCoroutine(
                ShowCoordinateSetPanel()
            );
        }
    }

    private IEnumerator
        ShowCoordinateSetPanel()
    {
        coordinateSetPanel
            .SetActive(true);

        yield return
            new WaitForSeconds(
                panelDuration
            );

        coordinateSetPanel
            .SetActive(false);
    }

    public Vector3 WorldToSharedPosition(
        Vector3 worldPosition
    )
    {
        if (!HasOrigin)
        {
            return Vector3.zero;
        }

        return SharedOrigin
            .InverseTransformPoint(
                worldPosition
            );
    }

    public Quaternion WorldToSharedRotation(
        Quaternion worldRotation
    )
    {
        if (!HasOrigin)
        {
            return Quaternion.identity;
        }

        return
            Quaternion.Inverse(
                SharedOrigin.rotation
            ) *
            worldRotation;
    }

    public Vector3 SharedToWorldPosition(
        Vector3 localPosition
    )
    {
        if (!HasOrigin)
        {
            return Vector3.zero;
        }

        return SharedOrigin
            .TransformPoint(
                localPosition
            );
    }

    public Quaternion SharedToWorldRotation(
        Quaternion localRotation
    )
    {
        if (!HasOrigin)
        {
            return Quaternion.identity;
        }

        return
            SharedOrigin.rotation *
            localRotation;
    }
}