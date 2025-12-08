using Fusion;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private SwordAttack sword;
    private NetworkCharacterController _cc;
    private Vector3 _forward;
    public Material _material;

    public enum PlayerRole : byte { Host = 1, Client = 0 }
    [Networked] public PlayerRole Role { get; set; }

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;
        _material = GetComponentInChildren<MeshRenderer>().material;
        sword = GetComponentInChildren<SwordAttack>();
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            Role = (Runner.IsServer && Object.HasInputAuthority) ? PlayerRole.Host : PlayerRole.Client;

        if (!Object.HasInputAuthority)
            Object.GetComponentInChildren<Camera>().gameObject.SetActive(false);

        ApplyColor();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            if (data.direction.sqrMagnitude > 0)
                _forward = data.direction;

            // Seul le State Authority déclenche l’attaque réseau
            if (Object.HasStateAuthority && data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
            {
                sword.PerformAttack();
            }
        }
    }

    private void Update()
    {
        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.R))
            RPC_SendMessage("Parer !");
    }

    private TMP_Text _messages;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendMessage(string message, RpcInfo info = default) => RPC_RelayMessage(message, info.Source);

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_RelayMessage(string message, PlayerRef messageSource)
    {
        if (_messages == null)
            _messages = FindFirstObjectByType<TMP_Text>();

        if (messageSource == Runner.LocalPlayer)
            message = $"You said: {message}\n";
        else
            message = $"Some other player said: {message}\n";

        _messages.text += message;
    }

    public void ApplyColor()
    {
        if (_material == null) return;
        _material.color = Role == PlayerRole.Host ? Color.red : Color.blue;
    }
}