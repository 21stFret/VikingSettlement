using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Displays all resources in the UI
/// </summary>
public class ResourceDisplayUI : MonoBehaviour
{    
    private List<SingleResourceDisplay> resourceElements = new List<SingleResourceDisplay>();
    [SerializeField] private string numberFormat = "F0"; // How to format numbers (F0 = no decimals, F1 = 1 decimal)
    [SerializeField] private bool showResourceName = true;
    [SerializeField] private float updateInterval = 0.1f; // Update 10 times per second
    
    private float updateTimer = 0f;

    private void Start()
    {
        resourceElements.Clear();
        resourceElements = GetComponentsInChildren<SingleResourceDisplay>().ToList();
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Update all resource displays
    /// </summary>
    private void UpdateDisplay()
    {
        if (ResourceManager.Instance == null) return;

        foreach (SingleResourceDisplay element in resourceElements)
        {
            if (element.amountText == null) continue;
            
            float amount = ResourceManager.Instance.GetResource(element.resourceType);
            
            // Format the text
            string displayText = "";
            if (showResourceName)
            {
                displayText = $"{element.resourceType}: {amount.ToString(numberFormat)}";
            }
            else
            {
                displayText = amount.ToString(numberFormat);
            }
            
            element.amountText.text = displayText;
            
        }
    }
    
    /// <summary>
    /// Force update the display immediately
    /// </summary>
    public void ForceUpdate()
    {
        UpdateDisplay();
    }
    
}
