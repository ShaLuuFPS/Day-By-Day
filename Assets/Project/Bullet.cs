using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f; // 子弹飞行的速度
    public float damage = 10f;

    void Start()
    {
        // 性能优化：子弹生成 3 秒后如果没撞到东西，自动毁灭，防止子弹飞到宇宙边缘导致电脑卡死
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        // 让子弹每帧沿着【它自己的正前方 (Vector3.forward)】高速移动
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 尝试去获取被撞物体身上挂载的 EnemyHealth 脚本组件
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();

        // 2. 如果成功获取到了（说明撞到的是敌人，而不是墙壁或地面）
        if (enemy != null)
        {
            // 让敌人执行受伤逻辑，把子弹的伤害量传过去
            enemy.TakeDamage(damage);

            // 3. 子弹完成了使命，立刻毁灭自身
            Destroy(gameObject);
        }
    }
}