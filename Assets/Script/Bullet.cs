using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // Bullet properties
    private Vector2 direction;
    private float speed;
    private int damage;
    private TileGrid tileGrid;
    
    // Visual components
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    
    // Animation properties
    [SerializeField] private float fadeOutTime = 0.1f;
    
    // Effect properties
    [SerializeField] private GameObject hitEffectPrefab;
    
    // Internal tracking
    private Vector2Int currentGridPosition;
    private bool isDestroying = false;
    
    private void Awake()
    {
        // Get components if not assigned in inspector
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();
    }
    
    public void Initialize(Vector2 dir, float spd, int dmg, TileGrid grid)
    {
        // Set the bullet properties
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        tileGrid = grid;
        
        // Adjust rotation to match direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Set initial grid position
        currentGridPosition = tileGrid.GetGridPosition(transform.position);
    }
    
    private void Update()
    {
        if (isDestroying) return;
        
        // Move the bullet
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        
        // Check if we've moved to a new grid position
        Vector2Int newGridPosition = tileGrid.GetGridPosition(transform.position);
        
        // If we changed grid cells, check for hits
        if (newGridPosition != currentGridPosition)
        {
            currentGridPosition = newGridPosition;
            
            // Check for enemies at this position (would connect to your enemy system)
            CheckForEnemyHit(currentGridPosition);
            
            // Check if bullet has gone past the rightmost grid
            CheckIfPastRightmostGrid();
        }
    }
    
    private void CheckForEnemyHit(Vector2Int gridPosition)
    {
        // This is where you would implement your enemy detection logic
        // For example, you might have a method to get all enemies at a grid position:
        
        // Example (pseudocode):
        // Enemy enemy = EnemyManager.GetEnemyAt(gridPosition);
        // if (enemy != null)
        // {
        //     enemy.TakeDamage(damage);
        //     SpawnHitEffect();
        //     DestroyBullet();
        // }
        
        // For demonstration purposes, we can detect if we're in an enemy tile area
        if (IsEnemyTilePosition(gridPosition))
        {
            // Check for actual enemy game objects at this position
            // This would need to be implemented based on your enemy system
            
            // For demonstration:
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero, 0.1f, LayerMask.GetMask("Enemy"));
            if (hit.collider != null)
            {
                // Deal damage to the enemy
                // hit.collider.GetComponent<Enemy>()?.TakeDamage(damage);
                
                Debug.Log("Hit enemy at position: " + gridPosition);
                
                // Visual effect
                SpawnHitEffect();
                
                // Destroy bullet
                DestroyBullet();
            }
        }
    }
    
    private void CheckIfPastRightmostGrid()
    {
        // If we're beyond the rightmost enemy grid column, destroy the bullet
        if (currentGridPosition.x >= tileGrid.gridWidth)
        {
            DestroyBullet();
            return;
        }
        
        // Determine if we've passed the rightmost enemy column
        // In your grid setup, enemies occupy the right half of the grid
        int enemyStartColumn = tileGrid.gridWidth / 2;
        
        // If we're past the rightmost column, destroy the bullet
        if (currentGridPosition.x > tileGrid.gridWidth - 1)
        {
            DestroyBullet();
        }
    }
    
    private bool IsEnemyTilePosition(Vector2Int gridPosition)
    {
        // Based on your TileGrid.cs, enemy tiles are on the right half
        return tileGrid.IsValidGridPosition(gridPosition) && 
               gridPosition.x >= tileGrid.gridWidth / 2;
    }
    
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
    }
    
    private void DestroyBullet()
    {
        if (isDestroying) return;
        isDestroying = true;
        
        // Disable collider if present
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Start fade-out animation
        StartCoroutine(FadeOutAndDestroy());
    }
    
    private IEnumerator FadeOutAndDestroy()
    {
        float startAlpha = 1f;
        Color startColor = spriteRenderer.color;
        float elapsedTime = 0;
        
        // Disable trail if present
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
        
        // Fade out the sprite
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutTime);
            
            // Set new color with faded alpha
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }
        
        // Destroy the gameObject
        Destroy(gameObject);
    }
    
    // Helper method that can be called when the bullet collides with enemies
    // This would be used if you're using Unity's collision system
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the collision is with an enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Deal damage to the enemy
            // collision.GetComponent<Enemy>()?.TakeDamage(damage);
            
            // Visual effect
            SpawnHitEffect();
            
            // Destroy bullet
            DestroyBullet();
        }
    }
}