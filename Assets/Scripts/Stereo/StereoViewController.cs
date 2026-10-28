using UnityEngine;
using UnityEngine.UI;

public class StereoViewController :
    MonoBehaviour
{
    [Header("AR Camera")]
    [SerializeField]
    private Camera arCamera;

    [Header("AR Background")]
    [SerializeField]
    private RenderTexture arBackgroundRenderTexture;

    [SerializeField]
    private RawImage leftEyeBackgroundDisplay;

    [SerializeField]
    private RawImage rightEyeBackgroundDisplay;

    [Header("Stereo Cameras")]
    [SerializeField]
    private Camera leftEyeCamera;

    [SerializeField]
    private Camera rightEyeCamera;

    [Header("Stereo Displays")]
    [SerializeField]
    private RawImage leftEyeDisplay;

    [SerializeField]
    private RawImage rightEyeDisplay;

    [Header("Stereo Display Canvas")]
    [SerializeField]
    private GameObject stereoDisplayCanvas;

    [Header("Stereo Settings")]
    [Tooltip(
        "Distance between the virtual left and right eye cameras."
    )]
    [SerializeField]
    private float interpupillaryDistance =
        0.064f;

    [Tooltip(
        "Width of each eye RenderTexture."
    )]
    [SerializeField]
    private int renderTextureWidth =
        1920;

    [Tooltip(
        "Height of each eye RenderTexture."
    )]
    [SerializeField]
    private int renderTextureHeight =
        1080;

    [SerializeField]
    private string stereoLayerName =
        "StereoAR";

    [SerializeField]
    private string uiLayerName =
        "UI";

    [Header("Stereo Projection")]
    [Tooltip(
        "Use the exact projection matrix supplied by " +
        "the AR camera."
    )]
    [SerializeField]
    private bool useARCameraProjection =
        true;

    [Header("Debug")]
    [SerializeField]
    private bool flipVertical =
        false;

    private bool stereoActive =
        false;

    private int stereoLayerMask;

    private int uiLayerMask;

    private RenderTexture
        leftRenderTexture;

    private RenderTexture
        rightRenderTexture;

    private int
        originalMainCameraCullingMask;

    private bool
        originalMainCameraMaskStored =
        false;

    private RenderTexture
        originalMainCameraTargetTexture;

    private bool
        originalMainCameraTargetStored =
        false;

    // ============================================================
    // AWAKE
    // ============================================================

    private void Awake()
    {
        stereoLayerMask =
            LayerMask.GetMask(
                stereoLayerName
            );

        uiLayerMask =
            LayerMask.GetMask(
                uiLayerName
            );

        // --------------------------------------------------------
        // STORE ORIGINAL MAIN CAMERA SETTINGS
        // --------------------------------------------------------

        if (
            arCamera != null
        )
        {
            originalMainCameraCullingMask =
                arCamera.cullingMask;

            originalMainCameraMaskStored =
                true;

            originalMainCameraTargetTexture =
                arCamera.targetTexture;

            originalMainCameraTargetStored =
                true;
        }

        // --------------------------------------------------------
        // CREATE EYE RENDER TEXTURES
        // --------------------------------------------------------

        CreateRenderTextures();

        // --------------------------------------------------------
        // CONFIGURE DISPLAY OBJECTS
        // --------------------------------------------------------

        ConfigureBackgroundDisplays();

        ConfigureEyeDisplays();

        // --------------------------------------------------------
        // START IN NORMAL NON-STEREO MODE
        // --------------------------------------------------------

        DisableStereo();

        Debug.Log(
            "[Stereo] " +
            "StereoViewController initialized. " +
            "Eye RT = " +
            renderTextureWidth +
            "x" +
            renderTextureHeight
        );
    }

    // ============================================================
    // CREATE EYE RENDER TEXTURES
    // ============================================================

    private void CreateRenderTextures()
    {
        // --------------------------------------------------------
        // LEFT EYE
        // --------------------------------------------------------

        if (
            leftRenderTexture == null ||
            leftRenderTexture.width !=
                renderTextureWidth ||
            leftRenderTexture.height !=
                renderTextureHeight
        )
        {
            if (
                leftRenderTexture != null
            )
            {
                if (
                    leftRenderTexture.IsCreated()
                )
                {
                    leftRenderTexture.Release();
                }

                Destroy(
                    leftRenderTexture
                );
            }

            leftRenderTexture =
                new RenderTexture(
                    renderTextureWidth,
                    renderTextureHeight,
                    24,
                    RenderTextureFormat.ARGB32
                );

            leftRenderTexture.name =
                "StereoLeftRenderTexture";

            leftRenderTexture
                .useMipMap =
                false;

            leftRenderTexture
                .autoGenerateMips =
                false;

            leftRenderTexture
                .filterMode =
                FilterMode.Bilinear;

            leftRenderTexture.Create();
        }

        // --------------------------------------------------------
        // RIGHT EYE
        // --------------------------------------------------------

        if (
            rightRenderTexture == null ||
            rightRenderTexture.width !=
                renderTextureWidth ||
            rightRenderTexture.height !=
                renderTextureHeight
        )
        {
            if (
                rightRenderTexture != null
            )
            {
                if (
                    rightRenderTexture.IsCreated()
                )
                {
                    rightRenderTexture.Release();
                }

                Destroy(
                    rightRenderTexture
                );
            }

            rightRenderTexture =
                new RenderTexture(
                    renderTextureWidth,
                    renderTextureHeight,
                    24,
                    RenderTextureFormat.ARGB32
                );

            rightRenderTexture.name =
                "StereoRightRenderTexture";

            rightRenderTexture
                .useMipMap =
                false;

            rightRenderTexture
                .autoGenerateMips =
                false;

            rightRenderTexture
                .filterMode =
                FilterMode.Bilinear;

            rightRenderTexture.Create();
        }

        // --------------------------------------------------------
        // LEFT EYE DISPLAY
        // --------------------------------------------------------

        if (
            leftEyeDisplay != null
        )
        {
            leftEyeDisplay.texture =
                leftRenderTexture;

            ConfigureDisplay(
                leftEyeDisplay
            );
        }

        // --------------------------------------------------------
        // RIGHT EYE DISPLAY
        // --------------------------------------------------------

        if (
            rightEyeDisplay != null
        )
        {
            rightEyeDisplay.texture =
                rightRenderTexture;

            ConfigureDisplay(
                rightEyeDisplay
            );
        }
    }

    // ============================================================
    // CONFIGURE AR BACKGROUND DISPLAYS
    // ============================================================

    private void ConfigureBackgroundDisplays()
    {
        if (
            arBackgroundRenderTexture == null
        )
        {
            Debug.LogWarning(
                "[Stereo] " +
                "AR Background RenderTexture " +
                "is not assigned."
            );

            return;
        }

        // --------------------------------------------------------
        // LEFT BACKGROUND
        // --------------------------------------------------------

        if (
            leftEyeBackgroundDisplay !=
            null
        )
        {
            leftEyeBackgroundDisplay.texture =
                arBackgroundRenderTexture;

            leftEyeBackgroundDisplay.color =
                Color.white;

            leftEyeBackgroundDisplay
                .raycastTarget =
                false;

            ConfigureBackgroundUV(
                leftEyeBackgroundDisplay
            );
        }

        // --------------------------------------------------------
        // RIGHT BACKGROUND
        // --------------------------------------------------------

        if (
            rightEyeBackgroundDisplay !=
            null
        )
        {
            rightEyeBackgroundDisplay.texture =
                arBackgroundRenderTexture;

            rightEyeBackgroundDisplay.color =
                Color.white;

            rightEyeBackgroundDisplay
                .raycastTarget =
                false;

            ConfigureBackgroundUV(
                rightEyeBackgroundDisplay
            );
        }
    }

    // ============================================================
    // CONFIGURE BACKGROUND UV
    // ============================================================

    private void ConfigureBackgroundUV(
        RawImage display
    )
    {
        if (
            display == null
        )
        {
            return;
        }

        if (
            flipVertical
        )
        {
            display.uvRect =
                new Rect(
                    0f,
                    1f,
                    1f,
                    -1f
                );
        }
        else
        {
            display.uvRect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );
        }
    }

    // ============================================================
    // CONFIGURE EYE DISPLAYS
    // ============================================================

    private void ConfigureEyeDisplays()
    {
        if (
            leftEyeDisplay != null
        )
        {
            ConfigureDisplay(
                leftEyeDisplay
            );
        }

        if (
            rightEyeDisplay != null
        )
        {
            ConfigureDisplay(
                rightEyeDisplay
            );
        }
    }

    // ============================================================
    // CONFIGURE RAW IMAGE
    // ============================================================

    private void ConfigureDisplay(
        RawImage display
    )
    {
        if (
            display == null
        )
        {
            return;
        }

        display.raycastTarget =
            false;

        display.color =
            Color.white;

        // --------------------------------------------------------
        // KEEP THE RAW IMAGE'S EXISTING RECT TRANSFORM.
        //
        // We intentionally do NOT add or configure an
        // AspectRatioFitter here.
        //
        // Your existing LeftEyeDisplay and RightEyeDisplay
        // RectTransforms control the split-screen layout.
        // --------------------------------------------------------

        if (
            flipVertical
        )
        {
            display.uvRect =
                new Rect(
                    0f,
                    1f,
                    1f,
                    -1f
                );
        }
        else
        {
            display.uvRect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );
        }
    }

    // ============================================================
    // LATE UPDATE
    // ============================================================

    private void LateUpdate()
    {
        if (
            !stereoActive
        )
        {
            return;
        }

        // --------------------------------------------------------
        // KEEP EYES ALIGNED TO AR CAMERA
        // --------------------------------------------------------

        UpdateEyeTransforms();

        // --------------------------------------------------------
        // KEEP AR PROJECTION SYNCHRONIZED
        // --------------------------------------------------------

        UpdateEyeProjection();
    }

    // ============================================================
    // ENABLE STEREO
    // ============================================================

    public void EnableStereo()
    {
        if (
            arCamera == null ||
            leftEyeCamera == null ||
            rightEyeCamera == null ||
            leftEyeDisplay == null ||
            rightEyeDisplay == null
        )
        {
            Debug.LogError(
                "[Stereo] " +
                "Stereo references are missing."
            );

            return;
        }

        if (
            arBackgroundRenderTexture == null
        )
        {
            Debug.LogError(
                "[Stereo] " +
                "AR Background RenderTexture " +
                "is missing."
            );

            return;
        }

        stereoActive =
            true;

        // --------------------------------------------------------
        // MAKE SURE TEXTURES EXIST
        // --------------------------------------------------------

        CreateRenderTextures();

        ConfigureBackgroundDisplays();

        ConfigureEyeDisplays();

        // --------------------------------------------------------
        // MAIN CAMERA → AR BACKGROUND TEXTURE
        // --------------------------------------------------------

        arCamera.targetTexture =
            arBackgroundRenderTexture;

        // --------------------------------------------------------
        // LEFT EYE
        // --------------------------------------------------------

        ConfigureEyeCamera(
            leftEyeCamera,
            leftRenderTexture
        );

        // --------------------------------------------------------
        // RIGHT EYE
        // --------------------------------------------------------

        ConfigureEyeCamera(
            rightEyeCamera,
            rightRenderTexture
        );

        // --------------------------------------------------------
        // IMMEDIATE UPDATE
        // --------------------------------------------------------

        UpdateEyeTransforms();

        UpdateEyeProjection();

        // --------------------------------------------------------
        // MAIN CAMERA CULLING MASK
        // --------------------------------------------------------

        if (
            originalMainCameraMaskStored
        )
        {
            arCamera.cullingMask =
                originalMainCameraCullingMask &
                ~stereoLayerMask &
                ~uiLayerMask;
        }

        // --------------------------------------------------------
        // SHOW STEREO DISPLAY CANVAS
        // --------------------------------------------------------

        if (
            stereoDisplayCanvas != null
        )
        {
            stereoDisplayCanvas.SetActive(
                true
            );
        }

        // --------------------------------------------------------
        // SHOW LEFT BACKGROUND
        // --------------------------------------------------------

        if (
            leftEyeBackgroundDisplay !=
            null
        )
        {
            leftEyeBackgroundDisplay
                .gameObject
                .SetActive(
                    true
                );
        }

        // --------------------------------------------------------
        // SHOW RIGHT BACKGROUND
        // --------------------------------------------------------

        if (
            rightEyeBackgroundDisplay !=
            null
        )
        {
            rightEyeBackgroundDisplay
                .gameObject
                .SetActive(
                    true
                );
        }

        // --------------------------------------------------------
        // SHOW LEFT EYE
        // --------------------------------------------------------

        if (
            leftEyeDisplay != null
        )
        {
            leftEyeDisplay.gameObject
                .SetActive(
                    true
                );
        }

        // --------------------------------------------------------
        // SHOW RIGHT EYE
        // --------------------------------------------------------

        if (
            rightEyeDisplay != null
        )
        {
            rightEyeDisplay.gameObject
                .SetActive(
                    true
                );
        }

        Debug.Log(
            "[Stereo] " +
            "Stereo ENABLED. " +
            "Eye RT=" +
            renderTextureWidth +
            "x" +
            renderTextureHeight +
            " | AR Projection=" +
            useARCameraProjection
        );
    }

    // ============================================================
    // CONFIGURE EYE CAMERA
    // ============================================================

    private void ConfigureEyeCamera(
        Camera eyeCamera,
        RenderTexture targetTexture
    )
    {
        if (
            eyeCamera == null ||
            targetTexture == null ||
            arCamera == null
        )
        {
            return;
        }

        // --------------------------------------------------------
        // COPY CLIPPING / CAMERA MODE
        // --------------------------------------------------------

        eyeCamera.nearClipPlane =
            arCamera.nearClipPlane;

        eyeCamera.farClipPlane =
            arCamera.farClipPlane;

        eyeCamera.orthographic =
            arCamera.orthographic;

        if (
            arCamera.orthographic
        )
        {
            eyeCamera.orthographicSize =
                arCamera.orthographicSize;
        }

        // --------------------------------------------------------
        // NO CUSTOM FOV
        // NO CUSTOM ASPECT
        //
        // AR camera projection is applied separately.
        // --------------------------------------------------------

        // --------------------------------------------------------
        // RENDER STEREO AR + UI
        // --------------------------------------------------------

        eyeCamera.cullingMask =
            stereoLayerMask |
            uiLayerMask;

        // --------------------------------------------------------
        // FULL RENDER TEXTURE
        // --------------------------------------------------------

        eyeCamera.rect =
            new Rect(
                0f,
                0f,
                1f,
                1f
            );

        // --------------------------------------------------------
        // TRANSPARENT BACKGROUND
        // --------------------------------------------------------

        eyeCamera.clearFlags =
            CameraClearFlags.SolidColor;

        eyeCamera.backgroundColor =
            new Color(
                0f,
                0f,
                0f,
                0f
            );

        // --------------------------------------------------------
        // OUTPUT
        // --------------------------------------------------------

        eyeCamera.targetTexture =
            targetTexture;

        eyeCamera.enabled =
            true;
    }

    // ============================================================
    // UPDATE EYE PROJECTION
    // ============================================================

    private void UpdateEyeProjection()
    {
        if (
            arCamera == null ||
            leftEyeCamera == null ||
            rightEyeCamera == null
        )
        {
            return;
        }

        if (
            useARCameraProjection
        )
        {
            Matrix4x4 projection =
                arCamera.projectionMatrix;

            leftEyeCamera
                .projectionMatrix =
                projection;

            rightEyeCamera
                .projectionMatrix =
                projection;

            return;
        }

        // --------------------------------------------------------
        // FALLBACK
        // --------------------------------------------------------

        leftEyeCamera.fieldOfView =
            arCamera.fieldOfView;

        rightEyeCamera.fieldOfView =
            arCamera.fieldOfView;

        leftEyeCamera.aspect =
            arCamera.aspect;

        rightEyeCamera.aspect =
            arCamera.aspect;

        if (
            arCamera.orthographic
        )
        {
            leftEyeCamera.orthographicSize =
                arCamera.orthographicSize;

            rightEyeCamera.orthographicSize =
                arCamera.orthographicSize;
        }
    }

    // ============================================================
    // DISABLE STEREO
    // ============================================================

    public void DisableStereo()
    {
        stereoActive =
            false;

        // --------------------------------------------------------
        // LEFT EYE
        // --------------------------------------------------------

        if (
            leftEyeCamera != null
        )
        {
            leftEyeCamera.enabled =
                false;

            leftEyeCamera.targetTexture =
                null;
        }

        // --------------------------------------------------------
        // RIGHT EYE
        // --------------------------------------------------------

        if (
            rightEyeCamera != null
        )
        {
            rightEyeCamera.enabled =
                false;

            rightEyeCamera.targetTexture =
                null;
        }

        // --------------------------------------------------------
        // RESTORE MAIN CAMERA TARGET
        // --------------------------------------------------------

        if (
            arCamera != null &&
            originalMainCameraTargetStored
        )
        {
            arCamera.targetTexture =
                originalMainCameraTargetTexture;
        }

        // --------------------------------------------------------
        // RESTORE MAIN CAMERA CULLING MASK
        // --------------------------------------------------------

        if (
            arCamera != null &&
            originalMainCameraMaskStored
        )
        {
            arCamera.cullingMask =
                originalMainCameraCullingMask;
        }

        // --------------------------------------------------------
        // HIDE STEREO DISPLAY CANVAS
        // --------------------------------------------------------

        if (
            stereoDisplayCanvas != null
        )
        {
            stereoDisplayCanvas.SetActive(
                false
            );
        }

        // --------------------------------------------------------
        // HIDE BACKGROUND DISPLAYS
        // --------------------------------------------------------

        if (
            leftEyeBackgroundDisplay !=
            null
        )
        {
            leftEyeBackgroundDisplay
                .gameObject
                .SetActive(
                    false
                );
        }

        if (
            rightEyeBackgroundDisplay !=
            null
        )
        {
            rightEyeBackgroundDisplay
                .gameObject
                .SetActive(
                    false
                );
        }

        // --------------------------------------------------------
        // HIDE LEFT EYE DISPLAY
        // --------------------------------------------------------

        if (
            leftEyeDisplay != null
        )
        {
            leftEyeDisplay.gameObject
                .SetActive(
                    false
                );
        }

        // --------------------------------------------------------
        // HIDE RIGHT EYE DISPLAY
        // --------------------------------------------------------

        if (
            rightEyeDisplay != null
        )
        {
            rightEyeDisplay.gameObject
                .SetActive(
                    false
                );
        }
    }

    // ============================================================
    // UPDATE EYE POSITIONS
    // ============================================================

    private void UpdateEyeTransforms()
    {
        if (
            arCamera == null ||
            leftEyeCamera == null ||
            rightEyeCamera == null
        )
        {
            return;
        }

        float halfIPD =
            interpupillaryDistance /
            2f;

        Quaternion rotation =
            arCamera.transform.rotation;

        Vector3 center =
            arCamera.transform.position;

        Vector3 cameraRight =
            rotation *
            Vector3.right;

        Vector3 leftPosition =
            center -
            cameraRight *
            halfIPD;

        Vector3 rightPosition =
            center +
            cameraRight *
            halfIPD;

        leftEyeCamera.transform
            .SetPositionAndRotation(
                leftPosition,
                rotation
            );

        rightEyeCamera.transform
            .SetPositionAndRotation(
                rightPosition,
                rotation
            );
    }

    // ============================================================
    // PUBLIC STATUS
    // ============================================================

    public bool IsStereoActive
    {
        get
        {
            return stereoActive;
        }
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void OnDestroy()
    {
        if (
            arCamera != null
        )
        {
            if (
                originalMainCameraTargetStored
            )
            {
                arCamera.targetTexture =
                    originalMainCameraTargetTexture;
            }

            if (
                originalMainCameraMaskStored
            )
            {
                arCamera.cullingMask =
                    originalMainCameraCullingMask;
            }
        }

        if (
            leftEyeCamera != null
        )
        {
            leftEyeCamera.targetTexture =
                null;
        }

        if (
            rightEyeCamera != null
        )
        {
            rightEyeCamera.targetTexture =
                null;
        }

        // --------------------------------------------------------
        // RELEASE LEFT
        // --------------------------------------------------------

        if (
            leftRenderTexture != null
        )
        {
            if (
                leftRenderTexture.IsCreated()
            )
            {
                leftRenderTexture.Release();
            }

            Destroy(
                leftRenderTexture
            );

            leftRenderTexture =
                null;
        }

        // --------------------------------------------------------
        // RELEASE RIGHT
        // --------------------------------------------------------

        if (
            rightRenderTexture != null
        )
        {
            if (
                rightRenderTexture.IsCreated()
            )
            {
                rightRenderTexture.Release();
            }

            Destroy(
                rightRenderTexture
            );

            rightRenderTexture =
                null;
        }
    }
}