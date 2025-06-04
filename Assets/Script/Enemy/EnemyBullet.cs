using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    // Bullet properties
    private Vector2 direction;
    private float speed;
    private int damage;
    private TileGrid tileGrid;
    
    // Animation properties
    [SerializeField] private float fadeOutTime = 0.1f;
    
    // Effect properties
    [SerializeField] private GameObject hitEffectPrefab;
    
    // Internal tracking
    private Vector2Int currentGridPosition;
    private bool isDestroying = false;
    
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
            
            // Check for player hit at this position
            CheckForPlayerHit(currentGridPosition);
            
            // Check if bullet has gone past the leftmost grid
            CheckIfPastLeftmostGrid();
        }
    }
    
    private void CheckForPlayerHit(Vector2Int gridPosition)
    {
        // Only check for player hits - not other enemies
        if (IsPlayerTilePosition(gridPosition))
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.zero, 0.1f, LayerMask.GetMask("Player"));
            if (hit.collider != null)
            {
                // Only damage objects tagged as "Player"
                if (hit.collider.CompareTag("Player"))
                {
                    // Deal damage to the player
                    hit.collider.GetComponent<PlayerStats>()?.TakeDamage(damage);
                    
                    Debug.Log("Hit player at position: " + gridPosition);
                    
                    // Visual effect
                    SpawnHitEffect();
                    
                    // Destroy bullet
                    DestroyBullet();
                }
            }
        }
    }
    
    private void CheckIfPastLeftmostGrid()
    {
        // If we're beyond the leftmost player grid column, destroy the bullet
        if (currentGridPosition.x < 0)
        {
            DestroyBullet();
            return;
        }
        
        // Determine if we've passed the leftmost enemy column
        // In your grid setup, players occupy the left half of the grid
        int playerEndColumn = tileGrid.gridWidth / 2 - 1;
        
        // If we're past the leftmost column, destroy the bullet
        if (currentGridPosition.x < 0)
        {
            DestroyBullet();
        }
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float startAlpha = 1f;
        float elapsedTime = 0;
        
        // Fade out the sprite
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutTime);
            
            
            yield return null;
        }
        
        // Destroy the gameObject
        Destroy(gameObject);
    }
    
    private bool IsPlayerTilePosition(Vector2Int gridPosition)
    {
        // Based on your TileGrid.cs, player tiles are on the left half
        return tileGrid.IsValidGridPosition(gridPosition) && 
               gridPosition.x < tileGrid.gridWidth / 2;
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
    
    // Helper method that can be called when the bullet collides with player
    // This would be used if you're using Unity's collision system
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only check for collisions with player, not other enemies
        if (collision.gameObject.CompareTag("Player"))
        {
            // Deal damage to the player
            collision.GetComponent<PlayerStats>()?.TakeDamage(damage);
            
            // Visual effect
            SpawnHitEffect();
            
            // Destroy bullet
            DestroyBullet();
        }
    }
}