using Fusion;
using UnityEngine;

public class NetworkGameState :
    NetworkBehaviour
{
    [Header("Timing")]
    [SerializeField]
    private float coordinateConfirmDuration =
        10f;

    [SerializeField]
    private float hiderDuration =
        120f;

    [SerializeField]
    private float seekerDuration =
        120f;

    [SerializeField]
    private float revealDuration =
        30f;

    [SerializeField]
    private float resultDuration =
        10f;

    [Networked]
    public MultiplayerPhase Phase
    {
        get;
        set;
    }

    [Networked]
    public MultiplayerResult Result
    {
        get;
        set;
    }

    [Networked]
    public PlayerRef HiderPlayer
    {
        get;
        set;
    }

    [Networked]
    public PlayerRef SeekerPlayer
    {
        get;
        set;
    }

    [Networked]
    public NetworkBool HiderOriginReady
    {
        get;
        set;
    }

    [Networked]
    public NetworkBool SeekerOriginReady
    {
        get;
        set;
    }

    [Networked]
    public TickTimer CoordinateTimer
    {
        get;
        set;
    }

    [Networked]
    public TickTimer HiderTimer
    {
        get;
        set;
    }

    [Networked]
    public NetworkBool HiderTimerPaused
    {
        get;
        set;
    }

    [Networked]
    public float HiderPausedRemaining
    {
        get;
        set;
    }

    [Networked]
    public TickTimer SeekerTimer
    {
        get;
        set;
    }

    [Networked]
    public TickTimer RevealTimer
    {
        get;
        set;
    }

    [Networked]
    public TickTimer ResultTimer
    {
        get;
        set;
    }

    [Networked]
    public int MecchaCount
    {
        get;
        set;
    }

    [Networked]
    public int RemainingMecchas
    {
        get;
        set;
    }

    [Networked]
    public int MecchasFound
    {
        get;
        set;
    }

    [Networked, Capacity(12)]
    public NetworkArray<NetworkMecchaData>
        Mecchas => default;

    // True only after Fusion has spawned this network object.
    public bool IsNetworkReady
    {
        get;
        private set;
    }

    public override void Spawned()
    {
        // Networked properties are now safe to access.
        IsNetworkReady = true;

        Debug.Log(
            "[NetworkGameState] Spawned. " +
            "State Authority: " +
            Object.HasStateAuthority
        );

        // Only the authority initializes the shared state.
        if (Object.HasStateAuthority)
        {
            ResetRound();
        }
    }

    public float GetRevealRemainingTime()
    {
        if (
            !IsNetworkReady ||
            Runner == null
        )
        {
            return 0f;
        }

        return Mathf.Max(
            0f,
            RevealTimer
                .RemainingTime(Runner)
                ?? 0f
        );
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (
            Phase ==
            MultiplayerPhase
                .CoordinateConfirmation &&
            CoordinateTimer
                .Expired(Runner)
        )
        {
            StartHiderPhase();
        }

        if (
            Phase ==
            MultiplayerPhase.HiderHiding &&
            !HiderTimerPaused &&
            HiderTimer.Expired(Runner)
        )
        {
            StartSeekerPhase();
        }

        if (
            Phase ==
            MultiplayerPhase.SeekerSearching &&
            SeekerTimer.Expired(Runner)
        )
        {
            if (MecchaCount <= 0)
            {
                StartResult(
                    MultiplayerResult.Neither
                );
            }
            else if (
                RemainingMecchas > 0
            )
            {
                StartRevealPhase();
            }
        }

        if (
            Phase ==
            MultiplayerPhase.Reveal &&
            RevealTimer.Expired(Runner)
        )
        {
            StartResult(
                MultiplayerResult.HiderWin
            );
        }

        if (
            Phase ==
            MultiplayerPhase.Result &&
            ResultTimer.Expired(Runner)
        )
        {
            ResetRound();
        }
    }

    public void RequestHiderRole()
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_RequestRole(
            (int)MultiplayerRole.Hider
        );
    }

    public void RequestSeekerRole()
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_RequestRole(
            (int)MultiplayerRole.Seeker
        );
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RequestRole(
        int requestedRole,
        RpcInfo info = default
    )
    {
        if (
            Phase !=
            MultiplayerPhase.WaitingForPlayers
        )
        {
            return;
        }

        PlayerRef player =
            info.Source;

        // Do not allow one player to hold both roles.
        if (
            player == HiderPlayer ||
            player == SeekerPlayer
        )
        {
            return;
        }

        MultiplayerRole role =
            (MultiplayerRole)requestedRole;

        if (
            role ==
            MultiplayerRole.Hider &&
            HiderPlayer ==
            PlayerRef.None
        )
        {
            HiderPlayer =
                player;
        }
        else if (
            role ==
            MultiplayerRole.Seeker &&
            SeekerPlayer ==
            PlayerRef.None
        )
        {
            SeekerPlayer =
                player;
        }

        CheckPlayersReady();
    }

    private void CheckPlayersReady()
    {
        if (
            HiderPlayer !=
            PlayerRef.None &&
            SeekerPlayer !=
            PlayerRef.None
        )
        {
            Phase =
                MultiplayerPhase
                    .WaitingForOrigins;
        }
    }

    public void RequestOriginReady()
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_SetOriginReady();
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_SetOriginReady(
        RpcInfo info = default
    )
    {
        if (
            Phase !=
            MultiplayerPhase.WaitingForOrigins
        )
        {
            return;
        }

        if (
            info.Source ==
            HiderPlayer
        )
        {
            HiderOriginReady =
                true;
        }

        if (
            info.Source ==
            SeekerPlayer
        )
        {
            SeekerOriginReady =
                true;
        }

        if (
            HiderOriginReady &&
            SeekerOriginReady
        )
        {
            Phase =
                MultiplayerPhase
                    .CoordinateConfirmation;

            CoordinateTimer =
                TickTimer.CreateFromSeconds(
                    Runner,
                    coordinateConfirmDuration
                );
        }
    }

    private void StartHiderPhase()
    {
        Phase =
            MultiplayerPhase.HiderHiding;

        HiderTimerPaused =
            false;

        HiderPausedRemaining =
            0f;

        HiderTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                hiderDuration
            );
    }

    private void StartSeekerPhase()
    {
        Phase =
            MultiplayerPhase.SeekerSearching;

        RemainingMecchas =
            MecchaCount;

        MecchasFound =
            0;

        SeekerTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                seekerDuration
            );
    }

    private void StartRevealPhase()
    {
        Debug.Log(
            "[NetworkGameState] " +
            "Seeker time finished. Starting Reveal."
        );

        Phase =
            MultiplayerPhase.Reveal;

        RevealTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                revealDuration
            );
    }

    private void StartResult(
        MultiplayerResult result
    )
    {
        Debug.Log(
            "[NetworkGameState] " +
            "Starting Result: " +
            result
        );

        Phase =
            MultiplayerPhase.Result;

        Result =
            result;

        ResultTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                resultDuration
            );
    }

    public void PauseHiderTimer()
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_SetHiderPause(
            true
        );
    }

    public void ResumeHiderTimer()
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_SetHiderPause(
            false
        );
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_SetHiderPause(
        bool pause,
        RpcInfo info = default
    )
    {
        // Only Hider can pause hiding time.
        if (
            info.Source !=
            HiderPlayer
        )
        {
            return;
        }

        if (
            Phase !=
            MultiplayerPhase.HiderHiding
        )
        {
            return;
        }

        if (
            pause &&
            !HiderTimerPaused
        )
        {
            HiderPausedRemaining =
                HiderTimer
                    .RemainingTime(Runner)
                    ?? 0f;

            HiderTimer =
                TickTimer.None;

            HiderTimerPaused =
                true;
        }
        else if (
            !pause &&
            HiderTimerPaused
        )
        {
            HiderTimer =
                TickTimer.CreateFromSeconds(
                    Runner,
                    HiderPausedRemaining
                );

            HiderTimerPaused =
                false;
        }
    }

    public float GetHiderRemainingTime()
    {
        // Do not access networked properties before Fusion is ready.
        if (
            !IsNetworkReady ||
            Runner == null
        )
        {
            return 0f;
        }

        if (HiderTimerPaused)
        {
            return Mathf.Max(
                0f,
                HiderPausedRemaining
            );
        }

        return Mathf.Max(
            0f,
            HiderTimer
                .RemainingTime(Runner)
                ?? 0f
        );
    }

    public float GetSeekerRemainingTime()
    {
        if (
            !IsNetworkReady ||
            Runner == null
        )
        {
            return 0f;
        }

        return Mathf.Max(
            0f,
            SeekerTimer
                .RemainingTime(Runner)
                ?? 0f
        );
    }

    public void RequestMecchaUpdate(
        int id,
        int type,
        Vector3 localPosition,
        Quaternion localRotation
    )
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_UpsertMeccha(
            id,
            type,
            localPosition,
            localRotation
        );
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_UpsertMeccha(
        int id,
        int type,
        Vector3 localPosition,
        Quaternion localRotation,
        RpcInfo info = default
    )
    {
        if (
            info.Source !=
            HiderPlayer ||
            Phase !=
            MultiplayerPhase.HiderHiding
        )
        {
            return;
        }

        int freeSlot =
            -1;

        for (
            int i = 0;
            i < Mecchas.Length;
            i++
        )
        {
            NetworkMecchaData data =
                Mecchas.Get(i);

            if (
                data.Active &&
                data.Id == id
            )
            {
                data.Type =
                    type;

                data.LocalPosition =
                    localPosition;

                data.LocalRotation =
                    localRotation;

                Mecchas.Set(
                    i,
                    data
                );

                return;
            }

            if (
                !data.Active &&
                freeSlot < 0
            )
            {
                freeSlot =
                    i;
            }
        }

        if (freeSlot < 0)
        {
            Debug.LogWarning(
                "[Network] Meccha buffer full."
            );

            return;
        }

        NetworkMecchaData newData =
            new NetworkMecchaData
            {
                Active = true,
                Id = id,
                Type = type,
                LocalPosition =
                    localPosition,
                LocalRotation =
                    localRotation,
                Found = false
            };

        Mecchas.Set(
            freeSlot,
            newData
        );

        MecchaCount++;
    }

    public void RequestRemoveMeccha(
        int id
    )
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_RemoveMeccha(
            id
        );
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_RemoveMeccha(
        int id,
        RpcInfo info = default
    )
    {
        if (
            info.Source !=
            HiderPlayer ||
            Phase !=
            MultiplayerPhase.HiderHiding
        )
        {
            return;
        }

        for (
            int i = 0;
            i < Mecchas.Length;
            i++
        )
        {
            NetworkMecchaData data =
                Mecchas.Get(i);

            if (
                data.Active &&
                data.Id == id
            )
            {
                Mecchas.Set(
                    i,
                    default
                );

                MecchaCount =
                    Mathf.Max(
                        0,
                        MecchaCount - 1
                    );

                return;
            }
        }
    }

    public void RequestMecchaFound(
        int id
    )
    {
        if (!IsNetworkReady)
        {
            Debug.LogWarning(
                "[Network] Cannot select Hider yet. " +
                "NetworkGameState is not ready."
            );

            return;
        }

        RPC_MecchaFound(
            id
        );
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RPC_MecchaFound(
        int id,
        RpcInfo info = default
    )
    {
        if (
            info.Source !=
            SeekerPlayer ||
            Phase !=
            MultiplayerPhase.SeekerSearching
        )
        {
            return;
        }

        for (
            int i = 0;
            i < Mecchas.Length;
            i++
        )
        {
            NetworkMecchaData data =
                Mecchas.Get(i);

            if (
                !data.Active ||
                data.Id != id ||
                data.Found
            )
            {
                continue;
            }

            data.Found =
                true;

            Mecchas.Set(
                i,
                data
            );

            MecchasFound++;

            RemainingMecchas =
                Mathf.Max(
                    0,
                    RemainingMecchas - 1
                );

            if (
                MecchaCount > 0 &&
                RemainingMecchas <= 0
            )
            {
                StartResult(
                    MultiplayerResult
                        .SeekerWin
                );
            }

            return;
        }
    }

    private void ResetRound()
    {
        Phase =
            MultiplayerPhase
                .WaitingForPlayers;

        Result =
            MultiplayerResult.None;

        HiderPlayer =
            PlayerRef.None;

        SeekerPlayer =
            PlayerRef.None;

        HiderOriginReady =
            false;

        SeekerOriginReady =
            false;

        CoordinateTimer =
            TickTimer.None;

        HiderTimer =
            TickTimer.None;

        SeekerTimer =
            TickTimer.None;

        RevealTimer =
            TickTimer.None;

        ResultTimer =
            TickTimer.None;

        HiderTimerPaused =
            false;

        HiderPausedRemaining =
            0f;

        MecchaCount =
            0;

        RemainingMecchas =
            0;

        MecchasFound =
            0;

        for (
            int i = 0;
            i < Mecchas.Length;
            i++
        )
        {
            Mecchas.Set(
                i,
                default
            );
        }
    }
}