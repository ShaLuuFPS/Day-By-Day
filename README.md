# Day By Day

> 第三人称丧尸生存射击游戏 · Unity 6 + URP

![Unity](https://img.shields.io/badge/Unity-6-000000?logo=unity)
![Release](https://img.shields.io/badge/release-v0.1.3-blue)

---

## 🚀 快速开始

1. 前往 [Releases](https://github.com/ShaLuuFPS/Day-By-Day/releases) 下载最新版本
2. 解压 zip 到任意目录
3. 双击 `DayByDay.exe` 开始游戏

> 无需安装 Unity 或其他依赖。

---

## 🎮 核心玩法

![main2](README.assets/main2.gif)

双武器切换  + 波次防守 + 肉鸽升级。

### 操作

| 按键 | 功能 |
|------|------|
| WASD | 移动（相对摄像机方向） |
| 鼠标移动 | 旋转视角 |
| 左键 | 射击 / 近战（按住预览范围，松开攻击） |
| R | 换弹 |
| 1 / 2 | 切换武器槽 |
| Shift | Dash 冲刺 |
| E | 拾取地面武器 |
| ESC | 暂停 |

---

## 🔫 武器 & 战斗

- **双武器槽**：1 / 2 键即时切换
- **瞄准射击**：屏幕准星 → 摄像机射线投射
- **武器拾取**：走近按 E 拾取，同款自动补弹
- **换弹**：R 键手动换弹，打空自动换弹
- **近战**：按住左键预览攻击范围，松开执行

### Dash 冲刺

![dash2](README.assets/dash2.gif)

Shift 位移，消耗体力，体力自动回复。

---

## 🧟 敌人

![敌人2](README.assets/敌人2.gif)

| 类型 | 特性 |
|------|------|
| 普通僵尸 | 靠近攻击 |
| 爆炸僵尸 (Boomer) | 突进 + 自爆，预警圈不减速 |
| Boss | 高血量 |

---

## ⬆ 升级系统

![升级2](README.assets/升级2.gif)

击杀敌人获取经验 → 升级 → 3 选 1 面板暂停游戏

| 升级 | 效果 |
|------|------|
| 穿透弹 | 子弹穿透 1 个敌人 |
| 分裂弹 | 命中后分裂 3 发（20% 伤害） |
| 尸体爆炸 | 击杀后 3m 范围 20 伤害 |
| 减速弹 | 命中减速 50%，持续 0.5s |
| 电磁弹 | 10% 触发连锁，30% 伤害，最多 5 连 |
| 击杀回耐 | 击杀恢复 20% 体力 |
| 吸血弹 | 伤害 3% 回血 |

---

## 🖥 UI

ESC 呼出暂停面板：继续游戏 / 重新开始 / 灵敏度调节 / 结束游戏（含确认弹窗）。死亡与通关结算面板同理。

---

## 🛠 技术方案

- **引擎**：Unity 6 + URP 管线
- **动画**：Animator + BlendTree 8 方向移动混合
- **输入**：Unity New Input System（键盘 + 鼠标）
- **架构**：ScriptableObject 配置、IResettable 统一重置契约、静态事件解耦
- **UI**：Canvas Scaler 锚点自适应 + TextMeshPro

---

## 📦 下载

最新版本见 [Releases](https://github.com/ShaLuuFPS/Day-By-Day/releases)
