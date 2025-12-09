using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 move;
    public Vector2 look;
    public NetworkButtons buttons;

    public const byte MOUSEBUTTON0 = 0;
    public const byte MOUSEBUTTON1 = 1;
}