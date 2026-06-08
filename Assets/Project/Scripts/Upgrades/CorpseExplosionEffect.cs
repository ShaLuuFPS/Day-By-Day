using UnityEngine;

/// <summary>
/// 尸体爆炸 —— 仅玩家直接击杀的敌人触发爆炸，爆炸波及的敌人只死亡不连环爆
/// </summary>
public class CorpseExplosionEffect : IUpgradeEffect
{
    private const float ExplosionRadius = 3f;
    private const float ExplosionDamage = 20f;

    /// <summary>防递归：正在处理爆炸时忽略新死亡事件，防止连环爆炸</summary>
    private bool isExploding = false;

    public void OnApply()
    {
        UpgradeManager.CorpseExplosionEnabled = true;
    }

    public void OnEnemyKilled(EnemyHealth enemy)
    {
        // 防连环爆炸：AOE 炸死的敌人不触发新爆炸
        if (isExploding) return;

        isExploding = true;
        DoExplosion(enemy.transform.position, enemy);
        isExploding = false;
    }

    void DoExplosion(Vector3 pos, EnemyHealth source)
    {
        SpawnExplosionVFX(pos);

        // 伤害周围敌人（不伤玩家、不伤自己）
        Collider[] hits = Physics.OverlapSphere(pos, ExplosionRadius);
        foreach (var h in hits)
        {
            EnemyHealth eh = h.GetComponent<EnemyHealth>();
            if (eh != null && eh != source)
                eh.TakeDamage(ExplosionDamage);
        }

        Debug.Log($"[尸体爆炸] 中心={pos} 半径={ExplosionRadius} 伤害={ExplosionDamage}");
    }

    void SpawnExplosionVFX(Vector3 pos)
    {
        // 简单半透明球体（URP 透明材质）
        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vfx.transform.position = pos;
        vfx.transform.localScale = Vector3.one * ExplosionRadius * 2f;
        Object.Destroy(vfx.GetComponent<Collider>());

        Renderer r = vfx.GetComponent<Renderer>();
        if (r != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            mat.color = new Color(1f, 0.3f, 0f, 0.3f);
            r.material = mat;
        }
        Object.Destroy(vfx, 0.5f);
    }

    public void OnBulletHit(Bullet bullet, EnemyHealth enemy) { }
}
