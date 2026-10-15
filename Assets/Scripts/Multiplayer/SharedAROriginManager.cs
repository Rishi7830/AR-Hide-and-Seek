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

    private Vector3 positionSum =
        Vector3.zero;

    private Quaternion averagedRotation =
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

        originObject.transform
            .SetPositionAndRotation(
                position,
                rotation
            );

        originObject.transform.localScale =
            Vector3.one;

        SharedOrigin =
            originObject.transform;

        // From this point onward,
        // this transform is never changed.
        originLocked =
            true;

        if (
            originDebugPrefab != null
        )
        {
            originDebugObject =
                Instantiate(
                    originDebugPrefab,
                    SharedOrigin.position,
                    SharedOrigin.rotation
                );

            originDebugObject.transform
                .SetParent(
                    SharedOrigin,
                    true
                );
        }

        Debug.Log(
            "[SharedOrigin] Origin permanently LOCKED. " +
            "Position = " +
            SharedOrigin.position +
            " | Rotation = " +
            SharedOrigin.rotation.eulerAngles +
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