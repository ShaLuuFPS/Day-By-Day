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
    /// 用 WeaponData 初始化此槽位。近战武器忽略弹药。
    /// </summary>
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
