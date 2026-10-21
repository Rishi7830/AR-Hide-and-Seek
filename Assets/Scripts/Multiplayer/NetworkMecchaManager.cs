using System.Collections.Generic;
using UnityEngine;

public class NetworkMecchaManager :
    MonoBehaviour
{
    [Header("Network")]
    [SerializeField]
    private NetworkGameState networkState;

    [Header("Texture Sync")]
    [SerializeField]
    private NetworkMecchaTextureManager
        textureManager;

    [SerializeField]
    private MultiplayerRoleController
        roleController;

    [SerializeField]
    private SharedAROriginManager
        originManager;

    [Header("Seeker Prefabs")]
    [SerializeField]
    private GameObject
        horizontalMecchaPrefab;

    [SerializeField]
    private GameObject
        verticalMecchaPrefab;

    private int nextMecchaId =
        1;

    // Store the real Hider Mecchas so
    // their shared poses can be updated
    // after an InitialAprilTag rescan.
    private Dictionary<int, GameObject>
        hiderMecchas =
            new Dictionary<
                int,
                GameObject
            >();

    private Dictionary<int, GameObject>
        seekerMecchas =
            new Dictionary<
                int,
                GameObject
            >();

    public void RegisterHiderMeccha(
        GameObject meccha,
        int type
    )
    {
        if (
            meccha == null ||
            originManager == null ||
            !originManager.HasOrigin ||
            roleController == null ||
            !roleController.IsHider()
        )
        {
            return;
        }

        NetworkMecchaIdentity identity =
            meccha.GetComponent<
                NetworkMecchaIdentity
            >();

        if (identity == null)
        {
            identity =
                meccha.AddComponent<
                    NetworkMecchaIdentity
                >();

            identity.Initialize(
                nextMecchaId,
                type
            );

            nextMecchaId++;
        }
        else
        {
            nextMecchaId =
                Mathf.Max(
                    nextMecchaId,
                    identity.MecchaId + 1
                );
        }

        // Remember the physical Hider
        // Meccha for later origin rescans.
        hiderMecchas[
            identity.MecchaId
        ] =
            meccha;

        SyncHiderMeccha(
            meccha
        );
    }

    public void SyncHiderMeccha(
        GameObject meccha
    )
    {
        if (
            meccha == null ||
            networkState == null ||
            !networkState.IsNetworkReady ||
            originManager == null ||
            !originManager.HasOrigin
        )
        {
            return;
        }

        if (
            networkState.Phase !=
            MultiplayerPhase.HiderHiding
        )
        {
            return;
        }

        NetworkMecchaIdentity identity =
            meccha.GetComponent<
                NetworkMecchaIdentity
            >();

        if (identity == null)
        {
            return;
        }

        Vector3 localPosition =
            originManager
                .WorldToSharedPosition(
                    meccha
                        .transform
                        .position
                );

        Quaternion localRotation =
            originManager
                .WorldToSharedRotation(
                    meccha
                        .transform
                        .rotation
                );

        Debug.Log(
            "[MecchaSync HIDER] ID=" +
            identity.MecchaId +
            " | World=" +
            meccha.transform.position +
            " | Shared=" +
            localPosition
        );

        networkState
            .RequestMecchaUpdate(
                identity.MecchaId,
                identity.MecchaType,
                localPosition,
                localRotation
            );
    }

    public void
        ResyncAllHiderMecchasAfterOriginChange()
    {
        if (
            roleController == null ||
            !roleController.IsHider() ||
            originManager == null ||
            !originManager.HasOrigin ||
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        if (
            networkState.Phase !=
            MultiplayerPhase.HiderHiding
        )
        {
            return;
        }

        List<int> invalidIds =
            new List<int>();

        int syncedCount =
            0;

        foreach (
            KeyValuePair<int, GameObject>
                pair
            in hiderMecchas
        )
        {
            if (pair.Value == null)
            {
                invalidIds.Add(
                    pair.Key
                );

                continue;
            }

            // The Hider Meccha itself is not
            // moved. Only its coordinates
            // relative to the NEW origin
            // are calculated again.
            SyncHiderMeccha(
                pair.Value
            );

            syncedCount++;
        }

        foreach (
            int id
            in invalidIds
        )
        {
            hiderMecchas.Remove(
                id
            );
        }

        Debug.Log(
            "[MecchaSync HIDER] SharedOrigin updated. " +
            "Re-synced " +
            syncedCount +
            " Meccha(s)."
        );
    }

    public void RemoveHiderMeccha(
        GameObject meccha
    )
    {
        if (meccha == null)
        {
            return;
        }

        NetworkMecchaIdentity identity =
            meccha.GetComponent<
                NetworkMecchaIdentity
            >();

        if (identity == null)
        {
            return;
        }

        hiderMecchas.Remove(
            identity.MecchaId
        );

        if (
            networkState != null &&
            networkState.IsNetworkReady
        )
        {
            networkState
                .RequestRemoveMeccha(
                    identity.MecchaId
                );
        }
    }

    public void ReportMecchaFound(
        GameObject meccha
    )
    {
        if (
            meccha == null ||
            roleController == null ||
            !roleController.IsSeeker()
        )
        {
            return;
        }

        NetworkMecchaIdentity identity =
            meccha.GetComponent<
                NetworkMecchaIdentity
            >();

        if (identity == null)
        {
            return;
        }

        if (
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        networkState
            .RequestMecchaFound(
                identity.MecchaId
            );
    }

    private void Update()
    {
        if (
            roleController == null ||
            !roleController.IsSeeker() ||
            originManager == null ||
            !originManager.HasOrigin ||
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        bool shouldShow =
            networkState.Phase ==
                MultiplayerPhase.SeekerSearching ||
            networkState.Phase ==
                MultiplayerPhase.Reveal ||
            networkState.Phase ==
                MultiplayerPhase.Result;

        if (!shouldShow)
        {
            HideAllSeekerMecchas();

            return;
        }

        RefreshSeekerMecchas();
    }

    private void RefreshSeekerMecchas()
    {
        if (
            originManager == null ||
            originManager.SharedOrigin == null
        )
        {
            return;
        }

        HashSet<int> activeIds =
            new HashSet<int>();

        for (
            int i = 0;
            i <
            networkState.Mecchas.Length;
            i++
        )
        {
            NetworkMecchaData data =
                networkState
                    .Mecchas
                    .Get(i);

            if (!data.Active)
            {
                continue;
            }

            activeIds.Add(
                data.Id
            );

            GameObject meccha;

            if (
                !seekerMecchas
                    .TryGetValue(
                        data.Id,
                        out meccha
                    ) ||
                meccha == null
            )
            {
                GameObject prefab =
                    data.Type == 0
                        ? horizontalMecchaPrefab
                        : verticalMecchaPrefab;

                if (prefab == null)
                {
                    Debug.LogError(
                        "[MecchaSync SEEKER] " +
                        "Missing Meccha prefab for type " +
                        data.Type
                    );

                    continue;
                }

                // Seeker Mecchas live directly
                // inside the shared coordinate frame.
                meccha =
                    Instantiate(
                        prefab,
                        originManager.SharedOrigin
                    );

                meccha.transform
                    .localPosition =
                    data.LocalPosition;

                meccha.transform
                    .localRotation =
                    data.LocalRotation;

                NetworkMecchaIdentity
                    identity =
                        meccha.GetComponent<
                            NetworkMecchaIdentity
                        >();

                if (identity == null)
                {
                    identity =
                        meccha.AddComponent<
                            NetworkMecchaIdentity
                        >();
                }

                identity.Initialize(
                    data.Id,
                    data.Type
                );

                seekerMecchas[
                    data.Id
                ] =
                    meccha;

                if (
                    textureManager != null
                )
                {
                    textureManager
                        .TryApplyCachedTexture(
                            data.Id,
                            meccha
                        );
                }

                Debug.Log(
                    "[MecchaSync SEEKER] Spawned ID=" +
                    data.Id +
                    " | Shared=" +
                    data.LocalPosition +
                    " | World=" +
                    meccha.transform.position
                );
            }

            // Safety check in case an existing
            // Meccha somehow lost its parent.
            if (
                meccha.transform.parent !=
                originManager.SharedOrigin
            )
            {
                meccha.transform.SetParent(
                    originManager.SharedOrigin,
                    false
                );
            }

            meccha.SetActive(
                true
            );

            // Photon values are already
            // SharedOrigin-local coordinates.
            meccha.transform.localPosition =
                data.LocalPosition;

            meccha.transform.localRotation =
                data.LocalRotation;
        }

        List<int> removeIds =
            new List<int>();

        foreach (
            KeyValuePair<int, GameObject>
                pair
            in seekerMecchas
        )
        {
            if (
                !activeIds.Contains(
                    pair.Key
                )
            )
            {
                if (
                    pair.Value != null
                )
                {
                    Destroy(
                        pair.Value
                    );
                }

                removeIds.Add(
                    pair.Key
                );
            }
        }

        foreach (
            int id
            in removeIds
        )
        {
            seekerMecchas.Remove(
                id
            );
        }
    }

    public bool TryGetSeekerMeccha(
        int mecchaId,
        out GameObject meccha
    )
    {
        return
            seekerMecchas.TryGetValue(
                mecchaId,
                out meccha
            ) &&
            meccha != null;
    }

    private void HideAllSeekerMecchas()
    {
        foreach (
            GameObject meccha
            in seekerMecchas.Values
        )
        {
            if (meccha != null)
            {
                meccha.SetActive(
                    false
                );
            }
        }
    }
}