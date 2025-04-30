using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Player,
    Enemy,
    Empty,
    Cracked,
    Broken
}

[System.Serializable]
public class TileSet
{
    public Sprite playerTileSprite;
    public Sprite enemyTileSprite;
    public Sprite emptyTileSprite;
    public Sprite crackedTileSprite; // Added sprite for cracked tiles
    public Sprite brokenTileSprite;  // Added sprite for broken tiles
}

public class TileGrid : MonoBehaviour
{
    [Header("Grid Setting")] 
    [SerializeField] public int gridWidth = 8;
    [SerializeField] public int gridHeight = 4;
    [SerializeField] private float tileSize = 1f;
    
    [Header("Tile Effect Durations")]
    [SerializeField] private float crackedTileDuration = 1.5f; // Duration before a cracked tile auto-repairs
    [SerializeField] private float brokenTileDuration = 2.0f; // Duration before a broken tile auto-repairs

    [Header("Tile Reference")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TileSet tileSet;
    
    [SerializeField] private Vector2 gridOffset = Vector2.zero;
    public TileType[,] grid;
    private GameObject[,] tileObjects;
    // Track original tile type to restore correctly after damage
    private TileType[,] originalTileTypes;

    private void Awake()
    {
        // Initialize the grid
        grid = new TileType[gridWidth, gridHeight];
        tileObjects = new GameObject[gridWidth, gridHeight];
        originalTileTypes = new TileType[gridWidth, gridHeight];
        
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
                Vector3 position = new Vector3(x * tileSize + gridOffset.x, y * tileSize + gridOffset.y, 0);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}";
                
                SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = tile.AddComponent<SpriteRenderer>();
                }
                
