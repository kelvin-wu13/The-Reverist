using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Player,
    Enemy,
    Empty
}

[System.Serializable]
public class TileSet
{
    public Sprite playerTileSprite;
    public Sprite enemyTileSprite;
    public Sprite emptyTileSprite;
}

public class TileGrid : MonoBehaviour
{
    [SerializeField] public int gridWidth = 8;
    [SerializeField] public int gridHeight = 4;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TileSet tileSet;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private GameObject enemyPrefab;
    
    private TileType[,] grid;
    private GameObject[,] tileObjects;

    private void Awake()
    {
        // Initialize the grid
        grid = new TileType[gridWidth, gridHeight];
        tileObjects = new GameObject[gridWidth, gridHeight];
        
        // Create the grid
        CreateGrid();
        
        // Setup initial player and enemy positions
        SetupInitialPositions();
    }
    
    private void CreateGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 position = new Vector3(x * tileSize, y * tileSize, 0);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}";
                
                SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = tile.AddComponent<SpriteRenderer>();
                }
                
                // All tiles start as empty
                grid[x, y] = TileType.Empty;
                spriteRenderer.sprite = tileSet.emptyTileSprite;
                
                tileObjects[x, y] = tile;
            }
        }
    }
    
    private void SetupInitialPositions()
    {
        // Set left half for player (first 4 columns)
        for (int x = 0; x < gridWidth / 2; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                SetTileType(new Vector2Int(x, y), TileType.Player);
            }
        }
        
        // Set right half for enemy (last 4 columns)
        for (int x = gridWidth / 2; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                SetTileType(new Vector2Int(x, y), TileType.Enemy);
                
                // Optionally, spawn enemy characters on their tiles
                // SpawnEnemy(new Vector2Int(x, y));
            }
        }
    }
    
    public void SetTileType(Vector2Int gridPosition, TileType type)
    {
        if (IsValidGridPosition(gridPosition))
        {
            grid[gridPosition.x, gridPosition.y] = type;
            
            SpriteRenderer spriteRenderer = tileObjects[gridPosition.x, gridPosition.y].GetComponent<SpriteRenderer>();
            
            switch (type)
            {
                case TileType.Player:
                    spriteRenderer.sprite = tileSet.playerTileSprite;
                    break;
                case TileType.Enemy:
                    spriteRenderer.sprite = tileSet.enemyTileSprite;
                    break;
                case TileType.Empty:
                    spriteRenderer.sprite = tileSet.emptyTileSprite;
                    break;
            }
        }
    }
    
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
               gridPosition.y >= 0 && gridPosition.y < gridHeight;
    }
    
    public bool IsValidPlayerPosition(Vector2Int gridPosition)
    {
        // Check if position is valid and not an enemy tile
        return IsValidGridPosition(gridPosition) && 
               grid[gridPosition.x, gridPosition.y] != TileType.Enemy;
    }
    
    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * tileSize, gridPosition.y * tileSize, 0);
    }
    
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / tileSize);
        int y = Mathf.FloorToInt(worldPosition.y / tileSize);
        
        return new Vector2Int(x, y);
    }
    
    // Example method to spawn an enemy at a specific grid position
    private void SpawnEnemy(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition) && grid[gridPosition.x, gridPosition.y] == TileType.Enemy)
        {
            // This is where you would instantiate your enemy prefab
            // GameObject enemy = Instantiate(enemyPrefab, GetWorldPosition(gridPosition), Quaternion.identity, enemyParent);
        }
    }
}