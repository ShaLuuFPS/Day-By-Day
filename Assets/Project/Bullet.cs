using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f; // 子弹飞行的速度

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
}