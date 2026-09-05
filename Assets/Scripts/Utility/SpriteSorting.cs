using System.Collections.Generic;
using UnityEngine;

public class SpriteSorting : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int sortingOrderBase = 5000; // Base value for calculations
    [SerializeField] public int offset = 0; // Manual adjustment if needed

    private ParticleSystemRenderer _particleSystem;
    
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
        else
        {
            _particleSystem = GetComponent<ParticleSystemRenderer>();
        }
        if(_particleSystem != null)
        {
            _particleSystem.sortingOrder = (int)(sortingOrderBase - transform.position.y * 100) + offset;
        }
    }

    private void LateUpdate()
    {
        if(gameObject.isStatic) return; // No need to update static objects
        // Lower Y position = higher sorting order (rendered on top)
        spriteRenderer.sortingOrder = (int)(sortingOrderBase - transform.position.y * 100) + offset;
    }
}