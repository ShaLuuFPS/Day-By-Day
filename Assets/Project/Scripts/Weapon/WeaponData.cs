using UnityEngine;

public enum WeaponType
{
    Gun,
    Melee
}

[CreateAssetMenu(menuName = "DayByDay/WeaponData (基类)")]
public class WeaponData : ScriptableObject
{
    [Header("武器身份")]
    public string weaponName = "未命名武器";

    [Header("武器类型")]
    public WeaponType weaponType = WeaponType.Gun;
}