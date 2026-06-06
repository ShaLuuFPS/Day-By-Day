# 近战武器系统（刀）实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 重构 WeaponData 继承体系，新增近战武器类型（刀）带连击系统。

**Architecture:** WeaponData → 基类（weaponName + weaponType），GunData 持有原枪械字段，MeleeData 持有 ComboStage[] 连击链。PlayerShooting 按 weaponType 分流射击/近战。近战用扇形 hitbox 检测。

**Tech Stack:** Unity 6 URP, C#, ScriptableObject, Input System

---

### Task 1: 重构 WeaponData 为基类

**Files:**
- Modify: `Assets/Project/Scripts/Weapon/WeaponData.cs`
- Create: `Assets/Project/Scripts/Weapon/GunData.cs`

- [ ] **Step 1: 重写 WeaponData.cs 为纯基类**

将 `Assets/Project/Scripts/Weapon/WeaponData.cs` 替换为：

```csharp
using UnityEngine;

public enum WeaponType
{
    Gun,
    Melee
}

[CreateAssetMenu(menuName = "DayByDay/WeaponData (基类)")]
public class WeaponData : ScriptableObject
{
    [Header("武器身份")]
    public string weaponName = "未命名武器";

    [Header("武器类型")]
    public WeaponType weaponType = WeaponType.Gun;
}
```

- [ ] **Step 2: 新建 GunData.cs**

创建 `Assets/Project/Scripts/Weapon/GunData.cs`：

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "DayByDay/GunData")]
public class GunData : WeaponData
{
    [Header("弹夹")]
    public int maxMagazineSize = 30;
    public float reloadTime = 2.0f;

    [Header("射击核心")]
    public float fireRate = 0.1f;
    public bool isAutomatic = true;
    public float damage = 10f;

    [Header("备弹")]
    public int defaultReserveAmmo = 90;

    [Header("换弹机制")]
    [Tooltip("true = 弹夹式一次性装满 / false = 逐发装填（霰弹枪）")]
    public bool isMagazineReload = true;
    [Tooltip("逐发装填时每发耗时（秒）")]
    public float perShellReloadTime = 0.3f;

    [Header("霰弹枪专属")]
    [Tooltip("每次开火弹丸数量")]
    public int bulletsPerShot = 1;
    [Tooltip("弹丸散布角度（度）")]
    public float spreadAngle = 0f;

    [Header("视觉")]
    public GameObject bulletPrefab;
}
```

- [ ] **Step 3: 标记现有 .asset 需手动迁移**

Unity Editor 中 3 个 `.asset` 需要改类型（从 WeaponData → GunData）：
- `Assets/Project/Weapondata/Pistol_Config.asset`
- `Assets/Project/Weapondata/Rifle_Config.asset`
- `Assets/Project/Weapondata/Shotgun_Config.asset`

**操作：** 选中每个 .asset → Inspector 顶部 Script 下拉 → 选 `GunData`。数据不会丢失——字段名完全匹配。

---

### Task 2: 新建 MeleeData + ComboStage

**Files:**
- Create: `Assets/Project/Scripts/Weapon/MeleeData.cs`

- [ ] **Step 1: 创建 ComboStage 和 MeleeData**

创建 `Assets/Project/Scripts/Weapon/MeleeData.cs`：

```csharp
using UnityEngine;

/// <summary>
/// 近战连击链中的一段。数组中索引 0 = 第 1 刀, 1 = 第 2 刀...
/// </summary>
[System.Serializable]
public class ComboStage
{
    [Header("伤害")]
    public float damage = 20f;

    [Header("检测范围")]
    [Tooltip("攻击距离（米）")]
    public float range = 2f;
    [Tooltip("扇形角度（度），180 = 前方半圆")]
    public float fanAngle = 90f;

    [Header("效果")]
    [Tooltip("击退力度")]
    public float knockback = 5f;

    [Header("时间")]
    [Tooltip("检测窗口持续时长（秒）")]
    public float hitboxActiveTime = 0.2f;

