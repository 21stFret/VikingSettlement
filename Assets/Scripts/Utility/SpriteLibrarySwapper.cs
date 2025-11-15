/*
 * RECOMMENDED APPROACH: Unity's Sprite Library System
 * 
 * This is the official Unity way to swap sprites in animations (Unity 2021.1+)
 * 
 * SETUP STEPS:
 * 
 * 1. Install the 2D Animation package:
 *    - Window > Package Manager
 *    - Search for "2D Animation"
 *    - Install it
 * 
 * 2. Create a Sprite Library Asset:
 *    - Right-click in Project > Create > 2D > Sprite Library Asset
 *    - Name it something like "VillagerSpriteLibrary"
 * 
 * 3. Set up Categories in the Sprite Library:
 *    - In the Sprite Library Asset, create categories like:
 *      - "idle_frame_0"
 *      - "idle_frame_1"
 *      - "walk_frame_0"
 *      - "walk_frame_1"
 *      etc.
 * 
 * 4. Add Variants for each category:
 *    - For "idle_frame_0" add variants:
 *      - "blue_villager" (blue idle frame 0 sprite)
 *      - "red_villager" (red idle frame 0 sprite)
 *      - "green_villager" (green idle frame 0 sprite)
 *    - Do this for ALL animation frames
 * 
 * 5. Add components to your character GameObject:
 *    - Add "Sprite Library" component
 *    - Assign your Sprite Library Asset
 *    - Add "Sprite Resolver" component to the GameObject with the SpriteRenderer
 *    - Set the Category and Label in Sprite Resolver
 * 
 * 6. In your animations:
 *    - Instead of animating the "Sprite" property of SpriteRenderer
 *    - Animate the "Sprite Resolver > Sprite Key" property
 *    - Set it to the category names you created
 * 
 * 7. To swap variants at runtime:
 *    - Use the script below
 * 
 * BENEFITS:
 * - One animation works for all variants
 * - Easy to add new color variants
 * - Works perfectly with 2D animation rigging
 * - Official Unity solution
 */

using UnityEngine;
using UnityEngine.U2D.Animation; // Requires 2D Animation package

/// <summary>
/// Swaps sprite variants using Unity's Sprite Library system
/// This is the RECOMMENDED approach for Unity 2021.1+
/// </summary>
public class SpriteLibrarySwapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteLibrary spriteLibrary;
    [SerializeField] private SpriteResolver[] spriteResolvers; // All sprite resolvers in the character
    
    [Header("Variants")]
    [SerializeField] private string[] availableVariants = new string[] 
    { 
        "blue_villager", 
        "red_villager", 
        "green_villager" 
    };
    
    [SerializeField] private int currentVariantIndex = 0;
    
    private void Start()
    {
        if (spriteLibrary == null)
            spriteLibrary = GetComponent<SpriteLibrary>();
        
        if (spriteResolvers == null || spriteResolvers.Length == 0)
            spriteResolvers = GetComponentsInChildren<SpriteResolver>();
        
        // Apply initial variant
        SetVariant(currentVariantIndex);
    }
    
    /// <summary>
    /// Change to a different sprite variant
    /// </summary>
    public void SetVariant(int index)
    {
        if (index < 0 || index >= availableVariants.Length)
            return;
        
        currentVariantIndex = index;
        string variantName = availableVariants[index];
        
        // Update all sprite resolvers to use the new variant
        foreach (var resolver in spriteResolvers)
        {
            if (resolver != null)
            {
                resolver.SetCategoryAndLabel(resolver.GetCategory(), variantName);
            }
        }
        
        Debug.Log($"Switched to variant: {variantName}");
    }
    
    /// <summary>
    /// Set variant by name
    /// </summary>
    public void SetVariant(string variantName)
    {
        for (int i = 0; i < availableVariants.Length; i++)
        {
            if (availableVariants[i] == variantName)
            {
                SetVariant(i);
                return;
            }
        }
    }

    /// <summary>
    /// Cycle to next variant
    /// </summary>
    public void NextVariant()
    {
        SetVariant((currentVariantIndex + 1) % availableVariants.Length);
    }
    
    /// <summary>
    /// Get a random variant name
    /// </summary>
    public string GetRandomVariant()
    {
        if (availableVariants.Length == 0)
            return "";
        
        return availableVariants[Random.Range(0, availableVariants.Length)];
    }
}
