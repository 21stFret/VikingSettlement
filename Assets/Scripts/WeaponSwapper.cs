using UnityEngine;

/// <summary>
/// Cycles the player through three weapon slots (sword, axe, torch) by toggling
/// pre-instantiated GOs in the right-hand attachment point.
/// Expandable: replace the 3 prefab fields with an inventory list later.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ItemAttachment))]
public class WeaponSwapper : MonoBehaviour
{
    [Header("Weapon Slots — assign prefabs in Inspector")]
    public EquipableItem swordPrefab;
    public EquipableItem axePrefab;
    public EquipableItem torchPrefab;

    private GameObject[] slots = new GameObject[3];
    private int currentIndex;
    private CharacterController charController;
    private ItemAttachment itemAttachment;

    public int CurrentSlot => currentIndex;
    public EquipableItem CurrentWeapon => slots[currentIndex] != null
        ? slots[currentIndex].GetComponent<EquipableItem>() : null;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
        itemAttachment = GetComponent<ItemAttachment>();
    }

    private void Start()
    {
        if (itemAttachment == null || charController == null) return;

        swordPrefab = WeaponDatabase.Instance.GetWeaponByName("Basic Sword");
        axePrefab = WeaponDatabase.Instance.GetWeaponByName("Basic Axe");
        torchPrefab = WeaponDatabase.Instance.GetWeaponByName("Torch");

        Transform hand = itemAttachment.rightHandAttachment;
        if (hand == null)
        {
            Debug.LogError("WeaponSwapper: rightHandAttachment is not assigned on ItemAttachment.");
            return;
        }

        // Clear whatever GiveRandomWeapon put here so we own the slot
        foreach (Transform child in hand)
            Destroy(child.gameObject);

        EquipableItem[] prefabs = { swordPrefab, axePrefab, torchPrefab };
        for (int i = 0; i < 3; i++)
        {
            if (prefabs[i] == null)
            {
                Debug.LogWarning($"WeaponSwapper: slot {i} prefab is not assigned.");
                continue;
            }
            slots[i] = Instantiate(prefabs[i].gameObject, hand);
            slots[i].transform.localPosition = Vector3.zero;
            slots[i].transform.localRotation = Quaternion.identity;
            slots[i].SetActive(i == 0);
        }

        currentIndex = 0;
        charController.weapon = slots[0] != null ? slots[0].GetComponent<EquipableItem>() : null;
    }

    public void SwapToNext()
    {
        slots[currentIndex]?.SetActive(false);
        currentIndex = (currentIndex + 1) % 3;
        slots[currentIndex]?.SetActive(true);
        charController.weapon = slots[currentIndex] != null
            ? slots[currentIndex].GetComponent<EquipableItem>() : null;

        Debug.Log($"WeaponSwapper: switched to slot {currentIndex} ({CurrentWeapon?.itemName ?? "empty"})");
    }
}
