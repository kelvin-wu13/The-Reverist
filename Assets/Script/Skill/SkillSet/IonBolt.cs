using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class IonBolt : Skill
    {
        [Header("IonBolt Skill Settings")]
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private int explosionTileRadius = 1; // Number of tiles in each direction from center
        [SerializeField] private int damage = 20; // Direct hit damage
        [SerializeField] private int explosionDamage = 15; // Explosion area damage
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private float explosionEffectDuration = 1.0f;
        [SerializeField] public float manaCost = 1.5f; // Mana cost for casting this skill
        [SerializeField] public float cooldownDuration = 1.5f; // Cooldown duration in seconds

        [Header("Manual Speed Adjustment")]
        [SerializeField] private float speedMultiplier = 1.0f; // Manual speed adjustment multiplier
        
        private GameObject activeProjectile;
        private bool isProjectileFired = false;
        private bool isOnCooldown = false;
        private Vector2Int currentGridPosition;
        private Vector2 direction = Vector2.right; // Direction for movement
        private TileGrid tileGrid;
        private PlayerMovement playerMovement;
        private PlayerStats playerStats;
        private PlayerShoot playerShoot;

        private void Awake()
        {
            FindReferences();
        }

        private void Update()
        {
            if (isProjectileFired && activeProjectile != null)
            {
                // Move the projectile using the same method as Bullet.cs
                float adjustedSpeed = projectileSpeed * speedMultiplier;
                activeProjectile.transform.Translate(direction * adjustedSpeed * Time.deltaTime, Space.World);
                
                // Use the same center alignment adjustment as Bullet.cs
                float tileCenterYOffset = tileGrid.GetTileHeight() * 0.5f;
                Vector3 adjusted = activeProjectile.transform.position - new Vector3(0, tileCenterYOffset, 0);
                Vector2Int newGridPosition = tileGrid.GetGridPosition(adjusted);
                
                // If we changed grid cells, check for hits
                if (newGridPosition != currentGridPosition)
                {
                    currentGridPosition = newGridPosition;
                    
                    // Check for enemies at this position
                    CheckForEnemyHit(currentGridPosition);
                    
                    // Check if projectile has gone past the rightmost grid
                    CheckIfPastRightmostGrid();
                }
            }
        }

        private void FindReferences()
        {
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("IonBolt: Could not find TileGrid in the scene!");
                }
            }
            
            if (playerMovement == null)
            {
                playerMovement = FindObjectOfType<PlayerMovement>();
                if (playerMovement == null)
                {
                    Debug.LogWarning("IonBolt: Could not find PlayerMovement in the scene.");
                }
            }
            
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
                if (playerStats == null)
                {
                    Debug.LogWarning("IonBolt: Could not find PlayerStats in the scene.");
                }
            }

            if (playerShoot == null)
            {
                playerShoot = FindObjectOfType<PlayerShoot>();
                if (playerShoot == null)
                {
                    Debug.LogWarning("IonBolt: Could not find PlayerShoot in the scene.");
                }
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            FindReferences();

            // Check if skill is on cooldown
            if (isOnCooldown)
            {
                Debug.Log("IonBolt is on cooldown!");
                return;
            }

            // Check if player has enough mana
            if (playerStats == null || playerStats.TryUseMana(Mathf.CeilToInt(manaCost)))
            {
                FireProjectileFromSpawnPoint();
                StartCoroutine(StartCooldown());
            }
            else
            {
                Debug.Log("IonBolt: Not enough mana to cast!");
            }
        }
        
        private IEnumerator StartCooldown()
        {
            isOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            isOnCooldown = false;
            Debug.Log("IonBolt cooldown finished!");
        }
        
        private void FireProjectileFromSpawnPoint()
        {
            // Get the bullet spawn point from PlayerShoot (same as Bullet.cs)
            Transform bulletSpawnPoint = null;
            if (playerShoot != null)
            {
                bulletSpawnPoint = playerShoot.GetBulletSpawnPoint();
            }

            // Fallback to player transform if PlayerShoot not found
            if (bulletSpawnPoint == null)
            {
                bulletSpawnPoint = transform;
            }

            Vector3 spawnPosition = bulletSpawnPoint.position;

            // Instantiate projectile at the spawn point
            activeProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

            AudioManager.Instance?.PlayIonBoltSFX();
            
            // Set rotation to match direction (same as Bullet.cs)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            activeProjectile.transform.rotation = Quaternion.Euler(0, 0, angle);

            isProjectileFired = true;

            // Set initial grid position for the projectile using the same adjustment as Bullet.cs
            float tileCenterYOffset = tileGrid.GetTileHeight() * 0.5f;
            Vector3 adjusted = activeProjectile.transform.position - new Vector3(0, tileCenterYOffset, 0);
            currentGridPosition = tileGrid.GetGridPosition(adjusted);

            Debug.Log($"IonBolt: Fired projectile from spawn point at speed {projectileSpeed * speedMultiplier}");
        }

        private void CheckForEnemyHit(Vector2Int gridPosition)
        {
            if (!IsEnemyTilePosition(gridPosition)) return;

            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                // Use the same center alignment adjustment as Bullet.cs
                Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);
                
                if (enemyGridPos == gridPosition)
                {
                    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                    if (enemyComponent != null)
                    {                        
                        enemyComponent.TakeDamage(damage);
                        Debug.Log($"IonBolt: Hit enemy at tile {gridPosition} for {damage} damage");

                        // Trigger explosion at enemy position
                        ExplodeAtGridPosition(gridPosition);
                        
                        // Destroy projectile
                        Destroy(activeProjectile);
                        isProjectileFired = false;
                        break;
                    }
                }
            }
        }

        private void CheckIfPastRightmostGrid()
        {
            // Use the same boundary checking as Bullet.cs
            if (currentGridPosition.x >= tileGrid.gridWidth || currentGridPosition.x > tileGrid.gridWidth - 1)
            {
                Destroy(activeProjectile);
                isProjectileFired = false;
            }
        }
        
        private bool IsEnemyTilePosition(Vector2Int gridPosition)
        {
            // Same enemy tile detection as Bullet.cs
            return tileGrid.IsValidGridPosition(gridPosition) && 
                   gridPosition.x >= tileGrid.gridWidth / 2;
        }
        
        private void ExplodeAtGridPosition(Vector2Int centerGridPosition)
        {
            Debug.Log($"IonBolt: Explosion at grid position {centerGridPosition}");
            
            // Create visual explosion effect at world position
            Vector3 worldPosition = tileGrid.GetWorldPosition(centerGridPosition);
            
            // Create explosion effect
            if (explosionEffectPrefab != null)
            {
                GameObject explosionEffect = Instantiate(explosionEffectPrefab, worldPosition, Quaternion.identity);
                Destroy(explosionEffect, explosionEffectDuration);
            }
            
            // Get all tiles within the explosion range (including the center tile)
            List<Vector2Int> affectedTiles = GetAffectedTiles(centerGridPosition, explosionTileRadius);
            
            // Find enemies in each affected tile
            foreach (Vector2Int tilePos in affectedTiles)
            {
                // Skip the center tile as it was already damaged by direct hit
                if (tilePos == centerGridPosition) continue;
                
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                
                foreach (GameObject enemy in enemies)
                {
                    // Use the same center alignment adjustment as Bullet.cs for explosion targets
                    Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
                    Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);
                    
                    if (enemyGridPos == tilePos)
                    {
                        Enemy enemyComponent = enemy.GetComponent<Enemy>();
                        if (enemyComponent != null)
                        {
                            // Use explosion damage for area effect
                            enemyComponent.TakeDamage(explosionDamage);
                            Debug.Log($"IonBolt: Explosion dealt {explosionDamage} damage to enemy at tile {tilePos}");
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

        // Public method to adjust speed at runtime
        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier); // Ensure minimum speed
            Debug.Log($"IonBolt: Speed multiplier set to {speedMultiplier}");
        }

        // Public method to get current effective speed
        public float GetEffectiveSpeed()
        {
            return projectileSpeed * speedMultiplier;
        }
    }
}