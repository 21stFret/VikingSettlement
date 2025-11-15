using System.Collections.Generic;
using UnityEngine;

public class SpriteSorting : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOrderBase = 5000; // Base value for calculations
    [SerializeField] private int offset = 0; // Manual adjustment if needed
    public List<SpriteRenderer> linkedSpriteRenderers = new List<SpriteRenderer>();
    
    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = (int)(sortingOrderBase - transform.position.y * 100) + offset;
        }

        linkedSpriteRenderers.Clear();
    }

    private void LateUpdate()
    {
        if(gameObject.isStatic) return; // No need to update static objects
        // Lower Y position = higher sorting order (rendered on top)
        spriteRenderer.sortingOrder = (int)(sortingOrderBase - transform.position.y * 100) + offset;

        // Update linked sprite renderers
        for( int i = linkedSpriteRenderers.Count - 1; i >= 0; i--)
        {
            var linkedRenderer = linkedSpriteRenderers[i];
            if (linkedRenderer != null)
            {
                linkedRenderer.sortingOrder = spriteRenderer.sortingOrder;
            }
            else
            {
                Debug.LogWarning("Linked SpriteRenderer is null in " + gameObject.name);
                linkedSpriteRenderers.RemoveAt(i);
            }
        }
    }
    

}