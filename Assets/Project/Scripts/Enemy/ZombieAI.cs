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
            if (_suicideBomber)
            {
                // 自爆：到达攻击范围直接引爆
                if (enemyHealth != null)
                    enemyHealth.TriggerDeath();
                isDead = true;
                return;
            }

            if (attackCooldownTimer <= 0f)
                ExecuteAttack();
        }

        // 冷却倒计时
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.fixedDeltaTime;

        // 移动
        if (currentDistance <= _stoppingDistance && !_suicideBomber)
            rb.linearVelocity = Vector3.zero;
        else
        {
            Vector3 targetVelocity = directionToPlayer.normalized * _moveSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
    }

    void ExecuteAttack()
    {
        if (playerTransform == null) return;
        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(_attackDamage);
            attackCooldownTimer = _attackRate;
        }
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
