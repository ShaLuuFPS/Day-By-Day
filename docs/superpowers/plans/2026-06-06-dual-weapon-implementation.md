# 双武器系统实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 重构 PlayerShooting 支持双武器槽位 + 1/2 键切枪 + 双槽 UI

**Architecture:** 新建 `WeaponSlot` 序列化数据类封装单武器状态；`PlayerShooting` 改为持有 `WeaponSlot[2]` 数组 + `activeSlotIndex`；属性全部委托到当前槽位；拾取逻辑从 3 分支改为 4 分支；UI 显示双行弹药

**Tech Stack:** Unity 6, URP, C#

---

## 文件结构

| 文件 | 操作 | 职责 |
|------|------|------|
| `Assets/Project/Scripts/Weapon/WeaponSlot.cs` | **新建** | 封装单武器运行时状态 |
| `Assets/Project/Scripts/Player/PlayerShooting.cs` | **重写** | 核心重构：双槽 + 切枪 + 4 分支拾取 |
| `Assets/Project/Scripts/Player/PlayerInteraction.cs` | **修改** | 同款自动拾取适配双槽遍历 |
| `Assets/Project/Scripts/UI/WeaponUIManager.cs` | **修改** | 双行弹药 + 当前槽位高亮 |
| `Assets/Project/Scripts/Weapon/AmmoSupplyBox.cs` | **修改** | 清理反射 hack，改用公开方法 |

---

### Task 1: 新建 WeaponSlot.cs 数据类

**Files:**
- Create: `Assets/Project/Scripts/Weapon/WeaponSlot.cs`

- [ ] **Step 1: 创建 WeaponSlot.cs**

```csharp
using UnityEngine;

/// <summary>
/// 封装单把武器的全部运行时状态。
/// 不含换弹状态——换弹锁定在 PlayerShooting 层，只有 activeSlot 能换弹。
/// </summary>
[System.Serializable]
public class WeaponSlot
{
    public WeaponData weaponData;
    public int currentAmmo;
    public int reserveAmmo;

    public bool IsEmpty => weaponData == null;
    public int MaxMagazine => weaponData != null ? weaponData.maxMagazineSize : 0;

    /// <summary>
    /// 清空槽位（丢弃武器时使用）
    /// </summary>
    public void Clear()
    {
        weaponData = null;
        currentAmmo = 0;
        reserveAmmo = 0;
    }

    /// <summary>
    /// 用 WeaponData 初始化此槽位
    /// </summary>
    public void LoadFromConfig(WeaponData config, bool giveInitialReserve)
    {
        weaponData = config;
        currentAmmo = config.maxMagazineSize;
        if (giveInitialReserve)
            reserveAmmo = config.defaultReserveAmmo;
    }
}
```

- [ ] **Step 2: 验证编译无报错**

暂时不测试（纯数据类，无行为），直接检查 Unity Console 无红字。

---

### Task 2: 重构 PlayerShooting.cs（核心）

**Files:**
- Modify: `Assets/Project/Scripts/Player/PlayerShooting.cs`

这是最大的一块。按字段 → 属性 → 初始化 → 输入 → 射击 → 换弹 → 交互 → 重置的顺序逐步替换。

- [ ] **Step 1: 替换字段声明区域（第 9-28 行）**

把旧的：

```csharp
[Header("核心武器配置")]
public WeaponData currentWeaponData;

[Header("枪械实时状态")]
public bool hasWeapon = false;
public int currentAmmo;
public int reserveAmmo;
public bool isReloading { get; private set; } = false;

[Header("射击物理枪口")]
public Transform firePoint;

private float nextFireTime = 0f;
private bool interruptReload = false;
private static HashSet<string> unlockedWeapons = new HashSet<string>();

// 事件广播
public static event Action OnAmmoChanged;
public static event Action<float> OnReloading;
public static event Action OnReloadComplete;
public static event Action OnEmptyClipFired;

// 属性简写
public string weaponName => currentWeaponData != null ? currentWeaponData.weaponName : "未知武器";
public int maxMagazineSize => currentWeaponData != null ? currentWeaponData.maxMagazineSize : 7;
public float reloadTime => currentWeaponData != null
    ? (currentWeaponData.isMagazineReload ? currentWeaponData.reloadTime : currentWeaponData.perShellReloadTime)
    : 1.5f;
```

