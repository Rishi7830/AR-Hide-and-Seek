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
            !originManager.HasOrigin ||
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

        // Send the initial spawn pose once.
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
            " World=" +
            meccha.transform.position +
            " Shared=" +
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

        networkState
            .RequestRemoveMeccha(
                identity.MecchaId
            );
    }

    public void ReportMecchaFound(
        GameObject meccha
    )
    {
        if (
            meccha == null ||
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

        networkState
            .RequestMecchaFound(
                identity.MecchaId
            );
    }

    private void Update()
    {
        if (
            !roleController.IsSeeker() ||
            !originManager.HasOrigin
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

    private void RefreshSeekerMecchas()
    {
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

                Vector3 worldPosition =
                    originManager
                        .SharedToWorldPosition(
                            data.LocalPosition
                        );

                Quaternion worldRotation =
                    originManager
                        .SharedToWorldRotation(
                            data.LocalRotation
                        );

                meccha =
                    Instantiate(
                        prefab,
                        worldPosition,
                        worldRotation
                    );

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
            }

            meccha.SetActive(
                true
            );

            //meccha.transform
            //.localPosition =
            //data.LocalPosition;

            //meccha.transform
            //.localRotation =
            //data.LocalRotation;

            meccha.transform.position =
                originManager
                    .SharedToWorldPosition(
                        data.LocalPosition
                    );

            meccha.transform.rotation =
                originManager
                    .SharedToWorldRotation(
                        data.LocalRotation
                    );
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