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
