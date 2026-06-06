using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    private float damage;
    private bool _hit = false;

    void Start()
    {
        Destroy(gameObject, 3f);
    }

    public void Initialize(float weaponDamage)
    {
        this.damage = weaponDamage;
    }

    void Update()
    {
        if (_hit) return;

        float moveDistance = speed * Time.deltaTime;

        // 射线检测防止高速子弹穿透（tunneling）：从当前位置向前扫一条射线，
        // 如果下一帧位置之前有敌人则命中。QueryTriggerInteraction.Collide
        // 确保能命中敌人身上的 Trigger 碰撞体。
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
                moveDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                _hit = true;
                enemy.TakeDamage(damage);
                Destroy(gameObject);
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
            _hit = true;
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}