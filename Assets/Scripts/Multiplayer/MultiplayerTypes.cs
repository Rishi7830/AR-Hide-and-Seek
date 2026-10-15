using Fusion;
using UnityEngine;

public enum MultiplayerRole
{
    None = 0,
    Hider = 1,
    Seeker = 2
}

public enum MultiplayerPhase
{
    WaitingForPlayers = 0,
    WaitingForOrigins = 1,
    CoordinateConfirmation = 2,
    HiderHiding = 3,
    SeekerSearching = 4,
    Reveal = 5,
    Result = 6
}

public enum MultiplayerResult
{
    None = 0,
    SeekerWin = 1,
    HiderWin = 2,
    Neither = 3
}

public struct NetworkMecchaData : INetworkStruct
{
    public NetworkBool Active;

    public int Id;

    // 0 = Horizontal
    // 1 = Vertical
    public int Type;

    // Position relative to InitialAprilTag.
    public Vector3 LocalPosition;

    // Rotation relative to InitialAprilTag.
    public Quaternion LocalRotation;

    public NetworkBool Found;
}