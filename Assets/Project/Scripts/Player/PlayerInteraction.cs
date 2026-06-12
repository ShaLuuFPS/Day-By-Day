using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour, IResettable
{
    [Header("交互防抖配置")]
    private static float globalPickupTimer = 0f;
    private const float PICKUP_COOLDOWN = 1.0f;

    [Header("缓存雷达队列")]
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

    // 缓存玩家身上的武器中枢组件，用于传参
    private WeaponManager weaponManager;
    private MeleeHitbox meleeHitbox;

    // 🌟 核心通用交互事件：改由雷达脚本来向 UI Manager 发送通知
    public static event Action<string> OnShowInteractionUI;
    public static event Action OnHideInteractionUI;

    void Awake()
    {
        weaponManager = GetComponent<WeaponManager>();
        meleeHitbox = GetComponentInChildren<MeleeHitbox>();
    }

    void Update()
    {
        if (GameStateManager.IsInputFrozen) return;
        HandleUniversalInteraction();
    }

    /// <summary>
    /// 处理通用交互与多物体冲突雷达
    /// </summary>
    private void HandleUniversalInteraction()
    {
        // 清理列表中可能已经被 Destroy 掉的无效残留（安全防护）
        nearbyInteractables.RemoveAll(x => x == null || (x is MonoBehaviour mb && mb == null));

        if (nearbyInteractables.Count == 0)
        {
            OnHideInteractionUI?.Invoke();
            return;
        }

        // 💡 冲突破局：通过计算物理距离，死死咬住【离玩家最近的那一个】！
        IInteractable closestInteractable = GetClosestInteractable();
        if (closestInteractable == null) return;

        // 通知 UI 实时刷新这个最近物体的提示文字
        OnShowInteractionUI?.Invoke(closestInteractable.InteractionPrompt);

        // 监听按 E 键
        if (Keyboard.current.eKey.wasPressedThisFrame && Time.time >= globalPickupTimer)
        {
            globalPickupTimer = Time.time + PICKUP_COOLDOWN;

            IInteractable target = closestInteractable;
            nearbyInteractables.Remove(target);

            // 砰！触发这个物体特有的 Interact 逻辑，把武器中枢传过去
            target.Interact(weaponManager);
        }
    }

    /// <summary>
    /// 算法：获取附近最近的交互物体
    /// </summary>
    private IInteractable GetClosestInteractable()
    {
        IInteractable closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (var interactable in nearbyInteractables)
        {
            if (interactable is MonoBehaviour mb)
            {
                float dist = Vector3.Distance(transform.position, mb.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = interactable;
                }
            }
        }
        return closest;
    }

    // 🌟 外来物体吐出时，提供一个公开接口，允许重新塞回雷达队列
    public void RegisterInteractable(IInteractable interactable)
    {
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    // 清空雷达，常用于死亡复活重置
    public void ClearRadar()
    {
        nearbyInteractables.Clear();
        OnHideInteractionUI?.Invoke();
    }

    // ── IResettable ──
    public void ResetData()
    {
        ClearRadar();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 🔒 防止近战 Hitbox 的大触发球误将武器加入雷达
        if (meleeHitbox != null && meleeHitbox.IsTriggerActive())
            return;

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            // 🌟 核心改动：如果是地面的枪，并且和玩家手里的枪同名，直接自动触发交互！
            if (interactable is GunPickup groundGun && weaponManager != null && weaponManager.hasWeapon)
            {
                if (groundGun.weaponConfig != null
                    && weaponManager.FindSlotByWeaponName(groundGun.weaponConfig.weaponName) >= 0)
                {
                    // 同款武器自动吸走补弹（遍历双槽查找匹配）
                    interactable.Interact(weaponManager);
                    return;
                }
            }

            // 如果不是同款枪（比如是不同的枪或未来的门），依然老老实实加入雷达队列，等玩家按 E
            if (!nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Remove(interactable);
        }
    }
}