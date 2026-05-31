using UnityEngine;
using System;
public class AmmoSupplyBox : MonoBehaviour
{
    [Header("补给箱配置")]
    [Tooltip("捡到这个箱子能给玩家补充多少发备弹")]
    public int supplyAmmoAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 检查踩过来的是不是主角
        PlayerShooting player = other.GetComponent<PlayerShooting>();

        // 2. 如果是主角，并且他身上现在拥有武器
        if (player != null && player.hasWeapon)
        {
            // 检查是不是已经达到该枪械的备弹上限了
            if (player.currentWeaponData != null && player.reserveAmmo >= player.currentWeaponData.defaultReserveAmmo)
            {
                // 如果备弹本来就是满的，玩家不需要这个箱子，保持留在地上不摧毁
                return;
            }

            // 3. 执行补充：直接加到背包备弹里
            player.reserveAmmo += supplyAmmoAmount;

            // 4. 防御性限高：最高只能补充到当前武器的预设备弹量上限
            if (player.currentWeaponData != null && player.reserveAmmo > player.currentWeaponData.defaultReserveAmmo)
            {
                player.reserveAmmo = player.currentWeaponData.defaultReserveAmmo;
            }

            // 5. 🌟 绝妙细节：因为换弹中完全可以拾取，此处直接通过反射或公开接口调用事件，刷新 UI 弹药数据显示
            // 我们利用反射去动态触发 PlayerShooting 内部的 OnAmmoChanged 静态通知
            System.Reflection.MethodInfo ammoChangedMethod = typeof(PlayerShooting).GetMethod("InitializeWeapon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 为了安全和干净，我们直接寻找 PlayerShooting 里的公开或隐式刷新动作，
            // 现有的 `PlayerShooting.cs` 是通过触发静态事件刷新的，我们可以直接在下面强行 Invoke 事件：
            var eventField = typeof(PlayerShooting).GetField("OnAmmoChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (eventField != null)
            {
                var ammoEvent = eventField.GetValue(null) as Action;
                ammoEvent?.Invoke();
            }

            Debug.Log($"玩家拾取了补给箱，备弹补充了 {supplyAmmoAmount} 发！");

            // 6. 功成身退，销毁场景中的补给箱实体
            Destroy(gameObject);
        }
    }
}