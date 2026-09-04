using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARDebugOcclusion : MonoBehaviour
{
    [Header("AR Occlusion Manager")]
    [SerializeField] private AROcclusionManager occlusionManager;

    private float timer;

    private void Start()
    {
        if (occlusionManager == null)
        {
            occlusionManager =
                FindFirstObjectByType<AROcclusionManager>();
        }

        if (occlusionManager == null)
        {
            Debug.LogError(
                "[OCCLUSION] AROcclusionManager NOT FOUND."
            );
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Print diagnostics every 2 seconds.
        if (timer < 2f)
            return;

        timer = 0f;

        PrintDiagnostics();
    }

    private void PrintDiagnostics()
    {
        if (occlusionManager == null)
        {
            Debug.LogError(
                "[OCCLUSION] AROcclusionManager NOT FOUND."
            );

            return;
        }

        Debug.Log(
            "========== AR OCCLUSION DIAGNOSTICS =========="
        );

        // ---------------------------------------------------------
        // ENVIRONMENT DEPTH MODES
        // ---------------------------------------------------------

        Debug.Log(
            "[OCCLUSION] Requested Environment Depth: " +
            occlusionManager.requestedEnvironmentDepthMode
        );

        Debug.Log(
            "[OCCLUSION] Current Environment Depth: " +
            occlusionManager.currentEnvironmentDepthMode
        );

        // ---------------------------------------------------------
        // ENVIRONMENT DEPTH TEXTURE
        // ---------------------------------------------------------

        bool hasEnvironmentDepth =
            occlusionManager.TryGetEnvironmentDepthTexture(
                out Texture environmentDepthTexture
            );

        Debug.Log(
            "[OCCLUSION] Environment Depth Texture: " +
            (
                hasEnvironmentDepth &&
                environmentDepthTexture != null
                    ? "AVAILABLE"
                    : "NULL / UNAVAILABLE"
            )
        );

        // ---------------------------------------------------------
        // HUMAN DEPTH
        // ---------------------------------------------------------

        Debug.Log(
            "[OCCLUSION] Requested Human Depth: " +
            occlusionManager.requestedHumanDepthMode
        );

        Debug.Log(
            "[OCCLUSION] Current Human Depth: " +
            occlusionManager.currentHumanDepthMode
        );

        Debug.Log(
            "[OCCLUSION] Human Depth Texture: " +
            (
                occlusionManager.humanDepthTexture != null
                    ? "AVAILABLE"
                    : "NULL / UNAVAILABLE"
            )
        );

        // ---------------------------------------------------------
        // HUMAN STENCIL
        // ---------------------------------------------------------

        Debug.Log(
            "[OCCLUSION] Requested Human Stencil: " +
            occlusionManager.requestedHumanStencilMode
        );

        Debug.Log(
            "[OCCLUSION] Current Human Stencil: " +
            occlusionManager.currentHumanStencilMode
        );

        Debug.Log(
            "[OCCLUSION] Human Stencil Texture: " +
            (
                occlusionManager.humanStencilTexture != null
                    ? "AVAILABLE"
                    : "NULL / UNAVAILABLE"
            )
        );

        // ---------------------------------------------------------
        // OCCLUSION PREFERENCE
        // ---------------------------------------------------------

        Debug.Log(
            "[OCCLUSION] Requested Preference: " +
            occlusionManager.requestedOcclusionPreferenceMode
        );

        Debug.Log(
            "[OCCLUSION] Current Preference: " +
            occlusionManager.currentOcclusionPreferenceMode
        );

        // ---------------------------------------------------------
        // TEMPORAL SMOOTHING
        // ---------------------------------------------------------

        Debug.Log(
            "[OCCLUSION] Temporal Smoothing Requested: " +
            occlusionManager
                .environmentDepthTemporalSmoothingRequested
        );

        Debug.Log(
            "[OCCLUSION] Temporal Smoothing Enabled: " +
            occlusionManager
                .environmentDepthTemporalSmoothingEnabled
        );

        Debug.Log(
            "================================================"
        );
    }
}