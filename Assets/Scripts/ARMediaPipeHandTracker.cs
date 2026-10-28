using System.Collections;
using System.Diagnostics;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using Mediapipe;
using Mediapipe.Tasks.Core;
using Mediapipe.Tasks.Vision.Core;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class ARMediaPipeHandTracker :
    MonoBehaviour
{
    [Header("AR Camera")]
    [SerializeField]
    private ARCameraManager arCameraManager;

    [Header("MediaPipe Model")]
    [Tooltip(
        "Assign the hand_landmarker.bytes TextAsset."
    )]
    [SerializeField]
    private TextAsset handLandmarkerModel;

    [Header("Hand Visualization")]

    [Tooltip(
        "Original single-screen visualizer. " +
        "Keep this assigned as your fallback."
    )]
    [SerializeField]
    private ARHandLandmarkVisualizer
        visualizer;

    [Tooltip(
        "Stereo visualizer. " +
        "Enable its GameObject during Seeker stereo."
    )]
    [SerializeField]
    private StereoARHandLandmarkVisualizer
        stereoVisualizer;

    [Header("Hand Tracking Settings")]
    [Tooltip(
        "Maximum number of hands to detect. Keep this at 1."
    )]
    [SerializeField]
    private int numHands =
        1;

    [Range(0f, 1f)]
    [SerializeField]
    private float minDetectionConfidence =
        0.5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float minPresenceConfidence =
        0.5f;

    [Range(0f, 1f)]
    [SerializeField]
    private float minTrackingConfidence =
        0.5f;

    [Header("Performance")]
    [Tooltip(
        "MediaPipe detection frequency. " +
        "15 FPS is recommended for responsive Hunter gameplay."
    )]
    [SerializeField]
    private float detectionFPS =
        15f;

    [Tooltip(
        "Width of the image sent to MediaPipe."
    )]
    [SerializeField]
    private int processingWidth =
        640;

    [Tooltip(
        "Height of the image sent to MediaPipe."
    )]
    [SerializeField]
    private int processingHeight =
        480;

    [Header("Camera Image Orientation")]
    [SerializeField]
    private bool mirrorX =
        true;

    [SerializeField]
    private bool mirrorY =
        true;

    [Header("Debug")]
    [SerializeField]
    private bool logDetection =
        false;

    [Header("Landmark Smoothing")]
    [Tooltip(
        "Smooths MediaPipe landmark movement without adding too much delay."
    )]
    [SerializeField]
    [Range(0f, 1f)]
    private float landmarkSmoothing =
        0.65f;

    private HandLandmarker handLandmarker;

    private HandLandmarkerResult result;

    private NativeArray<byte>
        pixelBuffer;

    private Texture2D
        cameraTexture;

    private Coroutine
        trackingCoroutine;

    private Stopwatch
        stopwatch;

    private bool initialized =
        false;

    private int actualImageWidth;

    private int actualImageHeight;

    // Stores the previous smoothed position of each landmark.
    private Vector2[] smoothedLandmarks =
        new Vector2[21];

    // Tells us whether a valid previous landmark position exists.
    private bool hasSmoothedLandmarks =
        false;

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        // --------------------------------------------------------
        // FIND AR CAMERA MANAGER
        // --------------------------------------------------------

        if (
            arCameraManager == null
        )
        {
            arCameraManager =
                FindFirstObjectByType<
                    ARCameraManager
                >();
        }

        if (
            arCameraManager == null
        )
        {
            UnityEngine.Debug.LogError(
                "[ARHand] " +
                "ARCameraManager was not found."
            );

            enabled =
                false;

            return;
        }

        // --------------------------------------------------------
        // FIND ORIGINAL VISUALIZER
        // --------------------------------------------------------

        if (
            visualizer == null
        )
        {
            visualizer =
                FindFirstObjectByType<
                    ARHandLandmarkVisualizer
                >();
        }

        // --------------------------------------------------------
        // FIND STEREO VISUALIZER
        // --------------------------------------------------------

        if (
            stereoVisualizer == null
        )
        {
            stereoVisualizer =
                FindFirstObjectByType<
                    StereoARHandLandmarkVisualizer
                >();
        }

        // --------------------------------------------------------
        // MAKE SURE AT LEAST ONE VISUALIZER EXISTS
        // --------------------------------------------------------

        if (
            visualizer == null &&
            stereoVisualizer == null
        )
        {
            UnityEngine.Debug.LogError(
                "[ARHand] " +
                "No hand landmark visualizer was found."
            );

            enabled =
                false;

            return;
        }

        // --------------------------------------------------------
        // CHECK MODEL
        // --------------------------------------------------------

        if (
            handLandmarkerModel == null
        )
        {
            UnityEngine.Debug.LogError(
                "[ARHand] " +
                "Hand Landmarker model has not been assigned."
            );

            enabled =
                false;

            return;
        }

        // --------------------------------------------------------
        // INITIALIZE MEDIAPIPE
        // --------------------------------------------------------

        InitializeMediaPipe();

        if (
            !initialized
        )
        {
            enabled =
                false;

            return;
        }

        // --------------------------------------------------------
        // START TIMER
        // --------------------------------------------------------

        stopwatch =
            new Stopwatch();

        stopwatch.Start();

        // --------------------------------------------------------
        // START TRACKING
        // --------------------------------------------------------

        trackingCoroutine =
            StartCoroutine(
                TrackingLoop()
            );

        UnityEngine.Debug.Log(
            "[ARHand] " +
            "Hand tracking started. " +
            "FPS = " +
            detectionFPS +
            ", Input = " +
            processingWidth +
            "x" +
            processingHeight
        );
    }

    // ============================================================
    // INITIALIZE MEDIAPIPE
    // ============================================================

    private void InitializeMediaPipe()
    {
        try
        {
            BaseOptions baseOptions =
                new BaseOptions(
                    BaseOptions.Delegate.CPU,
                    modelAssetBuffer:
                        handLandmarkerModel.bytes
                );

            HandLandmarkerOptions options =
                new HandLandmarkerOptions(
                    baseOptions,
                    runningMode:
                        RunningMode.VIDEO,
                    numHands:
                        numHands,
                    minHandDetectionConfidence:
                        minDetectionConfidence,
                    minHandPresenceConfidence:
                        minPresenceConfidence,
                    minTrackingConfidence:
                        minTrackingConfidence
                );

            handLandmarker =
                HandLandmarker.CreateFromOptions(
                    options
                );

            result =
                HandLandmarkerResult.Alloc(
                    numHands
                );

            initialized =
                true;

            UnityEngine.Debug.Log(
                "[ARHand] " +
                "MediaPipe HandLandmarker " +
                "initialized successfully. " +
                "Max hands = " +
                numHands
            );
        }
        catch (
            System.Exception exception
        )
        {
            initialized =
                false;

            UnityEngine.Debug.LogError(
                "[ARHand] " +
                "MediaPipe initialization failed:\n" +
                exception
            );
        }
    }

    // ============================================================
    // TRACKING LOOP
    // ============================================================

    private IEnumerator TrackingLoop()
    {
        float interval =
            1f /
            Mathf.Max(
                1f,
                detectionFPS
            );

        WaitForSeconds wait =
            new WaitForSeconds(
                interval
            );

        while (
            enabled
        )
        {
            ProcessLatestARFrame();

            yield return wait;
        }
    }

    // ============================================================
    // PROCESS AR FRAME
    // ============================================================

    private unsafe void ProcessLatestARFrame()
    {
        if (
            !initialized ||
            handLandmarker == null
        )
        {
            return;
        }

        if (
            !arCameraManager
                .TryAcquireLatestCpuImage(
                    out XRCpuImage cpuImage
                )
        )
        {
            return;
        }

        using (
            cpuImage
        )
        {
            XRCpuImage.Transformation
                transformation =
                XRCpuImage
                    .Transformation
                    .None;

            if (
                mirrorX
            )
            {
                transformation ^=
                    XRCpuImage
                        .Transformation
                        .MirrorX;
            }

            if (
                mirrorY
            )
            {
                transformation ^=
                    XRCpuImage
                        .Transformation
                        .MirrorY;
            }

            Vector2Int processingSize =
                new Vector2Int(
                    processingWidth,
                    processingHeight
                );

            XRCpuImage.ConversionParams
                conversionParams =
                new XRCpuImage
                    .ConversionParams(
                        cpuImage,
                        TextureFormat.RGBA32,
                        transformation
                    );

            conversionParams
                .outputDimensions =
                processingSize;

            int requiredBytes =
                cpuImage
                    .GetConvertedDataSize(
                        conversionParams
                    );

            if (
                !pixelBuffer.IsCreated ||
                pixelBuffer.Length !=
                    requiredBytes
            )
            {
                if (
                    pixelBuffer.IsCreated
                )
                {
                    pixelBuffer.Dispose();
                }

                pixelBuffer =
                    new NativeArray<byte>(
                        requiredBytes,
                        Allocator.Persistent,
                        NativeArrayOptions
                            .UninitializedMemory
                    );
            }

            System.IntPtr destination =
                (
                    System.IntPtr
                )
                NativeArrayUnsafeUtility
                    .GetUnsafePtr(
                        pixelBuffer
                    );

            cpuImage.Convert(
                conversionParams,
                destination,
                pixelBuffer.Length
            );

            actualImageWidth =
                processingWidth;

            actualImageHeight =
                processingHeight;

            if (
                cameraTexture == null ||
                cameraTexture.width !=
                    actualImageWidth ||
                cameraTexture.height !=
                    actualImageHeight
            )
            {
                if (
                    cameraTexture != null
                )
                {
                    Destroy(
                        cameraTexture
                    );
                }

                cameraTexture =
                    new Texture2D(
                        actualImageWidth,
                        actualImageHeight,
                        TextureFormat.RGBA32,
                        false
                    );

                cameraTexture.name =
                    "ARCamera_MediaPipe_Input";

                cameraTexture.wrapMode =
                    TextureWrapMode.Clamp;

                cameraTexture.filterMode =
                    FilterMode.Bilinear;
            }

            cameraTexture.LoadRawTextureData(
                pixelBuffer
            );

            cameraTexture.Apply(
                false,
                false
            );

            using Image image =
                new Image(
                    cameraTexture
                );

            long timestampMilliseconds =
                stopwatch != null
                    ? stopwatch
                        .ElapsedMilliseconds
                    : 0;

            ImageProcessingOptions
                imageProcessingOptions =
                new ImageProcessingOptions(
                    rotationDegrees:
                        0
                );

            bool detected =
                handLandmarker
                    .TryDetectForVideo(
                        image,
                        timestampMilliseconds,
                        imageProcessingOptions,
                        ref result
                    );

            if (
                detected
            )
            {
                ProcessResult(
                    result
                );
            }
            else
            {
                HideAllVisualizers();

                hasSmoothedLandmarks =
                    false;

                if (
                    logDetection
                )
                {
                    UnityEngine.Debug.Log(
                        "[ARHand] " +
                        "No hand detected."
                    );
                }
            }
        }
    }

    // ============================================================
    // PROCESS MEDIAPIPE RESULT
    // ============================================================

    private void ProcessResult(
        HandLandmarkerResult handResult
    )
    {
        if (
            handResult.handLandmarks == null ||
            handResult.handLandmarks.Count == 0
        )
        {
            HideAllVisualizers();

            hasSmoothedLandmarks =
                false;

            return;
        }

        var firstHand =
            handResult
                .handLandmarks[0];

        if (
            firstHand.landmarks == null ||
            firstHand.landmarks.Count < 21
        )
        {
            HideAllVisualizers();

            hasSmoothedLandmarks =
                false;

            return;
        }

        // --------------------------------------------------------
        // SMOOTH LANDMARKS
        // --------------------------------------------------------

        for (
            int i = 0;
            i < 21;
            i++
        )
        {
            var landmark =
                firstHand.landmarks[i];

            Vector2 currentPosition =
                new Vector2(
                    landmark.x,
                    landmark.y
                );

            if (
                !hasSmoothedLandmarks
            )
            {
                smoothedLandmarks[i] =
                    currentPosition;
            }
            else
            {
                smoothedLandmarks[i] =
                    Vector2.Lerp(
                        smoothedLandmarks[i],
                        currentPosition,
                        landmarkSmoothing
                    );
            }
        }

        hasSmoothedLandmarks =
            true;

        // --------------------------------------------------------
        // SELECT ACTIVE VISUALIZER
        // --------------------------------------------------------

        bool stereoIsActive =
            stereoVisualizer != null &&
            stereoVisualizer
                .isActiveAndEnabled;

        // --------------------------------------------------------
        // STEREO VISUALIZER
        // --------------------------------------------------------

        if (
            stereoIsActive
        )
        {
            if (
                visualizer != null
            )
            {
                visualizer.HideHand();
            }

            stereoVisualizer.UpdateHand(
                smoothedLandmarks,
                actualImageWidth,
                actualImageHeight
            );
        }
        else
        {
            // ----------------------------------------------------
            // ORIGINAL VISUALIZER
            // ----------------------------------------------------

            if (
                stereoVisualizer != null
            )
            {
                stereoVisualizer.HideHand();
            }

            if (
                visualizer != null
            )
            {
                visualizer.UpdateHand(
                    smoothedLandmarks,
                    actualImageWidth,
                    actualImageHeight
                );
            }
        }

        if (
            logDetection
        )
        {
            UnityEngine.Debug.Log(
                "[ARHand] " +
                "Hand detected - " +
                "21 landmarks updated. " +
                "Stereo=" +
                stereoIsActive
            );
        }
    }

    // ============================================================
    // HIDE ALL VISUALIZERS
    // ============================================================

    private void HideAllVisualizers()
    {
        if (
            visualizer != null
        )
        {
            visualizer.HideHand();
        }

        if (
            stereoVisualizer != null
        )
        {
            stereoVisualizer.HideHand();
        }
    }

    // ============================================================
    // DISABLE
    // ============================================================

    private void OnDisable()
    {
        if (
            trackingCoroutine != null
        )
        {
            StopCoroutine(
                trackingCoroutine
            );

            trackingCoroutine =
                null;
        }

        if (
            stopwatch != null
        )
        {
            stopwatch.Stop();
        }

        HideAllVisualizers();

        hasSmoothedLandmarks =
            false;
    }

    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (
            trackingCoroutine != null
        )
        {
            StopCoroutine(
                trackingCoroutine
            );

            trackingCoroutine =
                null;
        }

        if (
            stopwatch != null
        )
        {
            stopwatch.Stop();

            stopwatch =
                null;
        }

        if (
            pixelBuffer.IsCreated
        )
        {
            pixelBuffer.Dispose();
        }

        if (
            cameraTexture != null
        )
        {
            Destroy(
                cameraTexture
            );

            cameraTexture =
                null;
        }

        handLandmarker =
            null;

        initialized =
            false;
    }
}