using UnityEngine;

public class HunterGunPickupController : MonoBehaviour
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

    [Header("Raycast")]
    [SerializeField]
    private float rayDistance = 10f;

    private HunterGunPickup heldGun;

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

        Vector2 fingertip =
            handVisualizer
                .GetLandmarkUIPosition(8);

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

        // ---------------------------------------------------------
        // IF WE ALREADY HOLD A GUN
        // ---------------------------------------------------------

        if (heldGun != null)
        {
            return;
        }

        // ---------------------------------------------------------
        // LOOK FOR GUN
        // ---------------------------------------------------------

        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance
            )
        )
        {
            HunterGunPickup gun =
                hit.collider
                    .GetComponentInParent<
                        HunterGunPickup>();

            if (gun != null)
            {
                // For the prototype, immediately pick it up
                // when the fingertip ray hits it.
                heldGun = gun;

                gun.Pickup(
                    gunHandAnchor
                );

                Debug.Log(
                    "[Hunter] Gun selected: " +
                    gun.name
                );
            }
        }
    }

    public HunterGunPickup GetHeldGun()
    {
        return heldGun;
    }
}