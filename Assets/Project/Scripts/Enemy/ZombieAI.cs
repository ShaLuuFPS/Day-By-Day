using UnityEngine;
using System.Collections;

public class ZombieAI : MonoBehaviour
{
    [Header("僵尸配置")]
    public ZombieData zombieData;

    // 运行时缓存
    private float _moveSpeed        = 3.5f;
    private float _stoppingDistance = 2.0f;
    private float _attackRange      = 2.5f;
    private float _attackDamage     = 10f;
    private float _attackRate       = 1.0f;
    private float _warningRadius    = 0f;
    private bool  _suicideBomber    = false;

    private float attackCooldownTimer = 0f;
    private Transform playerTransform;
    private Rigidbody rb;
    private Renderer rend;
    private EnemyHealth enemyHealth;
    private Coroutine flickerRoutine;
    private bool isDead = false;

    // ── 攻击预警状态 ──
    private bool _isWarning = false;
    private Coroutine _warningRoutine;
    private GameObject _currentWarningIndicator;

    void Start()
    {
        rb          = GetComponent<Rigidbody>();
        rend        = GetComponent<Renderer>();
        enemyHealth = GetComponent<EnemyHealth>();
        ApplyConfig();
    }

    public void ApplyConfig()
    {
        if (zombieData == null) return;

        _moveSpeed        = zombieData.moveSpeed;
        _stoppingDistance = zombieData.stoppingDistance;
        _attackRange      = zombieData.attackRange;
        _attackDamage     = zombieData.attackDamage;
        _attackRate       = zombieData.attackRate;
        _warningRadius    = zombieData.warningRadius;
        _suicideBomber    = zombieData.suicideBomber;

        Debug.Log($"[ZombieAI] {name} ApplyConfig: showAttackWarning={zombieData.showAttackWarning} " +
                  $"warnDur={zombieData.attackWarningDuration} attackRange={_attackRange} " +
                  $"chargeSpeed={zombieData.chargeSpeed} chargeRange={zombieData.chargeRange}");

        if (rend != null)
            rend.material.color = zombieData.baseColor;

        // 如果 warningRadius == 0，立即开始闪烁；否则等玩家靠近
        if (zombieData.useFlickerEffect && _warningRadius <= 0f)
            StartFlicker();
    }

    void FixedUpdate()
    {
        if (rb == null || isDead) return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else return;
        }

        Vector3 directionToPlayer = playerTransform.position - transform.position;
        directionToPlayer.y = 0f;
        float currentDistance = directionToPlayer.magnitude;

        // 面向玩家
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        // === 预警闪烁控制 ===
        if (zombieData != null && zombieData.useFlickerEffect && _warningRadius > 0f)
        {
            if (currentDistance <= _warningRadius && flickerRoutine == null)
                StartFlicker();
            else if (currentDistance > _warningRadius && flickerRoutine != null)
                StopFlicker();
        }

        // === 攻击 / 自爆判定 ===
        if (currentDistance <= _attackRange)
        {
            if (_suicideBomber && attackCooldownTimer <= 0f)
            {
                if (zombieData != null && zombieData.showAttackWarning && !_isWarning)
                {
                    // 自爆预警：地面圈 → 等待 → 爆炸
                    attackCooldownTimer = 999f;
                    _warningRoutine = StartCoroutine(SuicideWarningRoutine());
                }
                else if (zombieData == null || !zombieData.showAttackWarning)
                {
                    // 即时自爆（无预警）
                    if (enemyHealth != null)
                        enemyHealth.TriggerDeath();
                    isDead = true;
                    return;
                }
            }

            if (attackCooldownTimer <= 0f)
            {
                if (zombieData != null && zombieData.showAttackWarning && !_isWarning)
                {
                    // 进入攻击预警阶段（协程内生成指示器 → 等待 → 攻击）
                    attackCooldownTimer = _attackRate + zombieData.attackWarningDuration;
                    _warningRoutine = StartCoroutine(AttackWarningRoutine());
                }
                else if (zombieData == null || !zombieData.showAttackWarning)
                {
                    Debug.Log($"[ZombieAI] {name} 直接攻击（无预警）showWarning={zombieData?.showAttackWarning}");
                    ExecuteAttack();
                }
            }
        }

        // 冷却倒计时
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.fixedDeltaTime;

        // 移动
        if (currentDistance <= _stoppingDistance && !_suicideBomber)
            rb.linearVelocity = Vector3.zero;
        else
        {
            float effectiveSpeed = _moveSpeed;
            if (zombieData != null && zombieData.chargeSpeed > 0f && currentDistance <= zombieData.chargeRange)
                effectiveSpeed = zombieData.chargeSpeed;

            float speedMultiplier = _isWarning ? 0.2f : 1f;
            Vector3 targetVelocity = directionToPlayer.normalized * effectiveSpeed * speedMultiplier;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    void ExecuteAttack()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
        );