替换为：

```csharp
[Header("双武器槽位")]
public WeaponSlot[] slots = new WeaponSlot[2];
public int activeSlotIndex = 0;

[Header("换弹状态（只有 activeSlot 能换弹）")]
public bool isReloading { get; private set; } = false;

[Header("射击物理枪口")]
public Transform firePoint;

private float nextFireTime = 0f;
private bool interruptReload = false;
private static HashSet<string> unlockedWeapons = new HashSet<string>();

// 事件广播
public static event Action OnAmmoChanged;
public static event Action<float> OnReloading;
public static event Action OnReloadComplete;
public static event Action OnEmptyClipFired;

// ─── 属性简写：全部委托到 activeSlot ───
private WeaponSlot activeSlot => (slots != null && slots.Length > activeSlotIndex) ? slots[activeSlotIndex] : null;
public WeaponData currentWeaponData => activeSlot?.weaponData;
public bool hasWeapon
{
    get
    {
        if (slots == null) return false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && !slots[i].IsEmpty) return true;
        }
        return false;
    }
}
public int currentAmmo
{
    get => activeSlot != null ? activeSlot.currentAmmo : 0;
    set { if (activeSlot != null) activeSlot.currentAmmo = value; }
}
public int reserveAmmo
{
    get => activeSlot != null ? activeSlot.reserveAmmo : 0;
    set { if (activeSlot != null) activeSlot.reserveAmmo = value; }
}
public string weaponName => currentWeaponData != null ? currentWeaponData.weaponName : "未知武器";
public int maxMagazineSize => currentWeaponData != null ? currentWeaponData.maxMagazineSize : 7;
public float reloadTime => currentWeaponData != null
    ? (currentWeaponData.isMagazineReload ? currentWeaponData.reloadTime : currentWeaponData.perShellReloadTime)
    : 1.5f;

/// <summary>
/// 查找武器名称匹配的槽位索引，没找到返回 -1
/// </summary>
public int FindSlotByWeaponName(string name)
{
    if (slots == null || string.IsNullOrEmpty(name)) return -1;
    for (int i = 0; i < slots.Length; i++)
    {
        if (slots[i] != null && slots[i].weaponData != null && slots[i].weaponData.weaponName == name)
            return i;
    }
    return -1;
}

/// <summary>
/// 找到第一个空槽的索引，没有空槽返回 -1
/// </summary>
public int FindEmptySlot()
{
    if (slots == null) return -1;
    for (int i = 0; i < slots.Length; i++)
    {
        if (slots[i] == null || slots[i].IsEmpty) return i;
    }
    return -1;
}

/// <summary>
/// 公开方法：外部可直接触发弹药 UI 刷新（替代 AmmoSupplyBox 的反射 hack）
/// </summary>
public static void InvokeAmmoChanged() => OnAmmoChanged?.Invoke();
```

- [ ] **Step 2: 修改 Awake/Start 初始化**

