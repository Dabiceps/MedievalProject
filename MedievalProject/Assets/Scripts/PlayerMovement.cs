using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 50f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpCooldown = 0.5f;
    private float lastJumpTime = -999f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    private float xRotation = 0f;

    [Header("Head Bobbing Settings")]
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private float bobAmplitude = 0.05f;
    private float bobTimer = 0f;
    private Vector3 cameraInitialLocalPos;

    [Header("Recoil Settings")]
    [SerializeField] private float recoilForce = 5f;
    [SerializeField] private float recoilDuration = 0.2f;
    private bool isRecoiling = false;

    [Header("Debug")]
    [SerializeField] private Vector3 currentVelocity;
    [SerializeField] private Vector2 inputDirection;

    public bool isGrounded = false;
    public bool canMove = true;

    private Rigidbody rb;
    private Vector3 targetVelocity;
    private Camera cam;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 10f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraInitialLocalPos = cam.transform.localPosition;
    }

    void Update()
    {
        if (canMove)
        {
            HandleInput();
            HandleJump();
            CalculateTargetVelocity();
            HandleMouseLook();
            HandleHeadBobbing();
        }
        else
        {
            inputDirection = Vector2.zero;
            targetVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    void FixedUpdate()
    {
        ApplyMovement();
        KeepUpright();
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

    private void CalculateTargetVelocity()
    {
        Vector3 movement = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        targetVelocity = movement * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;
    }

    private void ApplyMovement()
    {
        currentVelocity = rb.linearVelocity;
        float lerpSpeed = GetLerpSpeed();

        Vector3 horizontalTarget = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
        Vector3 newVelocity = Vector3.Lerp(currentVelocity, horizontalTarget, lerpSpeed * Time.fixedDeltaTime);

        rb.linearVelocity = newVelocity;
    }

    private float GetLerpSpeed()
    {
        Vector2 currentHorizontal = new Vector2(currentVelocity.x, currentVelocity.z);
        Vector2 targetHorizontal = new Vector2(targetVelocity.x, targetVelocity.z);

        return targetHorizontal.magnitude > currentHorizontal.magnitude ? acceleration : deceleration;
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
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

    private void HandleHeadBobbing()
    {
        bool isMoving = inputDirection.magnitude > 0.1f && isGrounded;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
            cam.transform.localPosition = cameraInitialLocalPos + new Vector3(0f, bobOffset, 0f);
        }
        else
        {
            bobTimer = 0f;
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, cameraInitialLocalPos, Time.deltaTime * 5f);
        }
    }

    private void KeepUpright()
    {
        Quaternion uprightRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, uprightRotation, Time.fixedDeltaTime * 10f);
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