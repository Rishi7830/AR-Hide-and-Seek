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

public class ARMediaPipeHandTracker : MonoBehaviour
{
    [Header("AR Camera")]
    [SerializeField]
    private ARCameraManager arCameraManager;

    [Header("MediaPipe Model")]
    [Tooltip("Assign the hand_landmarker.bytes TextAsset.")]
    [SerializeField]
    private TextAsset handLandmarkerModel;

    [Header("Hand Visualization")]
    [SerializeField]
    private ARHandLandmarkVisualizer visualizer;

    // HAND TRACKING SETTINGS
    [Header("Hand Tracking Settings")]
    [Tooltip("Maximum number of hands to detect. Keep this at 1.")]
    [SerializeField]
    private int numHands = 1;
    [Range(0f, 1f)]

    [SerializeField]
    private float minDetectionConfidence = 0.5f;
    [Range(0f, 1f)]
    
    [SerializeField]
    private float minPresenceConfidence = 0.5f;
    [Range(0f, 1f)]
    
    [SerializeField]
    private float minTrackingConfidence = 0.5f;
    
    [Header("Performance")]
    [Tooltip(
        "Number of MediaPipe detections per second. " +
        "Start with 15 on the Galaxy A53."
    )]
    
    [SerializeField]
    private float detectionFPS = 15f;
    
    [Header("Camera Image Orientation")]
    [SerializeField]
    private bool mirrorX = false;
    
    [SerializeField]
    private bool mirrorY = true;
    
    [Header("Debug")]
    [SerializeField]
    private bool logDetection = false;

    // INTERNAL VARIABLES
    private HandLandmarker handLandmarker;
    private HandLandmarkerResult result;
    private NativeArray<byte> pixelBuffer;
    private Texture2D cameraTexture;
    private Coroutine trackingCoroutine;
    private Stopwatch stopwatch;
    private bool initialized = false;
    private void Start()
    {
        // FIND AR CAMERA MANAGER
        if (arCameraManager == null)
        {
            arCameraManager =
                FindFirstObjectByType<ARCameraManager>();
        }

        if (arCameraManager == null)
        {
            UnityEngine.Debug.LogError(
                "[ARHand] ARCameraManager was not found."
            );

            enabled = false;
            return;
        }

        // FIND HAND VISUALIZER
        if (visualizer == null)
        {
            visualizer =
                FindFirstObjectByType<
                    ARHandLandmarkVisualizer>();
        }

        if (visualizer == null)
        {
            UnityEngine.Debug.LogError(
                "[ARHand] ARHandLandmarkVisualizer " +
                "was not found."
            );

            enabled = false;
            return;
        }

        // CHECK MEDIAPIPE MODEL
        if (handLandmarkerModel == null)
        {
            UnityEngine.Debug.LogError(
                "[ARHand] Hand Landmarker model " +
                "has not been assigned."
            );

            enabled = false;
            return;
        }

        // INITIALIZE MEDIAPIPE
        InitializeMediaPipe();

        if (!initialized)
        {
            enabled = false;
            return;
        }

        // CREATE TIMESTAMP TIMER
        stopwatch =
            new Stopwatch();

        stopwatch.Start();

        // START HAND TRACKING
        trackingCoroutine =
            StartCoroutine(
                TrackingLoop()
            );

        UnityEngine.Debug.Log(
            "[ARHand] AR hand tracking started successfully."
        );
    }

    // INITIALIZE MEDIAPIPE
    private void InitializeMediaPipe()
    {
        try
        {
            // BASE OPTIONS
            BaseOptions baseOptions =
                new BaseOptions(
                    BaseOptions.Delegate.CPU,
                    modelAssetBuffer:
                        handLandmarkerModel.bytes
                );

            // HAND LANDMARKER OPTIONS
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

            // CREATE HAND LANDMARKER
            handLandmarker =
                HandLandmarker.CreateFromOptions(
                    options
                );
            result =
                HandLandmarkerResult.Alloc(
                    numHands
                );

            initialized = true;

            UnityEngine.Debug.Log(
                "[ARHand] MediaPipe HandLandmarker " +
                "initialized successfully. " +
                "Max hands = " +
                numHands
            );
        }
        catch (System.Exception exception)
        {
            initialized = false;

            UnityEngine.Debug.LogError(
                "[ARHand] MediaPipe initialization failed:\n" +
                exception
            );
        }
    }

    // TRACKING LOOP
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

