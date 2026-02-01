using UnityEngine;

public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance { get; private set; }

    [Header("Weapons")]
    [SerializeField] private EquipableItem[] availableWeapons;

    [Header("Shields")]
    [SerializeField] private EquipableItem[] availableShields;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Get a random weapon from the database
    /// </summary>
    /// <returns>A random EquipableItem weapon</returns>
    public EquipableItem GetRandomWeapon()
    {
        if (availableWeapons.Length == 0)
            return null;

        int index = Random.Range(0, availableWeapons.Length);
        return availableWeapons[index];
    }

    /// <summary>
    /// Get a random shield from the database
    /// </summary>
    /// <returns>A random EquipableItem shield</returns>
    public EquipableItem GetRandomShield()
    {
        if (availableShields.Length == 0)
            return null;

        int index = Random.Range(0, availableShields.Length);
        return availableShields[index];
    }

    /// <summary>
    /// Get a weapon by name
    /// </summary>
    public EquipableItem GetWeaponByName(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return null;

        foreach (var weapon in availableWeapons)
        {
            if (weapon != null && weapon.itemName == weaponName)
            {
                return weapon;
            }
        }
        return null;
    }

    /// <summary>
    /// Get a shield by name
    /// </summary>
    public EquipableItem GetShieldByName(string shieldName)
    {
        if (string.IsNullOrEmpty(shieldName)) return null;

        foreach (var shield in availableShields)
        {
            if (shield != null && shield.itemName == shieldName)
            {
                return shield;
            }
        }
        return null;
    }
}
