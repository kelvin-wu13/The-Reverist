using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IonBolt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private ParticleSystem trailEffect;
    
    // Direction the bolt will travel
    private Vector2 direction;
    private TileGrid tileGrid;
    private Vector2Int currentGridPosition;
    
    public void Initialize(Vector2 startPosition, Vector2 targetPosition, TileGrid grid)
    {
        transform.position = startPosition;
        tileGrid = grid;
        
        // Calculate direction from start to target
        direction = (targetPosition - startPosition).normalized;
        
        // Rotate the bolt to face the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Start the trail effect if present
        if (trailEffect != null)
        {
            trailEffect.Play();
        }
        
        // Destroy after lifetime if nothing is hit
        Destroy(gameObject, lifetime);
    }
    
    private void Update()
    {
        // Move the bolt
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
        
        // Check current grid position
        Vector2Int newGridPosition = tileGrid.GetGridPosition(transform.position);
        
        // Only check for collision when moving to a new grid cell
        if (newGridPosition != currentGridPosition)
        {
            currentGridPosition = newGridPosition;
            CheckCollision();
        }
    }
    
    private void CheckCollision()
    {
        // Exit if position is invalid
        if (!tileGrid.IsValidGridPosition(currentGridPosition))
        {
            Explode();
            return;
        }
        
        // Check if we've hit an enemy tile
        if (tileGrid.grid[currentGridPosition.x, currentGridPosition.y] == TileType.Enemy)
        {
            Explode();
        }
    }
    
    private void Explode()
    {
        // Create explosion effect
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        
        // Apply damage to current tile and surrounding tiles
        DamageArea();
        
        // Destroy the bolt
        Destroy(gameObject);
    }
    
    private void DamageArea()
    {
        // Damage center tile (where the bolt hit)
        DamageTile(currentGridPosition);
        
        // Damage surrounding tiles (up, down, left, right)
        DamageTile(currentGridPosition + Vector2Int.up);
        DamageTile(currentGridPosition + Vector2Int.down);
        DamageTile(currentGridPosition + Vector2Int.left);
        DamageTile(currentGridPosition + Vector2Int.right);
    }
    
    private void DamageTile(Vector2Int position)
    {
        // Make sure the position is valid
        if (!tileGrid.IsValidGridPosition(position))
        {
            return;
        }
        
        // If there's an enemy on this tile, damage it
        // (You would need to add enemy detection/damage logic here)
        
        // For now, we'll just set enemy tiles to empty to show damage
        if (tileGrid.grid[position.x, position.y] == TileType.Enemy)
        {
            tileGrid.SetTileType(position, TileType.Empty);
            
            // You could also spawn damage effects, trigger enemy hurt animations, etc.
        }
    }
    
    private void OnDrawGizmos()
    {
        // Visualize the bolt path in the editor
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(direction * 3));
        }
    }
}