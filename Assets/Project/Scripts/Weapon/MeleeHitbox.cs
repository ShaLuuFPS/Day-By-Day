using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 挂在 Player 子物体上的 Trigger 碰撞体。
/// Activate 时做扇形检测并伤害敌人。
/// 攻击后短暂显示半透明扇形面片表示攻击范围。
/// </summary>
public class MeleeHitbox : MonoBehaviour
{
    [Header("面片显示")]
    [Tooltip("攻击后显示范围面片的时长（秒）")]
    public float fanShowDuration = 0.5f;
    [Tooltip("面片材质（留空则自动创建半透明橙色）")]
    public Material fanMaterialOverride;

    private float damage;
    private float range;
    private float fanAngle;
    private float knockback;
    private float activeTime;
    private float fanShowTimer = 0f;
    private HashSet<EnemyHealth> hitTargets = new HashSet<EnemyHealth>();
    private SphereCollider sphereCollider;
    private bool isAttacking = false;

    // ── 半透明面片 ──
    private GameObject fanVisual;
    private MeshFilter fanMeshFilter;
    private MeshRenderer fanMeshRenderer;
    private Material fanMaterial;
    private Mesh fanMesh;
    private float lastDrawnRange = -1f;
    private float lastDrawnAngle = -1f;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.enabled = false;

        CreateFanVisual();
        if (fanVisual != null) fanVisual.SetActive(false);
    }

    void Update()
    {
        if (fanShowTimer > 0f)
        {
            fanShowTimer -= Time.deltaTime;

            if (fanVisual != null && !fanVisual.activeSelf)
                fanVisual.SetActive(true);

            if (range != lastDrawnRange || fanAngle != lastDrawnAngle)
                UpdateFanVisual(range, fanAngle);
        }
        else
        {
            if (fanVisual != null && fanVisual.activeSelf)
                fanVisual.SetActive(false);
        }
    }

    /// <summary>
    /// 按住左键时显示攻击范围（不攻击），PlayerShooting.HandleMeleeInput 调用
    /// </summary>
    public void ShowRangeIndicator(float drawRange, float drawAngle)
    {
        this.range = drawRange;
        this.fanAngle = drawAngle;
        fanShowTimer = 9999f; // 持续显示直到松开
        UpdateFanVisual(drawRange, drawAngle);
        if (fanVisual != null) fanVisual.SetActive(true);
    }

    /// <summary>
    /// 按住期间每帧刷新范围朝向（跟随玩家旋转）
    /// </summary>
    public void KeepShowingRange()
    {
        if (fanShowTimer > 0f)
        {
            fanShowTimer = 9999f; // 保持显示
            UpdateFanVisual(range, fanAngle);
        }
    }

    /// <summary>
    /// PlayerShooting 调用此方法激活一刀
    /// </summary>
    public void Activate(ComboStage stage)
    {
        damage   = stage.damage * stage.damageMultiplier;
        range    = stage.range * stage.rangeMultiplier;
        fanAngle = stage.fanAngle;
        knockback = stage.knockback;
        activeTime = stage.hitboxActiveTime;

        sphereCollider.radius = range;
        sphereCollider.enabled = true;
        hitTargets.Clear();
        isAttacking = true;

        // 刷新面片 + 重置显示计时器（显示时长 = 攻击窗口时长）
        UpdateFanVisual(range, fanAngle);
        fanShowTimer = activeTime;

        ScanHit();

        CancelInvoke(nameof(Deactivate));
        Invoke(nameof(Deactivate), activeTime);
    }

    void OnTriggerEnter(Collider other) { TryHit(other); }
    void OnTriggerStay(Collider other)  { TryHit(other); }

    void TryHit(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;
        if (hitTargets.Contains(enemy)) return;

        Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToEnemy);
        if (angle > fanAngle * 0.5f) return;

        hitTargets.Add(enemy);
        enemy.TakeDamage(damage);

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null && knockback > 0f)
            rb.AddForce(dirToEnemy * knockback, ForceMode.Impulse);
    }

    void ScanHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        foreach (var hit in hits) TryHit(hit);
    }

    void Deactivate()
    {
        hitTargets.Clear();
        sphereCollider.enabled = false;
        isAttacking = false;
    }

    // ─── 半透明扇形面片 ───

    void CreateFanVisual()
    {
        fanVisual = new GameObject("FanVisual");
        fanVisual.transform.SetParent(transform);
        fanVisual.transform.localPosition = Vector3.zero;
        fanVisual.transform.localRotation = Quaternion.identity;

        fanMeshFilter = fanVisual.AddComponent<MeshFilter>();
        fanMeshRenderer = fanVisual.AddComponent<MeshRenderer>();

        if (fanMaterialOverride != null)
        {
            fanMaterial = fanMaterialOverride;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            fanMaterial = new Material(shader);
            fanMaterial.color = new Color(1f, 0.5f, 0f, 0.25f);
            fanMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fanMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fanMaterial.SetInt("_ZWrite", 0);
            fanMaterial.renderQueue = 3000;
        }
        fanMeshRenderer.material = fanMaterial;

        fanMesh = new Mesh();
        fanMesh.name = "FanMesh";
        fanMeshFilter.mesh = fanMesh;
    }

    void UpdateFanVisual(float drawRange, float drawAngle)
    {
        if (fanMesh == null) return;

        lastDrawnRange = drawRange;
        lastDrawnAngle = drawAngle;

        int segments = 32;
        int vertCount = segments + 2;
        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[segments * 3];
        Vector2[] uv = new Vector2[vertCount];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0f);

        float halfAngle = drawAngle * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float a = -halfAngle + (drawAngle / segments) * i;
            float rad = a * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Sin(rad) * drawRange, 0f, Mathf.Cos(rad) * drawRange);
            uv[i + 1] = new Vector2((float)i / segments, 1f);

            if (i < segments)
            {
                int t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = i + 2;
            }
        }

        fanMesh.Clear();
        fanMesh.vertices = vertices;
        fanMesh.triangles = triangles;
        fanMesh.uv = uv;
        fanMesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        if (fanMaterial != null) Destroy(fanMaterial);
        if (fanMesh != null) Destroy(fanMesh);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (fanShowTimer <= 0f && !isAttacking) return;

        float drawRange = range;
        float drawAngle = fanAngle;

        Gizmos.color = isAttacking
            ? new Color(0f, 1f, 1f, 0.3f)
            : new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, drawRange);

        Gizmos.color = isAttacking ? Color.red : new Color(1f, 0.5f, 0f, 0.4f);
        Vector3 forward = transform.forward;
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 dir = Quaternion.Euler(0f, i * drawAngle * 0.5f, 0f) * forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * drawRange);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + forward * drawRange);
    }
#endif
}
