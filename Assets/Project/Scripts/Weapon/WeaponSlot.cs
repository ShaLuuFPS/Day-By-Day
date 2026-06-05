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
