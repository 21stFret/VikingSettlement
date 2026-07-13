using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BreakableObject2D))]
public class BreakableBarrel : TargetHealth
{
    [Header("Breakable Barrel Settings")]
    [SerializeField] private float respawnTime = -1f;
    public ParticleSystem breakEffect;
    public ParticleSystem moveEffect;
    public List<LootTableEntry> lootTable = new List<LootTableEntry>();
    private BreakableObject2D _breakable;
    private float respawnTimer;
    private bool isBroken = false;
    public bool canRespawn;

    public override void Awake()
    {
        base.Awake();
        _breakable = GetComponent<BreakableObject2D>();
        breakEffect.transform.parent = null; // Detach from barrel to prevent scaling issues
        moveEffect.transform.parent = null; // Detach from barrel to prevent scaling issues
    }

    public override void Die()
    {
        if (isDead) return;
        base.Die();
        BreakBarrel();

        if (LootDropManager.Instance != null)
        {
            LootDropManager.Instance.SpawnDrops(transform.position, lootTable);
        }

        ToggleHiddenAllChildren(false);
    }

    private void ToggleHiddenAllChildren(bool hidden)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(hidden);
        }
    }

    private void BreakBarrel()
    {
        isBroken = true;
        respawnTimer = respawnTime;

        if (breakEffect != null)
        {
            breakEffect.transform.position = transform.position;
            breakEffect.Play();
        }

        if (_breakable != null)
        {
            _breakable.Break();
        }
    }

    private void Update()
    {
        if (isBroken && respawnTime >= 0f && canRespawn)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
        }
        moveEffect.transform.position = transform.position + (Vector3.up * 0.4f); // Adjust the position of the move effect
    }

    private void Respawn()
    {
        if (_breakable != null)
        {
            _breakable.Respawn();
        }

        isBroken = false;
        isDead = false;
        currentHealth = maxHealth;
        ToggleHiddenAllChildren(true);
    }
}