        Debug.Log($"[ZombieAI] {name} ExecuteAttack! dist={dist:F2} attackRange={_attackRange} " +
                  $"showWarning={zombieData?.showAttackWarning} isWarning={_isWarning} dmg={_attackDamage}");

        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(_attackDamage);
            attackCooldownTimer = _attackRate;
        }
    }

    // ---- 攻击预警 ----

    IEnumerator AttackWarningRoutine()
    {
        _isWarning = true;

        // 生成地面预警指示器
        if (_currentWarningIndicator != null)
            Destroy(_currentWarningIndicator);

        _currentWarningIndicator = AttackWarningIndicator.Create(
            transform.position,
            _attackRange,
            zombieData.attackWarningDuration,
            zombieData.attackWarningPrefab
        );

        float elapsed = 0f;
        float warningDur = zombieData.attackWarningDuration;

        while (elapsed < warningDur)
        {
            elapsed += Time.deltaTime;

            // 动态更新指示器位置（跟随敌人）
            if (_currentWarningIndicator != null)
                _currentWarningIndicator.transform.position = transform.position;

            // 检查玩家是否还在攻击范围内（10% 容错避免边界抖动）
            if (playerTransform == null)
                break;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
            );

            if (dist > _attackRange * 1.1f)
            {
                // 玩家逃出范围 → 取消攻击
                if (_currentWarningIndicator != null)
                {
                    Destroy(_currentWarningIndicator);
                    _currentWarningIndicator = null;
                }
                _isWarning = false;
                _warningRoutine = null;
                attackCooldownTimer = 0f;
                yield break;
            }

            yield return null;
        }

        // 预警结束 → 严格距离检查，防止幽灵伤害
        if (playerTransform != null)
        {
            float finalDist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
            );
            if (finalDist > _attackRange)
            {
                Debug.Log($"[ZombieAI] {name} 预警完成但玩家已离开范围 finalDist={finalDist:F2} > {_attackRange} → 取消");
                // 玩家已离开攻击范围 → 取消
                if (_currentWarningIndicator != null)
                {
                    Destroy(_currentWarningIndicator);
                    _currentWarningIndicator = null;
                }
                _isWarning = false;
                _warningRoutine = null;
                attackCooldownTimer = 0f;
                yield break;
            }
        }

        ExecuteAttack();

        // 清理指示器
        if (_currentWarningIndicator != null)
        {
            Destroy(_currentWarningIndicator);
            _currentWarningIndicator = null;
        }

        _isWarning = false;
        _warningRoutine = null;
    }

    IEnumerator SuicideWarningRoutine()
    {
        _isWarning = true;

        // 地面预警圈
        if (_currentWarningIndicator != null)
            Destroy(_currentWarningIndicator);

        _currentWarningIndicator = AttackWarningIndicator.Create(
            transform.position,
            _attackRange,
            zombieData.attackWarningDuration,
            zombieData.attackWarningPrefab
        );

        float elapsed = 0f;
        float warningDur = zombieData.attackWarningDuration;

        while (elapsed < warningDur)
        {
            elapsed += Time.deltaTime;

            if (_currentWarningIndicator != null)
                _currentWarningIndicator.transform.position = transform.position;

            if (playerTransform == null)
                break;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
            );

            if (dist > _attackRange * 1.1f)
            {
                // 玩家逃出范围 → 取消自爆
                if (_currentWarningIndicator != null)
                {
                    Destroy(_currentWarningIndicator);
                    _currentWarningIndicator = null;
                }
                _isWarning = false;
                _warningRoutine = null;
                attackCooldownTimer = 0f;
                yield break;
            }

            yield return null;
        }

        // 预警结束 → 严格距离检查
        if (playerTransform != null)
        {
            float finalDist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(playerTransform.position.x, 0, playerTransform.position.z)
            );
            if (finalDist > _attackRange)
            {
                if (_currentWarningIndicator != null)
                {
                    Destroy(_currentWarningIndicator);
                    _currentWarningIndicator = null;
                }
                _isWarning = false;
                _warningRoutine = null;
                attackCooldownTimer = 0f;
                yield break;
            }
        }

        // 自爆
        if (enemyHealth != null)
            enemyHealth.TriggerDeath();
        isDead = true;

        if (_currentWarningIndicator != null)
        {
            Destroy(_currentWarningIndicator);
            _currentWarningIndicator = null;
        }

        _isWarning = false;
        _warningRoutine = null;
    }

    void OnDisable()
    {
        if (_warningRoutine != null)
        {
            StopCoroutine(_warningRoutine);
            _warningRoutine = null;
        }
        if (_currentWarningIndicator != null)
        {
            Destroy(_currentWarningIndicator);
            _currentWarningIndicator = null;
        }
        _isWarning = false;
    }

    // ---- 闪烁 ----

    void StartFlicker()
    {
        if (flickerRoutine != null) return;
        flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    void StopFlicker()
    {
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }
        // 恢复基础颜色
        if (rend != null && zombieData != null)
            rend.material.color = zombieData.baseColor;
    }

    IEnumerator FlickerRoutine()
    {
        if (zombieData == null || rend == null) yield break;

        Color baseC  = zombieData.baseColor;
        Color flickC = zombieData.flickerColor;
        float interval = zombieData.flickerInterval;

        while (true)
        {
            rend.material.color = flickC;
            yield return new WaitForSeconds(interval);
            rend.material.color = baseC;
            yield return new WaitForSeconds(interval);
        }
    }
}
