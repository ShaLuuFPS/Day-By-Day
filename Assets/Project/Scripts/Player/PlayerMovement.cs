using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 2.5f;
    public float acceleration = 2.5f;
    public float deceleration = 10f;
    public float rotationSpeed = 12f;

    [Header("Animation")]
    public Animator animator;

    public Vector3 moveDirection { get; private set; }
    public float currentSpeed { get; private set; }

    private PlayerHealth playerHealth;
    private PlayerStamina playerStamina;
    private bool isDead;
    private float currentVelocity;
    private Vector3 lastMoveDir = Vector3.forward;
    private Rigidbody rb;

    private Vector3 cachedWorldMoveDir;
    private bool cachedIsDashing;
    private bool cachedHasForwardInput;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerStamina = GetComponent<PlayerStamina>();
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            if (!isDead)
            {
                isDead = true;
                currentVelocity = 0f;
                if (animator != null) animator.SetBool("IsDead", true);
            }
            return;
        }

        if (GameStateManager.IsInputFrozen)
        {
            if (animator != null) { animator.SetFloat("MoveX", 0f); animator.SetFloat("MoveZ", 0f); }
            return;
        }

        float inputX = 0f, inputZ = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputZ = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputZ = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX = 1f;
        }
        Vector3 rawInput = new Vector3(inputX, 0f, inputZ);
        float inputMagnitude = rawInput.magnitude;
        if (inputMagnitude > 1f) rawInput /= inputMagnitude;

        Vector3 worldMoveDir = Vector3.zero;
        if (inputMagnitude > 0.01f && Camera.main != null)
        {
            Vector3 camFwd = Camera.main.transform.forward; camFwd.y = 0; camFwd.Normalize();
            Vector3 camRgt = Camera.main.transform.right;   camRgt.y = 0; camRgt.Normalize();
            worldMoveDir = (camFwd * rawInput.z + camRgt * rawInput.x).normalized;
            lastMoveDir = worldMoveDir;
        }
        moveDirection = worldMoveDir;

        cachedWorldMoveDir = worldMoveDir;
        cachedIsDashing = playerStamina != null && playerStamina.isDashing;
        cachedHasForwardInput = inputZ > 0.01f;

        if (animator != null)
        {
            float spd = maxSpeed > 0.01f ? Mathf.Min(currentSpeed / maxSpeed, 0.45f) : 0f;;
            if (worldMoveDir.magnitude > 0.01f)
            {
                Vector3 localDir = transform.InverseTransformDirection(worldMoveDir);
                float moveX = localDir.x * spd;
                if (Mathf.Abs(moveX) < 0.15f) moveX = 0f;
                animator.SetFloat("MoveX", moveX);
                animator.SetFloat("MoveZ", localDir.z * spd);
            }
            else
            {
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveZ", 0f);
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead || GameStateManager.IsInputFrozen) return;
        if (rb == null) return;

        Vector3 worldMoveDir = cachedWorldMoveDir;
        bool isDashing = cachedIsDashing;
        if (!isDashing && worldMoveDir.magnitude > 0.01f)
            currentVelocity = Mathf.MoveTowards(currentVelocity, maxSpeed, acceleration * Time.fixedDeltaTime);
        else
            currentVelocity = Mathf.MoveTowards(currentVelocity, 0f, deceleration * Time.fixedDeltaTime);
        currentSpeed = currentVelocity;

        if (!isDashing && worldMoveDir.magnitude > 0.01f)
        {
            Vector3 newPos = rb.position + worldMoveDir * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }

        if (cachedHasForwardInput && worldMoveDir.magnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(worldMoveDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
