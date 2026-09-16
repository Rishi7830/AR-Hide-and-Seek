using UnityEngine;

public class HunterGunPickup : MonoBehaviour
{
    [Header("Gun")]
    [SerializeField]
    private Transform gunVisual;

    [Header("Pickup")]
    [SerializeField]
    private Collider pickupCollider;

    private bool isPickedUp = false;

    private Transform originalParent;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private void Awake()
    {
        if (gunVisual == null)
        {
            gunVisual = transform;
        }

        if (pickupCollider == null)
        {
            pickupCollider =
                GetComponent<Collider>();
        }

        originalParent =
            transform.parent;

        originalLocalPosition =
            transform.localPosition;

        originalLocalRotation =
            transform.localRotation;
    }

    public bool IsPickedUp()
    {
        return isPickedUp;
    }

    public void Pickup(
        Transform handAnchor
    )
    {
        if (handAnchor == null)
        {
            return;
        }

        isPickedUp = true;

        transform.SetParent(
            handAnchor,
            false
        );

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        Debug.Log(
            "[HunterGun] Gun picked up: " +
            gameObject.name
        );
    }

    public void Drop()
    {
        isPickedUp = false;

        transform.SetParent(
            originalParent,
            false
        );

        transform.localPosition =
            originalLocalPosition;

        transform.localRotation =
            originalLocalRotation;

        if (pickupCollider != null)
        {
            pickupCollider.enabled = true;
        }
    }
}