```csharp
void Awake()
{
    // 确保两个槽位都被初始化（数组声明只分配了 null 引用）
    for (int i = 0; i < slots.Length; i++)
    {
        if (slots[i] == null) slots[i] = new WeaponSlot();
    }
}

void Start()
{
    InitializeWeapon();
}

private void InitializeWeapon()
{
    // slots[0] 可能已经有武器（Inspector 中预设），按 WeaponData 初始化
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

- [ ] **Step 3: 添加 SwapToSlot 方法 + 更新 HandlePlayerInput**

在 `HandlePlayerInput` **之前**（约第 60 行位置）插入：

```csharp
/// <summary>
/// 切换到指定槽位的武器。空槽不能切、已在当前槽不操作。
/// 切枪会打断正在进行的换弹。
/// </summary>
private void SwapToSlot(int index)
{
    if (index == activeSlotIndex) return;
    if (slots == null || index < 0 || index >= slots.Length) return;
    if (slots[index] == null || slots[index].IsEmpty) return;

    // 打断换弹
    if (isReloading)
    {
        StopAllCoroutines();
        isReloading = false;
        OnReloadComplete?.Invoke();
    }

    activeSlotIndex = index;
    OnAmmoChanged?.Invoke();
}
```

更新 `HandlePlayerInput`（替换整个方法）：

```csharp
private void HandlePlayerInput()
{
    // ── 切枪输入（同一帧内阻止射击/换弹）──
    if (Keyboard.current.digit1Key.wasPressedThisFrame)
    {
        SwapToSlot(0);
        return;
    }
    if (Keyboard.current.digit2Key.wasPressedThisFrame)
    {
        SwapToSlot(1);
        return;
    }

    if (!hasWeapon || currentWeaponData == null) return;

    // 逐发装填中：允许射击键打断（装完当前发就停），其余输入屏蔽
    if (isReloading)
    {
        if (!currentWeaponData.isMagazineReload && Mouse.current.leftButton.wasPressedThisFrame)
        {
            interruptReload = true;
        }
        return;
    }

    // 处理射击输入
    if (currentWeaponData.isAutomatic)
    {
        if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            ExecuteShootLogic();
            nextFireTime = Time.time + currentWeaponData.fireRate;
        }
    }
    else
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ExecuteShootLogic();
        }
    }

    // 处理换弹输入
    if (Keyboard.current.rKey.wasPressedThisFrame
        && activeSlot.currentAmmo < maxMagazineSize
        && activeSlot.reserveAmmo > 0)
    {
        StartCoroutine(ReloadRoutine());
    }
}
```

- [ ] **Step 4: 更新 ExecuteShootLogic（最小改动：currentAmmo 属性已有 setter）**

`ExecuteShootLogic` 中 `currentAmmo--` 通过属性 setter 自动写到 `activeSlot.currentAmmo`，无需大改。只需确认 `currentWeaponData.bulletPrefab` 和 `currentWeaponData.damage` 走的是属性。

当前代码已经用 `currentWeaponData.xxx` 访问 → 属性委托后直接生效。不变。

- [ ] **Step 5: 更新 ReloadRoutine（操作对象改为 activeSlot）**

`ReloadRoutine` 中所有 `currentAmmo` / `reserveAmmo` 直接赋值 → 通过属性 setter 自动走 `activeSlot`，逻辑无需改动。唯一需要确认的是 `interruptReload` 重置和 `isReloading` 设置依然在方法体里。当前代码已经操作 `this.reserveAmmo` / `this.currentAmmo` → 委托后自动正确。不变。

- [ ] **Step 6: 重写 ExecuteInteraction — 4 分支逻辑**

完全替换 `ExecuteInteraction` 方法（第 200-272 行）：

```csharp
/// <summary>
/// 核心交互：接收地面武器的交互指令（支持靠近自动触发 + 打断换弹）
/// 4 分支：双手空 / 同款补满 / 有空槽 / 双手满替换
/// </summary>
public void ExecuteInteraction(GunPickup groundGun, GameObject groundGameObject)
{
    // ── 分支 1：双手都空 → 新枪放入槽 0，切到槽 0 ──
    if (!hasWeapon)
    {
        slots[0].LoadFromConfig(groundGun.weaponConfig, true);
        if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
            unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
        // 如果有地面记录的弹药则使用地面数据
        slots[0].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[0].MaxMagazine;
        slots[0].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve : groundGun.weaponConfig.defaultReserveAmmo;
        activeSlotIndex = 0;
        OnAmmoChanged?.Invoke();
        Destroy(groundGameObject);
        return;
    }

    // ── 分支 2：有同款武器 → 自动补满弹药，不占新槽 ──
    int matchingSlot = FindSlotByWeaponName(groundGun.weaponConfig.weaponName);
    if (matchingSlot >= 0)
    {
        // 如果同款武器正在换弹且恰是 activeSlot → 打断
        if (isReloading && matchingSlot == activeSlotIndex)
        {
            StopAllCoroutines();
            isReloading = false;
            OnReloadComplete?.Invoke();
        }

        WeaponData cfg = groundGun.weaponConfig;
        slots[matchingSlot].currentAmmo = cfg.maxMagazineSize;
        slots[matchingSlot].reserveAmmo = cfg.defaultReserveAmmo;

        // 切到同款武器槽
        if (activeSlotIndex != matchingSlot)
        {
            activeSlotIndex = matchingSlot;
        }

        OnAmmoChanged?.Invoke();
        Destroy(groundGameObject);
        return;
    }

    // ── 分支 3：有空槽 → 新枪放入空槽，切到该槽 ──
    int emptySlot = FindEmptySlot();
    if (emptySlot >= 0)
    {
        slots[emptySlot].LoadFromConfig(groundGun.weaponConfig, true);
        if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
            unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
        slots[emptySlot].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[emptySlot].MaxMagazine;
        slots[emptySlot].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve : groundGun.weaponConfig.defaultReserveAmmo;
        activeSlotIndex = emptySlot;
        OnAmmoChanged?.Invoke();
        Destroy(groundGameObject);
        return;
    }

    // ── 分支 4：双手都满 → 替换当前 activeSlot，旧枪丢地上 ──
    if (isReloading) return;

    // 备份旧枪数据
    WeaponData playerOldConfig = slots[activeSlotIndex].weaponData;
    int playerOldAmmo = slots[activeSlotIndex].currentAmmo;
    int playerOldReserve = slots[activeSlotIndex].reserveAmmo;

    // 灌入新枪
    slots[activeSlotIndex].LoadFromConfig(groundGun.weaponConfig, true);
    if (!unlockedWeapons.Contains(groundGun.weaponConfig.weaponName))
        unlockedWeapons.Add(groundGun.weaponConfig.weaponName);
    slots[activeSlotIndex].currentAmmo = groundGun.runtimeAmmo > 0 ? groundGun.runtimeAmmo : slots[activeSlotIndex].MaxMagazine;
    slots[activeSlotIndex].reserveAmmo = groundGun.runtimeReserve > 0 ? groundGun.runtimeReserve : groundGun.weaponConfig.defaultReserveAmmo;

    // 旧枪数据写到地面物体
    groundGun.Initialize(playerOldConfig, playerOldAmmo, playerOldReserve);
    groundGameObject.transform.position = transform.position + transform.forward * 1.8f + Vector3.up * 0.2f;
    groundGun.RefreshPickupState();

    // 重置地面物体的限时销毁
    TimedDestroyer oldDestroyer = groundGameObject.GetComponent<TimedDestroyer>();
    if (oldDestroyer != null) Destroy(oldDestroyer);
    groundGameObject.AddComponent<TimedDestroyer>();

    // 喂回雷达
    PlayerInteraction radar = GetComponent<PlayerInteraction>();
    if (radar != null)
    {
        IInteractable newGroundInteractable = groundGameObject.GetComponent<IInteractable>();
        radar.RegisterInteractable(newGroundInteractable);
    }

    OnAmmoChanged?.Invoke();
}
```

- [ ] **Step 7: 更新 ResetData**

```csharp
/// <summary>
/// 游戏重置契约：复活或重新开局时触发全局重置
/// </summary>
public void ResetData()
{
    OnReloadComplete?.Invoke();
    StopAllCoroutines();
    isReloading = false;

    // 1. 让雷达脚本清空当前的缓存数据
    PlayerInteraction radar = GetComponent<PlayerInteraction>();
    if (radar != null) radar.ClearRadar();

    // 2. 将场景中所有现存的地面枪械全部初始化回默认满状态
    GunPickup[] allSceneGuns = FindObjectsByType<GunPickup>();
    foreach (GunPickup gun in allSceneGuns)
    {
        if (gun != null && gun.weaponConfig != null)
        {
            gun.Initialize(gun.weaponConfig, gun.weaponConfig.maxMagazineSize, gun.weaponConfig.defaultReserveAmmo);
            gun.RefreshPickupState();
        }
    }

    // 3. 遍历两个槽位，重置弹药到满状态
    for (int i = 0; i < slots.Length; i++)
    {
        if (slots[i] != null && slots[i].weaponData != null)
        {
            slots[i].currentAmmo = slots[i].MaxMagazine;
            slots[i].reserveAmmo = slots[i].weaponData.defaultReserveAmmo;
        }
    }

    OnAmmoChanged?.Invoke();
}
```

- [ ] **Step 8: 验证编译**

在 Unity 中打开项目，确认 PlayerShooting.cs 无编译错误。

---

### Task 3: 修改 PlayerInteraction.cs（同款自动拾取适配双槽）

**Files:**
- Modify: `Assets/Project/Scripts/Player/PlayerInteraction.cs:105-118`

- [ ] **Step 1: 替换 OnTriggerEnter 中的同款判断**

把第 111-118 行：

```csharp
if (interactable is GunPickup groundGun && playerShooting != null && playerShooting.hasWeapon)
{
    if (groundGun.weaponConfig != null && groundGun.weaponConfig.weaponName == playerShooting.weaponName)
    {
        interactable.Interact(playerShooting);
        return;
    }
}
```

替换为：

```csharp
if (interactable is GunPickup groundGun && playerShooting != null && playerShooting.hasWeapon)
{
    if (groundGun.weaponConfig != null
        && playerShooting.FindSlotByWeaponName(groundGun.weaponConfig.weaponName) >= 0)
    {
        // 同款武器自动吸走补弹
        interactable.Interact(playerShooting);
        return;
    }
}
```

- [ ] **Step 2: 验证编译**

确认 Unity Console 无错误。

---

### Task 4: 修改 WeaponUIManager.cs（双槽 UI + 高亮）

**Files:**
- Modify: `Assets/Project/Scripts/UI/WeaponUIManager.cs`

- [ ] **Step 1: 添加 UI 引用字段**

在类的字段区域新增：

```csharp
[Header("🎮 双武器 UI — 每个槽位独立一行")]
public TextMeshProUGUI slot0Text;   // 槽 0 的 "武器名: 弹夹/备弹"
public TextMeshProUGUI slot1Text;   // 槽 1 的 "武器名: 弹夹/备弹"
```

- [ ] **Step 2: 替换 RefreshAmmoUI**

把第 62 行的一行式 `RefreshAmmoUI` 替换为：

```csharp
void RefreshAmmoUI()
{
    if (playerWeapon == null) return;

    // ── 武器名 ──
    if (weaponNameText != null)
    {
        weaponNameText.text = playerWeapon.hasWeapon ? playerWeapon.weaponName : "空手";
    }

    // ── 双槽弹药行 ──
    RefreshSlotText(slot0Text, 0);
    RefreshSlotText(slot1Text, 1);

    // ── 清除空弹警告（有弹药时） ──
    if (playerWeapon.hasWeapon && playerWeapon.currentAmmo > 0 && !playerWeapon.isReloading)
    {
        if (reloadStatusText != null) reloadStatusText.text = "";
    }
}