                // All tiles start as empty
                grid[x, y] = TileType.Empty;
                originalTileTypes[x, y] = TileType.Empty;
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
                    // Update originalTileType only for base types (not damage states)
                    originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Player;
                    break;
                case TileType.Enemy:
                    spriteRenderer.sprite = tileSet.enemyTileSprite;
                    // Update originalTileType only for base types (not damage states)
                    originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Enemy;
                    break;
                case TileType.Empty:
                    spriteRenderer.sprite = tileSet.emptyTileSprite;
                    // Update originalTileType only for base types (not damage states)
                    originalTileTypes[gridPosition.x, gridPosition.y] = TileType.Empty;
                    break;
                case TileType.Cracked:
                    spriteRenderer.sprite = tileSet.crackedTileSprite;
                    // Don't update originalTileType for damage states
                    break;
                case TileType.Broken:
                    spriteRenderer.sprite = tileSet.brokenTileSprite;
                    // Don't update originalTileType for damage states
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
        // Check if position is valid and not an enemy tile or a broken tile
        return IsValidGridPosition(gridPosition) && 
               grid[gridPosition.x, gridPosition.y] != TileType.Enemy &&
               grid[gridPosition.x, gridPosition.y] != TileType.Broken;
    }

    public void CrackTile(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition) && grid[gridPosition.x, gridPosition.y] != TileType.Broken)
        {
            grid[gridPosition.x, gridPosition.y] = TileType.Cracked;
            
            // Update tile sprite to cracked
            SpriteRenderer spriteRenderer = tileObjects[gridPosition.x, gridPosition.y].GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = tileSet.crackedTileSprite;
            
            // Optional: Add some visual effect to indicate the crack
            StartCoroutine(TileCrackEffect(gridPosition));
            
            // Start timer to auto-repair the cracked tile
            StartCoroutine(AutoRepairCrackedTile(gridPosition, crackedTileDuration));
        }
    }

    public void BreakTile(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
        {
            grid[gridPosition.x, gridPosition.y] = TileType.Broken;
            
            // Update tile sprite to broken
            SpriteRenderer spriteRenderer = tileObjects[gridPosition.x, gridPosition.y].GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = tileSet.brokenTileSprite;
            
            // Optional: Add some visual effect to indicate the breaking
            StartCoroutine(TileBreakEffect(gridPosition));
            
            // Start timer to auto-repair the broken tile
            StartCoroutine(AutoRepairBrokenTile(gridPosition, brokenTileDuration));
        }
    }
    
    private IEnumerator TileCrackEffect(Vector2Int gridPosition)
    {
        // Get the tile game object
        GameObject tile = tileObjects[gridPosition.x, gridPosition.y];
        if (tile == null) yield break;
        
        // Flash the tile
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        Color originalColor = renderer.color;
        
        // Quick flash effect
        renderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;
        
        // Optional: Add a slight shake effect
        Vector3 originalPosition = tile.transform.position;
        
        for (int i = 0; i < 3; i++)
        {
            // Small random movement
            tile.transform.position = originalPosition + new Vector3(
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f),
                0
            );
            yield return new WaitForSeconds(0.05f);
        }
        
        // Return to original position
        tile.transform.position = originalPosition;
    }
    
    private IEnumerator TileBreakEffect(Vector2Int gridPosition)
    {
        // Get the tile game object
        GameObject tile = tileObjects[gridPosition.x, gridPosition.y];
        if (tile == null) yield break;
        
        // Flash the tile
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        Color originalColor = renderer.color;
        
        // Quick flash effect
        renderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;
        
        // More dramatic shake effect
        Vector3 originalPosition = tile.transform.position;
        
        for (int i = 0; i < 5; i++)
        {
            // Larger random movement
            tile.transform.position = originalPosition + new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                0
            );
            yield return new WaitForSeconds(0.05f);
        }
        
        // Return to original position
        tile.transform.position = originalPosition;
    }
    
    // Auto-repair methods for timed duration
    private IEnumerator AutoRepairCrackedTile(Vector2Int gridPosition, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // Only repair if the tile is still cracked
        if (IsValidGridPosition(gridPosition) && grid[gridPosition.x, gridPosition.y] == TileType.Cracked)
        {
            // Reset to the original tile type
            TileType originalType = originalTileTypes[gridPosition.x, gridPosition.y];
            SetTileType(gridPosition, originalType);
            
            // Add a subtle repair effect
            StartCoroutine(TileRepairEffect(gridPosition));
        }
    }
    
    private IEnumerator AutoRepairBrokenTile(Vector2Int gridPosition, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // Only repair if the tile is still broken
        if (IsValidGridPosition(gridPosition) && grid[gridPosition.x, gridPosition.y] == TileType.Broken)
        {
            // Reset to the original tile type
            TileType originalType = originalTileTypes[gridPosition.x, gridPosition.y];
            SetTileType(gridPosition, originalType);
            
            // Add a subtle repair effect
            StartCoroutine(TileRepairEffect(gridPosition));
        }
    }
    
    private IEnumerator TileRepairEffect(Vector2Int gridPosition)
    {
        // Get the tile game object
        GameObject tile = tileObjects[gridPosition.x, gridPosition.y];
        if (tile == null) yield break;
        
        // Flash the tile
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        Color originalColor = renderer.color;
        
        // Quick flash effect - blue for repair
        renderer.color = Color.cyan;
        yield return new WaitForSeconds(0.1f);
        renderer.color = originalColor;
        
        // Subtle pop effect
        Vector3 originalScale = tile.transform.localScale;
        
        // Quick scale up
        tile.transform.localScale = originalScale * 1.2f;
        yield return new WaitForSeconds(0.1f);
        
        // Return to original scale
        tile.transform.localScale = originalScale;
    }
    
    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * tileSize + gridOffset.x, gridPosition.y * tileSize + gridOffset.y, 0);
    }
    
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - gridOffset.x) / tileSize);
        int y = Mathf.FloorToInt((worldPosition.y - gridOffset.y) / tileSize);
        
        return new Vector2Int(x, y);
    }
}