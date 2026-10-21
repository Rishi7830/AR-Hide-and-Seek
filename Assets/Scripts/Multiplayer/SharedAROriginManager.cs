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

    [SerializeField]
    private ARAnchorManager anchorManager;

    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkState;

    [SerializeField]
    private MultiplayerRoleController roleController;

    [SerializeField]
    private NetworkMecchaManager networkMecchaManager;

    [Header("Reference Image")]
    [SerializeField]
    private string originImageName =
        "InitialAprilTag";

    [Header("Calibration")]
    [SerializeField]
    private float calibrationTime =
        1.0f;

    [Tooltip(
        "How long the InitialAprilTag must be lost " +
        "before another scan is allowed."
    )]
    [SerializeField]
    private float rescanLostDuration =
        0.5f;

    [Tooltip(
        "Minimum time between successful calibrations."
    )]
    [SerializeField]
    private float rescanCooldown =
        2.0f;

    [Tooltip(
        "Use world-up for the SharedOrigin because " +
        "InitialAprilTag is placed flat on the table."
    )]
    [SerializeField]
    private bool forceHorizontalOrigin =
        true;

    [Header("UI")]
    [SerializeField]
    private GameObject coordinateSetPanel;

    [SerializeField]
    private float panelDuration =
        10f;

    [SerializeField]
    private GameObject coordinateUpdatedPanel;

    [SerializeField]
    private float updatedPanelDuration =
        2f;

    [Header("Debug")]
    [SerializeField]
    private GameObject originDebugPrefab;

    private GameObject originObject;

    private GameObject originDebugObject;

    private ARAnchor sharedAnchor;

    private ARTrackedImage calibrationImage;

    private Coroutine rearmRescanCoroutine;

    private Coroutine updatedPanelCoroutine;

    private bool isCalibrating =
        false;

    private bool originLocked =
        false;

    private bool originImageCurrentlyTracking =
        false;

    private bool readyForRescan =
        true;

    private bool originReadyReported =
        false;

    private Vector3 positionSum =
        Vector3.zero;

    private Quaternion averagedRotation =
        Quaternion.identity;

    private int sampleCount =
        0;

    private float lastCalibrationTime =
        -1000f;

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

        if (rearmRescanCoroutine != null)
        {
            StopCoroutine(
                rearmRescanCoroutine
            );

            rearmRescanCoroutine =
                null;
        }

        if (updatedPanelCoroutine != null)
        {
            StopCoroutine(
                updatedPanelCoroutine
            );

            updatedPanelCoroutine =
                null;
        }
    }

    private void OnTrackablesChanged(
        ARTrackablesChangedEventArgs<
            ARTrackedImage
        > eventArgs
    )
    {
        foreach (
            ARTrackedImage image
            in eventArgs.added
        )
        {
            ProcessOriginImage(
                image
            );
        }

        foreach (
            ARTrackedImage image
            in eventArgs.updated
        )
        {
            ProcessOriginImage(
                image
            );
        }
    }

    private void ProcessOriginImage(
        ARTrackedImage trackedImage
    )
    {
        if (trackedImage == null)
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

        bool isTracking =
            trackedImage.trackingState ==
            TrackingState.Tracking;

        if (!isTracking)
        {
            HandleOriginImageLost();

            return;
        }

        bool wasAlreadyTracking =
            originImageCurrentlyTracking;

        originImageCurrentlyTracking =
            true;

        // If the tag returned before the lost
        // timer finished, do not treat it as
        // a completely new scan.
        if (rearmRescanCoroutine != null)
        {
            StopCoroutine(
                rearmRescanCoroutine
            );

            rearmRescanCoroutine =
                null;
        }

        if (isCalibrating)
        {
            return;
        }

        // First ever scan.
        if (!HasOrigin)
        {
            TryStartCalibration(
                trackedImage,
                false
            );

            return;
        }

        // Do not continuously recalibrate
        // while looking at the same marker.
        if (wasAlreadyTracking)
        {
            return;
        }

        if (!readyForRescan)
        {
            return;
        }

        if (
            Time.time -
            lastCalibrationTime <
            rescanCooldown
        )
        {
            return;
        }

        if (!CanRescanOrigin())
        {
            return;
        }

        // The marker was previously lost
        // and has now been found again.
        TryStartCalibration(
            trackedImage,
            true
        );
    }

    private void HandleOriginImageLost()
    {
        if (!originImageCurrentlyTracking)
        {
            return;
        }

        originImageCurrentlyTracking =
            false;

        if (!HasOrigin)
        {
            return;
        }

        if (rearmRescanCoroutine != null)
        {
            StopCoroutine(
                rearmRescanCoroutine
            );
        }

        rearmRescanCoroutine =
            StartCoroutine(
                RearmRescanAfterLoss()
            );
    }

    private IEnumerator
        RearmRescanAfterLoss()
    {
        yield return
            new WaitForSeconds(
                rescanLostDuration
            );

        rearmRescanCoroutine =
            null;

        if (
            originImageCurrentlyTracking ||
            isCalibrating
        )
        {
            yield break;
        }

        if (!CanRescanOrigin())
        {
            yield break;
        }

        readyForRescan =
            true;

        Debug.Log(
            "[SharedOrigin] InitialAprilTag lost. " +
            "Ready for another scan."
        );
    }

    private void TryStartCalibration(
        ARTrackedImage trackedImage,
        bool isRescan
    )
    {
        if (isCalibrating)
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
            networkState == null ||
            !networkState.IsNetworkReady
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

        if (isRescan)
        {
            if (!CanRescanOrigin())
            {
                return;
            }
        }
        else
        {
            if (
                networkState.Phase !=
                MultiplayerPhase.WaitingForOrigins
            )
            {
                return;
            }
        }

        calibrationImage =
            trackedImage;

        readyForRescan =
            false;

        StartCoroutine(
            CalibrateAndLock(
                isRescan
            )
        );
    }

    private IEnumerator
        CalibrateAndLock(
            bool isRescan
        )
    {
        isCalibrating =
            true;

        positionSum =
            Vector3.zero;

        averagedRotation =
            Quaternion.identity;

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
            // If Seeker gameplay begins during
            // a rescan, cancel the recalibration.
            if (
                isRescan &&
                !CanRescanOrigin()
            )
            {
                Debug.Log(
                    "[SharedOrigin] Recalibration cancelled."
                );

                isCalibrating =
                    false;

                calibrationImage =
                    null;

                yield break;
            }

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

        if (
            sampleCount == 0 ||
            !rotationInitialized
        )
        {
            isCalibrating =
                false;

            calibrationImage =
                null;

            yield break;
        }

        Vector3 averagePosition =
            positionSum /
            sampleCount;

        Quaternion stableRotation =
            GetStableOriginRotation(
                averagedRotation
            );

        if (isRescan)
        {
            RecalibrateExistingOrigin(
                averagePosition,
                stableRotation
            );
        }
        else
        {
            CreateInitialOrigin(
                averagePosition,
                stableRotation
            );
        }

        lastCalibrationTime =
            Time.time;

        isCalibrating =
            false;

        calibrationImage =
            null;
    }

    private Quaternion
        GetStableOriginRotation(
            Quaternion rawRotation
        )
    {
        if (!forceHorizontalOrigin)
        {
            return rawRotation;
        }

        // InitialAprilTag is flat on the
        // table, so use one image edge to
        // establish the shared X direction.
        Vector3 tagRight =
            rawRotation *
            Vector3.right;

        // Remove pitch/roll noise by
        // projecting onto the table plane.
        tagRight =
            Vector3.ProjectOnPlane(
                tagRight,
                Vector3.up
            );

        if (
            tagRight.sqrMagnitude <
            0.0001f
        )
        {
            Debug.LogWarning(
                "[SharedOrigin] Could not calculate " +
                "horizontal marker rotation. " +
                "Using raw rotation."
            );

            return rawRotation;
        }

        tagRight.Normalize();

        Vector3 forward =
            Vector3.Cross(
                tagRight,
                Vector3.up
            );

        forward.Normalize();

        return Quaternion.LookRotation(
            forward,
            Vector3.up
        );
    }

    private bool CanRescanOrigin()
    {
        if (
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return false;
        }

        return
            networkState.Phase ==
                MultiplayerPhase.WaitingForOrigins ||
            networkState.Phase ==
                MultiplayerPhase.CoordinateConfirmation ||
            networkState.Phase ==
                MultiplayerPhase.HiderHiding;
    }

    private void CreateInitialOrigin(
        Vector3 position,
        Quaternion rotation
    )
    {
        bool success =
            ReplaceSharedOriginAnchor(
                position,
                rotation
            );

        if (!success)
        {
            return;
        }

        Debug.Log(
            "[SharedOrigin] Initial ARAnchor created. " +
            "Position = " +
            SharedOrigin.position +
            " | Rotation = " +
            SharedOrigin.rotation.eulerAngles +
            " | Samples = " +
            sampleCount
        );

        // Only the first successful scan
        // reports OriginReady.
        if (
            !originReadyReported &&
            networkState != null &&
            networkState.IsNetworkReady
        )
        {
            originReadyReported =
                true;

            networkState
                .RequestOriginReady();
        }

        // Only the first scan shows the
        // long coordinate-set panel.
        if (
            coordinateSetPanel != null
        )
        {
            StartCoroutine(
                ShowCoordinateSetPanel()
            );
        }
    }

    private void RecalibrateExistingOrigin(
        Vector3 position,
        Quaternion rotation
    )
    {
        bool success =
            ReplaceSharedOriginAnchor(
                position,
                rotation
            );

        if (!success)
        {
            return;
        }

        Debug.Log(
            "[SharedOrigin] Origin RECALIBRATED " +
            "with a new ARAnchor. " +
            "Position = " +
            SharedOrigin.position +
            " | Rotation = " +
            SharedOrigin.rotation.eulerAngles +
            " | Samples = " +
            sampleCount
        );

        // Hider Mecchas stay in their current
        // world positions. Only their shared
        // network coordinates are recalculated.
        if (
            roleController != null &&
            roleController.IsHider() &&
            networkState != null &&
            networkState.Phase ==
                MultiplayerPhase.HiderHiding &&
            networkMecchaManager != null
        )
        {
            networkMecchaManager
                .ResyncAllHiderMecchasAfterOriginChange();
        }

        // Later scans only show the small
        // two-second confirmation popup.
        if (coordinateUpdatedPanel != null)
        {
            if (updatedPanelCoroutine != null)
            {
                StopCoroutine(
                    updatedPanelCoroutine
                );
            }

            updatedPanelCoroutine =
                StartCoroutine(
                    ShowCoordinateUpdatedPanel()
                );
        }
    }

    private bool ReplaceSharedOriginAnchor(
        Vector3 position,
        Quaternion rotation
    )
    {
        if (anchorManager == null)
        {
            Debug.LogError(
                "[SharedOrigin] ARAnchorManager " +
                "is not assigned."
            );

            return false;
        }

        if (!anchorManager.enabled)
        {
            Debug.LogError(
                "[SharedOrigin] ARAnchorManager " +
                "is disabled."
            );

            return false;
        }

        // Keep references to the old anchor
        // until the new one has been created.
        GameObject oldOriginObject =
            originObject;

        // Create the new shared-origin object
        // at the newly calibrated marker pose.
        GameObject newOriginObject =
            new GameObject(
                "SharedGameOrigin"
            );

        newOriginObject.transform
            .SetPositionAndRotation(
                position,
                rotation
            );

        newOriginObject.transform.localScale =
            Vector3.one;

        // Convert the new origin into a
        // persistent AR Foundation anchor.
        ARAnchor newAnchor =
            newOriginObject.AddComponent<
                ARAnchor
            >();

        if (newAnchor == null)
        {
            Debug.LogError(
                "[SharedOrigin] Failed to create " +
                "the new ARAnchor."
            );

            Destroy(
                newOriginObject
            );

            return false;
        }

        originObject =
            newOriginObject;

        sharedAnchor =
            newAnchor;

        SharedOrigin =
            sharedAnchor.transform;

        originLocked =
            true;

        // Create a new debug sphere as
        // a child of the latest ARAnchor.
        if (originDebugPrefab != null)
        {
            originDebugObject =
                Instantiate(
                    originDebugPrefab,
                    SharedOrigin
                );

            originDebugObject.transform
                .localPosition =
                Vector3.zero;

            originDebugObject.transform
                .localRotation =
                Quaternion.identity;
        }
        else
        {
            originDebugObject =
                null;
        }

        // Destroying the old origin also
        // destroys its old ARAnchor and
        // old debug-sphere child.
        if (
            oldOriginObject != null &&
            oldOriginObject !=
                originObject
        )
        {
            Destroy(
                oldOriginObject
            );
        }

        return true;
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

    private IEnumerator
        ShowCoordinateUpdatedPanel()
    {
        coordinateUpdatedPanel
            .SetActive(true);

        yield return
            new WaitForSeconds(
                updatedPanelDuration
            );

        coordinateUpdatedPanel
            .SetActive(false);

        updatedPanelCoroutine =
            null;
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