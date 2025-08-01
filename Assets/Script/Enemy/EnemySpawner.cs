using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private int numberOfEnemies = 5;
    [SerializeField] private bool spawnOnStart = false;
    [SerializeField] private bool useRandomPositions = true;

    [Header("References")]
    [SerializeField] private TileGrid tileGrid;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyContainer;

    [SerializeField] private List<Vector2Int> specificSpawnPositions = new List<Vector2Int>();

    // Keep track of spawned enemies
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Start()
    {
        // Auto-find TileGrid if not assigned
        if (tileGrid == null)
        {
            Debug.LogError("EnemySpawner: No TileGrid found in the scene!");
        }

        // Create container if not assigned
        if (enemyContainer == null)
        {
            GameObject container = new GameObject("EnemyContainer");
            enemyContainer = container.transform;
        }

        if (spawnOnStart)
        {
            SpawnEnemies();
            AudioManager.Instance?.PlayEnemySpawnSFX();
        }
    }

    // Main method to spawn enemies
    public void SpawnEnemies()
    {
        // Clear any existing enemies first
        ClearAllEnemies();

        if (useRandomPositions)
        {
            SpawnRandomEnemies();
        }
        else
        {
            SpawnAtSpecificPositions();
        }
    }

    // Spawn enemies at random positions on enemy tiles
    private void SpawnRandomEnemies()
    {
        List<Vector2Int> positions = GetRandomEnemyPositions(numberOfEnemies);

        foreach (Vector2Int pos in positions)
        {
            SpawnSingleEnemy(pos);
        }
    }

    private void SpawnAtSpecificPositions()
    {
        int spawnCount = Mathf.Min(numberOfEnemies, specificSpawnPositions.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnSingleEnemy(specificSpawnPositions[i]);
        }
    }

    private List<Vector2Int> GetRandomEnemyPositions(int count)
    {
        List<Vector2Int> validPositions = new List<Vector2Int>();
        List<Vector2Int> selectedPositions = new List<Vector2Int>();

        // Collect all enemy tiles
        for (int x = 0; x < tileGrid.gridWidth; x++)
        {
            for (int y = 0; y < tileGrid.gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsValidEnemyPosition(pos))
                {
                    validPositions.Add(pos);
                }
            }
        }

        ShuffleList(validPositions);
        int spawnCount = Mathf.Min(count, validPositions.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            selectedPositions.Add(validPositions[i]);
        }

        return selectedPositions;
    }

    private bool IsValidEnemyPosition(Vector2Int position)
    {
        return tileGrid.IsValidGridPosition(position) &&
               tileGrid.grid[position.x, position.y] == TileType.Enemy;
    }

    private GameObject SpawnSingleEnemy(Vector2Int gridPosition)
    {
        if (!IsValidEnemyPosition(gridPosition))
        {
            Debug.LogWarning($"EnemySpawner: Cannot spawn enemy at invalid position {gridPosition}");
            return null;
        }

        // Get the center position of the tile
        Vector3 worldPosition = tileGrid.GetWorldPosition(gridPosition);

        // Adjust the Y position to be at the bottom of the tile
        float tileHeight = tileGrid.GetTileHeight();
        worldPosition.y -= tileHeight / 2f;

        GameObject enemy = Instantiate(enemyPrefab, worldPosition, Quaternion.identity, enemyContainer);
        enemy.name = $"Enemy_{gridPosition.x}_{gridPosition.y}";

        // Add to our tracking list
        spawnedEnemies.Add(enemy);

        return enemy;
    }

    // Clear all spawned enemies
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        spawnedEnemies.Clear();
    }

    // Helper method to shuffle a list
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int j = Random.Range(i, n);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
    public int GetAliveEnemyCount()
    {
        int count = 0;
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                count++;
        }
        return count;
    }
}