using UnityEngine;

public class HunterGunHandController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private Camera arCamera;

    [Header("Hand")]
    [SerializeField]
    private ARHandLandmarkVisualizer handVisualizer;

    [Header("Gun Hand Anchor")]
    [SerializeField]
    private Transform gunHandAnchor;

    [Header("3D Depth")]
    [Tooltip(
        "Distance in front of the AR camera at which " +
        "the virtual hand anchor is placed."
    )]
    [SerializeField]
    private float handDepth = 0.5f;

    private void Start()
    {
        if (arCamera == null)
        {
            arCamera =
                Camera.main;
        }

        if (handVisualizer == null)
        {
            handVisualizer =
                FindFirstObjectByType<
                    ARHandLandmarkVisualizer>();
        }
    }

    private void Update()
    {
        if (
            arCamera == null ||
            handVisualizer == null ||
            gunHandAnchor == null
        )
        {
            return;
        }

        // MediaPipe landmark 8 =
        // index fingertip.
        Vector2 fingertip =
            handVisualizer
                .GetLandmarkUIPosition(8);

        // Convert UI position to screen position.
        RectTransform overlay =
            handVisualizer
                .GetComponent<RectTransform>();

        Vector2 screenPosition =
            new Vector2(
                fingertip.x +
                Screen.width * 0.5f,

                fingertip.y +
                Screen.height * 0.5f
            );

        Ray ray =
            arCamera.ScreenPointToRay(
                screenPosition
            );

        gunHandAnchor.position =
            ray.origin +
            ray.direction *
            handDepth;
    }
}