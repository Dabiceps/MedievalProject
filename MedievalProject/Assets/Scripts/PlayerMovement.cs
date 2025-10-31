using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpCooldown = 0.5f;
    private float lastJumpTime = -999f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 1000f;
    private float xRotation = 0f;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilForce = 5f;
    [SerializeField] private float recoilDuration = 0.2f;
    private bool isRecoiling = false;

    [Header("Debug")]
    [SerializeField] private Vector2 inputDirection;

    public bool isGrounded = false;
    public bool canMove = true;

    private Rigidbody rb;
    private Camera cam;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.mass = 2f;
        rb.angularDamping = 20f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (canMove)
        {
            HandleInput();
            HandleJump();
            HandleMouseLook();
        }
        else
        {
            inputDirection = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    private void HandleInput()
    {
        Vector2 moveInput = Vector2.zero;

        if (Input.GetKey(KeyCode.W)) moveInput.y = 1f;
        if (Input.GetKey(KeyCode.S)) moveInput.y = -1f;
        if (Input.GetKey(KeyCode.A)) moveInput.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveInput.x = 1f;

        inputDirection = moveInput.normalized;
    }

    private void ApplyMovement()
    {
        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        Vector3 move = moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isGrounded && Time.time >= lastJumpTime + jumpCooldown)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                lastJumpTime = Time.time;
                isGrounded = false;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void ApplyRecoil(Vector3 direction)
    {
        if (!isRecoiling)
            StartCoroutine(RecoilEffect(direction));
    }

    private System.Collections.IEnumerator RecoilEffect(Vector3 direction)
    {
        isRecoiling = true;

        Vector3 recoilDirection = -direction.normalized;
        recoilDirection.y = 0;

        rb.AddForce(recoilDirection * recoilForce, ForceMode.Impulse);

        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float dampingFactor = 1f - (elapsed / recoilDuration * 0.5f);
            Vector3 dampedVelocity = horizontalVelocity * dampingFactor;

            rb.linearVelocity = new Vector3(dampedVelocity.x, rb.linearVelocity.y, dampedVelocity.z);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isRecoiling = false;
    }
}