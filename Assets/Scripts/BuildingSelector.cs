using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Allows buildings to be clicked and selected
/// </summary>
[RequireComponent(typeof(Building))]
public class BuildingSelector : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private GameObject selectionIndicator; // Optional visual indicator
    [SerializeField] private BuildingInfoPanel infoPanelPrefab; // Reference to info panel
    
    private Building building;
    private bool isSelected = false;
    private static BuildingInfoPanel sharedInfoPanel;
    private Collider2D buildingCollider;
    
    private void Awake()
    {
        building = GetComponent<Building>();
        buildingCollider = GetComponent<Collider2D>();
    }
    
    private void Start()
    {
        // Find or create the info panel
        if (sharedInfoPanel == null && infoPanelPrefab != null)
        {
            sharedInfoPanel = FindFirstObjectByType<BuildingInfoPanel>(FindObjectsInactive.Include);
            if (sharedInfoPanel == null)
            {
                // Create one if it doesn't exist
                sharedInfoPanel = Instantiate(infoPanelPrefab);
            }
        }

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
            
    }

    private void Update()
    {
        // Check for mouse click using new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if(EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                //print($"Clicked on {EventSystem.current.currentSelectedGameObject}, ignoring building selection.");
                // Clicked on UI, ignore
                return;
            }
            CheckClick();
        }
    }
    
    private void CheckClick()
    {
        // Get mouse position in world space
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // Check if mouse is over this building's collider
        if (buildingCollider != null && buildingCollider.OverlapPoint(mousePosition))
        {
            SelectBuilding();
        }

    }
    
    /// <summary>
    /// Select this building and show its info panel
    /// </summary>
    public void SelectBuilding()
    {
        if (building == null || !building.isConstructed) return;

        if (isSelected) return; // Already selected
        
        Debug.Log("Building selected: " + building.data.buildingName);
        
        // Deselect all other buildings
        List<BuildingSelector> allSelectors = SettlementManager.Instance.GetAllBuildingSelectors();
        foreach (BuildingSelector selector in allSelectors)
        {
            if (selector != this)
            {
                selector.Deselect();
            }
        }
        
        // Select this building
        isSelected = true;
        
        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        // Show info panel
        if (sharedInfoPanel != null)
        {
            sharedInfoPanel.ShowBuilding(building, this);
        }
        
        // Optionally focus camera on this building
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetTarget(transform);
        }
    }
    
    /// <summary>
    /// Deselect this building
    /// </summary>
    public void Deselect()
    {
        isSelected = false;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
        // Hide info panel if it is showing this building
        if (sharedInfoPanel != null && sharedInfoPanel.GetCurrentBuilding() == building)
        {
            sharedInfoPanel.Hide();
        }

        // Optionally reset camera target
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetPlayerTarget();
        }
    }
    
    /// <summary>
    /// Set the shared info panel (call this from a manager if needed)
    /// </summary>
    public static void SetSharedInfoPanel(BuildingInfoPanel panel)
    {
        sharedInfoPanel = panel;
    }
}