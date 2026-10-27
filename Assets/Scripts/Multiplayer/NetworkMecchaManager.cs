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

    private Dictionary<int, GameObject>
        hiderMecchas =
            new Dictionary<
                int,
                GameObject
            >();

    // SEEKER MECCHAS

    private Dictionary<int, GameObject>
        seekerMecchas =
            new Dictionary<
                int,
                GameObject
            >();

    // REGISTER HIDER MECCHA

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

        // FIND IDENTITY

        NetworkMecchaIdentity identity =
            meccha.GetComponent<
                NetworkMecchaIdentity
            >();

        // ADD IDENTITY IF MISSING

        if (identity == null)
        {
            identity =
                meccha.AddComponent<
                    NetworkMecchaIdentity
                >();
        }

        if (!identity.IsInitialized)
        {
            identity.Initialize(
                nextMecchaId,
                type
            );

            Debug.Log(
                "[MecchaSync HIDER] " +
                "Assigned Meccha ID=" +
                identity.MecchaId +
                " | Type=" +
                identity.MecchaType +
                " | Object=" +
                meccha.name
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

        // STORE HIDER MECCHA

        hiderMecchas[
            identity.MecchaId
        ] =
            meccha;

        // SEND INITIAL POSE

        SyncHiderMeccha(
            meccha
        );
    }

    // SYNC HIDER MECCHA

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

        // CONVERT WORLD POSE TO SHARED ORIGIN POSE

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
            "[MecchaSync HIDER] " +
            "ID=" +
            identity.MecchaId +
            " | World=" +
            meccha.transform.position +
            " | Shared=" +
            localPosition
        );

        // SEND TO NETWORK STATE

        networkState
            .RequestMecchaUpdate(
                identity.MecchaId,
                identity.MecchaType,
                localPosition,
                localRotation
            );
    }

    // RESYNC ALL HIDER MECCHAS AFTER ORIGIN RESCAN

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

            SyncHiderMeccha(
                pair.Value
            );

            syncedCount++;
        }

        // REMOVE INVALID REFERENCES

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
            "[MecchaSync HIDER] " +
            "SharedOrigin updated. " +
            "Re-synced " +
            syncedCount +
            " Meccha(s)."
        );
    }

    // REMOVE HIDER MECCHA

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

        // Remove from local Hider dictionary.
        hiderMecchas.Remove(
            identity.MecchaId
        );

        // Remove from network.
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

    // REPORT MECCHA FOUND

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
                MultiplayerPhase
                    .SeekerSearching ||
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

    // REFRESH SEEKER MECCHAS

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

        // PROCESS EVERY NETWORK MECCHA

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

            // CREATE SEEKER MECCHA IF NEEDED

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

                // CREATE DIRECTLY UNDER SHARED ORIGIN

                meccha =
                    Instantiate(
                        prefab,
                        originManager
                            .SharedOrigin
                    );

                // SET INITIAL SHARED POSE

                meccha.transform
                    .localPosition =
                    data.LocalPosition;

                meccha.transform
                    .localRotation =
                    data.LocalRotation;

                // FIND / CREATE IDENTITY

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
                }

                // IMPORTANT:
                // Initialize the identity with the
                // networked ID received from Photon.
                identity.Initialize(
                    data.Id,
                    data.Type
                );

                // STORE SEEKER MECCHA

                seekerMecchas[
                    data.Id
                ] =
                    meccha;

                // APPLY CACHED PAINT

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
                    "[MecchaSync SEEKER] " +
                    "Spawned ID=" +
                    data.Id +
                    " | Shared=" +
                    data.LocalPosition +
                    " | World=" +
                    meccha.transform.position
                );
            }

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

            // APPLY NETWORKED SHARED POSE

            // Photon stores Meccha coordinates relative
            // to SharedOrigin.

            meccha.transform.localPosition =
                data.LocalPosition;

            meccha.transform.localRotation =
                data.LocalRotation;
        }

        // REMOVE LOCAL MECCHAS THAT ARE NO LONGER ACTIVE

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

    // GET SEEKER MECCHA

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

    // HIDE ALL SEEKER MECCHAS

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