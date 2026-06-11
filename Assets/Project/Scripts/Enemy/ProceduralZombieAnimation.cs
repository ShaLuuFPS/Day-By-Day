using UnityEngine;

/// <summary>
/// 程序化僵尸骨骼动画：直接旋转骨骼实现行走、攻击、死亡动画。
/// 不需要 Animator / AnimationClip，适用于简单骨骼结构。
/// </summary>
public class ProceduralZombieAnimation : MonoBehaviour
{
    [Header("骨骼引用（自动查找）")]
    public Transform armatureRoot;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform rightArm;
    public Transform head;
    public Transform body;

    [Header("行走动画")]
    public float walkSwingAngle = 25f;      // 四肢摆动最大角度
    public float walkSwingSpeed = 8f;       // 摆动速度
    public float bodyBobHeight = 0.08f;     // 身体上下浮动幅度
    public float bodyBobSpeed = 8f;

    [Header("攻击动画")]
    public float attackLungeAngle = 30f;    // 攻击时身体前倾角度
    public float attackDuration = 0.4f;
    public float attackRecoveryTime = 0.3f;

    [Header("受伤动画")]
    public float hurtFlashAngle = 10f;      // 受伤后仰角度
    public float hurtDuration = 0.15f;

    // 运行时状态
    private float _animTime;
    private float _attackAnimTimer;
    private float _hurtAnimTimer;
    private bool _isAttacking;
    private bool _isDead;
    private bool _isMoving;

    // 原始旋转
    private Quaternion _bodyOrigRot;
    private Quaternion _leftLegOrigRot;
    private Quaternion _rightLegOrigRot;
    private Quaternion _leftArmOrigRot;
    private Quaternion _rightArmOrigRot;
    private Vector3 _bodyOrigPos;
    private Vector3 _headOrigPos;

    void Start()
    {
        AutoFindBones();
        CacheOriginalTransforms();
    }

