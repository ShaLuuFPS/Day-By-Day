using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour, IResettable
{
    [Header("Movement")]
    public float maxSpeed = 2.5f;
    public float acceleration = 2.5f;
    public float deceleration = 10f;
    public float rotationSpeed = 12f;
    public float aimRotationSpeed = 15f;
    public float walkSpeedMultiplier = 0.5f;

    [Header("Jump")]
    public float jumpForce = 8f;

    [Header("Animation")]
    public Animator animator;

    public Vector3 moveDirection { get; private set; }
    public float currentSpeed { get; private set; }

    /// <summary>是否处于举枪瞄准状态（RMB 按住）</summary>
    public bool IsAiming { get; private set; }

    private PlayerHealth playerHealth;
    private PlayerStamina playerStamina;
    private bool isDead;
    private float currentVelocity;
    private Vector3 lastFacingDir = Vector3.forward;
    private Rigidbody rb;
    private Vector3 initialPosition;

    // 平滑过渡
    private float currentIsAiming;

    // 跳跃
    private bool isGrounded;
    private bool cachedJumpPressed;
    private bool spaceWasHeld;

    // FixedUpdate 缓存
    private Vector3 cachedWorldMoveDir;
    private bool cachedIsDashing;
    private float cachedInputX, cachedInputZ;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerStamina = GetComponent<PlayerStamina>();
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.sleepThreshold = 0f;  // 禁止休眠，否则静止时 OnCollisionStay 停止触发
        }
    }

    void Update()
    {
        // ── 死亡处理 ──
        if (playerHealth != null && playerHealth.IsDead)
        {
            if (!isDead)
            {
                isDead = true;
                currentVelocity = 0f;
                currentSpeed = 0f;
                if (animator != null)
                {
                    animator.SetBool("IsDead", true);
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                }
            }
            return;
        }

        // ── 输入冻结 ──
        if (GameStateManager.IsInputFrozen)
        {
            if (animator != null)
            {
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveZ", 0f);
                animator.SetFloat("Speed", 0f);
            }
            return;
        }

        // ── 读取 WASD ──
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

        // ── 跳跃输入（闩锁边沿检测：只设不消，FixedUpdate 消费后清零） ──
        bool spaceHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        if (spaceHeld && !spaceWasHeld)
            cachedJumpPressed = true;  // 锁存，不被后续 Update 覆盖
        spaceWasHeld = spaceHeld;

        // ── 右键举枪 ──
        bool wantsToAim = Mouse.current != null && Mouse.current.rightButton.isPressed;
        float aimTarget = wantsToAim ? 1f : 0f;
        currentIsAiming = Mathf.MoveTowards(currentIsAiming, aimTarget, Time.deltaTime / 0.25f);
        IsAiming = currentIsAiming > 0.5f;

        // ── 摄像机相对方向 ──
        Vector3 camFwd = Vector3.forward, camRgt = Vector3.right;
        if (Camera.main != null)
        {
            camFwd = Camera.main.transform.forward; camFwd.y = 0; camFwd.Normalize();
            camRgt = Camera.main.transform.right;   camRgt.y = 0; camRgt.Normalize();
        }
        Vector3 worldMoveDir = (camFwd * rawInput.z + camRgt * rawInput.x).normalized;
        moveDirection = worldMoveDir;

        // 缓存到 FixedUpdate
        cachedWorldMoveDir = worldMoveDir;
        cachedIsDashing = playerStamina != null && playerStamina.isDashing;
        cachedInputX = rawInput.x;
        cachedInputZ = rawInput.z;

        // ── 更新 Animator ──
        if (animator != null)
        {
            animator.SetFloat("IsAiming", currentIsAiming);
            animator.SetLayerWeight(1, currentIsAiming);

            if (IsAiming)
            {
                // 举枪模式：四方向扫射，速度上限步行
                animator.SetFloat("MoveX", rawInput.x);
                animator.SetFloat("MoveZ", rawInput.z);
                animator.SetFloat("Speed", Mathf.Min(inputMagnitude, 1f) * walkSpeedMultiplier);
            }
            else
            {
                // 非举枪模式：移动即朝向，只有正前方
                float spd = maxSpeed > 0.01f ? Mathf.Clamp01(currentSpeed / maxSpeed) : 0f;
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveZ", inputMagnitude > 0.01f ? spd : 0f);
                animator.SetFloat("Speed", spd);

                // 检测 180° 转身
                if (inputMagnitude > 0.5f && worldMoveDir.magnitude > 0.01f)
                {
                    float dot = Vector3.Dot(lastFacingDir, worldMoveDir);
                    if (dot < -0.7f)
                        animator.SetTrigger("Turn180");
                }
                if (worldMoveDir.magnitude > 0.01f)
                    lastFacingDir = worldMoveDir;
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead || GameStateManager.IsInputFrozen) return;
        if (rb == null) return;

        Vector3 worldMoveDir = cachedWorldMoveDir;
        bool isDashing = cachedIsDashing;
        bool hasInput = worldMoveDir.magnitude > 0.01f;

        // ── 跳跃 ──
        // 每帧消费闩锁（不满足条件就丢弃，防止跨帧遗留）
        bool wantsToJump = cachedJumpPressed;
        cachedJumpPressed = false;
        if (wantsToJump && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        // ── 水平速度 ──
        float effectiveMaxSpeed = IsAiming ? maxSpeed * walkSpeedMultiplier : maxSpeed;

        if (IsAiming && currentVelocity > effectiveMaxSpeed)
            currentVelocity = effectiveMaxSpeed;

        if (!isDashing && hasInput)
            currentVelocity = Mathf.MoveTowards(currentVelocity, effectiveMaxSpeed,
                acceleration * Time.fixedDeltaTime);
        else
            currentVelocity = Mathf.MoveTowards(currentVelocity, 0f,
                deceleration * Time.fixedDeltaTime);
        currentSpeed = currentVelocity;

        // ── 水平移动：用 MovePosition 移动，不碰 velocity，Y 完全由物理引擎管理 ──
        Vector3 horizVel = (!isDashing && hasInput) ? worldMoveDir * currentSpeed : Vector3.zero;
        if (horizVel.magnitude > 0.001f)
            rb.MovePosition(rb.position + horizVel * Time.fixedDeltaTime);

        // ── 旋转 ──
        if (IsAiming)
        {
            if (Camera.main != null)
            {
                Vector3 aimDir = Camera.main.transform.forward;
                aimDir.y = 0;
                if (aimDir.magnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(aimDir.normalized);
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, target,
                        aimRotationSpeed * Time.fixedDeltaTime));
                }
            }
        }
        else if (hasInput)
        {
            Quaternion target = Quaternion.LookRotation(worldMoveDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target,
                rotationSpeed * Time.fixedDeltaTime));
        }

        // 本帧结束，重置着地状态，等待下一物理帧 OnCollisionStay 重新确认
        isGrounded = false;
    }

    void OnCollisionStay(Collision c)
    {
        for (int i = 0; i < c.contactCount; i++)
        {
            if (Vector3.Dot(c.GetContact(i).normal, Vector3.up) > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    public void ResetData()
    {
        isDead = false;
        currentIsAiming = 0f;
        IsAiming = false;
        isGrounded = true;

        transform.position = initialPosition;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.SetFloat("IsAiming", 0f);
            animator.SetLayerWeight(1, 0f);
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.Play("Normal_Loco", 0, 0f);
            animator.Play("Empty", 1, 0f);
        }
    }
}
