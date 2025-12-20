using Fusion;
using UnityEngine;
using UnityEngine.UI; // Nécessaire pour manipuler l'Image de la flèche

public class SwordControlIK : NetworkBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform rightHandTarget; 
    
    [Header("UI Combat")]
    [SerializeField] private RectTransform combatBoxRect;
    [SerializeField] private RectTransform virtualCursorRect;
    [SerializeField] private RectTransform arrowRect; 

    [Header("Réglages")]
    [SerializeField] private float mouseSensitivityX = 2.0f; 
    [SerializeField] private float mouseSensitivityY = 2.0f; 
    
    [Header("Limites 3D")]
    [SerializeField] private float xMin = -0.5f, xMax = 0.5f; 
    [SerializeField] private float yMin = -0.9f, yMax = 0.5f; 

    [Header("Rotations (Chargement)")]
    [SerializeField] private float rotX_Static = -30f; 
    [SerializeField] private float rotY_Static = 45f; 
    [SerializeField] private float rotZ_Static = -60f;

    // Variables internes
    private Vector2 boxSize;
    private Vector2 virtualCursorPos;
    private Vector3 currentPos; 
    private Quaternion initialRotation; 
    private bool isCombatMode = false;
    private bool isStriking = false;
    private Vector2 lastCursorPos;
    private Vector2 moveDirection;

    public override void Spawned()
    {
        if (rightHandTarget != null)
        {
            initialRotation = rightHandTarget.localRotation;
            currentPos = new Vector3(0.2f, 0, 0.5f); 
        }

        if (Object.HasInputAuthority)
        {
            GameObject boxGO = GameObject.Find("CombatBox");
            GameObject cursorGO = GameObject.Find("VirtualCursor");
            
            Transform arrowTrans = cursorGO != null ? cursorGO.transform.Find("DirectionArrow") : null;

            if(boxGO != null && cursorGO != null)
            {
                combatBoxRect = boxGO.GetComponent<RectTransform>();
                virtualCursorRect = cursorGO.GetComponent<RectTransform>();
                if(arrowTrans != null) arrowRect = arrowTrans.GetComponent<RectTransform>();

                boxSize = new Vector2(combatBoxRect.rect.width/2, combatBoxRect.rect.height/2);
                boxGO.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!Object.HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.C)) ToggleCombatMode();

        if (isCombatMode)
        {
            if (Input.GetMouseButtonDown(0)) isStriking = true;
            if (Input.GetMouseButtonUp(0)) isStriking = false;
            
            HandleCursorAndHand();
        }
    }

    private void ToggleCombatMode()
    {
        isCombatMode = !isCombatMode;
        if (isCombatMode)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false; 
            currentPos = new Vector3(0.2f, 0.4f, 0.5f); 
            if(combatBoxRect != null) combatBoxRect.gameObject.SetActive(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            rightHandTarget.localPosition = new Vector3(0.2f, -0.8f, 0.2f);
            rightHandTarget.localRotation = initialRotation;
            isStriking = false;
            if(combatBoxRect != null) combatBoxRect.gameObject.SetActive(false);
        }
    }

    private void HandleCursorAndHand()
    {
        float deltaX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float deltaY = Input.GetAxis("Mouse Y") * mouseSensitivityY;


        lastCursorPos = virtualCursorPos;

        virtualCursorPos.x += deltaX;
        virtualCursorPos.y += deltaY;

        virtualCursorPos.x = Mathf.Clamp(virtualCursorPos.x, -boxSize.x, boxSize.x);
        virtualCursorPos.y = Mathf.Clamp(virtualCursorPos.y, -boxSize.y, boxSize.y);

        if(virtualCursorRect != null) virtualCursorRect.anchoredPosition = virtualCursorPos;
        Vector2 rawDirection = (virtualCursorPos - lastCursorPos).normalized;

        if (Vector2.Distance(virtualCursorPos, lastCursorPos) > 0.1f)
        {
            moveDirection = Vector2.Lerp(moveDirection, rawDirection, Time.deltaTime * 10f);
        }

        // 3. UI FLÈCHE (Rotation 2D)
        if (arrowRect != null)
        {
            if (isStriking && moveDirection.magnitude > 0.1f)
            {
                arrowRect.gameObject.SetActive(true);
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                arrowRect.rotation = Quaternion.Euler(0, 0, angle - 90); 
            }
            else
            {
                arrowRect.gameObject.SetActive(false);
            }
        }

        float ratioX = virtualCursorPos.x / boxSize.x; 
        float ratioY = virtualCursorPos.y / boxSize.y; 

        currentPos.x = Mathf.Lerp(xMin, xMax, (ratioX + 1) / 2);
        currentPos.y = Mathf.Lerp(yMin, yMax, (ratioY + 1) / 2);
        
        float zTarget = isStriking ? 0.7f : Mathf.Lerp(0.5f, 0.2f, new Vector2(ratioX, ratioY).magnitude);
        currentPos.z = Mathf.Lerp(currentPos.z, zTarget, Time.deltaTime * 10f);

        ApplyRotation(ratioX, ratioY);
        
        rightHandTarget.localPosition = currentPos;
    }

    private void ApplyRotation(float ratioX, float ratioY)
    {
        Quaternion targetRot;

        if (isStriking)
        {
            // On convertit le vecteur 2D en angle
            float angleStrike = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

            
            targetRot = initialRotation * Quaternion.Euler(angleStrike, 0, 0);
        }
        else
        {
            float angleX = ratioY * rotX_Static; 
            float angleY = ratioX * rotY_Static; 
            float angleZ = ratioY * rotZ_Static;
            
            targetRot = initialRotation * Quaternion.Euler(angleX, angleY, angleZ);
        }

        // Application fluide
        rightHandTarget.localRotation = Quaternion.Slerp(rightHandTarget.localRotation, targetRot, Time.deltaTime * 20f);
    }
}