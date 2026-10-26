using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetworkMecchaTextureManager :
    MonoBehaviour,
    INetworkRunnerCallbacks
{
    // ============================================================
    // NETWORK REFERENCES
    // ============================================================

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

    // ============================================================
    // MESSAGE TYPE
    // ============================================================

    // Unique identifier for Meccha texture
    // reliable-data messages.
    private const int TextureMessageType =
        77001;

    // ============================================================
    // RECEIVED TEXTURE CACHE
    // ============================================================

    // Key:
    //     Meccha ID
    //
    // Value:
    //     PNG texture bytes
    //
    // This allows the texture to arrive before
    // the Seeker Meccha itself is spawned.
    private readonly Dictionary<
        int,
        byte[]
    >
        receivedTextures =
            new Dictionary<
                int,
                byte[]
            >();

    // ============================================================
    // ROUND STATE
    // ============================================================

    private MultiplayerPhase previousPhase =
        (MultiplayerPhase)(-1);

    private bool texturesSentThisRound =
        false;

    // ============================================================
    // RUNNER CALLBACK STATE
    // ============================================================

    private bool callbacksRegistered =
        false;

    private Coroutine
        callbackRegistrationCoroutine;

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        callbackRegistrationCoroutine =
            StartCoroutine(
                EnsureRunnerCallbacks()
            );
    }

    // ============================================================
    // WAIT FOR RUNNER
    // ============================================================

    private IEnumerator
        EnsureRunnerCallbacks()
    {
        while (
            runner == null ||
            !runner.IsRunning
        )
        {
            if (runner == null)
            {
                runner =
                    FindFirstObjectByType<
                        NetworkRunner
                    >();
            }

            yield return null;
        }

        if (!callbacksRegistered)
        {
            runner.AddCallbacks(
                this
            );

            callbacksRegistered =
                true;

            Debug.Log(
                "[MecchaTextureNet] " +
                "Registered with NetworkRunner. " +
                "LocalPlayer = " +
                runner.LocalPlayer
            );
        }

        callbackRegistrationCoroutine =
            null;
    }

    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (
            callbackRegistrationCoroutine !=
            null
        )
        {
            StopCoroutine(
                callbackRegistrationCoroutine
            );

            callbackRegistrationCoroutine =
                null;
        }

        if (
            runner != null &&
            callbacksRegistered
        )
        {
            runner.RemoveCallbacks(
                this
            );

            callbacksRegistered =
                false;
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================

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

        // ========================================================
        // HIDER HIDING -> SEEKER SEARCHING
        // ========================================================

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

            Debug.Log(
                "[MecchaTextureNet] " +
                "Hider phase ended. " +
                "Sending final Meccha textures."
            );

            SendAllHiderTextures();
        }

        // ========================================================
        // NEW ROUND
        // ========================================================

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

            Debug.Log(
                "[MecchaTextureNet] " +
                "New round. Texture cache cleared."
            );
        }

        previousPhase =
            phase;
    }

    // ============================================================
    // SEND ALL HIDER TEXTURES
    // ============================================================

    private void SendAllHiderTextures()
    {
        if (
            runner == null ||
            !runner.IsRunning
        )
        {
            Debug.LogWarning(
                "[MecchaTextureNet] " +
                "Cannot send textures because " +
                "NetworkRunner is not running."
            );

            return;
        }

        if (networkState == null)
        {
            Debug.LogWarning(
                "[MecchaTextureNet] " +
                "NetworkGameState is missing."
            );

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

        Debug.Log(
            "[MecchaTextureNet] " +
            "Preparing textures for Seeker " +
            seeker
        );

        NetworkMecchaIdentity[] identities =
            FindObjectsByType<
                NetworkMecchaIdentity
            >(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        int sentCount =
            0;

        foreach (
            NetworkMecchaIdentity identity
            in identities
        )
        {
            if (identity == null)
            {
                continue;
            }

            // ====================================================
            // FIND PAINTER
            // ====================================================

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

            if (painter == null)
            {
                Debug.LogWarning(
                    "[MecchaTextureNet] " +
                    "No MecchaTexturePainter found " +
                    "for Meccha ID " +
                    identity.MecchaId
                );

                continue;
            }

            // ====================================================
            // CHECK WHETHER MECCHA WAS PAINTED
            // ====================================================

            if (!painter.HasPaintedData())
            {
                Debug.Log(
                    "[MecchaTextureNet] " +
                    "Meccha ID " +
                    identity.MecchaId +
                    " has no painted data. " +
                    "Skipping."
                );

                continue;
            }

            // ====================================================
            // ENCODE TEXTURE
            // ====================================================

            byte[] pngData =
                painter.GetPaintTexturePNG();

            if (
                pngData == null ||
                pngData.Length == 0
            )
            {
                Debug.LogWarning(
                    "[MecchaTextureNet] " +
                    "Meccha ID " +
                    identity.MecchaId +
                    " produced empty PNG data."
                );

                continue;
            }

            int mecchaId =
                identity.MecchaId;

            // ====================================================
            // CREATE RELIABLE MESSAGE KEY
            // ====================================================

            ReliableKey key =
                ReliableKey.FromInts(
                    TextureMessageType,
                    mecchaId,
                    0,
                    0
                );

            // ====================================================
            // SEND TO SEEKER
            // ====================================================

            runner.SendReliableDataToPlayer(
                seeker,
                key,
                pngData
            );

            sentCount++;

            Debug.Log(
                "[MecchaTextureNet] " +
                "Sent texture. " +
                "ID = " +
                mecchaId +
                " | Bytes = " +
                pngData.Length +
                " | To = " +
                seeker
            );
        }

        Debug.Log(
            "[MecchaTextureNet] " +
            "Finished sending textures. " +
            "Count = " +
            sentCount
        );
    }

    // ============================================================
    // HANDLE RECEIVED TEXTURE
    // ============================================================

    private void HandleReceivedTexture(
        ReliableKey key,
        byte[] data
    )
    {
        // ========================================================
        // ONLY PROCESS ON SEEKER
        // ========================================================

        if (
            roleController == null ||
            !roleController.IsSeeker()
        )
        {
            Debug.Log(
                "[MecchaTextureNet] " +
                "Reliable texture arrived, " +
                "but local player is not Seeker. " +
                "Ignoring."
            );

            return;
        }

        // ========================================================
        // DECODE MESSAGE KEY
        // ========================================================

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
            Debug.LogWarning(
                "[MecchaTextureNet] " +
                "Received unknown reliable " +
                "message type = " +
                messageType
            );

            return;
        }

        // ========================================================
        // VALIDATE DATA
        // ========================================================

        if (
            data == null ||
            data.Length == 0
        )
        {
            Debug.LogWarning(
                "[MecchaTextureNet] " +
                "Received empty texture data."
            );

            return;
        }

        // ========================================================
        // CACHE TEXTURE
        // ========================================================

        receivedTextures[
            mecchaId
        ] =
            data;

        Debug.Log(
            "[MecchaTextureNet] " +
            "Received texture. " +
            "ID = " +
            mecchaId +
            " | Bytes = " +
            data.Length
        );

        // ========================================================
        // APPLY IMMEDIATELY IF MECCHA EXISTS
        // ========================================================

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
        else
        {
            Debug.Log(
                "[MecchaTextureNet] " +
                "Meccha ID " +
                mecchaId +
                " is not spawned yet. " +
                "Texture cached for later."
            );
        }
    }

    // ============================================================
    // TRY APPLY CACHED TEXTURE
    // ============================================================

    public void TryApplyCachedTexture(
        int mecchaId,
        GameObject meccha
    )
    {
        if (
            meccha == null
        )
        {
            return;
        }

        ApplyTexture(
            mecchaId,
            meccha
        );
    }

    // ============================================================
    // APPLY TEXTURE TO MECCHA
    // ============================================================

    private void ApplyTexture(
        int mecchaId,
        GameObject meccha
    )
    {
        if (
            meccha == null
        )
        {
            return;
        }

        // ========================================================
        // FIND CACHED PNG
        // ========================================================

        if (
            !receivedTextures.TryGetValue(
                mecchaId,
                out byte[] pngData
            )
        )
        {
            Debug.Log(
                "[MecchaTextureNet] " +
                "No cached texture yet for ID " +
                mecchaId
            );

            return;
        }

        // ========================================================
        // FIND PAINTER
        // ========================================================

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
                "No MecchaTexturePainter found " +
                "for Seeker Meccha ID " +
                mecchaId
            );

            return;
        }

        // ========================================================
        // APPLY PNG
        // ========================================================

        bool applied =
            painter.ApplyNetworkPaintPNG(
                pngData
            );

        if (applied)
        {
            Debug.Log(
                "[MecchaTextureNet] " +
                "Applied texture to Seeker " +
                "Meccha ID " +
                mecchaId
            );
        }
        else
        {
            Debug.LogError(
                "[MecchaTextureNet] " +
                "Failed to apply texture to " +
                "Seeker Meccha ID " +
                mecchaId
            );
        }
    }

    // ============================================================
    // FUSION RELIABLE DATA
    // ============================================================

    // ------------------------------------------------------------
    // Fusion 2.0.x
    // ------------------------------------------------------------

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ArraySegment<byte> data
    )
    {
        Debug.Log(
            "[MecchaTextureNet] " +
            "RELIABLE DATA ARRIVED! " +
            "CallbackPlayer=" +
            player +
            " | LocalPlayer=" +
            runner.LocalPlayer +
            " | Bytes=" +
            data.Count
        );

        // DO NOT compare this PlayerRef to
        // networkState.HiderPlayer.
        //
        // SendReliableDataToPlayer() already
        // targets the intended Seeker.
        //
        // We only need to make sure THIS
        // device is acting as the Seeker.

        if (
            roleController == null ||
            !roleController.IsSeeker()
        )
        {
            Debug.Log(
                "[MecchaTextureNet] " +
                "Reliable texture arrived, " +
                "but local player is not Seeker. " +
                "Ignoring."
            );

            return;
        }

        // ========================================================
        // COPY ARRAY SEGMENT
        // ========================================================

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

        // ========================================================
        // PROCESS
        // ========================================================

        HandleReceivedTexture(
            key,
            copy
        );
    }

    // ------------------------------------------------------------
    // Fusion 2.1+
    // ------------------------------------------------------------

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ReadOnlySpan<byte> data
    )
    {
        Debug.Log(
            "[MecchaTextureNet] " +
            "RELIABLE DATA ARRIVED! " +
            "CallbackPlayer=" +
            player +
            " | LocalPlayer=" +
            runner.LocalPlayer +
            " | Bytes=" +
            data.Length
        );

        // Only process texture data on the Seeker.
        //
        // IMPORTANT:
        // Do NOT compare player against
        // networkState.HiderPlayer.

        if (
            roleController == null ||
            !roleController.IsSeeker()
        )
        {
            Debug.Log(
                "[MecchaTextureNet] " +
                "Reliable texture arrived, " +
                "but local player is not Seeker. " +
                "Ignoring."
            );

            return;
        }

        HandleReceivedTexture(
            key,
            data.ToArray()
        );
    }

    // ============================================================
    // OTHER FUSION CALLBACKS
    // ============================================================

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress
    )
    {
        // Not required for this implementation.
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
        NetworkRunnerCallbackArgs.ConnectRequest request,
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