    void AutoFindBones()
    {
        // 查找 armature 根节点
        if (armatureRoot == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("armature") || child.name.ToLower().Contains("root"))
                {
                    armatureRoot = child;
                    break;
                }
            }
        }

        if (armatureRoot == null) return;

        // 递归查找骨骼
        FindBoneRecursive(armatureRoot);
    }

    void FindBoneRecursive(Transform t)
    {
        string n = t.name.ToLower();

        if (n.Contains("leg_l") || n.Contains("legl") || n.Contains("leftleg")) leftLeg = t;
        if (n.Contains("leg_r") || n.Contains("legr") || n.Contains("rightleg")) rightLeg = t;
        if (n.Contains("arm_l") || n.Contains("arml") || n.Contains("leftarm")) leftArm = t;
        if (n.Contains("arm_r") || n.Contains("armr") || n.Contains("rightarm")) rightArm = t;
        if (n.Contains("head")) head = t;
        if (n.Contains("body") || n.Contains("torso") || n.Contains("spine")) body = t;

        foreach (Transform child in t)
            FindBoneRecursive(child);
    }

    void CacheOriginalTransforms()
    {
        if (body != null) _bodyOrigRot = body.localRotation;
        if (leftLeg != null) _leftLegOrigRot = leftLeg.localRotation;
        if (rightLeg != null) _rightLegOrigRot = rightLeg.localRotation;
        if (leftArm != null) _leftArmOrigRot = leftArm.localRotation;
        if (rightArm != null) _rightArmOrigRot = rightArm.localRotation;
        if (body != null) _bodyOrigPos = body.localPosition;
        if (head != null) _headOrigPos = head.localPosition;
    }

    void Update()
    {
        if (_isDead || armatureRoot == null) return;

        _animTime += Time.deltaTime;

        if (_isAttacking)
        {
            DoAttackAnimation();
            return;
        }

        if (_hurtAnimTimer > 0f)
        {
            DoHurtAnimation();
            return;
        }

        if (_isMoving)
        {
            DoWalkAnimation();
        }
    }

    void DoWalkAnimation()
    {
        float swing = Mathf.Sin(_animTime * walkSwingSpeed) * walkSwingAngle;

        // 腿部交替摆动
        if (leftLeg != null)
            leftLeg.localRotation = _leftLegOrigRot * Quaternion.Euler(swing, 0, 0);
        if (rightLeg != null)
            rightLeg.localRotation = _rightLegOrigRot * Quaternion.Euler(-swing, 0, 0);

        // 手臂反向摆动（与腿相反）
        if (leftArm != null)
            leftArm.localRotation = _leftArmOrigRot * Quaternion.Euler(-swing, 0, 0);
        if (rightArm != null)
            rightArm.localRotation = _rightArmOrigRot * Quaternion.Euler(swing, 0, 0);

        // 身体上下浮动
        float bob = Mathf.Abs(Mathf.Sin(_animTime * bodyBobSpeed * 2f)) * bodyBobHeight;
        if (body != null)
            body.localPosition = _bodyOrigPos + new Vector3(0, bob, 0);
    }

    void DoAttackAnimation()
    {
        _attackAnimTimer += Time.deltaTime;
        float t = _attackAnimTimer / attackDuration;

        if (t <= 1f)
        {
            // 攻击前冲阶段：身体前倾 + 手臂前伸
            float lunge = Mathf.Sin(t * Mathf.PI * 0.5f) * attackLungeAngle;
            if (body != null)
                body.localRotation = _bodyOrigRot * Quaternion.Euler(-lunge, 0, 0);
            if (leftArm != null)
                leftArm.localRotation = _leftArmOrigRot * Quaternion.Euler(-lunge * 2f, 0, 0);
            if (rightArm != null)
                rightArm.localRotation = _rightArmOrigRot * Quaternion.Euler(-lunge * 2f, 0, 0);
        }
        else
        {
            // 恢复阶段
            float recoveryT = (t - 1f) / (attackRecoveryTime / attackDuration);
            recoveryT = Mathf.Clamp01(recoveryT);

            if (body != null)
                body.localRotation = Quaternion.Slerp(
                    _bodyOrigRot * Quaternion.Euler(-attackLungeAngle, 0, 0),
                    _bodyOrigRot, recoveryT);
            if (leftArm != null)
                leftArm.localRotation = Quaternion.Slerp(
                    _leftArmOrigRot * Quaternion.Euler(-attackLungeAngle * 2f, 0, 0),
                    _leftArmOrigRot, recoveryT);
            if (rightArm != null)
                rightArm.localRotation = Quaternion.Slerp(
                    _rightArmOrigRot * Quaternion.Euler(-attackLungeAngle * 2f, 0, 0),
                    _rightArmOrigRot, recoveryT);

            if (recoveryT >= 1f)
            {
                _isAttacking = false;
                _attackAnimTimer = 0f;
            }
        }
    }

    void DoHurtAnimation()
    {
        _hurtAnimTimer -= Time.deltaTime;
        float t = _hurtAnimTimer / hurtDuration;
        float angle = Mathf.Sin(t * Mathf.PI) * hurtFlashAngle;

        if (body != null)
            body.localRotation = _bodyOrigRot * Quaternion.Euler(-angle, 0, 0);
    }

    // ---- 公共接口，由 ZombieAI / EnemyHealth 调用 ----

    public void SetMoving(bool moving)
    {
        _isMoving = moving;
    }

    public void TriggerAttack()
    {
        _isAttacking = true;
        _attackAnimTimer = 0f;
    }

    public void TriggerHurt()
    {
        _hurtAnimTimer = hurtDuration;
    }

    public void TriggerDeath()
    {
        _isDead = true;
        // 简单死亡姿态：所有骨骼前倾倒地
        if (body != null)
            body.localRotation = _bodyOrigRot * Quaternion.Euler(-90f, 0, 0);
        if (head != null)
            head.localRotation = Quaternion.Euler(30f, 0, 0);
    }

    /// <summary>由 ZombieAI 每帧调用，传入当前速度 magnitude</summary>
    public void UpdateMovementState(float speed)
    {
        SetMoving(speed > 0.1f);
    }
}