void RefreshSlotText(TextMeshProUGUI text, int slotIndex)
{
    if (text == null) return;

    WeaponSlot slot = (playerWeapon.slots != null && slotIndex < playerWeapon.slots.Length)
        ? playerWeapon.slots[slotIndex] : null;

    if (slot == null || slot.IsEmpty)
    {
        text.text = $"槽{slotIndex + 1}: 空";
        // 非手持槽位用灰色
        text.color = Color.gray;
    }
    else
    {
        string weaponName = slot.weaponData != null ? slot.weaponData.weaponName : "?";
        bool isActive = (slotIndex == playerWeapon.activeSlotIndex);
        // 手持槽位亮白，非手持灰色
        text.color = isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        text.text = $"{(isActive ? "▶ " : "  ")}{weaponName}: {slot.currentAmmo} / {slot.reserveAmmo}";
    }
}
```

- [ ] **Step 3: 更新 HandleReloadingUI 和 ShowEmptyWarning**

保持原有逻辑不变——它们已经从 `playerWeapon.reloadTime` / `playerWeapon.currentAmmo` 取数据，属性委托后自动正确。

- [ ] **Step 4: 在场景中连线 UI**

在 Unity Editor 中打开 `SampleScene`，把 Canvas 下新增的两个 `TextMeshProUGUI`（或已有文本）拖到 `WeaponUIManager` 的 `slot0Text` / `slot1Text` 字段上。

- [ ] **Step 5: 验证编译**

确认 Unity Console 无错误。

---

### Task 5: 清理 AmmoSupplyBox.cs 反射 hack

**Files:**
- Modify: `Assets/Project/Scripts/Weapon/AmmoSupplyBox.cs`

- [ ] **Step 1: 删除反射代码，改为调用公开静态方法**

把第 35-44 行：

```csharp
// 5. 🌟 绝妙细节：...
System.Reflection.MethodInfo ammoChangedMethod = typeof(PlayerShooting).GetMethod("InitializeWeapon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

var eventField = typeof(PlayerShooting).GetField("OnAmmoChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
if (eventField != null)
{
    var ammoEvent = eventField.GetValue(null) as Action;
    ammoEvent?.Invoke();
}
```

替换为：

```csharp
// 5. 通过 PlayerShooting 公开的静态方法刷新 UI（替代原先的反射 hack）
PlayerShooting.InvokeAmmoChanged();
```

- [ ] **Step 2: 同时去掉不再需要的 using**

删除文件顶部的 `using System;`（如果仅为此反射使用的话）。检查：`OnTriggerEnter` 中不再需要 `Action` 类型，可删 `using System;`。

- [ ] **Step 3: 验证编译**

确认 Unity Console 无错误。

---

### Task 6: 手动验证

- [ ] **Step 1: 运行场景，验证空手状态**

启动 `SampleScene`，Console 无报错。UI 显示两行 "槽1: 空" / "槽2: 空"。

- [ ] **Step 2: 拾取第一把枪**

走到手枪旁按 E → 槽 0 显示手枪 + 弹药，▶ 标记在槽 0。

- [ ] **Step 3: 拾取第二把枪（不同款）**

走到步枪旁按 E → 槽 1 显示步枪，▶ 切到槽 1。按 1/2 切换回手枪/步枪，弹药各自独立。

- [ ] **Step 4: 同款自动补满**

打完手枪几发子弹，走到另一把手枪旁 → 自动补满弹药，不占新槽。

- [ ] **Step 5: 双手满时替换**

双手都有枪时走到霰弹枪旁按 E → 替换当前手持那把枪，旧枪掉在地上可捡回。

- [ ] **Step 6: 切枪打断换弹**

手持霰弹枪按 R 换弹，换弹中途按 1 切到手枪 → 换弹进度条消失。切回霰弹枪 → 子弹数没变（换弹未完成）。

- [ ] **Step 7: 游戏重置**

按下 Restart → 所有武器回到满弹药状态。

- [ ] **Step 8: 提交**

```bash
git add -A
git commit -m "feat: 双武器系统——双槽位 + 1/2切枪 + 4分支拾取 + UI适配

- 新建 WeaponSlot.cs 封装单武器运行时状态
- 重构 PlayerShooting.cs：双槽数组 + activeSlot 索引 + 委托属性
- ExecuteInteraction 改为 4 分支逻辑
- PlayerInteraction 同款自动拾取适配双槽遍历
- WeaponUIManager 双行弹药显示 + 手持槽位高亮
- AmmoSupplyBox 清理反射 hack，改用 InvokeAmmoChanged()"
```

---

## 依赖关系

```
Task 1 (WeaponSlot) ──→ Task 2 (PlayerShooting)
                              ├──→ Task 3 (PlayerInteraction)
                              ├──→ Task 4 (WeaponUIManager)
                              └──→ Task 5 (AmmoSupplyBox)
                                        └──→ Task 6 (验证)
```

Task 2 依赖 Task 1。Task 3/4/5 都依赖 Task 2 完成，但它们之间独立可并行。Task 6 需要所有前序任务完成。
