using Fusion;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private SwordAttack sword;
    private CharacterController _cc;

    //Rotation Souris
    [SerializeField] private float lookSensitivity = 5f;
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;

    private float _yaw;
    private float _pitch;

    [SerializeField] private Camera _cam;
    public Material _material;

    public enum PlayerRole : byte { Host = 1, Client = 0 }
    [Networked] public PlayerRole Role { get; set; }

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cam = GetComponentInChildren<Camera>();
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

        if (Object.HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.move.Normalize();


            ApplyLook(data.look, Object.HasInputAuthority);

            var camForward = _cam ? _cam.transform.forward : transform.forward;
            var camRight   = _cam ? _cam.transform.right   : transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            Vector3 wishDir = camForward * data.move.y + camRight * data.move.x;
            if (wishDir.sqrMagnitude > 1) wishDir.Normalize();

            _cc.Move(5 * wishDir * Runner.DeltaTime);

            if (Object.HasStateAuthority && data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
                sword.PerformAttack();
        }
    }

    private void ApplyLook(Vector2 lookInput, bool applyCameraPitch)
    {
        // Intégration du Delta 
        _yaw += lookInput.x * lookSensitivity;
        _pitch -= lookInput.y * lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch); //On limite la rotation verticale entre 75 et -75 degrés

        //Appliquer la rotation Yaw sur le joueur (rotation horizontale)
        transform.rotation = Quaternion.Euler(0, _yaw, 0);

        //appliquer le pitch sur la caméra (rotation verticale)
        if (applyCameraPitch && _cam != null)
            _cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);

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