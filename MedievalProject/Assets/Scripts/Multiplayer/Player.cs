using Fusion;
using UnityEditor.Build.Content;
using UnityEngine;

public class Player : NetworkBehaviour
{
  [SerializeField] private Ball _prefabBall;
  [SerializeField] private PhysxBall _prefabPhysxBall;

  [Networked] private TickTimer delay { get; set; }

  private NetworkCharacterController _cc;
  private Vector3 _forward;

  public enum PlayerRole : byte
  {
    Host = 1,
    Client = 0
  }

  [Networked] public PlayerRole Role { get; set; }

  private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if(Object.HasStateAuthority)
        {
          Role = (Runner.IsServer && Object.HasInputAuthority) ? PlayerRole.Host : PlayerRole.Client;
        }

        ApplyColor();
    }

  public Material _material;

  private void Awake()
  {
    _cc = GetComponent<NetworkCharacterController>();
    _forward = transform.forward;
    _material = GetComponentInChildren<MeshRenderer>().material;
  }

  public override void FixedUpdateNetwork()
  {
    if (GetInput(out NetworkInputData data))
    {
      data.direction.Normalize();
      _cc.Move(5*data.direction*Runner.DeltaTime);

      if (data.direction.sqrMagnitude > 0)
        _forward = data.direction;

      if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
      {
        if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
        {
          delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
            Runner.Spawn(_prefabBall,
            transform.position+_forward, Quaternion.LookRotation(_forward),
            Object.InputAuthority, (runner, o) =>
            {
              // Initialize the Ball before synchronizing it
              o.GetComponent<Ball>().Init();
            });
        }else if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON1))
        {
          delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
            Runner.Spawn(_prefabPhysxBall,
            transform.position+_forward, Quaternion.LookRotation(_forward),
            Object.InputAuthority, (runner, o) =>
            {
              // Initialize the PhysxBall before synchronizing it
              o.GetComponent<PhysxBall>().Init(_forward * 10f);
            });
        }
      }
    }
  }

    public override void Render()
    {
      foreach (var change in _changeDetector.DetectChanges(this))
      {

        switch (change)
            {
                case nameof(Role):
                    ApplyColor();
                  break;
            }      

      }

    }

    public void ApplyColor()
    {
        if (_material == null) return;
        
        _material.color = Role == PlayerRole.Host ? Color.red : Color.blue;
    }
}