    [Header("倍率（可选）")]
    [Tooltip("伤害倍率（相对基础值，默认 1）")]
    public float damageMultiplier = 1f;
    [Tooltip("范围倍率（相对基础值，默认 1）")]
    public float rangeMultiplier = 1f;
}

[CreateAssetMenu(menuName = "DayByDay/MeleeData")]
public class MeleeData : WeaponData
{
    [Header("连击链")]
    [Tooltip("挥砍序列：索引 0=第1刀, 1=第2刀, 2=第3刀...")]
    public ComboStage[] comboChain = new ComboStage[]
    {
        new ComboStage { damage = 15f, range = 2f, fanAngle = 90f, knockback = 3f, hitboxActiveTime = 0.2f },
        new ComboStage { damage = 20f, range = 2f, fanAngle = 120f, knockback = 5f, hitboxActiveTime = 0.25f },
        new ComboStage { damage = 35f, range = 2.4f, fanAngle = 180f, knockback = 8f, hitboxActiveTime = 0.3f, damageMultiplier = 1.5f, rangeMultiplier = 1.2f }
    };

    [Header("连击节奏")]
    [Tooltip("两刀之间最长时间间隔（秒），超时重置到第 1 段")]
    public float comboCooldown = 1.0f;
}
```

---

### Task 3: 新建 MeleeHitbox 组件

**Files:**
- Create: `Assets/Project/Scripts/Weapon/MeleeHitbox.cs`

- [ ] **Step 1: 创建 MeleeHitbox.cs**

创建 `Assets/Project/Scripts/Weapon/MeleeHitbox.cs`：

```csharp
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 挂在 Player 子物体上的 Trigger 碰撞体。
/// Enable 时自动做扇形检测并伤害敌人，hitboxActiveTime 后自动 Disable。
/// </summary>
public class MeleeHitbox : MonoBehaviour
{
    private float damage;
    private float range;
    private float fanAngle;
    private float knockback;
    private float activeTime;
    private HashSet<EnemyHealth> hitTargets = new HashSet<EnemyHealth>();
    private SphereCollider sphereCollider;

    void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// PlayerShooting 调用此方法激活一刀
    /// </summary>
    public void Activate(ComboStage stage)
    {
        damage = stage.damage * stage.damageMultiplier;
        range = stage.range * stage.rangeMultiplier;
        fanAngle = stage.fanAngle;
        knockback = stage.knockback;
        activeTime = stage.hitboxActiveTime;

        sphereCollider.radius = range;
        hitTargets.Clear();
        gameObject.SetActive(true);

        // 立即扫一圈（帧初命中）
        ScanHit();

        // 到时间自动关
        CancelInvoke(nameof(Deactivate));
        Invoke(nameof(Deactivate), activeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    void TryHit(Collider other)
    {
        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null) return;
        if (hitTargets.Contains(enemy)) return;

        // 扇形角度检测
        Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToEnemy);
        if (angle > fanAngle * 0.5f) return;

        hitTargets.Add(enemy);
        enemy.TakeDamage(damage);

        // 击退
        ZombieAI ai = enemy.GetComponent<ZombieAI>();
        if (ai != null && knockback > 0f)
        {
            Vector3 knockDir = dirToEnemy;
            // 使用反射或公开方法应用击退——这里用 Rigidbody 直接推
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(knockDir * knockback, ForceMode.Impulse);
        }
    }

    void ScanHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        foreach (var hit in hits)
            TryHit(hit);
    }

    void Deactivate()
    {
        hitTargets.Clear();
        gameObject.SetActive(false);
    }
}
```

---

### Task 4: 更新 WeaponSlot 兼容近战

**Files:**
- Modify: `Assets/Project/Scripts/Weapon/WeaponSlot.cs`

- [ ] **Step 1: 替换 WeaponSlot.cs**

用以下内容替换 `Assets/Project/Scripts/Weapon/WeaponSlot.cs`：

```csharp
using UnityEngine;

