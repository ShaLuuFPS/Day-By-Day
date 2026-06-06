using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    [Header("僵尸配置")]
    public ZombieData zombieData;

    private float _maxHealth         = 30f;
    private Color _hurtColor         = Color.red;
    private float _hurtFlashDuration = 0.1f;
    private Color _currentBaseColor  = Color.white;

    private float currentHealth;
    private Renderer enemyRenderer;
    private bool _isDead = false;

    void Start()
    {
        enemyRenderer = GetComponent<Renderer>();
        ApplyConfig();
    }

    public void ApplyConfig()
    {
        if (zombieData == null) return;

        _maxHealth         = zombieData.maxHealth;
        _hurtColor         = zombieData.hurtColor;
        _hurtFlashDuration = zombieData.hurtFlashDuration;
        _currentBaseColor  = zombieData.baseColor;

        if (currentHealth <= 0f || currentHealth > _maxHealth)
            currentHealth = _maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        if (_isDead) return;

        currentHealth -= damageAmount;
        ShowHurtEffect();

        // 敌人受伤 → 红色伤害数字
        DamageNumberManager.Spawn(transform, transform.position, damageAmount, Color.red);

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// 供 ZombieAI 自爆模式调用
    /// </summary>
    public void TriggerDeath()
    {
        if (!_isDead) Die();
    }

    void ShowHurtEffect()
    {
        if (enemyRenderer == null) return;
        enemyRenderer.material.color = _hurtColor;
        StartCoroutine(ResetColorAfterDelay(_hurtFlashDuration));
    }

    IEnumerator ResetColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemyRenderer != null)
            enemyRenderer.material.color = _currentBaseColor;
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // 立即禁用 AI，防止 Destroy 延迟导致的同帧"幽灵攻击"
        ZombieAI ai = GetComponent<ZombieAI>();
        if (ai != null) ai.enabled = false;

        if (zombieData != null && zombieData.explodeOnDeath)
            Explode();

        TryDropLoot();
        Destroy(gameObject);
    }

    void Explode()
    {
        float radius = zombieData.explosionRadius;
        float damage = zombieData.explosionDamage;

        // 自杀式爆炸：半径小于攻击距离会导致炸不到人
        if (zombieData.suicideBomber && radius < zombieData.attackRange)
            Debug.LogWarning($"[{zombieData.zombieName}] 爆炸半径({radius}) < 攻击距离({zombieData.attackRange})，自杀爆炸可能炸不到玩家！");

        // --- 视觉特效（半圆球体，赤道贴地）---
        // 射线找真实地面高度，球心贴地 → 上半球可见、下半球在地面以下
        Vector3 vfxPos = transform.position;
        float groundY = vfxPos.y;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, 10f))
            groundY = groundHit.point.y;
        vfxPos.y = groundY;

        if (zombieData.explosionEffectPrefab != null)
        {
            GameObject vfx = Instantiate(zombieData.explosionEffectPrefab, vfxPos, Quaternion.identity);
            // 自动对齐：特效可视半径 == 伤害半径（考虑预制体自身 scale）
            float meshRadius   = GetMeshHorizontalRadius(vfx);
            Vector3 prefabScale = zombieData.explosionEffectPrefab.transform.localScale;
            float effectiveR   = meshRadius * Mathf.Max(prefabScale.x, prefabScale.z);
            float autoScale    = effectiveR > 0.001f ? radius / effectiveR : radius;
            vfx.transform.localScale = prefabScale * autoScale;
            Destroy(vfx, 0.5f);
        }
        else
        {
            // 代码兜底：Unity 默认球体半径=0.5，scale * 2 = 直径翻倍至 2*radius
            GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vfx.transform.position = vfxPos;
            vfx.transform.localScale = Vector3.one * radius * 2f;
            Destroy(vfx.GetComponent<Collider>());
            Destroy(vfx, 0.5f);
        }

        // --- 范围伤害（去重，防止多碰撞体重复扣血）---
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        Debug.Log($"[Explode] 中心={transform.position} 半径={radius} 伤害={damage} 命中数={hits.Length}");

        HashSet<PlayerHealth> damagedPlayers = new HashSet<PlayerHealth>();

        foreach (Collider hit in hits)
        {
            // 玩家可能在碰撞体的父对象上（如 CharacterController 子物体）
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph == null) ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph != null && damagedPlayers.Add(ph))
            {
                Debug.Log($"[Explode] 对玩家造成 {damage} 点伤害");
                ph.TakeDamage(damage);
                continue;
            }

            EnemyHealth eh = hit.GetComponent<EnemyHealth>();
            // 不对自己造成伤害（自身已在 Die() 中标记 _isDead，TakeDamage 也会拦截）
            if (eh != null && eh != this)
                eh.TakeDamage(damage);
        }
    }

    void TryDropLoot()
    {
        if (zombieData == null || zombieData.dropPrefab == null) return;

        if (UnityEngine.Random.value <= zombieData.dropProbability)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            GameObject drop = Instantiate(zombieData.dropPrefab, spawnPos, Quaternion.identity);
            // 标记以便重启游戏时清理
            if (!drop.GetComponent<LootDrop>())
                drop.AddComponent<LootDrop>();
        }
    }

    /// <summary>
    /// 获取预制体 mesh 在水平方向的最大半径（XZ 平面），用于对齐特效与伤害范围
    /// </summary>
    float GetMeshHorizontalRadius(GameObject obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Bounds b = mf.sharedMesh.bounds;
            return Mathf.Max(b.extents.x, b.extents.z);
        }
        return 0.5f; // 兜底：默认球体半径
    }
}
