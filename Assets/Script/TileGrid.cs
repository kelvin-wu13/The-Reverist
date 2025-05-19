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
    public Sprite crackedTileSprite;
    public Sprite brokenTileSprite;
}

public class TileGrid : MonoBehaviour
{
    [Header("Grid Setting")] 
    [SerializeField] public int gridWidth = 8;
    [SerializeField] public int gridHeight = 4;
    
    [Header("Tile Size and Spacing")]
    [SerializeField] private float tileWidth = 1f;
    [SerializeField] private float tileHeight = 1f;
    [SerializeField] private float horizontalSpacing = 0.1f;
    [SerializeField] private float verticalSpacing = 0.1f;
    
    [Header("Grid Visualization")]
    [SerializeField] private bool showGridInEditor = true;
    [SerializeField] private Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color playerAreaColor = new Color(0, 1, 0, 0.2f);
    [SerializeField] private Color enemyAreaColor = new Color(1, 0, 0, 0.2f);
    
    [Header("Tile Effect Durations")]
    [SerializeField] private float crackedTileDuration = 1.5f;
    [SerializeField] private float brokenTileDuration = 2.0f;

    [Header("Tile Reference")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TileSet tileSet;
    [SerializeField] private int tileRenderingLayer = 0;
    
    [SerializeField] private Vector2 gridOffset = Vector2.zero;
    public TileType[,] grid;
    private GameObject[,] tileObjects;
    private TileType[,] originalTileTypes;

    // Dictionary to store objects currently in each grid position
    private Dictionary<Vector2Int, List<GameObject>> objectsInTiles = new Dictionary<Vector2Int, List<GameObject>>();

    // Calculated total tile size including spacing
    private float totalTileWidth => tileWidth + horizontalSpacing;
    private float totalTileHeight => tileHeight + verticalSpacing;

    private void Awake()
    {
        InitializeGrid();
        InitializeObjectsInTilesDict();
    }

    private void InitializeObjectsInTilesDict()
    {
        objectsInTiles.Clear();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                objectsInTiles[new Vector2Int(x, y)] = new List<GameObject>();
            }
        }
    }
    
    // Add these public getter methods for tile size
    public float GetTileWidth()
    {
        return tileWidth;
    }
    
    public float GetTileHeight()
    {
        return tileHeight;
    }

    private void OnValidate()
    {
        // Ensure grid dimensions are always positive
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);

        // Ensure tile dimensions are always positive
        tileWidth = Mathf.Max(0.1f, tileWidth);
        tileHeight = Mathf.Max(0.1f, tileHeight);

        // Spacing can be zero but not negative
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);

        // If the grid is already initialized in play mode, update it
        if (Application.isPlaying && grid != null)
        {
            UpdateGridLayout();
        }
    }
    
    private void UpdateGridLayout()
    {
        // Store the current grid state
        TileType[,] oldGrid = grid;
        GameObject[,] oldTileObjects = tileObjects;
        TileType[,] oldOriginalTypes = originalTileTypes;
        
        int oldWidth = oldGrid.GetLength(0);
        int oldHeight = oldGrid.GetLength(1);
        
        // Initialize with new dimensions
        grid = new TileType[gridWidth, gridHeight];
        tileObjects = new GameObject[gridWidth, gridHeight];
        originalTileTypes = new TileType[gridWidth, gridHeight];
        
        // Update the objectsInTiles dictionary for the new dimensions
        InitializeObjectsInTilesDict();
        
        // Copy over the old data where possible
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (x < oldWidth && y < oldHeight)
                {
                    grid[x, y] = oldGrid[x, y];
                    originalTileTypes[x, y] = oldOriginalTypes[x, y];
                    
                    if (oldTileObjects[x, y] != null)
                    {
                        // Update position and scale of existing tiles
                        tileObjects[x, y] = oldTileObjects[x, y];
                        UpdateTileTransform(new Vector2Int(x, y));
                    }
                    else
                    {
                        // Create tile if it doesn't exist
                        CreateTile(new Vector2Int(x, y));
                    }
                }
                else
                {
                    // Create new tiles for expanded grid
                    CreateTile(new Vector2Int(x, y));
                }
            }
        }
        
        // Clean up any tiles that are now out of bounds
        for (int x = 0; x < oldWidth; x++)
        {
            for (int y = 0; y < oldHeight; y++)
            {
                if (x >= gridWidth || y >= gridHeight)
                {
                    if (oldTileObjects[x, y] != null)
                    {
                        Destroy(oldTileObjects[x, y]);
                    }
                }
            }
        }
    }
    
    private void InitializeGrid()
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
                CreateTile(new Vector2Int(x, y));
            }
        }
    }
    
    private void CreateTile(Vector2Int position)
    {
        Vector3 worldPosition = GetWorldPosition(position);
        GameObject tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, transform);
        tile.name = $"Tile_{position.x}_{position.y}";
        
        // Scale the tile to match the specified width and height
        tile.transform.localScale = new Vector3(tileWidth, tileHeight, 1f);
        
        // Set the rendering order to be behind other elements
        SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = tile.AddComponent<SpriteRenderer>();
        }
        spriteRenderer.sortingOrder = tileRenderingLayer;
        
        // All tiles start as empty
        grid[position.x, position.y] = TileType.Empty;
        originalTileTypes[position.x, position.y] = TileType.Empty;
        spriteRenderer.sprite = tileSet.emptyTileSprite;
        
        tileObjects[position.x, position.y] = tile;
    }
    
    private void UpdateTileTransform(Vector2Int position)
    {
        GameObject tile = tileObjects[position.x, position.y];
        if (tile != null)
        {
            // Update position and scale
            tile.transform.position = GetWorldPosition(position);
            tile.transform.localScale = new Vector3(tileWidth, tileHeight, 1f);
            
            // Update the sorting order
            SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = tileRenderingLayer;
            }
        }
    }
    
    private void SetupInitialPositions()
    {
        // Set left half for player (first half of columns)
        for (int x = 0; x < gridWidth / 2; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                SetTileType(new Vector2Int(x, y), TileType.Player);
            }
        }
        
        // Set right half for enemy (second half of columns)
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
        // Calculate world position with spacing
        float x = gridPosition.x * totalTileWidth + gridOffset.x;
        float y = gridPosition.y * totalTileHeight + gridOffset.y;
        return new Vector3(x, y, 0);
    }
    
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        // Convert world position to grid position accounting for spacing
        int x = Mathf.FloorToInt((worldPosition.x - gridOffset.x) / totalTileWidth);
        int y = Mathf.FloorToInt((worldPosition.y - gridOffset.y) / totalTileHeight);
        
        return new Vector2Int(x, y);
    }
    
    // Methods for managing objects in tiles
    public void RegisterObjectInTile(GameObject obj, Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
        {
            if (!objectsInTiles.ContainsKey(gridPosition))
            {
                objectsInTiles[gridPosition] = new List<GameObject>();
            }
            
            if (!objectsInTiles[gridPosition].Contains(obj))
            {
                objectsInTiles[gridPosition].Add(obj);
            }
        }
    }
    
    public void UnregisterObjectFromTile(GameObject obj, Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition) && objectsInTiles.ContainsKey(gridPosition))
        {
            objectsInTiles[gridPosition].Remove(obj);
        }
    }
    
    public List<GameObject> GetObjectsInTile(Vector2Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition) && objectsInTiles.ContainsKey(gridPosition))
        {
            return new List<GameObject>(objectsInTiles[gridPosition]);
        }
        return new List<GameObject>();
    }
    
    public bool HasObjectWithTag(Vector2Int gridPosition, string tag)
    {
        if (IsValidGridPosition(gridPosition) && objectsInTiles.ContainsKey(gridPosition))
        {
            foreach (GameObject obj in objectsInTiles[gridPosition])
            {
                if (obj != null && obj.CompareTag(tag))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public List<GameObject> GetObjectsWithTagInTile(Vector2Int gridPosition, string tag)
    {
        List<GameObject> result = new List<GameObject>();
        
        if (IsValidGridPosition(gridPosition) && objectsInTiles.ContainsKey(gridPosition))
        {
            foreach (GameObject obj in objectsInTiles[gridPosition])
            {
                if (obj != null && obj.CompareTag(tag))
                {
                    result.Add(obj);
                }
            }
        }
        
        return result;
    }
    
    // Draw grid lines in the scene view
    private void OnDrawGizmos()
    {
        if (!showGridInEditor) return;
        
        Vector3 startPos = transform.position + new Vector3(gridOffset.x, gridOffset.y, 0);
        
        // Draw horizontal grid lines
        for (int y = 0; y <= gridHeight; y++)
        {
            float yPos = y * totalTileHeight;
            Vector3 lineStart = startPos + new Vector3(0, yPos, 0);
            Vector3 lineEnd = startPos + new Vector3(gridWidth * totalTileWidth, yPos, 0);
            Gizmos.color = gridLineColor;
            Gizmos.DrawLine(lineStart, lineEnd);
        }
        
        // Draw vertical grid lines
        for (int x = 0; x <= gridWidth; x++)
        {
            float xPos = x * totalTileWidth;
            Vector3 lineStart = startPos + new Vector3(xPos, 0, 0);
            Vector3 lineEnd = startPos + new Vector3(xPos, gridHeight * totalTileHeight, 0);
            Gizmos.color = gridLineColor;
            Gizmos.DrawLine(lineStart, lineEnd);
        }
        
        // Draw player area and enemy area if not in play mode
        if (!Application.isPlaying)
        {
            // Player area (left half)
            Vector3 playerAreaStart = startPos;
            Vector3 playerAreaSize = new Vector3(gridWidth * totalTileWidth / 2, gridHeight * totalTileHeight, 0.1f);
            Gizmos.color = playerAreaColor;
            Gizmos.DrawCube(playerAreaStart + playerAreaSize / 2, playerAreaSize);
            
            // Enemy area (right half)
            Vector3 enemyAreaStart = startPos + new Vector3(gridWidth * totalTileWidth / 2, 0, 0);
            Vector3 enemyAreaSize = new Vector3(gridWidth * totalTileWidth / 2, gridHeight * totalTileHeight, 0.1f);
            Gizmos.color = enemyAreaColor;
            Gizmos.DrawCube(enemyAreaStart + enemyAreaSize / 2, enemyAreaSize);
        }
        
        // Draw tile types if in play mode
        if (Application.isPlaying && grid != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 tilePos = GetWorldPosition(new Vector2Int(x, y));
                    Vector3 tileCenter = tilePos + new Vector3(tileWidth / 2, tileHeight / 2, 0);
                    Vector3 tileSize3D = new Vector3(tileWidth * 0.8f, tileHeight * 0.8f, 0.1f);
                    
                    // Color based on tile type
                    switch (grid[x, y])
                    {
                        case TileType.Player:
                            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green
                            break;
                        case TileType.Enemy:
                            Gizmos.color = new Color(1, 0, 0, 0.3f); // Red
                            break;
                        case TileType.Empty:
                            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.3f); // Gray
                            break;
                        case TileType.Cracked:
                            Gizmos.color = new Color(1, 1, 0, 0.3f); // Yellow
                            break;
                        case TileType.Broken:
                            Gizmos.color = new Color(0, 0, 0, 0.3f); // Black
                            break;
                    }
                    
                    Gizmos.DrawCube(tileCenter, tileSize3D);
                }
            }
        }
    }
}