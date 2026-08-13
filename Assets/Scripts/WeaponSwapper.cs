using UnityEngine;

/// <summary>
/// Cycles the player through three weapon slots (sword, axe, torch) by toggling
/// pre-instantiated GOs in the right-hand attachment point.
/// Expandable: replace the 3 prefab fields with an inventory list later.
/// </summary>
[RequireComponent(typeof(CharacterBase))]
[RequireComponent(typeof(ItemAttachment))]
public class WeaponSwapper : MonoBehaviour
{
    [Header("Weapon Slots — assign prefabs in Inspector")]
    public EquipableItem swordPrefab;
    public EquipableItem axePrefab;
    public EquipableItem torchPrefab;

    private GameObject[] slots = new GameObject[3];
    private int currentIndex;
    private CharacterBase charController;
    private ItemAttachment itemAttachment;
    private Villager villager;

    public int CurrentSlot => currentIndex;
    public EquipableItem CurrentWeapon => slots[currentIndex] != null
        ? slots[currentIndex].GetComponent<EquipableItem>() : null;

    private void Awake()
    {
        charController = GetComponent<CharacterBase>();
        itemAttachment = GetComponent<ItemAttachment>();
        villager = GetComponent<Villager>();
    }

    private void Start()
    {
        if (itemAttachment == null || charController == null) return;
        if(!villager.isJarl)
        {
            return;
        }
        swordPrefab = WeaponDatabase.Instance.GetWeaponByName("Rare Sword");
        axePrefab = WeaponDatabase.Instance.GetWeaponByName("Hammer");
        torchPrefab = WeaponDatabase.Instance.GetWeaponByName("Basic Axe");

        Transform hand = itemAttachment.rightHandAttachment;
        if (hand == null)
        {
            Debug.LogError("WeaponSwapper: rightHandAttachment is not assigned on ItemAttachment.");
            return;
        }

        itemAttachment.UnequipWeapon();

        EquipableItem[] prefabs = { swordPrefab, axePrefab, torchPrefab };
        for (int i = 0; i < 3; i++)
        {
            if (prefabs[i] == null)
            {
                //Debug.LogWarning($"WeaponSwapper: slot {i} prefab is not assigned.");
                continue;
            }
            slots[i] = Instantiate(prefabs[i].gameObject, hand);
            slots[i].transform.localPosition = Vector3.zero;
            slots[i].transform.localRotation = Quaternion.identity;
            slots[i].SetActive(i == 0);
        }

        itemAttachment.EquipWeapon(slots[0]);
        currentIndex = 0;
    }

    public void SwapToNext()
    {
        itemAttachment.UnequipWeapon();
        currentIndex = (currentIndex + 1) % 3;
        itemAttachment.EquipWeapon(slots[currentIndex]);

        Debug.Log($"WeaponSwapper: switched to slot {currentIndex} ({CurrentWeapon?.itemName ?? "empty"})");
    }
}
