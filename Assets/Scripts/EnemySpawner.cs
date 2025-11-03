using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] enemyPrefabs; // Different enemy types
    [SerializeField] private Transform[] spawnPoints; // Designated spawn locations
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float spawnInterval = 30f; // Time between spawn waves
    [SerializeField] private float initialDelay = 10f; // Delay before first spawn

    [Header("Wave Settings")]
    [SerializeField] private int enemiesPerWave = 3;
    [SerializeField] private int maxActiveEnemies = 10;
    [SerializeField] private bool increaseWaveDifficulty = true;
    [SerializeField] private float difficultyIncreaseRate = 0.1f; // +10% per wave

    [Header("Spawn Area (if no spawn points)")]
    [SerializeField] private Vector2 spawnAreaCenter = Vector2.zero;
    [SerializeField] private float spawnAreaRadius = 20f;
    [SerializeField] private float minDistanceFromPlayer = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private float spawnTimer;
    private int currentWave = 0;
    private List<Enemy> activeEnemies = new List<Enemy>();

    private void Start()
    {
        spawnTimer = initialDelay;

        // Validate setup
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefabs assigned!");
        }

        // Use spawn area center if not set
        if (spawnAreaCenter == Vector2.zero)
        {
            spawnAreaCenter = transform.position;
        }
    }

    private void Update()
    {
        if (!autoSpawn) return;

        // Clean up dead enemies from list
        activeEnemies.RemoveAll(e => e == null || e.IsDead());

        // Spawn timer
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            TrySpawnWave();
        }
    }

    private void TrySpawnWave()
    {
        // Check if we have room for more enemies
        if (activeEnemies.Count >= maxActiveEnemies)
        {
            if (showDebugInfo)
            {
                Debug.Log($"EnemySpawner: Max active enemies ({maxActiveEnemies}) reached. Delaying spawn.");
            }
            return;
        }

        currentWave++;
        int enemiesToSpawn = CalculateWaveSize();

        if (showDebugInfo)
        {
            Debug.Log($"EnemySpawner: Spawning Wave {currentWave} with {enemiesToSpawn} enemies");
        }

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Don't exceed max
            if (activeEnemies.Count >= maxActiveEnemies) break;

            SpawnEnemy();
        }
    }

    private int CalculateWaveSize()
    {
        if (!increaseWaveDifficulty) return enemiesPerWave;

        // Increase wave size based on wave number
        float multiplier = 1f + (currentWave - 1) * difficultyIncreaseRate;
        return Mathf.CeilToInt(enemiesPerWave * multiplier);
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // Pick random enemy type
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // Get spawn position
        Vector2 spawnPos = GetSpawnPosition();

        // Spawn enemy
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // Track enemy
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            activeEnemies.Add(enemy);

            // Apply wave difficulty scaling
            if (increaseWaveDifficulty && currentWave > 1)
            {
                float healthMultiplier = 1f + (currentWave - 1) * difficultyIncreaseRate;
                enemy.maxHealth *= healthMultiplier;
                // Re-initialize to apply new max health
                enemy.GetComponent<TargetHealth>().maxHealth = enemy.maxHealth;
            }

            if (showDebugInfo)
            {
                Debug.Log($"EnemySpawner: Spawned {enemy.enemyName} at {spawnPos}");
            }
        }
    }

    private Vector2 GetSpawnPosition()
    {
        // Use spawn points if available
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint.position;
        }

        // Otherwise, use spawn area
        Vector2 spawnPos = Vector2.zero;
        int attempts = 0;
        int maxAttempts = 20;

        do
        {
            // Generate random position in spawn area
            Vector2 randomOffset = Random.insideUnitCircle * spawnAreaRadius;
            spawnPos = spawnAreaCenter + randomOffset;
            attempts++;

            // Check distance from settlement/player
            if (IsValidSpawnPosition(spawnPos))
            {
                break;
            }

        } while (attempts < maxAttempts);

        return spawnPos;
    }

    private bool IsValidSpawnPosition(Vector2 position)
    {
        // Check distance from all villagers
        Villager[] villagers = FindObjectsOfType<Villager>();

        foreach (var villager in villagers)
        {
            float distance = Vector2.Distance(position, villager.transform.position);
            if (distance < minDistanceFromPlayer)
            {
                return false; // Too close to a villager
            }
        }

        return true;
    }

    /// <summary>
    /// Manually spawn a wave
    /// </summary>
    public void SpawnWaveNow()
    {
        TrySpawnWave();
    }

    /// <summary>
    /// Manually spawn a single enemy
    /// </summary>
    public void SpawnSingleEnemy(int enemyTypeIndex = -1)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // Use specified type or random
        int index = enemyTypeIndex >= 0 && enemyTypeIndex < enemyPrefabs.Length
            ? enemyTypeIndex
            : Random.Range(0, enemyPrefabs.Length);

        GameObject enemyPrefab = enemyPrefabs[index];
        Vector2 spawnPos = GetSpawnPosition();
        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Toggle auto spawning
    /// </summary>
    public void SetAutoSpawn(bool enabled)
    {
        autoSpawn = enabled;
        if (enabled)
        {
            spawnTimer = spawnInterval;
        }
    }

    /// <summary>
    /// Reset spawn system
    /// </summary>
    public void ResetSpawner()
    {
        currentWave = 0;
        spawnTimer = initialDelay;

        // Clear all active enemies
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
    }

    /// <summary>
    /// Get count of active enemies
    /// </summary>
    public int GetActiveEnemyCount()
    {
        activeEnemies.RemoveAll(e => e == null || e.IsDead());
        return activeEnemies.Count;
    }

    /// <summary>
    /// Get current wave number
    /// </summary>
    public int GetCurrentWave()
    {
        return currentWave;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw spawn area
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnAreaCenter, spawnAreaRadius);

        // Draw min distance radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnAreaCenter, minDistanceFromPlayer);

        // Draw spawn points
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = Color.magenta;
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 1f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
            }
        }

        // Display info
        #if UNITY_EDITOR
        if (showDebugInfo && Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"Wave: {currentWave}\nActive: {activeEnemies.Count}/{maxActiveEnemies}\nNext in: {spawnTimer:F1}s");
        }
        #endif
    }
}
