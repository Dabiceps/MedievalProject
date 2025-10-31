using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    public float directionInputWindow = 1.0f;
    public KeyCode attackKey = KeyCode.Space;
    public Transform swordTransform;
    public float rayLength = 3f;

    private bool isPreparingAttack = false;
    private float attackTimer = 0f;
    private Vector3 attackDirection;
    private bool hasAttacked = false;

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            isPreparingAttack = true;
            attackTimer = directionInputWindow;
            attackDirection = Vector3.zero;
            hasAttacked = false;
            Debug.Log("Préparation de l'attaque...");
        }

        if (isPreparingAttack)
        {
            attackTimer -= Time.deltaTime;

            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            Vector3 direction = (mouseWorldPosition - transform.position).normalized;

            if (direction.magnitude > 0.1f)
            {
                attackDirection = direction;
                Debug.DrawRay(transform.position, attackDirection * rayLength, Color.yellow);
            }

            if (attackTimer <= 0f && !hasAttacked)
            {
                isPreparingAttack = false;
                PerformAttack(attackDirection);
                hasAttacked = true;
            }
        }
    }

    void PerformAttack(Vector3 direction)
    {
        swordTransform.forward = direction;
        Debug.Log("Attaque lancée dans la direction : " + direction);

        // Raycast pour détecter un ennemi
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, rayLength))
        {
            Debug.Log("Touché : " + hit.collider.name);
        }
        else
        {
            Debug.Log("Aucun ennemi touché.");
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return transform.position;
    }

    void OnDrawGizmos()
    {
        if (attackDirection != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + attackDirection * rayLength);
        }
    }
}