using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a single resource type
/// </summary>
public class SingleResourceDisplay : MonoBehaviour
{
    [Header("Settings")]
    public ResourceType resourceType;
    public TextMeshProUGUI amountText;
    public Image icon; // Optional

    [Header("Format")]
    [SerializeField] private string numberFormat = "F0"; // F0 = no decimals, F1 = 1 decimal
    
    [Header("Color Coding (Optional)")]
    [SerializeField] private bool useColorCoding = false;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowColor = Color.red;
    [SerializeField] private float lowThreshold = 10f; // Below this = red
    
}
