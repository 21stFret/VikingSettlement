using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class BreakableObject2D : MonoBehaviour
{
    [Header("Broken Parts")]
    [SerializeField] private GameObject[] brokenPartPrefabs;

    [Header("Timing")]
    [SerializeField] private float hideDelay = 5f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce = 300f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float upwardModifier = 1.5f;
    [SerializeField] private float torqueMultiplier = 20f;

    [Header("Object Settings")]
    public float localScale = 1f;
    public int layer = 21;
    public bool canCollide;

    private static Transform _poolContainer;
    private List<GameObject>[] brokenPartPools;
    private List<GameObject> activeParts = new List<GameObject>();
    private bool isBroken = false;

    // Cached components
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        InitializeObjectPools();
    }

    private void InitializeObjectPools()
    {
        if (_poolContainer == null)
        {
            _poolContainer = new GameObject("BrokenPartsPool").transform;
        }

        brokenPartPools = new List<GameObject>[brokenPartPrefabs.Length];
        for (int i = 0; i < brokenPartPrefabs.Length; i++)
        {
            brokenPartPools[i] = new List<GameObject>();
        }
    }

    private GameObject GetPooledPart(int partIndex)
    {
        foreach (GameObject part in brokenPartPools[partIndex])
        {
            if (!part.activeInHierarchy)
            {
                return part;
            }
        }

        GameObject newPart = Instantiate(brokenPartPrefabs[partIndex], transform.position, Quaternion.identity, _poolContainer);
        newPart.SetActive(false);
        brokenPartPools[partIndex].Add(newPart);
        return newPart;
    }

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        // Hide the main object
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        if (_collider != null) _collider.enabled = false;

        Vector3 explosionCenter = transform.position;

        for (int i = 0; i < brokenPartPrefabs.Length; i++)
        {
            GameObject part = GetPooledPart(i);

            // Reset sprite alpha using MaterialPropertyBlock to avoid material leaks
            var spriteRend = part.GetComponent<SpriteRenderer>();
            if (spriteRend != null)
            {
                Color color = spriteRend.color;
                color.a = 1f;
                spriteRend.color = color;
            }

            // Position with slight random offset
            Vector2 randomOffset = Random.insideUnitCircle * 0.2f;
            part.transform.position = (Vector2)transform.position + randomOffset;

            // Random Z rotation only for 2D
            part.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            part.transform.localScale = new Vector3(localScale, localScale, localScale);
            part.layer = layer;

            part.SetActive(true);
            activeParts.Add(part);

            Collider2D col = part.GetComponent<Collider2D>();
            if (col != null) col.enabled = canCollide;

            Rigidbody2D rb = part.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = false;

                // Random torque (Z-axis only in 2D)
                rb.AddTorque(Random.Range(-torqueMultiplier, torqueMultiplier), ForceMode2D.Impulse);

                // Calculate explosion direction from center to part
                Vector2 direction = ((Vector2)part.transform.position - (Vector2)explosionCenter);
                if (direction.sqrMagnitude < 0.001f)
                {
                    direction = Random.insideUnitCircle.normalized;
                }
                else
                {
                    direction = direction.normalized;
                }

                // Add upward bias
                direction += Vector2.up * upwardModifier;
                direction.Normalize();

                // Apply explosion force with distance falloff
                float distance = Vector2.Distance(part.transform.position, explosionCenter);
                float falloff = 1f - Mathf.Clamp01(distance / explosionRadius);
                float force = Random.Range(explosionForce * 0.9f, explosionForce * 1.5f) * falloff;

                rb.AddForce(direction * force, ForceMode2D.Impulse);

                // Additional random impulse for chaotic movement
                rb.AddForce(Random.insideUnitCircle * explosionForce * 0.3f, ForceMode2D.Impulse);
            }
        }

        StartCoroutine(HideBrokenParts());
    }

    private IEnumerator HideBrokenParts()
    {
        yield return new WaitForSeconds(hideDelay);

        foreach (GameObject part in activeParts)
        {
            if (part == null) continue;

            SpriteRenderer spriteRend = part.GetComponent<SpriteRenderer>();
            Collider2D col = part.GetComponent<Collider2D>();
            Rigidbody2D rb = part.GetComponent<Rigidbody2D>();

            if (spriteRend == null) continue;

            // Freeze the part in place
            if (rb != null) rb.isKinematic = true;
            if (col != null) col.enabled = false;

            // Fade out using SpriteRenderer color (no material leak)
            spriteRend.DOFade(0f, fadeDuration);
        }

        yield return new WaitForSeconds(fadeDuration + 0.1f);

        DeactivateAllParts();
    }

    private void DeactivateAllParts()
    {
        foreach (GameObject part in activeParts)
        {
            if (part == null) continue;

            // Kill any active DOTween animations on this part
            DOTween.Kill(part.GetComponent<SpriteRenderer>());

            part.SetActive(false);
        }
        activeParts.Clear();
        isBroken = false;
    }

    public void Respawn()
    {
        // Kill all running DOTween animations on active parts
        foreach (GameObject part in activeParts)
        {
            if (part == null) continue;
            var sr = part.GetComponent<SpriteRenderer>();
            if (sr != null) DOTween.Kill(sr);
        }

        StopAllCoroutines();

        // Deactivate all broken parts
        foreach (GameObject part in activeParts)
        {
            if (part == null) continue;
            part.SetActive(false);
        }
        activeParts.Clear();

        // Restore the main object
        if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        if (_collider != null) _collider.enabled = true;
        isBroken = false;
    }
}
