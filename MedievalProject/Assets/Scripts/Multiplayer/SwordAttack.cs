using Fusion;
using UnityEngine;

public class SwordAttack : NetworkBehaviour
{
    [Networked] public TickTimer attackDelay { get; set; }
    [SerializeField] private float attackCooldown = 5f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    [Networked] private bool Attacking { get; set; }
    private bool reset;

    public override void Spawned()
    {
        initialPosition = Object.transform.localPosition;
        initialRotation = Object.transform.localRotation;

        if (Object.HasStateAuthority)
            attackDelay = TickTimer.CreateFromSeconds(Runner, 0f); // prêt à attaquer
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
}