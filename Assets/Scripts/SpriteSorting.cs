using UnityEngine;

public class SpriteSorting : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOrderBase = 5000; // Base value for calculations
    [SerializeField] private int offset = 0; // Manual adjustment if needed
    
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
    }

    private void LateUpdate()
    {
        if(gameObject.isStatic) return; // No need to update static objects
        // Lower Y position = higher sorting order (rendered on top)
        spriteRenderer.sortingOrder = (int)(sortingOrderBase - transform.position.y * 100) + offset;
    }
    

}