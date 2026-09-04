using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARGameOcclusionManager : MonoBehaviour
{
    [Header("AR Occlusion Manager")]
    [SerializeField] private AROcclusionManager occlusionManager;

    [Header("Options")]
    [SerializeField] private bool enableEnvironmentDepth = true;
    [SerializeField] private bool enableHumanDepth = true;
    [SerializeField] private bool enableHumanStencil = true;

    private void Awake()
    {
        if (occlusionManager == null)
        {
            occlusionManager =
                FindFirstObjectByType<AROcclusionManager>();
        }
    }

    private void OnEnable()
    {
        ConfigureOcclusion();
    }

    private void ConfigureOcclusion()
    {
        if (occlusionManager == null)
        {
            Debug.LogWarning(
                "ARGameOcclusionManager: " +
                "No AROcclusionManager found."
            );

            return;
        }

        // ENVIRONMENT DEPTH

        if (enableEnvironmentDepth)
        {
            occlusionManager.requestedEnvironmentDepthMode =
                EnvironmentDepthMode.Fastest;
        }
        else
        {
            occlusionManager.requestedEnvironmentDepthMode =
                EnvironmentDepthMode.Disabled;
        }

        // HUMAN DEPTH

        if (enableHumanDepth)
        {
            occlusionManager.requestedHumanDepthMode =
                HumanSegmentationDepthMode.Fastest;
        }
        else
        {
            occlusionManager.requestedHumanDepthMode =
                HumanSegmentationDepthMode.Disabled;
        }

        // HUMAN STENCIL

        if (enableHumanStencil)
        {
            occlusionManager.requestedHumanStencilMode =
                HumanSegmentationStencilMode.Fastest;
        }
        else
        {
            occlusionManager.requestedHumanStencilMode =
                HumanSegmentationStencilMode.Disabled;
        }

        // LOG SUPPORT

        if (occlusionManager.descriptor != null)
        {
            Debug.Log(
                "AR Occlusion Support:"
            );

            Debug.Log(
                "Environment Depth: " +
                occlusionManager.descriptor
                    .environmentDepthImageSupported
            );

            Debug.Log(
                "Human Depth: " +
                occlusionManager.descriptor
                    .humanSegmentationDepthImageSupported
            );

            Debug.Log(
                "Human Stencil: " +
                occlusionManager.descriptor
                    .humanSegmentationStencilImageSupported
            );
        }
    }
}