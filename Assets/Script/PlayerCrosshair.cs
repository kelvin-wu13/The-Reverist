using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrosshair : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private TileGrid tileGrid;
    [SerializeField] private GameObject crosshairVisual;
    
    [Header("Settings")]
    [SerializeField] private int distanceFromPlayer = 4;
    
    // Direction tracking
    private Vector2Int playerFacingDirection = Vector2Int.right; // Default facing right
    private Vector2Int playerGridPosition;
    private Vector2Int targetGridPosition;
    private SpriteRenderer crosshairRenderer;
    
    // Skill freeze state
    private bool isFrozen = false;
    private Vector2Int frozenPosition;
    
    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.Log("PlayerCrosshair: Player Transform reference is missing!");
        }
        
        if (tileGrid == null)
        {
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.Log("PlayerCrosshair: Could not find TileGrid in the scene!");
            }
        }
        
        else
        {
            crosshairRenderer = crosshairVisual.GetComponent<SpriteRenderer>();
            if (crosshairRenderer == null)
            {
                crosshairRenderer = crosshairVisual.AddComponent<SpriteRenderer>();
            }
        }
        
        // Initialize positions
        UpdatePositions();
    }
    
    private void Update()
    {
        UpdatePositions();
    }
    
    private void UpdatePositions()
    {
        // If frozen, don't update positions
        if (isFrozen)
        {
            // Keep crosshair at frozen position
            Vector3 frozenTileWorldPos = tileGrid.GetWorldPosition(frozenPosition);
            Vector3 frozenTileCenterPos = frozenTileWorldPos + new Vector3(-0.05f, -0.01f, 0);
            transform.position = frozenTileCenterPos;
            return;
        }
        
        // Get current player grid position
        playerGridPosition = tileGrid.GetGridPosition(playerTransform.position);
        
        targetGridPosition = playerGridPosition + (playerFacingDirection * distanceFromPlayer);

        // Clamp to grid boundaries
        targetGridPosition.x = Mathf.Clamp(targetGridPosition.x, 0, tileGrid.gridWidth - 1);
        targetGridPosition.y = Mathf.Clamp(targetGridPosition.y, 0, tileGrid.gridHeight - 1);
        
        // Update crosshair world position
        // Get the base tile position and add 0.5 to both X and Y to center within the tile
        Vector3 tileWorldPos = tileGrid.GetWorldPosition(targetGridPosition);
        Vector3 tileCenterPos = tileWorldPos + new Vector3(-0.05f, -0.01f, 0);
        transform.position = tileCenterPos;
    }
    
    private Sprite CreateCrosshairSprite()
    {
        // Create a simple crosshair texture
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        
        Color transparent = new Color(0, 0, 0, 0);
        Color crosshairPixel = Color.white;
        
        // Fill with transparent
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }
        
        // Draw crosshair
        int thickness = 2;
        int border = 2;
        
        // Outer square
        for (int x = border; x < size - border; x++)
        {
            // Top and bottom borders
            for (int t = 0; t < thickness; t++)
            {
                texture.SetPixel(x, border + t, crosshairPixel);
                texture.SetPixel(x, size - border - t - 1, crosshairPixel);
            }
        }
        
        for (int y = border; y < size - border; y++)
        {
            // Left and right borders
            for (int t = 0; t < thickness; t++)
            {
                texture.SetPixel(border + t, y, crosshairPixel);
                texture.SetPixel(size - border - t - 1, y, crosshairPixel);
            }
        }
        
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return sprite;
    }
    
    // Public methods for other scripts to access targeting information
    
    public Vector2Int GetTargetGridPosition()
    {
        return isFrozen ? frozenPosition : targetGridPosition;
    }
    
    public Vector3 GetTargetWorldPosition()
    {
        // Return the center of the targeted tile
        Vector2Int currentTarget = isFrozen ? frozenPosition : targetGridPosition;
        return tileGrid.GetWorldPosition(currentTarget) + new Vector3(-5f, 0f, 0);
    }
    
    public bool IsCellTargeted(Vector2Int cellPosition)
    {
        Vector2Int currentTarget = isFrozen ? frozenPosition : targetGridPosition;
        return cellPosition == currentTarget;
    }
    
    // Methods to control crosshair freezing during skills
    public void FreezeCrosshair()
    {
        isFrozen = true;
        frozenPosition = targetGridPosition;
        Debug.Log($"PlayerCrosshair: Frozen at position {frozenPosition}");
    }
    
    public void UnfreezeCrosshair()
    {
        isFrozen = false;
        Debug.Log("PlayerCrosshair: Unfrozen");
        // Force an immediate position update
        UpdatePositions();
    }
    
    public bool IsFrozen()
    {
        return isFrozen;
    }
    
    // Visual debugging
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && tileGrid != null)
        {
            Vector2Int currentTarget = isFrozen ? frozenPosition : targetGridPosition;
            
            // Change color if frozen
            Gizmos.color = isFrozen ? new Color(1f, 0f, 0f, 0.5f) : new Color(1f, 1f, 0f, 0.5f);
            Vector3 targetPos = tileGrid.GetWorldPosition(currentTarget);
            Vector3 targetCenter = targetPos + new Vector3(0.5f, 0.5f, 0);
            Gizmos.DrawCube(targetCenter, new Vector3(1, 1, 0.1f));
            
            // Draw a line from player to crosshair
            if (playerTransform != null)
            {
                Gizmos.color = isFrozen ? new Color(1f, 0f, 0f, 0.2f) : new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawLine(
                    playerTransform.position,
                    targetCenter
                );
            }
        }
    }
    
    // Method to update the player's facing direction (can be called by input or movement scripts)
    public void SetPlayerFacingDirection(Vector2Int newDirection)
    {
        if (newDirection != Vector2Int.zero && !isFrozen)
        {
            playerFacingDirection = newDirection;
            UpdatePositions();
        }
    }
}