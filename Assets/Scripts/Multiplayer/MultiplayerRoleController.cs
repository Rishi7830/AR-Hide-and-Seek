using Fusion;
using UnityEngine;

public class MultiplayerRoleController :
    MonoBehaviour
{
    [SerializeField]
    private NetworkGameState networkState;

    public void SelectHider()
    {
        if (
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        networkState.RequestHiderRole();
    }

    public void SelectSeeker()
    {
        if (
            networkState == null ||
            !networkState.IsNetworkReady
        )
        {
            return;
        }

        networkState.RequestSeekerRole();
    }

    public MultiplayerRole GetLocalRole()
    {
        if (
            networkState == null ||
            !networkState.IsNetworkReady ||
            networkState.Runner == null
        )
        {
            return MultiplayerRole.None;
        }

        PlayerRef localPlayer =
            networkState.Runner.LocalPlayer;

        if (
            networkState.HiderPlayer ==
            localPlayer
        )
        {
            return MultiplayerRole.Hider;
        }

        if (
            networkState.SeekerPlayer ==
            localPlayer
        )
        {
            return MultiplayerRole.Seeker;
        }

        return MultiplayerRole.None;
    }

    public bool IsHider()
    {
        return
            GetLocalRole() ==
            MultiplayerRole.Hider;
    }

    public bool IsSeeker()
    {
        return
            GetLocalRole() ==
            MultiplayerRole.Seeker;
    }
}