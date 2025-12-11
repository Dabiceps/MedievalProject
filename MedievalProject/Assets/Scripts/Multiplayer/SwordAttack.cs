using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SwordAttack : NetworkBehaviour
{
    [Networked] public TickTimer attackDelay { get; set; }
    [SerializeField] private float attackCooldown = 5f;
    [SerializeField] private int damageAmount = 10;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    [Networked] private bool Attacking { get; set; }
    private bool reset;
    private List<NetworkId> _HitTargets = new List<NetworkId>();

    public GameManager gm;
    

    public override void Spawned()
    {
        gm = FindFirstObjectByType<GameManager>();
        initialPosition = Object.transform.localPosition;
        initialRotation = Object.transform.localRotation;

        if (Object.HasStateAuthority)
            attackDelay = TickTimer.CreateFromSeconds(Runner, 0f); // prêt à attaquer
    }

    public void ResetHitList()
    {
        _HitTargets.Clear();
    }

    // Appelé seulement par le State Authority (voir Player.FixedUpdateNetwork)
    public void PerformAttack()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!attackDelay.ExpiredOrNotRunning(Runner))
        {
            Debug.Log("Attack on cooldown");
            return;
        }
        ResetHitList();
        Debug.Log("Attack not on cooldown");
        Attacking = true;
        reset = false;
        // durée de l'attaque
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        // Fin d'action
        if (attackDelay.Expired(Runner) && Attacking)
        {
            Debug.Log("Attacking!");
            Object.transform.Rotate(90f, 0, 0);
            Object.transform.localPosition += new Vector3(0, 0, 0.5f) * Runner.DeltaTime;
            Attacking = false;

            // cooldown
            attackDelay = TickTimer.CreateFromSeconds(Runner, attackCooldown);
        }

        // Reset pose à la fin du cooldown
        if (attackDelay.Expired(Runner) && !Attacking&& !reset)
        {
            Debug.Log("Attack finished, resetting pose");
            Object.transform.localRotation = initialRotation;
            Object.transform.localPosition = initialPosition;
            reset = true;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if(!Object.HasStateAuthority ) return;
        var TargetPlayer = other.GetComponentInParent<Player>();

        if(TargetPlayer != null )
        {   
            Debug.Log($"Sword hit player {TargetPlayer.Object.Id}");
            if(TargetPlayer.Object.InputAuthority == Object.InputAuthority) return; // Ne pas s'auto-infliger des dégâts
            if(_HitTargets.Contains(TargetPlayer.Object.Id)) return; // Ne pas toucher plusieurs fois la même cible dans une attaque
            _HitTargets.Add(TargetPlayer.Object.Id);
            OnHit(TargetPlayer);
        }else
        {
            Debug.Log("AH CHEF AAAA CHEEEEF TU TOUCHE RIEN LA");
        }
    }

    public void OnHit(Player target)
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }
        Debug.Log($"Hit player {target.Object.Id}, dealing {damageAmount} damage.");
        target.Health -= damageAmount;

        if(target.Health <= 0)
        {
            Debug.Log("CHEEEF LE JOUEUR EST MORT");
            gm.TriggerGameOver(target.Object.Id);
        }
    }

    

    
}