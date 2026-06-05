# 双武器系统设计文档

**日期：** 2026-06-06  
**状态：** 已确认  
**范围：** PlayerShooting 重构、武器槽位、切枪、拾取逻辑、UI 适配

---

## 目标

玩家同时持有两把武器，通过 1/2 键切换。拾取和弹药管理全部适配双槽位。

---

## 新建文件

### `WeaponSlot.cs` — 武器槽位数据类

```csharp
[System.Serializable]
public class WeaponSlot
{
    public WeaponData weaponData;
    public int currentAmmo;
    public int reserveAmmo;
}
```

- 可序列化（Editor 可见，但运行时由代码填充）
- 不含换弹状态（换弹锁定在 PlayerShooting 层）

---

## 修改：`PlayerShooting.cs`

### 字段变更

| 旧 | 新 |
|----|----|
| `WeaponData currentWeaponData` | `WeaponSlot[] slots = new WeaponSlot[2]` |
| `int currentAmmo` | → `slots[i].currentAmmo` |
| `int reserveAmmo` | → `slots[i].reserveAmmo` |
| `int activeSlotIndex` | **新增**，默认 0 |
| `bool hasWeapon` | → 检查任一 slot 非空 |
| `isReloading` | 保留，仍在 PlayerShooting 层 |

### 属性简写

全部委托到 `slots[activeSlotIndex]`：

- `weaponName` → `slots[activeSlotIndex].weaponData?.weaponName`
- `maxMagazineSize` → 同理
- `currentAmmo` → `slots[activeSlotIndex].currentAmmo`
- `reserveAmmo` → `slots[activeSlotIndex].reserveAmmo`

### 新增方法

**`SwapToSlot(int index)`：**
1. 若 index == activeSlotIndex，return
2. 若 `slots[index].weaponData == null`，return（空槽不能切）
3. 若 `isReloading`：`StopAllCoroutines()` + `isReloading = false` + 触发 `OnReloadComplete`
4. `activeSlotIndex = index`
5. 触发 `OnAmmoChanged`（UI 刷新）

### 修改方法

**`HandlePlayerInput`：**
- 新增数字键 1/2 检测：`Keyboard.current.digit1Key.wasPressedThisFrame` → `SwapToSlot(0)`，2 同理
- 切枪时阻止射击/换弹输入（同一帧内）

**`ExecuteInteraction(GunPickup groundGun, GameObject groundGameObject)`：** 改为 4 分支：

1. **双手都空**（slots[0] 和 slots[1] 都无武器）→ 新枪放入 slot 0，activeSlot = 0，标记已解锁
2. **有同款武器**（遍历两个槽找 weaponName 匹配）→ 将该槽弹药补满到最大，打断该槽换弹（若在换弹中），activeSlot 切到该槽。销毁地上物体
3. **只有一只手有枪**（另一槽为空）→ 新枪放入空槽，activeSlot 切到新槽。销毁地上物体
4. **双手都满** → 替换 activeSlot 那把：备份 → 灌入新枪 → 旧枪丢地上（复用现有交换逻辑）

**`InitializeWeapon`：** 改为初始化 `slots[0]`（而非当前仅有的 `currentWeaponData`）。

**`ReloadRoutine`：** 操作对象改为 `slots[activeSlotIndex]`。

**`ExecuteShootLogic`：** 从 `slots[activeSlotIndex]` 取数据和弹药。

**`ResetData`：** 遍历 slots 数组重置每个槽的弹药。

---

## 修改：`PlayerInteraction.cs`

**`OnTriggerEnter` 同款自动拾取判断：**
- 当前检测 `groundGun.weaponConfig.weaponName == playerShooting.weaponName`
- 改为遍历 `playerShooting.slots` 两个槽，只要有任一槽的武器名匹配且 playerShooting.hasWeapon，就触发自动拾取

注：`PlayerInteraction` 需要能访问 slots 数组。给 `PlayerShooting` 加一个 `public WeaponSlot[] slots` 字段即可。

---

## 修改：`WeaponUIManager.cs`

- 新增两行弹药显示的区域（两个 TextMeshProUGUI 或一个分两行）
- 当前手持槽位高亮（如颜色或 `[手持]` 前缀）
- 订阅切枪事件刷新高亮（复用 `OnAmmoChanged` 即可，在 `RefreshAmmoUI` 里同时更新高亮）
- 空槽显示 "空"

---

## 不变文件

- `WeaponData.cs` — 无需修改
- `GunPickup.cs` — Interact 接口不变
- `Bullet.cs` — 无影响
- `AmmoSupplyBox.cs` — 需小改：补弹时补到 activeSlot 的 reserveAmmo（目前也是补到 playerShooting.reserveAmmo，属性委托后自然正确）

---

## 删除

- `static HashSet<string> unlockedWeapons` — 当前用于追踪首次拾取，双武器下改为每个 slot 独立判断。保留但改为标记「是否已领取过初始备弹」，逻辑不变。

---

## 事件一览

| 事件 | 触发时机 |
|------|---------|
| `OnAmmoChanged` | 射击、换弹完成、切枪、拾取后 |
| `OnReloading(float)` | 换弹进行中 |
| `OnReloadComplete` | 换弹结束、换弹被打断、切枪打断换弹 |
| `OnEmptyClipFired` | 空弹夹扣扳机 |

无需新增事件，切枪后 UI 刷新完全复用 `OnAmmoChanged`。

---

## 验收标准

1. 按 1 切到槽 0，按 2 切到槽 1，空槽无法切入
2. 两把武器弹药独立跟踪，切枪后 UI 显示正确的弹药数
3. 双手空时走近枪按 E → 直接拿起
4. 只有一把枪时走近不同枪按 E → 放入空槽
5. 双手满时走近枪按 E → 替换当前手持那把，旧枪掉地上
6. 走近同款武器 → 自动补满弹药，不占新槽
7. 切枪打断换弹，切回来弹夹不变（换弹未完成）
8. 游戏重置后两把武器都回到满弹药
