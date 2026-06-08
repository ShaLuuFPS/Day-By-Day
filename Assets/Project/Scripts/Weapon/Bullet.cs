using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    private float damage;
    public float Damage => damage;
    private bool _hit = false;

    /// <summary>穿透弹剩余穿透次数（>0 时命中不销毁）</summary>
    public int remainingPierce = 0;
    /// <summary>标记为分裂弹（防止无限递归分裂）</summary>
    public bool isSplitBullet = false;

    /// <summary>子弹命中敌人时触发，供 UpgradeManager / Effect 订阅</summary>
    public static event System.Action<Bullet, EnemyHealth> OnBulletHitEnemy;

    void Start()
    {
        Destroy(gameObject, 3f);

        // 穿透弹：从 UpgradeManager 读取当前穿透次数
        if (UpgradeManager.PierceCount > 0 && remainingPierce <= 0)
            remainingPierce = UpgradeManager.PierceCount;
    }

    public void Initialize(float weaponDamage)
    {
        this.damage = weaponDamage;
    }

    void Update()
    {
        if (_hit) return;

        float moveDistance = speed * Time.deltaTime;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
                moveDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                HandleHit(enemy);
                return;
            }
        }

        transform.Translate(Vector3.forward * moveDistance, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hit) return;

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            HandleHit(enemy);
        }
    }

    void HandleHit(EnemyHealth enemy)
    {
        enemy.TakeDamage(damage);

        // 通知所有升级效果（分裂弹、减速弹、电磁弹等在此响应）
        OnBulletHitEnemy?.Invoke(this, enemy);

        // 穿透弹：命中后不销毁，继续飞行
        if (remainingPierce > 0)
        {
            remainingPierce--;
            return; // 不销毁，继续穿透
        }

        _hit = true;
        Destroy(gameObject);
    }
}
