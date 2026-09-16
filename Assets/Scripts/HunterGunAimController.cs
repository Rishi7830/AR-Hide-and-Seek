using UnityEngine;

public class HunterGunAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private ARHandLandmarkVisualizer handVisualizer;

    [SerializeField]
    private Transform heldGun;

    [Header("Gun")]
    [SerializeField]
    private Transform gunForward;

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

    public void UpdateAim(
        Transform gunTransform
    )
    {
        if (
            gunTransform == null ||
            handVisualizer == null
        )
        {
            return;
        }

        Vector2 wrist =
            handVisualizer
                .GetLandmarkUIPosition(0);

        Vector2 fingertip =
            handVisualizer
                .GetLandmarkUIPosition(8);

        Vector2 direction =
            fingertip -
            wrist;

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            return;
        }

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        gunTransform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }
}