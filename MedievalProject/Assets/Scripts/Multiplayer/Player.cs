using Fusion;
using TMPro;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private SwordAttack sword;
    
    // On utilise le NCC standard de Fusion
    private NetworkCharacterController _ncc;

    [Header("Mouse Look")]
    [SerializeField] private float lookSensitivity = 2.0f; // Valeur plus basse car on multiplie plus tard
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;

    [Networked] private float _yaw { get; set; } // Synchronisé pour que tout le monde voit la rotation
    private float _pitch; // Local seulement (caméra)

    [SerializeField] private Camera _cam;
    public Material _material;

    public enum PlayerRole : byte { Host = 1, Client = 0 }
    [Networked] public PlayerRole Role { get; set; }

    [Networked] public int Health { get; set; }

    private ChangeDetector _changes;
    [SerializeField] public TMP_Text healthText;
    [SerializeField] private int maxHealth = 100;

    public GameManager gm;

    public bool InputsAllowed = true;

    private void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _cam = GetComponentInChildren<Camera>();
        gm = FindFirstObjectByType<GameManager>();
        _material = GetComponentInChildren<MeshRenderer>().material;
        sword = GetComponentInChildren<SwordAttack>();
        GameObject uiObj = GameObject.Find("HealthText");
        if (uiObj != null)
            healthText = uiObj.GetComponent<TMP_Text>();
    }

    public override void Spawned()
    {

        if (Object.HasStateAuthority)
        {
            Role = (Runner.IsServer && Object.HasInputAuthority) ? PlayerRole.Host : PlayerRole.Client;
            gm.CurrentState = GameManager.GameState.Playing;
            Health = maxHealth;
        }
            

        if (!Object.HasInputAuthority)
        {
            // Désactiver la caméra des autres joueurs
            if(_cam) _cam.gameObject.SetActive(false);
        }
        else
        {
            // Verrouiller la souris pour le joueur local
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        ApplyColor();
    }

    public override void FixedUpdateNetwork()
    {   
        if (!InputsAllowed)
            return;
        // 1. Récupérer l'input (Gère automatiquement le cas Host vs Client vs Proxy)
        if (GetInput(out NetworkInputData data))
        {
            data.move.Normalize();

            // 2. Calculer les rotations (Yaw et Pitch)
            // On le fait AVANT le mouvement pour que le vecteur "Forward" soit à jour
            CalculateLook(data.look);

            // 3. Appliquer le Yaw au transform du joueur
            // C'est ici qu'on remplace le "Rotate" manquant.
            // On applique la rotation synchronisée (_yaw).
            transform.rotation = Quaternion.Euler(0, _yaw, 0 );

            // 4. Calculer la direction de mouvement relative à cette nouvelle rotation
            Vector3 moveDirection = transform.forward * data.move.y + transform.right * data.move.x;
            
            // 5. Déplacer avec le NCC
            // Remets de la vitesse dans ton composant NCC dans l'inspecteur !
            _ncc.Move(moveDirection * Runner.DeltaTime * 5f); // *5f est un multiplicateur de vitesse temporaire

            // 6. Gestion de l'attaque
            if (Object.HasStateAuthority && data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0))
            {
                sword.PerformAttack();
            }
        }
    }

    // Calcul des angles à partir de l'input souris
    private void CalculateLook(Vector2 lookInput)
    {
        // On ne met à jour les angles que si on a l'autorité d'Input (le joueur local)
        // OU si on est le serveur qui traite l'input du client.
        
        // Note: lookInput.x/y sont des deltas (différences) envoyés par OnInput
        float deltaYaw = lookInput.x * lookSensitivity * Runner.DeltaTime;
        float deltaPitch = lookInput.y * lookSensitivity * Runner.DeltaTime;

        _yaw += deltaYaw;
        _pitch -= deltaPitch;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    // LateUpdate pour la caméra (fluidité visuelle locale)
    // On met à jour la caméra dans Render pour éviter les saccades liées aux Ticks physiques
    public override void Render()
    {
        if(!InputsAllowed)
            return;
        if (Object.HasInputAuthority && _cam != null)
        {
            // Appliquer le Pitch localement sur la caméra
            _cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
            
            // On s'assure que le visuel du corps suit bien le yaw réseau (interpolation auto de Fusion)
            // transform.rotation est géré par Fusion pour l'interpolation entre les ticks
        }
        if (Object.HasInputAuthority)
        {
            foreach(var change in _changes.DetectChanges(this, out var previousBuffer, out var currentBuffer))
            {
                switch (change)
                {
                    case nameof(Health):
                        var reader = GetPropertyReader<int>(nameof(Health));
                        var (previous,current) = reader.Read(previousBuffer, currentBuffer);
                        Debug.Log($"Health changed from {previous} to {current}");
                        UpdateHealthUi(current);
                        break;

                }
            }
        }
    }

    public void ApplyColor()
    {
        if (_material == null) return;
        _material.color = Role == PlayerRole.Host ? Color.red : Color.blue;
    }
    
    // RPCs conservés pour ton système de chat
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendMessage(string message, RpcInfo info = default) => RPC_RelayMessage(message, info.Source);

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_RelayMessage(string message, PlayerRef messageSource)
    {
        Debug.Log($"Player {messageSource} says: {message}");
    }

    public void UpdateHealthUi(int newhealth)
    {
        if(Object.HasInputAuthority && healthText != null)
        {
            healthText.text = $"Health: {newhealth}";
        }
        
    }

    
}