[System.Serializable]
public class WeaponSlot
{
    public WeaponData weaponData;
    public int currentAmmo;
    public int reserveAmmo;

    public bool IsEmpty => weaponData == null;
    public bool IsMelee => weaponData != null && weaponData.weaponType == WeaponType.Melee;

    public int MaxMagazine
    {
        get
        {
            if (weaponData == null) return 0;
            if (weaponData.weaponType == WeaponType.Melee) return 0;
            GunData gd = weaponData as GunData;
            return gd != null ? gd.maxMagazineSize : 0;
        }
    }

    public void Clear()
    {
        weaponData = null;
        currentAmmo = 0;
        reserveAmmo = 0;
    }

    public void LoadFromConfig(WeaponData config, bool giveInitialReserve)
    {
        weaponData = config;
        if (config.weaponType == WeaponType.Melee)
        {
            currentAmmo = 0;
            reserveAmmo = 0;
        }
        else
        {
            GunData gd = config as GunData;
            if (gd != null)
            {
                currentAmmo = gd.maxMagazineSize;
                if (giveInitialReserve)
                    reserveAmmo = gd.defaultReserveAmmo;
            }
        }
    }
}
```

---

### Task 5: 更新 PlayerShooting —— 核心分流

**Files:**
- Modify: `Assets/Project/Scripts/Player/PlayerShooting.cs`

这是最大的改动。按部位逐一修改。

- [ ] **Step 1: 添加 MeleeHitbox 字段和近战状态**

在 `PlayerShooting` 类顶部字段区，紧跟 `activeSlotIndex` 后添加：

```csharp
[Header("近战")]
public MeleeHitbox meleeHitbox;          // 拖入 Player 子物体上的 MeleeHitbox
private int meleeComboStage = 0;         // 当前连击段索引
private float meleeComboTimer = 0f;      // 连击窗口计时器
```

- [ ] **Step 2: 加 GunData 快捷属性**

在现有属性简写区末尾加：

```csharp
private GunData currentGunData => currentWeaponData as GunData;
private MeleeData currentMeleeData => currentWeaponData as MeleeData;
```

- [ ] **Step 3: 修改 maxMagazineSize / reloadTime 等属性安全化**

`maxMagazineSize`、`reloadTime` 属性当前直接读 `currentWeaponData.xxx`，那些字段已移到 GunData。改为：

```csharp
public int maxMagazineSize => currentGunData != null ? currentGunData.maxMagazineSize : 0;
public float reloadTime => currentGunData != null
    ? (currentGunData.isMagazineReload ? currentGunData.reloadTime : currentGunData.perShellReloadTime)
    : 0f;
```

- [ ] **Step 4: 修改 HandlePlayerInput —— 屏蔽近战时的换弹**

在 `HandlePlayerInput()` 中，切枪输入后的 `if (!hasWeapon || currentWeaponData == null) return;` 之后，加一行：

```csharp
// 近战武器不处理射击/换弹（另走 ExecuteShootLogic 分流）
if (currentWeaponData.weaponType == WeaponType.Melee)
{
    if (Mouse.current.leftButton.wasPressedThisFrame)
        ExecuteShootLogic();
    return;
}
```

然后把原有的 `Mouse.current.leftButton.wasPressedThisFrame` / `isPressed` 射击逻辑包在 `else` 或不改——实际上因为上面 return 了，下面的枪械射击代码不会被近战触发。但原有的全自动/半自动判断对近战不适用，所以靠上面的 return 阻断即可。

- [ ] **Step 5: 修改 HandlePlayerInput —— 换弹输入加 GunData 检查**

换弹输入前加近战排除：

```csharp
// 处理换弹输入（近战没有换弹）
if (currentWeaponData.weaponType != WeaponType.Melee
    && Keyboard.current.rKey.wasPressedThisFrame
    && activeSlot.currentAmmo < maxMagazineSize
    && activeSlot.reserveAmmo > 0)
{
    StartCoroutine(ReloadRoutine());
}
```

- [ ] **Step 6: 修改 ExecuteShootLogic —— 按类型分流**

替换 `ExecuteShootLogic()` 方法：

```csharp
private void ExecuteShootLogic()
{
    if (currentWeaponData.weaponType == WeaponType.Melee)
    {
        ExecuteMelee();
    }
    else
    {
        ExecuteGunShoot();
    }
}