        while (enabled)
        {
            ProcessLatestARFrame();

            yield return wait;
        }
    }

    // PROCESS LATEST AR CAMERA FRAME
    private unsafe void ProcessLatestARFrame()
    {
        if (
            !initialized ||
            handLandmarker == null
        )
        {
            return;
        }

        // GET LATEST AR FOUNDATION CPU CAMERA IMAGE
        if (
            !arCameraManager
                .TryAcquireLatestCpuImage(
                    out XRCpuImage cpuImage
                )
        )
        {
            return;
        }

        using (cpuImage)
        {
            int width =
                cpuImage.width;

            int height =
                cpuImage.height;

            int requiredBytes =
                width *
                height *
                4;

            // CREATE / RESIZE PIXEL BUFFER
            if (
                !pixelBuffer.IsCreated ||
                pixelBuffer.Length !=
                requiredBytes
            )
            {
                if (pixelBuffer.IsCreated)
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

            // IMAGE TRANSFORMATION
            XRCpuImage.Transformation transformation =
                XRCpuImage.Transformation.None;

            if (mirrorX)
            {
                transformation ^=
                    XRCpuImage.Transformation.MirrorX;
            }

            if (mirrorY)
            {
                transformation ^=
                    XRCpuImage.Transformation.MirrorY;
            }

            // CONVERT AR IMAGE TO RGBA32
            XRCpuImage.ConversionParams conversionParams =
                new XRCpuImage.ConversionParams(
                    cpuImage,
                    TextureFormat.RGBA32,
                    transformation
                );

            System.IntPtr destination =
                (System.IntPtr)
                NativeArrayUnsafeUtility
                    .GetUnsafePtr(
                        pixelBuffer
                    );

            cpuImage.Convert(
                conversionParams,
                destination,
                pixelBuffer.Length
            );

            // CREATE / RESIZE UNITY TEXTURE
            if (
                cameraTexture == null ||
                cameraTexture.width != width ||
                cameraTexture.height != height
            )
            {
                if (cameraTexture != null)
                {
                    Destroy(
                        cameraTexture
                    );
                }

                cameraTexture =
                    new Texture2D(
                        width,
                        height,
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

            // COPY AR CAMERA PIXELS INTO TEXTURE
            cameraTexture.LoadRawTextureData(
                pixelBuffer
            );

            cameraTexture.Apply(
                false,
                false
            );

            // CREATE MEDIAPIPE IMAGE
            using Image image =
                new Image(
                    cameraTexture
                );

            // TIMESTAMP
            long timestampMilliseconds =
                stopwatch != null
                    ? stopwatch.ElapsedMilliseconds
                    : 0;

            // MEDIAPIPE IMAGE PROCESSING OPTIONS
            ImageProcessingOptions imageProcessingOptions =
                new ImageProcessingOptions(
                    rotationDegrees: 0
                );

            // RUN MEDIAPIPE HAND LANDMARKER
            bool detected =
                handLandmarker.TryDetectForVideo(
                    image,
                    timestampMilliseconds,
                    imageProcessingOptions,
                    ref result
                );

            // HANDLE RESULT
            if (detected)
            {
                ProcessResult(
                    result
                );
            }
            else
            {
                visualizer.HideHand();

                if (logDetection)
                {
                    UnityEngine.Debug.Log(
                        "[ARHand] No hand detected."
                    );
                }
            }
        }
    }

    // PROCESS HAND LANDMARK RESULT
    private void ProcessResult(
        HandLandmarkerResult handResult
    )
    {
        // CHECK FOR HAND
        if (
            handResult.handLandmarks == null ||
            handResult.handLandmarks.Count == 0
        )
        {
            visualizer.HideHand();
            return;
        }

        // USE FIRST HAND ONLY
        var firstHand =
            handResult.handLandmarks[0];

        if (
            firstHand.landmarks == null ||
            firstHand.landmarks.Count < 21
        )
        {
            visualizer.HideHand();
            return;
        }

        // COPY THE 21 NORMALIZED LANDMARK POSITIONS
        Vector2[] normalizedLandmarks =
            new Vector2[21];

        for (
            int i = 0;
            i < 21;
            i++
        )
        {
            var landmark =
                firstHand.landmarks[i];

            normalizedLandmarks[i] =
                new Vector2(
                    landmark.x,
                    landmark.y
                );
        }

        // DRAW HAND POINTS + LINES
        visualizer.UpdateHand(
        normalizedLandmarks,
        cameraTexture.width,
        cameraTexture.height
    );

        if (logDetection)
        {
            UnityEngine.Debug.Log(
                "[ARHand] Hand detected - " +
                "21 landmarks updated."
            );
        }
    }

    // CLEANUP
    private void OnDestroy()
    {
        // STOP TRACKING LOOP
        if (trackingCoroutine != null)
        {
            StopCoroutine(
                trackingCoroutine
            );

            trackingCoroutine = null;
        }

        // STOP TIMER
        if (stopwatch != null)
        {
            stopwatch.Stop();

            stopwatch = null;
        }

        // RELEASE PIXEL BUFFER
        if (pixelBuffer.IsCreated)
        {
            pixelBuffer.Dispose();
        }

        // RELEASE CAMERA TEXTURE
        if (cameraTexture != null)
        {
            Destroy(
                cameraTexture
            );

            cameraTexture = null;
        }

        handLandmarker = null;
        initialized = false;
    }
}