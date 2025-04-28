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
    [SerializeField] private Color crosshairColor = Color.yellow;
    
    // Direction tracking
    private Vector2Int playerFacingDirection = Vector2Int.right; // Default facing right
    private Vector2Int playerGridPosition;
    private Vector2Int targetGridPosition;
    private SpriteRenderer crosshairRenderer;
    
    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("PlayerCrosshair: Player Transform reference is missing!");
        }
        
        if (tileGrid == null)
        {
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("PlayerCrosshair: Could not find TileGrid in the scene!");
            }
        }
        
        if (crosshairVisual == null)
        {
            // Create a visual representation for the crosshair
            crosshairVisual = new GameObject("CrosshairVisual");
            crosshairVisual.transform.SetParent(transform);
            crosshairVisual.transform.localPosition = Vector3.zero;
            
            crosshairRenderer = crosshairVisual.AddComponent<SpriteRenderer>();
            crosshairRenderer.sprite = CreateCrosshairSprite();
            crosshairRenderer.color = crosshairColor;
            crosshairRenderer.sortingOrder = 10; // Make sure it's visible above tiles
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
        UpdateCrosshairVisual();
    }
    
    private void UpdatePositions()
    {
        // Get current player grid position
        playerGridPosition = tileGrid.GetGridPosition(playerTransform.position);
        
        targetGridPosition = playerGridPosition + (playerFacingDirection * distanceFromPlayer);

        // Clamp to grid boundaries
        targetGridPosition.x = Mathf.Clamp(targetGridPosition.x, 0, tileGrid.gridWidth - 1);
        targetGridPosition.y = Mathf.Clamp(targetGridPosition.y, 0, tileGrid.gridHeight - 1);
        
        // Update crosshair world position
        transform.position = tileGrid.GetWorldPosition(targetGridPosition) + new Vector3(0.5f, 0.5f, 0); // Center in the tile
    }
    
    
    private void UpdateCrosshairVisual()
    {
        // Add a pulsing effect to make the crosshair more visible
        if (crosshairRenderer != null)
        {
            Color color = crosshairRenderer.color;
            crosshairRenderer.color = color;
        }
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
        return targetGridPosition;
    }
    
    public Vector3 GetTargetWorldPosition()
    {
        return tileGrid.GetWorldPosition(targetGridPosition) + new Vector3(0.5f, 0.5f, 0);
    }
    
    public bool IsCellTargeted(Vector2Int cellPosition)
    {
        return cellPosition == targetGridPosition;
    }
    
    // Visual debugging
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && tileGrid != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Vector3 targetPos = tileGrid.GetWorldPosition(targetGridPosition);
            Gizmos.DrawCube(targetPos + new Vector3(0.5f, 0.5f, 0), new Vector3(1, 1, 0.1f));
            
            // Draw a line from player to crosshair
            if (playerTransform != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawLine(
                    playerTransform.position,
                    targetPos + new Vector3(0.5f, 0.5f, 0)
                );
            }
        }
    }
}