using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetworkMecchaTextureManager :
    MonoBehaviour,
    INetworkRunnerCallbacks
{
    [Header("Network")]
    [SerializeField]
    private NetworkRunner runner;

    [SerializeField]
    private NetworkGameState networkState;

    [SerializeField]
    private MultiplayerRoleController
        roleController;

    [SerializeField]
    private NetworkMecchaManager
        mecchaManager;

    private const int TextureMessageType =
        77001;

    private readonly Dictionary<
        int,
        byte[]
    > receivedTextures =
        new Dictionary<
            int,
            byte[]
        >();

    private MultiplayerPhase previousPhase =
        (MultiplayerPhase)(-1);

    private bool texturesSentThisRound =
        false;

    private void Start()
    {
        if (runner != null)
        {
            runner.AddCallbacks(
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(
                this
            );
        }
    }

    private void Update()
    {
        if (
            networkState == null ||
            roleController == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        MultiplayerPhase phase =
            networkState.Phase;

        // Send textures once when
        // Hider gameplay finishes.
        if (
            roleController.IsHider() &&
            !texturesSentThisRound &&
            previousPhase ==
                MultiplayerPhase.HiderHiding &&
            phase ==
                MultiplayerPhase.SeekerSearching
        )
        {
            texturesSentThisRound =
                true;

            SendAllHiderTextures();
        }

        // Prepare for another round.
        if (
            phase ==
            MultiplayerPhase.WaitingForPlayers &&
            previousPhase !=
                MultiplayerPhase.WaitingForPlayers
        )
        {
            texturesSentThisRound =
                false;

            receivedTextures.Clear();
        }

        previousPhase =
            phase;
    }

    private void SendAllHiderTextures()
    {
        if (
            runner == null ||
            networkState == null
        )
        {
            return;
        }

        PlayerRef seeker =
            networkState.SeekerPlayer;

        if (
            seeker ==
            PlayerRef.None
        )
        {
            Debug.LogWarning(
                "[MecchaTextureNet] " +
                "No Seeker player available."
            );

            return;
        }

        NetworkMecchaIdentity[] identities =
            FindObjectsByType<
                NetworkMecchaIdentity
            >(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (
            NetworkMecchaIdentity identity
            in identities
        )
        {
            if (identity == null)
            {
                continue;
            }

            MecchaTexturePainter painter =
                identity.GetComponent<
                    MecchaTexturePainter
                >();

            if (painter == null)
            {
                painter =
                    identity.GetComponentInChildren<
                        MecchaTexturePainter
                    >(
                        true
                    );
            }

            if (
                painter == null ||
                !painter.HasPaintedData()
            )
            {
                continue;
            }

            byte[] pngData =
                painter.GetPaintTexturePNG();

            if (
                pngData == null ||
                pngData.Length == 0
            )
            {
                continue;
            }

            int mecchaId =
                identity.MecchaId;

            ReliableKey key =
                ReliableKey.FromInts(
                    TextureMessageType,
                    mecchaId,
                    0,
                    0
                );

            runner.SendReliableDataToPlayer(
                seeker,
                key,
                pngData
            );

            Debug.Log(
                "[MecchaTextureNet] Sent texture. " +
                "ID = " +
                mecchaId +
                " | Bytes = " +
                pngData.Length
            );
        }
    }

    private void HandleReceivedTexture(
        ReliableKey key,
        byte[] data
    )
    {
        if (
            roleController == null ||
            !roleController.IsSeeker()
        )
        {
            return;
        }

        key.GetInts(
            out int messageType,
            out int mecchaId,
            out int unused1,
            out int unused2
        );

        if (
            messageType !=
            TextureMessageType
        )
        {
            return;
        }

        if (
            data == null ||
            data.Length == 0
        )
        {
            return;
        }

        // Keep a copy in case the Seeker
        // Meccha has not spawned yet.
        receivedTextures[
            mecchaId
        ] =
            data;

        Debug.Log(
            "[MecchaTextureNet] Received texture. " +
            "ID = " +
            mecchaId +
            " | Bytes = " +
            data.Length
        );

        if (
            mecchaManager != null &&
            mecchaManager
                .TryGetSeekerMeccha(
                    mecchaId,
                    out GameObject meccha
                )
        )
        {
            ApplyTexture(
                mecchaId,
                meccha
            );
        }
    }

    public void TryApplyCachedTexture(
        int mecchaId,
        GameObject meccha
    )
    {
        if (meccha == null)
        {
            return;
        }

        ApplyTexture(
            mecchaId,
            meccha
        );
    }

    private void ApplyTexture(
        int mecchaId,
        GameObject meccha
    )
    {
        if (
            !receivedTextures.TryGetValue(
                mecchaId,
                out byte[] pngData
            )
        )
        {
            return;
        }

        MecchaTexturePainter painter =
            meccha.GetComponent<
                MecchaTexturePainter
            >();

        if (painter == null)
        {
            painter =
                meccha.GetComponentInChildren<
                    MecchaTexturePainter
                >(
                    true
                );
        }

        if (painter == null)
        {
            Debug.LogWarning(
                "[MecchaTextureNet] " +
                "No MecchaTexturePainter found for ID " +
                mecchaId
            );

            return;
        }

        if (
            painter.ApplyNetworkPaintPNG(
                pngData
            )
        )
        {
            Debug.Log(
                "[MecchaTextureNet] " +
                "Applied texture to Seeker Meccha ID " +
                mecchaId
            );
        }
    }

    // Fusion 2.0.x callback.
    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ArraySegment<byte> data
    )
    {
        // Only accept texture data from the Hider.
        if (
            networkState != null &&
            player !=
                networkState.HiderPlayer
        )
        {
            return;
        }

        byte[] copy =
            new byte[
                data.Count
            ];

        if (
            data.Array != null
        )
        {
            Buffer.BlockCopy(
                data.Array,
                data.Offset,
                copy,
                0,
                data.Count
            );
        }

        HandleReceivedTexture(
            key,
            copy
        );
    }

    // Fusion 2.1 callback.
    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data
    )
    {
        if (
            networkState != null &&
            player !=
                networkState.HiderPlayer
        )
        {
            return;
        }

        HandleReceivedTexture(
            key,
            data.ToArray()
        );
    }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress
    )
    {
    }

    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player
    )
    {
    }

    public void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player
    )
    {
    }

    public void OnInput(
        NetworkRunner runner,
        NetworkInput input
    )
    {
    }

    public void OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input
    )
    {
    }

    public void OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason
    )
    {
    }

    public void OnConnectedToServer(
        NetworkRunner runner
    )
    {
    }

    public void OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason
    )
    {
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs
            .ConnectRequest request,
        byte[] token
    )
    {
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason
    )
    {
    }

    public void OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message
    )
    {
    }

    public void OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList
    )
    {
    }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data
    )
    {
    }

    public void OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken
    )
    {
    }

    public void OnSceneLoadDone(
        NetworkRunner runner
    )
    {
    }

    public void OnSceneLoadStart(
        NetworkRunner runner
    )
    {
    }

    public void OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    )
    {
    }

    public void OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    )
    {
    }
}