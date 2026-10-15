using UnityEngine;

public class MecchaNetworkPoseReporter :
    MonoBehaviour
{
    private NetworkMecchaManager
        manager;

    private GameObject
        target;

    private Vector3
        previousPosition;

    private Quaternion
        previousRotation;

    private float
        nextSyncTime;

    private const float
        SyncInterval =
            0.2f;

    public void Initialize(
        NetworkMecchaManager
            networkManager,
        GameObject targetObject
    )
    {
        manager =
            networkManager;

        target =
            targetObject;

        previousPosition =
            target.transform.position;

        previousRotation =
            target.transform.rotation;
    }

    private void Update()
    {
        if (
            manager == null ||
            target == null
        )
        {
            return;
        }

        if (
            Time.time <
            nextSyncTime
        )
        {
            return;
        }

        nextSyncTime =
            Time.time +
            SyncInterval;

        float positionChange =
            Vector3.Distance(
                previousPosition,
                target.transform.position
            );

        float rotationChange =
            Quaternion.Angle(
                previousRotation,
                target.transform.rotation
            );

        if (
            positionChange <
                0.002f &&
            rotationChange <
                0.5f
        )
        {
            return;
        }

        previousPosition =
            target.transform.position;

        previousRotation =
            target.transform.rotation;

        manager.SyncHiderMeccha(
            target
        );
    }
}