private void ExecuteGunShoot()
{
    if (currentAmmo > 0)
    {
        currentAmmo--;
        OnAmmoChanged?.Invoke();

        if (currentGunData.bulletPrefab != null && firePoint != null)
        {
            int pellets = currentGunData.bulletsPerShot;
            float spread = currentGunData.spreadAngle;

            for (int i = 0; i < pellets; i++)
            {
                Quaternion pelletRotation = firePoint.rotation;

                if (spread > 0f)
                {
                    float randomYaw   = UnityEngine.Random.Range(-spread, spread);
                    float randomPitch = UnityEngine.Random.Range(-spread, spread);
                    pelletRotation = firePoint.rotation * Quaternion.Euler(randomPitch, randomYaw, 0f);
                }

                GameObject bulletObj = Instantiate(currentGunData.bulletPrefab, firePoint.position, pelletRotation);
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(currentGunData.damage);
                }
            }
        }
    }
    else
    {
        OnEmptyClipFired?.Invoke();
    }
}

private void ExecuteMelee()
{
    if (currentMeleeData == null) return;
    if (currentMeleeData.comboChain == null || currentMeleeData.comboChain.Length == 0) return;
    if (meleeHitbox == null) return;

    // 连击窗口超时 → 重置
    if (Time.time - meleeComboTimer > currentMeleeData.comboCooldown)
        meleeComboStage = 0;

    // 防止越界
    if (meleeComboStage >= currentMeleeData.comboChain.Length)
        meleeComboStage = 0;

    ComboStage stage = currentMeleeData.comboChain[meleeComboStage];
    meleeHitbox.Activate(stage);

    meleeComboStage++;
    meleeComboTimer = Time.time;
    OnAmmoChanged?.Invoke(); // 触发 UI 刷新（显示连击段）
}
```

- [ ] **Step 7: 修改自动射击的 fireRate 判断**

在 `HandlePlayerInput` 中全自动判断部分，`nextFireTime` 和 `currentWeaponData.fireRate` 改为用 `currentGunData`：

找到：
```csharp
if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
{
    ExecuteShootLogic();
    nextFireTime = Time.time + currentWeaponData.fireRate;
}
```

改为：
```csharp
if (currentGunData != null && Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
{
    ExecuteShootLogic();
    nextFireTime = Time.time + currentGunData.fireRate;
}
```

- [ ] **Step 8: 修改半自动射击判断**

找到：
```csharp
if (Mouse.current.leftButton.wasPressedThisFrame)
{
    ExecuteShootLogic();
}
```

由于上面加了近战提前返回，这段对近战不会执行，不改也可以。但 `nextFireTime` 检查在 ExecuteGunShoot 里不做，所以 semi-auto 分支也加个保护：

```csharp
if (currentGunData != null && Mouse.current.leftButton.wasPressedThisFrame)
{
    ExecuteShootLogic();
}
```

- [ ] **Step 9: 修改 ReloadRoutine —— 安全化**

协程内 `currentWeaponData.isMagazineReload`、`currentWeaponData.reloadTime`、`currentWeaponData.perShellReloadTime` 改为 `currentGunData.xxx`。将所有 `currentWeaponData` 引用替换为 `currentGunData`，并在开头加 null 守卫：

```csharp
private IEnumerator ReloadRoutine()
{
    if (currentGunData == null) yield break;
    isReloading = true;
    interruptReload = false;

    if (currentGunData.isMagazineReload)
    {
        // ... 弹夹式逻辑不变，所有 currentWeaponData → currentGunData
    }
    else
    {
        // ... 逐发装填逻辑不变
    }
    // ... 结尾不变
}
```

需要将协程内的 `currentWeaponData.xxx` 全部替换为 `currentGunData.xxx`（共 4 处：isMagazineReload、reloadTime、perShellReloadTime 两次）。

- [ ] **Step 10: 修改 ExecuteInteraction —— 近战无弹药**

在 `ExecuteInteraction` 中，4 个分支里都有 `currentAmmo` / `reserveAmmo` 赋值。近战武器不需要这些：

分支 1（双手空拾取）的弹药赋值，改为仅对 Gun 类型生效：

```csharp
if (groundGun.weaponConfig.weaponType != WeaponType.Melee)
{
    slots[0].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[0].MaxMagazine;
    slots[0].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve : ((GunData)groundGun.weaponConfig).defaultReserveAmmo;
}
```

分支 2（同款补满）同样加近战跳过。分支 3（填空槽）、分支 4（替换）同样。

但更简单的方式：`WeaponSlot.LoadFromConfig` 已经处理了近战，所以这些弹药覆盖行只需要对 Gun 做：

实际上 `LoadFromConfig` 里已经分支了。外层这些 `currentAmmo = groundGun.runtimeAmmo...` 行是覆盖 `LoadFromConfig` 的值。对于近战，这些行没必要执行。统一加：

```csharp
if (groundGun.weaponConfig.weaponType != WeaponType.Melee)
{
    // 弹药覆盖逻辑
}
```

包裹 4 个分支中的弹药覆盖行即可。

- [ ] **Step 11: 修改 InitializeWeapon —— 安全化**

```csharp
private void InitializeWeapon()
{
    if (slots[0] != null && slots[0].weaponData != null)
    {
        slots[0].LoadFromConfig(slots[0].weaponData,
            !unlockedWeapons.Contains(slots[0].weaponData.weaponName));
        if (!unlockedWeapons.Contains(slots[0].weaponData.weaponName))
            unlockedWeapons.Add(slots[0].weaponData.weaponName);
    }
    OnAmmoChanged?.Invoke();
}
```

`LoadFromConfig` 已处理分支，这段不需要改动。

- [ ] **Step 12: 修改 ResetData —— 近战连击状态重置**

在 `ResetData()` 末尾加：

```csharp
meleeComboStage = 0;
meleeComboTimer = 0f;
```

- [ ] **Step 13: 修改 SwapToSlot —— 切换时重置连击**

在 `SwapToSlot` 方法的 `activeSlotIndex = index;` 后加：

```csharp
meleeComboStage = 0;
meleeComboTimer = 0f;
```

---

### Task 6: 更新 WeaponUIManager

**Files:**
- Modify: `Assets/Project/Scripts/UI/WeaponUIManager.cs`

- [ ] **Step 1: 修改 RefreshSlot —— 近战不显示弹药**

在 `RefreshSlot` 方法中，`if (slot == null || slot.IsEmpty)` 后，加近战判断：

```csharp
if (slot.IsMelee)
{
    // 近战：不显示弹药，只显示武器名 + 连击段数
    if (ammoText != null) { ammoText.text = ""; ammoText.color = Gray2; }
    if (nameText != null)
    {
        int combo = playerWeapon.activeSlotIndex == slotIndex && playerWeapon.currentWeaponData?.weaponType == WeaponType.Melee
            ? /* 怎么拿连击段？需要暴露 */ 0 : 0;
        nameText.color = isActive ? Color.white : Gray1;
        nameText.text = isActive
            ? $"<size=36>{weaponName}</size>"
            : $"<size=28>{weaponName}</size>";
    }
    ApplySlotShift(ammoText, nameText, slotIndex, isActive);
    return;
}
```

但这里有个问题：`RefreshSlot` 拿不到 `meleeComboStage`。需要在 `PlayerShooting` 暴露：

```csharp
public int MeleeComboStage => meleeComboStage;
```

然后 WeaponUIManager 中可以读到：

```csharp
int combo = (isActive && slot.IsMelee) ? playerWeapon.MeleeComboStage : 0;
```

UI 暂时简洁处理：近战槽展示武器名，弹药区留空即可。连击数后续加。

---

### Task 7: 更新 GunPickup

**Files:**
- Modify: `Assets/Project/Scripts/Weapon/GunPickup.cs`

- [ ] **Step 1: 安全化 Initialize 中的弹药默认值**

`GunPickup.Start()` 中：

```csharp
if (weaponConfig != null && runtimeAmmo == 0 && runtimeReserve == 0)
{
    if (weaponConfig.weaponType != WeaponType.Melee)
    {
        GunData gd = weaponConfig as GunData;
        if (gd != null)
        {
            runtimeAmmo = gd.maxMagazineSize;
            runtimeReserve = gd.defaultReserveAmmo;
        }
    }
}
```

- [ ] **Step 2: RefreshPickupState 近战颜色**

加一个 Melee 颜色分支：

```csharp
public void RefreshPickupState()
{
    Renderer renderer = GetComponentInChildren<Renderer>();
    if (renderer != null && weaponConfig != null)
    {
        renderer.material.color = weaponConfig.weaponType switch
        {
            WeaponType.Melee => Color.cyan,
            _ => weaponConfig.weaponName switch
            {
                "手枪" => Color.green,
                "霰弹枪" => Color.yellow,
                _ => Color.red
            }
        };
    }
}
```

---

### Task 8: Unity Editor 手动操作

- [ ] **Step 1: 迁移 3 个 .asset 文件**

选中以下文件，Inspector 顶部 Script 改为 `GunData`：
- `Assets/Project/Weapondata/Pistol_Config.asset`
- `Assets/Project/Weapondata/Rifle_Config.asset`
- `Assets/Project/Weapondata/Shotgun_Config.asset`

- [ ] **Step 2: 创建 MeleeHitbox 子物体**

在 Player GameObject 下：
1. 创建空子物体，命名 `MeleeHitbox`
2. Position 设为 (0, 1, 0.5)（角色前方偏上）
3. 挂 `MeleeHitbox` 组件
4. 拖到 PlayerShooting 的 `Melee Hitbox` 字段

- [ ] **Step 3: 创建刀配置资产**

`Assets/Project/ScriptableObjects/` → Create → DayByDay → MeleeData，命名 `Knife_Config`，默认 3 段连击已有。

- [ ] **Step 4: 测试**

1. 编译通过
2. 场景中放一个 `GunPickup`，weaponConfig 设为 `Knife_Config`
3. 运行游戏，按 E 捡刀
4. 按 1/2 切到刀槽
5. 靠近僵尸，按左键 → 扇形检测命中 + 伤害 + 击退
6. 连按 3 次 → 伤害递增（15 → 20 → 35）
7. 等 1 秒不按 → 重置到第 1 段

---

### Task 9: Commit

- [ ] **Step 1: 提交所有改动**

```bash
git add Assets/Project/Scripts/Weapon/WeaponData.cs \
        Assets/Project/Scripts/Weapon/GunData.cs \
        Assets/Project/Scripts/Weapon/MeleeData.cs \
        Assets/Project/Scripts/Weapon/MeleeHitbox.cs \
        Assets/Project/Scripts/Weapon/WeaponSlot.cs \
        Assets/Project/Scripts/Player/PlayerShooting.cs \
        Assets/Project/Scripts/UI/WeaponUIManager.cs \
        Assets/Project/Scripts/Weapon/GunPickup.cs \
        Assets/Project/Weapondata/ \
        docs/superpowers/specs/2026-06-06-melee-weapon-design.md \
        docs/superpowers/plans/2026-06-06-melee-weapon-implementation.md
git commit -m "feat: 近战武器系统 — WeaponData 继承重构 + MeleeData 连击链 + 扇形 Hitbox"
```
