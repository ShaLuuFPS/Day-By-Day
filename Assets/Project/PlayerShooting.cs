using UnityEngine;
using UnityEngine.InputSystem;
using System; // 🔴 必须引入 System 命名空间来使用 Action
using System.Collections;

public class PlayerShooting : MonoBehaviour, IResettable
{
    [Header("枪械基础设置")]
    public string weaponName = "手枪";
    public int maxMagazineSize = 7;
    public int reserveAmmo = 28;
    public float reloadTime = 1.5f;

    [Header("枪械实时状态")]
    public bool hasWeapon = false;
    public int currentAmmo = 7;
    public bool isReloading { get; private set; } = false; // 让外部UI能读取这个状态  

    [Header("射击预制体")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    // 🌟🌟🌟 工业核心：定义 3 个事件（通知广播）
    // 这样别的脚本就可以订阅这些事件，当逻辑发生变化时，自动通知 UI 刷新
    public static event Action OnAmmoChanged; // 弹药改变时广播
    public static event Action<float> OnReloading; // 换弹中广播，传递已过去的时间
    public static event Action OnReloadComplete; // 换弹结束或需要清理UI时广播
    public static event Action OnEmptyClipFired; // 空弹夹开火警告广播

    void Start()
    {
        // 游戏刚开始，大喊一声初始化UI
        OnAmmoChanged?.Invoke();
    }

    void Update()
    {
        if (!hasWeapon) return;

        if (isReloading) return;

        // 1. 监听开火
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }

        // 2. 监听 R 键换弹
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentAmmo < maxMagazineSize && reserveAmmo > 0)
            {
                StartCoroutine(ReloadRoutine());
            }
        }
    }

    void Shoot()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;

            // 📢 广播出去：“我开枪了，子弹变了！” UI 收到后会自动更新数字
            OnAmmoChanged?.Invoke();

            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
        else
        {
            // 📢 广播出去：“没子弹了！” UI 收到后会弹出警告文字
            OnEmptyClipFired?.Invoke();
        }
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        float elapsed = 0f;
        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;

            // 📢 广播出去：“正在换弹，当前进度是 elapsed！” UI 收到后会自己去算百分比和倒计时
            OnReloading?.Invoke(elapsed);

            yield return null;
        }

        // --- ⏳ 时间到！核心数值交换 ---
        reserveAmmo += currentAmmo;
        currentAmmo = 0;
        int ammoNeeded = maxMagazineSize;

        if (reserveAmmo >= ammoNeeded)
        {
            currentAmmo = ammoNeeded;
            reserveAmmo -= ammoNeeded;
        }
        else
        {
            currentAmmo = reserveAmmo;
            reserveAmmo = 0;
        }

        isReloading = false;

        // 📢 广播出去：“换弹完成了！” UI 收到后会自动隐藏进度条、刷新数字
        OnReloadComplete?.Invoke();
        OnAmmoChanged?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "GunPickup")
        {
            hasWeapon = true;
            OnAmmoChanged?.Invoke(); // 捡到枪了，刷新 UI 显示名字和数字
            Destroy(other.gameObject);
        }
    }

    // 契约重置
    public void ResetData()
    {
        // 先广播让UI隐藏清除，再重置数据
        OnReloadComplete?.Invoke();

        StopAllCoroutines();
        isReloading = false;
        currentAmmo = maxMagazineSize;
        reserveAmmo = 28;

        OnAmmoChanged?.Invoke();
    }
}