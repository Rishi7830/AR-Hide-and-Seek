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
    [Tooltip("MediaPipe detection frequency. 10 FPS is recommended for mobile.")]
    [SerializeField]
    private float detectionFPS = 10f;

    [Tooltip("Width of the image sent to MediaPipe.")]
    [SerializeField]
    private int processingWidth = 640;

    [Tooltip("Height of the image sent to MediaPipe.")]
    [SerializeField]
    private int processingHeight = 480;

    [Header("Camera Image Orientation")]
    [SerializeField]
    private bool mirrorX = true;

    [SerializeField]
    private bool mirrorY = true;

    [Header("Debug")]
    [SerializeField]
    private bool logDetection = false;

    private HandLandmarker handLandmarker;
    private HandLandmarkerResult result;

    private NativeArray<byte> pixelBuffer;
    private Texture2D cameraTexture;

    private Coroutine trackingCoroutine;
    private Stopwatch stopwatch;

    private bool initialized = false;

    private int actualImageWidth;
    private int actualImageHeight;

    private void Start()
    {
        // Find AR camera manager
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

        // Find hand landmark visualizer
        if (visualizer == null)
        {
            visualizer =
                FindFirstObjectByType<
                    ARHandLandmarkVisualizer>();
        }

        if (visualizer == null)
        {
            UnityEngine.Debug.LogError(
                "[ARHand] ARHandLandmarkVisualizer was not found."
            );

            enabled = false;
            return;
        }

        // Check that the MediaPipe model is assigned
        if (handLandmarkerModel == null)
        {
            UnityEngine.Debug.LogError(
                "[ARHand] Hand Landmarker model has not been assigned."
            );

            enabled = false;
            return;
        }

        // Initialize MediaPipe
        InitializeMediaPipe();

        if (!initialized)
        {
            enabled = false;
            return;
        }

        // Start timestamp timer used by VIDEO mode
        stopwatch =
            new Stopwatch();

        stopwatch.Start();

        // Start hand tracking loop
        trackingCoroutine =
            StartCoroutine(
                TrackingLoop()
            );

        UnityEngine.Debug.Log(
            "[ARHand] Hand tracking started. " +
            "FPS = " +
            detectionFPS +
            ", Input = " +
            processingWidth +
            "x" +
            processingHeight
        );
    }

    private void InitializeMediaPipe()
    {
        try
        {
            // Configure MediaPipe to use CPU inference
            BaseOptions baseOptions =
                new BaseOptions(
                    BaseOptions.Delegate.CPU,
                    modelAssetBuffer:
                        handLandmarkerModel.bytes
                );

            // Configure the hand landmarker
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

            // Create MediaPipe hand landmarker
            handLandmarker =
                HandLandmarker.CreateFromOptions(
                    options
                );

            // Allocate result storage
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

    private IEnumerator TrackingLoop()
    {
        // Calculate delay between MediaPipe detections
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

        // Process camera frames at the selected detection FPS
        while (enabled)
        {
            ProcessLatestARFrame();

            yield return wait;
        }
    }

    private unsafe void ProcessLatestARFrame()
    {
        // Make sure MediaPipe is ready
        if (
            !initialized ||
            handLandmarker == null
        )
        {
            return;
        }

        // Acquire the latest AR Foundation CPU camera image
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
            // Configure image mirroring
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

            // Set the lower processing resolution for MediaPipe
            Vector2Int processingSize =
                new Vector2Int(
                    processingWidth,
                    processingHeight
                );

            // Configure AR camera image conversion
            XRCpuImage.ConversionParams conversionParams =
                new XRCpuImage.ConversionParams(
                    cpuImage,
                    TextureFormat.RGBA32,
                    transformation
                );

            conversionParams.outputDimensions =
                processingSize;

            // Calculate the required RGBA buffer size
            int requiredBytes =
                cpuImage.GetConvertedDataSize(
                    conversionParams
                );

            // Create or resize the reusable pixel buffer
            if (
                !pixelBuffer.IsCreated ||
                pixelBuffer.Length != requiredBytes
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
                        NativeArrayOptions.UninitializedMemory
                    );
            }

            // Get pointer to the native pixel buffer
            System.IntPtr destination =
                (System.IntPtr)
                NativeArrayUnsafeUtility
                    .GetUnsafePtr(
                        pixelBuffer
                    );

            // Convert AR camera image to resized RGBA32
            cpuImage.Convert(
                conversionParams,
                destination,
                pixelBuffer.Length
            );

            actualImageWidth =
                processingWidth;

            actualImageHeight =
                processingHeight;

            // Create or resize the Unity texture
            if (
                cameraTexture == null ||
                cameraTexture.width != actualImageWidth ||
                cameraTexture.height != actualImageHeight
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

            // Copy converted camera pixels into the Unity texture
            cameraTexture.LoadRawTextureData(
                pixelBuffer
            );

            // Upload texture data
            cameraTexture.Apply(
                false,
                false
            );

            // Wrap Unity texture as a MediaPipe image
            using Image image =
                new Image(
                    cameraTexture
                );

            // Generate timestamp required by MediaPipe VIDEO mode
            long timestampMilliseconds =
                stopwatch != null
                    ? stopwatch.ElapsedMilliseconds
                    : 0;

            // No additional image rotation is required
            ImageProcessingOptions imageProcessingOptions =
                new ImageProcessingOptions(
                    rotationDegrees: 0
                );

            // Run MediaPipe hand landmark detection
            bool detected =
                handLandmarker.TryDetectForVideo(
                    image,
                    timestampMilliseconds,
                    imageProcessingOptions,
                    ref result
                );

            // Process detected hand or hide landmarks
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

    private void ProcessResult(
        HandLandmarkerResult handResult
    )
    {
        // Check that MediaPipe returned a hand
        if (
            handResult.handLandmarks == null ||
            handResult.handLandmarks.Count == 0
        )
        {
            visualizer.HideHand();
            return;
        }

        // Use only the first detected hand
        var firstHand =
            handResult.handLandmarks[0];

        // Make sure all 21 landmarks are available
        if (
            firstHand.landmarks == null ||
            firstHand.landmarks.Count < 21
        )
        {
            visualizer.HideHand();
            return;
        }

        // Copy the 21 normalized landmark positions
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

        // Update the purple landmark points and connecting lines
        visualizer.UpdateHand(
            normalizedLandmarks,
            actualImageWidth,
            actualImageHeight
        );

        if (logDetection)
        {
            UnityEngine.Debug.Log(
                "[ARHand] Hand detected - " +
                "21 landmarks updated."
            );
        }
    }

    private void OnDisable()
    {
        // Stop tracking when MediaPipeHandManager is disabled
        if (trackingCoroutine != null)
        {
            StopCoroutine(
                trackingCoroutine
            );

            trackingCoroutine = null;
        }

        // Stop the MediaPipe timestamp timer
        if (stopwatch != null)
        {
            stopwatch.Stop();
        }

        // Hide any remaining hand landmarks
        if (visualizer != null)
        {
            visualizer.HideHand();
        }
    }

    private void OnDestroy()
    {
        // Stop tracking coroutine
        if (trackingCoroutine != null)
        {
            StopCoroutine(
                trackingCoroutine
            );

            trackingCoroutine = null;
        }

        // Stop and release timestamp timer
        if (stopwatch != null)
        {
            stopwatch.Stop();

            stopwatch = null;
        }

        // Release native camera pixel buffer
        if (pixelBuffer.IsCreated)
        {
            pixelBuffer.Dispose();
        }

        // Destroy MediaPipe input texture
        if (cameraTexture != null)
        {
            Destroy(
                cameraTexture
            );

            cameraTexture = null;
        }

        // Clear MediaPipe references
        handLandmarker = null;

        initialized = false;
    }
}