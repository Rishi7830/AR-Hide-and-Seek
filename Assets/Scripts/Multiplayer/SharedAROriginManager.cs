using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SharedAROriginManager :
    MonoBehaviour
{
    // ============================================================
    // AR
    // ============================================================

    [Header("AR")]

    [SerializeField]
    private ARTrackedImageManager
        trackedImageManager;

    [SerializeField]
    private ARAnchorManager
        anchorManager;

    // ============================================================
    // NETWORK
    // ============================================================

    [Header("Network")]

    [SerializeField]
    private NetworkGameState
        networkState;

    [SerializeField]
    private MultiplayerRoleController
        roleController;

    [SerializeField]
    private NetworkMecchaManager
        networkMecchaManager;

    // ============================================================
    // REFERENCE IMAGE
    // ============================================================

    [Header("Reference Image")]

    [SerializeField]
    private string originImageName =
        "InitialAprilTag";

    // ============================================================
    // CALIBRATION
    // ============================================================

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

    // ============================================================
    // ORIGINAL / NON-STEREO UI
    // ============================================================

    [Header("UI - Original / Non-Stereo")]

    [Tooltip(
        "Original single-screen coordinate-set panel. " +
        "Used when stereo panel references are not assigned."
    )]
    [SerializeField]
    private GameObject
        coordinateSetPanel;

    [SerializeField]
    private float panelDuration =
        10f;

    [Tooltip(
        "Original single-screen coordinate-updated panel. " +
        "Used when stereo panel references are not assigned."
    )]
    [SerializeField]
    private GameObject
        coordinateUpdatedPanel;

    [SerializeField]
    private float updatedPanelDuration =
        2f;

    // ============================================================
    // STEREO UI
    // ============================================================

    [Header("UI - Stereo Seeker")]

    [Tooltip(
        "Left-eye coordinate-set panel. " +
        "Assign this in the stereo Seeker scene."
    )]
    [SerializeField]
    private GameObject
        leftCoordinateSetPanel;

    [Tooltip(
        "Right-eye coordinate-set panel. " +
        "Assign this in the stereo Seeker scene."
    )]
    [SerializeField]
    private GameObject
        rightCoordinateSetPanel;

    [Tooltip(
        "Left-eye coordinate-updated panel. " +
        "Assign this in the stereo Seeker scene."
    )]
    [SerializeField]
    private GameObject
        leftCoordinateUpdatedPanel;

    [Tooltip(
        "Right-eye coordinate-updated panel. " +
        "Assign this in the stereo Seeker scene."
    )]
    [SerializeField]
    private GameObject
        rightCoordinateUpdatedPanel;

    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    [SerializeField]
    private GameObject
        originDebugPrefab;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private GameObject
        originObject;

    private GameObject
        originDebugObject;

    private ARAnchor
        sharedAnchor;

    private ARTrackedImage
        calibrationImage;

    private Coroutine
        rearmRescanCoroutine;

    private Coroutine
        updatedPanelCoroutine;

    private Coroutine
        coordinateSetPanelCoroutine;

    private bool
        isCalibrating =
        false;

    private bool
        originLocked =
        false;

    private bool
        originImageCurrentlyTracking =
        false;

    private bool
        readyForRescan =
        true;

    private bool
        originReadyReported =
        false;

    private Vector3
        positionSum =
        Vector3.zero;

    private Quaternion
        averagedRotation =
        Quaternion.identity;

    private int
        sampleCount =
        0;

    private float
        lastCalibrationTime =
        -1000f;

    // ============================================================
    // SHARED ORIGIN
    // ============================================================

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

    // ============================================================
    // STEREO UI MODE
    // ============================================================

    private bool UsesStereoPanels()
    {
        return
            leftCoordinateSetPanel != null &&
            rightCoordinateSetPanel != null &&
            leftCoordinateUpdatedPanel != null &&
            rightCoordinateUpdatedPanel != null;
    }

    // ============================================================
    // ENABLE
    // ============================================================

    private void OnEnable()
    {
        if (
            trackedImageManager != null
        )
        {
            trackedImageManager
                .trackablesChanged
                .AddListener(
                    OnTrackablesChanged
                );
        }
    }

    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        if (
            trackedImageManager != null
        )
        {
            trackedImageManager
                .trackablesChanged
                .RemoveListener(
                    OnTrackablesChanged
                );
        }

        if (
            rearmRescanCoroutine != null
        )
        {
            StopCoroutine(
                rearmRescanCoroutine
            );

            rearmRescanCoroutine =
                null;
        }

        if (
            updatedPanelCoroutine != null
        )
        {
            StopCoroutine(
                updatedPanelCoroutine
            );

            updatedPanelCoroutine =
                null;
        }

        if (
            coordinateSetPanelCoroutine != null
        )
        {
            StopCoroutine(
                coordinateSetPanelCoroutine
            );

            coordinateSetPanelCoroutine =
                null;
        }
    }

    // ============================================================
    // TRACKABLES CHANGED
    // ============================================================

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

    // ============================================================
    // PROCESS ORIGIN IMAGE
    // ============================================================

    private void ProcessOriginImage(
        ARTrackedImage trackedImage
    )
    {
        if (
            trackedImage == null
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

        bool isTracking =
            trackedImage.trackingState ==
            TrackingState.Tracking;

        if (
            !isTracking
        )
        {
            HandleOriginImageLost();

            return;
        }

        bool wasAlreadyTracking =
            originImageCurrentlyTracking;

        originImageCurrentlyTracking =
            true;

        // --------------------------------------------------------
        // TAG RETURNED BEFORE LOSS TIMER FINISHED
        // --------------------------------------------------------

        if (
            rearmRescanCoroutine != null
        )
        {
            StopCoroutine(
                rearmRescanCoroutine
            );

            rearmRescanCoroutine =
                null;
        }

        // --------------------------------------------------------
        // ALREADY CALIBRATING
        // --------------------------------------------------------

        if (
            isCalibrating
        )
        {
            return;
        }

        // --------------------------------------------------------
        // FIRST EVER SCAN
        // --------------------------------------------------------

        if (
            !HasOrigin
        )
        {
            TryStartCalibration(
                trackedImage,
                false
            );

            return;
        }

        // --------------------------------------------------------
        // SAME TRACKING SESSION
        // --------------------------------------------------------

        if (
            wasAlreadyTracking
        )
        {
            return;
        }

        // --------------------------------------------------------
        // WAITING FOR RESCAN
        // --------------------------------------------------------

        if (
            !readyForRescan
        )
        {
            return;
        }

        // --------------------------------------------------------
        // COOLDOWN
        // --------------------------------------------------------

        if (
            Time.time -
            lastCalibrationTime <
            rescanCooldown
        )
        {
            return;
        }

        // --------------------------------------------------------
        // RESCAN CHECK
        // --------------------------------------------------------

        if (
            !CanRescanOrigin()
        )
        {
            return;
        }

        // --------------------------------------------------------
        // MARKER FOUND AGAIN
        // --------------------------------------------------------

        TryStartCalibration(
            trackedImage,
            true
        );
    }

    // ============================================================
    // ORIGIN IMAGE LOST
    // ============================================================

    private void HandleOriginImageLost()
    {
        if (
            !originImageCurrentlyTracking
        )
        {
            return;
        }

        originImageCurrentlyTracking =
            false;

        if (
            !HasOrigin
        )
        {
            return;
        }

        if (
            rearmRescanCoroutine != null
        )
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

    // ============================================================
    // REARM RESCAN
    // ============================================================

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

        if (
            !CanRescanOrigin()
        )
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

    // ============================================================
    // START CALIBRATION
    // ============================================================

    private void TryStartCalibration(
        ARTrackedImage trackedImage,
        bool isRescan
    )
    {
        if (
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

        if (
            isRescan
        )
        {
            if (
                !CanRescanOrigin()
            )
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

    // ============================================================
    // CALIBRATE AND LOCK
    // ============================================================

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
            // ----------------------------------------------------
            // CANCEL RESCAN IF GAMEPLAY HAS STARTED
            // ----------------------------------------------------

            if (
                isRescan &&
                !CanRescanOrigin()
            )
            {
                Debug.Log(
                    "[SharedOrigin] " +
                    "Recalibration cancelled."
                );

                isCalibrating =
                    false;

                calibrationImage =
                    null;

                yield break;
            }

            // ----------------------------------------------------
            // SAMPLE TRACKED IMAGE
            // ----------------------------------------------------

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

                if (
                    !rotationInitialized
                )
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
                        (
                            sampleCount +
                            1f
                        );

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

        // --------------------------------------------------------
        // VALIDATION
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // AVERAGE POSITION
        // --------------------------------------------------------

        Vector3 averagePosition =
            positionSum /
            sampleCount;

        Quaternion stableRotation =
            GetStableOriginRotation(
                averagedRotation
            );

        // --------------------------------------------------------
        // CREATE / RECALIBRATE
        // --------------------------------------------------------

        if (
            isRescan
        )
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

    // ============================================================
    // STABLE ORIGIN ROTATION
    // ============================================================

    private Quaternion
        GetStableOriginRotation(
            Quaternion rawRotation
        )
    {
        if (
            !forceHorizontalOrigin
        )
        {
            return rawRotation;
        }

        // --------------------------------------------------------
        // INITIAL APRILTAG IS FLAT ON TABLE
        // --------------------------------------------------------

        Vector3 tagRight =
            rawRotation *
            Vector3.right;

        // --------------------------------------------------------
        // REMOVE PITCH / ROLL
        // --------------------------------------------------------

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
                "[SharedOrigin] " +
                "Could not calculate horizontal " +
                "marker rotation. " +
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

    // ============================================================
    // RESCAN CHECK
    // ============================================================

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

    // ============================================================
    // INITIAL ORIGIN
    // ============================================================

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

        if (
            !success
        )
        {
            return;
        }

        Debug.Log(
            "[SharedOrigin] " +
            "Initial ARAnchor created. " +
            "Position = " +
            SharedOrigin.position +
            " | Rotation = " +
            SharedOrigin.rotation.eulerAngles +
            " | Samples = " +
            sampleCount
        );

        // --------------------------------------------------------
        // REPORT ORIGIN READY ONCE
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // SHOW COORDINATE SET UI
        // --------------------------------------------------------

        ShowCoordinateSetUI();
    }

    // ============================================================
    // RECALIBRATE EXISTING ORIGIN
    // ============================================================

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

        if (
            !success
        )
        {
            return;
        }

        Debug.Log(
            "[SharedOrigin] " +
            "Origin RECALIBRATED with a new ARAnchor. " +
            "Position = " +
            SharedOrigin.position +
            " | Rotation = " +
            SharedOrigin.rotation.eulerAngles +
            " | Samples = " +
            sampleCount
        );

        // --------------------------------------------------------
        // HIDER MECCHA RESYNC
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // SHOW UPDATED UI
        // --------------------------------------------------------

        ShowCoordinateUpdatedUI();
    }

    // ============================================================
    // REPLACE SHARED ORIGIN ANCHOR
    // ============================================================

    private bool ReplaceSharedOriginAnchor(
        Vector3 position,
        Quaternion rotation
    )
    {
        if (
            anchorManager == null
        )
        {
            Debug.LogError(
                "[SharedOrigin] " +
                "ARAnchorManager is not assigned."
            );

            return false;
        }

        if (
            !anchorManager.enabled
        )
        {
            Debug.LogError(
                "[SharedOrigin] " +
                "ARAnchorManager is disabled."
            );

            return false;
        }

        // --------------------------------------------------------
        // KEEP OLD OBJECT UNTIL NEW ANCHOR EXISTS
        // --------------------------------------------------------

        GameObject oldOriginObject =
            originObject;

        // --------------------------------------------------------
        // CREATE NEW SHARED ORIGIN
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // CREATE AR ANCHOR
        // --------------------------------------------------------

        ARAnchor newAnchor =
            newOriginObject.AddComponent<
                ARAnchor
            >();

        if (
            newAnchor == null
        )
        {
            Debug.LogError(
                "[SharedOrigin] " +
                "Failed to create the new ARAnchor."
            );

            Destroy(
                newOriginObject
            );

            return false;
        }

        // --------------------------------------------------------
        // STORE NEW REFERENCES
        // --------------------------------------------------------

        originObject =
            newOriginObject;

        sharedAnchor =
            newAnchor;

        SharedOrigin =
            sharedAnchor.transform;

        originLocked =
            true;

        // --------------------------------------------------------
        // DEBUG SPHERE
        // --------------------------------------------------------

        if (
            originDebugPrefab != null
        )
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

        // --------------------------------------------------------
        // DESTROY OLD ORIGIN
        // --------------------------------------------------------

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

    // ============================================================
    // STEREO MODE CHECK
    // ============================================================

    private bool IsStereoUIConfigured()
    {
        return
            leftCoordinateSetPanel != null &&
            rightCoordinateSetPanel != null &&
            leftCoordinateUpdatedPanel != null &&
            rightCoordinateUpdatedPanel != null;
    }

    // ============================================================
    // SHOW COORDINATE SET UI
    // ============================================================

    private void ShowCoordinateSetUI()
    {
        if (
            coordinateSetPanelCoroutine !=
            null
        )
        {
            StopCoroutine(
                coordinateSetPanelCoroutine
            );

            coordinateSetPanelCoroutine =
                null;
        }

        coordinateSetPanelCoroutine =
            StartCoroutine(
                ShowCoordinateSetPanel()
            );
    }

    // ============================================================
    // SHOW COORDINATE UPDATED UI
    // ============================================================

    private void ShowCoordinateUpdatedUI()
    {
        if (
            updatedPanelCoroutine !=
            null
        )
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

    // ============================================================
    // SHOW COORDINATE SET PANEL
    // ============================================================

    private IEnumerator
        ShowCoordinateSetPanel()
    {
        bool stereo =
            IsStereoUIConfigured();

        // --------------------------------------------------------
        // HIDE ALL VERSIONS FIRST
        // --------------------------------------------------------

        SetPanelActive(
            coordinateSetPanel,
            false
        );

        SetPanelActive(
            leftCoordinateSetPanel,
            false
        );

        SetPanelActive(
            rightCoordinateSetPanel,
            false
        );

        // --------------------------------------------------------
        // SHOW APPROPRIATE UI
        // --------------------------------------------------------

        if (
            stereo
        )
        {
            SetPanelActive(
                leftCoordinateSetPanel,
                true
            );

            SetPanelActive(
                rightCoordinateSetPanel,
                true
            );
        }
        else
        {
            SetPanelActive(
                coordinateSetPanel,
                true
            );
        }

        // --------------------------------------------------------
        // WAIT
        // --------------------------------------------------------

        yield return
            new WaitForSeconds(
                panelDuration
            );

        // --------------------------------------------------------
        // HIDE
        // --------------------------------------------------------

        SetPanelActive(
            coordinateSetPanel,
            false
        );

        SetPanelActive(
            leftCoordinateSetPanel,
            false
        );

        SetPanelActive(
            rightCoordinateSetPanel,
            false
        );

        coordinateSetPanelCoroutine =
            null;
    }

    // ============================================================
    // SHOW COORDINATE UPDATED PANEL
    // ============================================================

    private IEnumerator
        ShowCoordinateUpdatedPanel()
    {
        bool stereo =
            IsStereoUIConfigured();

        // --------------------------------------------------------
        // HIDE ALL VERSIONS FIRST
        // --------------------------------------------------------

        SetPanelActive(
            coordinateUpdatedPanel,
            false
        );

        SetPanelActive(
            leftCoordinateUpdatedPanel,
            false
        );

        SetPanelActive(
            rightCoordinateUpdatedPanel,
            false
        );

        // --------------------------------------------------------
        // SHOW APPROPRIATE UI
        // --------------------------------------------------------

        if (
            stereo
        )
        {
            SetPanelActive(
                leftCoordinateUpdatedPanel,
                true
            );

            SetPanelActive(
                rightCoordinateUpdatedPanel,
                true
            );
        }
        else
        {
            SetPanelActive(
                coordinateUpdatedPanel,
                true
            );
        }

        // --------------------------------------------------------
        // WAIT
        // --------------------------------------------------------

        yield return
            new WaitForSeconds(
                updatedPanelDuration
            );

        // --------------------------------------------------------
        // HIDE
        // --------------------------------------------------------

        SetPanelActive(
            coordinateUpdatedPanel,
            false
        );

        SetPanelActive(
            leftCoordinateUpdatedPanel,
            false
        );

        SetPanelActive(
            rightCoordinateUpdatedPanel,
            false
        );

        updatedPanelCoroutine =
            null;
    }

    // ============================================================
    // SET PANEL ACTIVE SAFELY
    // ============================================================

    private void SetPanelActive(
        GameObject panel,
        bool active
    )
    {
        if (
            panel != null
        )
        {
            panel.SetActive(
                active
            );
        }
    }

    // ============================================================
    // WORLD → SHARED
    // ============================================================

    public Vector3 WorldToSharedPosition(
        Vector3 worldPosition
    )
    {
        if (
            !HasOrigin
        )
        {
            return Vector3.zero;
        }

        return SharedOrigin
            .InverseTransformPoint(
                worldPosition
            );
    }

    // ============================================================
    // WORLD → SHARED ROTATION
    // ============================================================

    public Quaternion WorldToSharedRotation(
        Quaternion worldRotation
    )
    {
        if (
            !HasOrigin
        )
        {
            return Quaternion.identity;
        }

        return
            Quaternion.Inverse(
                SharedOrigin.rotation
            ) *
            worldRotation;
    }

    // ============================================================
    // SHARED → WORLD
    // ============================================================

    public Vector3 SharedToWorldPosition(
        Vector3 localPosition
    )
    {
        if (
            !HasOrigin
        )
        {
            return Vector3.zero;
        }

        return SharedOrigin
            .TransformPoint(
                localPosition
            );
    }

    // ============================================================
    // SHARED → WORLD ROTATION
    // ============================================================

    public Quaternion SharedToWorldRotation(
        Quaternion localRotation
    )
    {
        if (
            !HasOrigin
        )
        {
            return Quaternion.identity;
        }

        return
            SharedOrigin.rotation *
            localRotation;
    }
}