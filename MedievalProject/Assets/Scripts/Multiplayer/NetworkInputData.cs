using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
    public NetworkButtons buttons;

    public const byte MOUSEBUTTON0 = 0;
    public const byte MOUSEBUTTON1 = 1;
}