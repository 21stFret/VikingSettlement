using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows buildings to be clicked and selected
/// </summary>
[RequireComponent(typeof(Building))]
public class BuildingSelector : WorldInteractable, IClickable
{
    [Header("Selection")]
    [SerializeField] private GameObject selectionIndicator; // Optional visual indicator
    [SerializeField] private BuildingInfoPanel infoPanelPrefab; // Reference to info panel
    [SerializeField] private int clickPriority = 0;

    private Building building;
    private bool isSelected = false;
    private static BuildingInfoPanel sharedInfoPanel;
    private Collider2D buildingCollider;

    // WorldInteractable
    public override bool IsInteractable => building != null && building.isConstructed;

    public override void FocusPanel() => BuildingInfoPanel.Instance?.FocusForController();

    // IClickable (mouse)
    public Collider2D Collider => buildingCollider;
    public int ClickPriority => clickPriority;
    public bool IsClickable => IsInteractable;

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
            sharedInfoPanel = FindAnyObjectByType<BuildingInfoPanel>(FindObjectsInactive.Include);
        }

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        // Register with MouseInputController (handles case where Instance isn't ready yet)
        MouseInputController.RegisterClickable(this);
    }

    private void OnDestroy()
    {
        // Unregister when destroyed
        MouseInputController.UnregisterClickable(this);
    }

    public override void Interact(WorldInteractionZone zone = null)
    {
        base.Interact(zone);
        SelectBuilding();
    }


    /// <summary>
    /// Called by MouseInputController when this building is clicked
    /// </summary>
    public void OnClicked()
    {
        SelectBuilding();
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

        // Show info panel and disable player input while it is open
        if (sharedInfoPanel != null)
        {
            sharedInfoPanel.ShowBuilding(building, this);
        }

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetInputEnabled(false);


        // Optionally focus camera on this building
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetTarget(transform);
        }
    }
    
    /// <summary>
    /// Deselect this building
    /// </summary>
    public override void Deselect(WorldInteractionZone zone = null)
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
            CameraController.Instance.ReturnToPlayerTarget();
        }
        base.Deselect();
    }
    
    /// <summary>
    /// Set the shared info panel (call this from a manager if needed)
    /// </summary>
    public static void SetSharedInfoPanel(BuildingInfoPanel panel)
    {
        sharedInfoPanel = panel;
    }
}