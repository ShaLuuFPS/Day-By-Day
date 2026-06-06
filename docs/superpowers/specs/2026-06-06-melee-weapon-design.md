# 近战武器系统（刀）设计文档

## 概述

Phase 2 Step 1。引入近战武器类型，刀作为和枪同等的武器占一个槽位。需要重构 `WeaponData` 继承体系以支持多种武器类型。

## 武器继承体系

```
WeaponData (基类 ScriptableObject)
├── weaponName: string
├── icon: Sprite (预留)
├── weaponType: enum { Gun, Melee }
│
├── GunData : WeaponData
│   ├── maxMagazineSize: int
│   ├── reloadTime: float
│   ├── fireRate: float
│   ├── isAutomatic: bool
│   ├── damage: float
│   ├── defaultReserveAmmo: int
│   ├── isMagazineReload: bool
│   ├── perShellReloadTime: float
│   ├── bulletsPerShot: int
│   ├── spreadAngle: float
│   └── bulletPrefab: GameObject
│
└── MeleeData : WeaponData
    └── comboChain: ComboStage[]
        ├── damage: float          // 本段伤害
        ├── range: float           // 攻击距离
        ├── fanAngle: float        // 扇形角度 (0~360)
        ├── knockback: float       // 击退力度
        ├── hitboxActiveTime: float // 检测窗口时长 (秒)
        └── comboCooldown: float   // 连击窗口 (秒，超时重置)
```

## 近战攻击流

1. 左键按下 → `PlayerShooting.ExecuteShootLogic()` 按 `weaponType` 分流
2. Gun → 现有射击逻辑（不变）
3. Melee → `TryMelee()`:
   - 激活子物体 Hitbox（扇形 Trigger Collider）
   - 持续 `hitboxActiveTime` 秒后自动关闭
   - 命中敌人 → 造成 `damage` 伤害 + `knockback` 击退
   - 连击计数 +1
   - 若在 `comboCooldown` 内未再按 → 重置到第 1 段

## MeleeHitbox 组件

- 挂在武器 Prefab 或 Player 子物体上
- `OnTriggerEnter` 检测 EnemyHealth → 造成伤害
- 每段激活时间独立配置

## 改动文件

| 文件 | 操作 |
|------|------|
| `WeaponData.cs` | 重构为基类，提取 `weaponType` 枚举 |
| `GunData.cs` | 新建，继承 WeaponData |
| `MeleeData.cs` | 新建，继承 WeaponData + ComboStage |
| `PlayerShooting.cs` | `ExecuteShootLogic` 按类型分流，加近战逻辑 |
| `WeaponSlot.cs` | 基类引用不变（子类兼容） |
| `WeaponUIManager.cs` | 近战槽不显示弹药 |
| `PlayerMovement.cs` | 可能影响（根据引用用 WeaponData 还是 GunData） |

## 资产迁移

现有 3 个 `.asset` 需在 Unity Editor 中手动改为 `GunData` 类型：
- `Pistol_Config.asset`
- `Rifle_Config.asset`
- `Shotgun_Config.asset`

## 未来扩展

- 角色分化：某角色初始带刀/只用枪（Phase 4+）
- 高级刀 4 段连击、带技能效果
- 刀模型 + 挥砍动画（资源到位后）
