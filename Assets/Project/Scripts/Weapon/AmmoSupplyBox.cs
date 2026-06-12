using UnityEngine;
public class AmmoSupplyBox : MonoBehaviour
{
    [Header("补给箱配置")]
    [Tooltip("捡到这个箱子能给玩家补充多少发备弹")]
    public int supplyAmmoAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 检查踩过来的是不是主角
        WeaponManager weaponManager = other.GetComponent<WeaponManager>();

        // 2. 如果是主角，并且他身上现在拥有武器（近战不补弹药）
        if (weaponManager != null && weaponManager.hasWeapon && !weaponManager.IsCurrentMelee)
        {
            GunData gunData = weaponManager.CurrentGunData;
            if (gunData == null) return;

            int maxReserve = gunData.defaultReserveAmmo;

            // 检查是不是已经达到该枪械的备弹上限了
            if (weaponManager.reserveAmmo >= maxReserve)
            {
                // 如果备弹本来就是满的，玩家不需要这个箱子，保持留在地上不摧毁
                return;
            }

            // 3. 执行补充：直接加到背包备弹里
            weaponManager.reserveAmmo += supplyAmmoAmount;

            // 4. 防御性限高：最高只能补充到当前武器的预设备弹量上限
            if (weaponManager.reserveAmmo > maxReserve)
            {
                weaponManager.reserveAmmo = maxReserve;
            }

            // 5. 通过 WeaponManager 的公开方法刷新 UI
            weaponManager.InvokeAmmoChanged();

            Debug.Log($"玩家拾取了补给箱，备弹补充了 {supplyAmmoAmount} 发！");

            // 6. 功成身退，销毁场景中的补给箱实体
            Destroy(gameObject);
        }
    }
}