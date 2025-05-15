using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class QQSkill : Skill
    {
        [Header("QQ Skill Settings")]
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private int explosionTileRadius = 1; // Number of tiles in each direction from center
        [SerializeField] private int damage = 20;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private float explosionEffectDuration = 1.0f;
        
        [Header("Grid-Based Settings")]
        [SerializeField] private bool useGridBasedShooting = true; // Toggle for grid-based or direct shooting
        
        private GameObject activeProjectile;
        private bool isProjectileFired = false;
        private Vector3 direction;
        private TileGrid tileGrid;
        private Vector2Int currentGridPosition;
        private PlayerMovement playerMovement;

        private void Awake()
        {
            FindTileGrid();
            FindPlayerMovement();
        }

        private void FindTileGrid()
        {
            if (tileGrid == null)
            {
                // Find the TileGrid in the scene
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("QQSkill: Could not find TileGrid in the scene!");
                }
            }
        }
        
        private void FindPlayerMovement()
        {
            if (playerMovement == null)
            {
                playerMovement = FindObjectOfType<PlayerMovement>();
                if (playerMovement == null)
                {
                    Debug.LogWarning("QQSkill: Could not find PlayerMovement in the scene. Grid-based targeting may not work correctly.");
                }
            }
        }

        // Override the parent class's method
        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Find the grid
            FindTileGrid();

            if (useGridBasedShooting && playerMovement != null)
            {
                // Use grid-based shooting (shoot along the grid row)
                FireGridBasedProjectile(casterTransform);
            }
            else
            {
                // Use the original targeting method (shoot toward a specific target position)
                FireProjectile(casterTransform.position, targetPosition);
            }
        }
        
        private void FireGridBasedProjectile(Transform casterTransform)
        {
            // Get the player's current grid position
            Vector2Int playerGridPos = playerMovement.GetCurrentGridPosition();
            
            // Calculate spawn position - on the right edge of the current tile
            Vector3 tileWorldPos = tileGrid.GetWorldPosition(playerGridPos);
            float tileWidth = tileGrid.GetTileWidth();
            Vector3 spawnPosition = new Vector3(
                tileWorldPos.x + tileWidth,
                tileWorldPos.y + (tileGrid.GetTileHeight() / 2),
                0
            );
            
            // Always shoot to the right
            direction = Vector3.right;
            
            // Instantiate projectile
            activeProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            
            // Set the projectile's forward direction to match the firing direction
            activeProjectile.transform.up = direction;
            
            isProjectileFired = true;
            
            // Set initial grid position for the projectile
            currentGridPosition = tileGrid.GetGridPosition(activeProjectile.transform.position);
            
            Debug.Log($"QQ Skill: Fired grid-based projectile from player tile {playerGridPos}");
        }

        private void FireProjectile(Vector3 startPosition, Vector2Int targetGridPosition)
        {
            // Ensure TileGrid is available
            if (tileGrid == null)
            {
                Debug.LogError("QQSkill: TileGrid reference is null when trying to fire projectile!");
                return;
            }

            // Convert target grid position to world position
            Vector3 targetPosition = tileGrid.GetWorldPosition(targetGridPosition);
            
            // Calculate direction
            direction = (targetPosition - startPosition).normalized;
            
            // Instantiate projectile
            activeProjectile = Instantiate(projectilePrefab, startPosition, Quaternion.identity);
            
            // Set the projectile's forward direction to match the firing direction
            activeProjectile.transform.up = direction;
            
            isProjectileFired = true;

            // Set initial grid position for the projectile
            currentGridPosition = tileGrid.GetGridPosition(activeProjectile.transform.position);
            
            Debug.Log($"QQ Skill: Fired projectile toward {targetGridPosition}");
        }
        
        private void Update()
        {
            if (isProjectileFired && activeProjectile != null)
            {
                // Move projectile
                activeProjectile.transform.position += direction * projectileSpeed * Time.deltaTime;
                
                // Get the current grid position
                Vector2Int newGridPosition = tileGrid.GetGridPosition(activeProjectile.transform.position);
                
                // Check if we've moved to a new grid cell
                if (newGridPosition != currentGridPosition)
                {
                    currentGridPosition = newGridPosition;
                    
                    // Check if the projectile is in enemy territory
                    if (IsEnemyTilePosition(currentGridPosition))
                    {
                        // Check for enemies at this position
                        CheckForEnemyHit(currentGridPosition);
                    }
                    
                    // Check if projectile has gone past the rightmost grid
                    if (currentGridPosition.x >= tileGrid.gridWidth)
                    {
                        Destroy(activeProjectile);
                        isProjectileFired = false;
                    }
                }
                
                // Destroy projectile if it goes too far (failsafe)
                if (Vector3.Distance(transform.position, activeProjectile.transform.position) > 50f)
                {
                    Destroy(activeProjectile);
                    isProjectileFired = false;
                }
            }
        }
        
        private void CheckForEnemyHit(Vector2Int gridPosition)
        {
            // Get world position of this tile's center
            Vector3 tileWorldPos = tileGrid.GetWorldPosition(gridPosition);
            
            // Get all colliders at this tile position
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(tileWorldPos, 0.4f);
            
            foreach (Collider2D collider in hitColliders)
            {
                if (collider.CompareTag("Enemy"))
                {
                    // Hit an enemy, trigger explosion at its grid position
                    ExplodeAtGridPosition(gridPosition);
                    Destroy(activeProjectile);
                    isProjectileFired = false;
                    break;
                }
            }
        }
        
        private bool IsEnemyTilePosition(Vector2Int gridPosition)
        {
            // Based on your TileGrid.cs, enemy tiles are on the right half
            return tileGrid.IsValidGridPosition(gridPosition) && 
                   gridPosition.x >= tileGrid.gridWidth / 2;
        }
        
        private void ExplodeAtGridPosition(Vector2Int centerGridPosition)
        {
            Debug.Log($"QQ Skill: Explosion at grid position {centerGridPosition}");
            
            // Create visual explosion effect at world position
            Vector3 worldPosition = tileGrid.GetWorldPosition(centerGridPosition);
            
            //Create and manage explosion
            GameObject explosionEffect = null;

            // Create basic explosion effect if we don't have the asset yet
            if (explosionEffectPrefab == null)
            {
                explosionEffect = CreateBasicExplosionEffect(worldPosition);
            }
            else
            {
                explosionEffect = Instantiate(explosionEffectPrefab, worldPosition, Quaternion.identity);
                Destroy(explosionEffect, explosionEffectDuration);
            }
            
            // Get all tiles within the explosion range (including the center tile)
            List<Vector2Int> affectedTiles = GetAffectedTiles(centerGridPosition, explosionTileRadius);
            
            // Find enemies in each affected tile
            foreach (Vector2Int tilePos in affectedTiles)
            {
                // Get world position of this tile's center
                Vector3 tileWorldPos = tileGrid.GetWorldPosition(tilePos);
                
                // Get all colliders at this tile position (using a small radius to detect objects in this tile)
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(tileWorldPos, 0.4f);
                
                foreach (Collider2D collider in hitColliders)
                {
                    if (collider.CompareTag("Enemy"))
                    {
                        Enemy enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(damage);
                            Debug.Log($"QQ Skill: Dealt {damage} damage to enemy at tile {tilePos}");
                        }
                    }
                }
            }
        }
        
        private List<Vector2Int> GetAffectedTiles(Vector2Int centerPos, int radius)
        {
            List<Vector2Int> affectedTiles = new List<Vector2Int>();
            
            // Loop through the surrounding tiles within the specified radius
            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                for (int yOffset = -radius; yOffset <= radius; yOffset++)
                {
                    Vector2Int tilePos = new Vector2Int(centerPos.x + xOffset, centerPos.y + yOffset);
                    
                    // Check if this position is within the grid bounds
                    if (tileGrid.IsValidGridPosition(tilePos))
                    {
                        affectedTiles.Add(tilePos);
                    }
                }
            }
            
            return affectedTiles;
        }
        
        private GameObject CreateBasicExplosionEffect(Vector3 position)
        {
            // Create a simple explosion effect
            GameObject explosion = new GameObject("ExplosionEffect");
            explosion.transform.position = position;
            
            // Add a simple particle system
            ParticleSystem particles = explosion.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = Color.red;
            main.startSize = 3f;
            main.startSpeed = 5f;
            main.startLifetime = 0.5f;
            main.duration = 0.5f;
            
            // Emission module
            var emission = particles.emission;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst(0f, 30);
            emission.SetBurst(0, burst);
            
            // Shape module
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            
            float safeRadius = explosionTileRadius;
            if (tileGrid != null)
            {
                safeRadius = explosionTileRadius * tileGrid.GetTileWidth();
            }
            shape.radius = safeRadius;
            
            // Auto-destroy after effect completes
            Destroy(explosion, explosionEffectDuration);
            
            return explosion;
        }
        
        // Visualize the explosion radius in the Scene view
        private void OnDrawGizmosSelected()
        {
            if (tileGrid == null) tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid != null)
            {
                Gizmos.color = Color.red;
                
                // Get the center position of this skill's object
                Vector2Int centerGridPos = tileGrid.GetGridPosition(transform.position);
                
                // Draw wire cube for each affected tile
                List<Vector2Int> affectedTiles = GetAffectedTiles(centerGridPos, explosionTileRadius);
                foreach (Vector2Int tilePos in affectedTiles)
                {
                    Vector3 tileWorldPos = tileGrid.GetWorldPosition(tilePos);
                    Gizmos.DrawWireCube(tileWorldPos, new Vector3(1f, 1f, 0.1f));
                }
            }
        }
    }
}