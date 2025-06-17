using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillSystem;

public class PulseFall : Skill
{
    [Header("Skill Properties")]
    [SerializeField] public float cooldownDuration = 3.0f;
    [SerializeField] public float manaCost = 15.0f;

    [Header("QE Skill Settings")]
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private int horizontalRange = 1; // 1 means 3 tiles total (center + 1 on each side)
    [SerializeField] private float impactDelay = 0.5f; // Time between projectile landing and tile effects
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject skillProjectilePrefab; // The falling object
    [SerializeField] private float projectileDropHeight = 5f; // Height from which to drop the object
    [SerializeField] private float projectileDropSpeed = 10f; // Speed of the falling object
    [SerializeField] private GameObject impactEffectPrefab; // Effect when projectile lands
    [SerializeField] private AudioClip impactSound; // Optional sound effect when projectile impacts
    
    [Header("Tile Effects")]
    [SerializeField] private GameObject crackedTileEffectPrefab;
    [SerializeField] private GameObject brokenTileEffectPrefab;
    
    private TileGrid tileGrid;
    private PlayerCrosshair playerCrosshair;
    private List<Vector2Int> crackedTilePositions = new List<Vector2Int>();
    private List<Vector2Int> enemyTilePositions = new List<Vector2Int>(); // Track enemy positions
    private AudioSource audioSource;
    private PlayerStats playerStats;
    
    private void Awake()
    {
        tileGrid = FindObjectOfType<TileGrid>();
        if (tileGrid == null)
        {
            Debug.LogError("QESkill: Could not find TileGrid in the scene!");
        }

        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.Log("PulseFall : Cant find playerStats component");
        }

        playerCrosshair = FindObjectOfType<PlayerCrosshair>();
        if (playerCrosshair == null)
        {
            Debug.LogError("QESkill: Could not find PlayerCrosshair in the scene!");
        }
    }
    
    // Override the ExecuteSkillEffect method from the base class
    public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
    {
        base.ExecuteSkillEffect(targetPosition, casterTransform);
        
        if (tileGrid == null) return;

        //Check Mana Cost
        if (playerStats != null && !playerStats.TryUseMana(manaCost))
        {
            Debug.Log("Not enough mana to cast PulseFall!");
            return;
        }
        
        // Get the target world position using the PlayerCrosshair
        Vector3 targetWorldPos;
        Vector2Int crosshairTargetPosition;
        
        if (playerCrosshair != null)
        {
            // Use the crosshair position instead of the passed target position
            crosshairTargetPosition = playerCrosshair.GetTargetGridPosition();
            targetWorldPos = playerCrosshair.GetTargetWorldPosition();
        }
        else
        {
            // Fallback to using the passed target position
            crosshairTargetPosition = targetPosition;
            targetWorldPos = tileGrid.GetWorldPosition(targetPosition) + new Vector3(0.5f, 0.5f, 0);
        }
        
        // Drop the skill projectile first, then apply effects after it lands
        StartCoroutine(DropProjectileAndCrackTiles(crosshairTargetPosition, targetWorldPos));
    }
    
    private IEnumerator DropProjectileAndCrackTiles(Vector2Int targetPosition, Vector3 targetWorldPos)
    {
        // Create the projectile at a height above the target
        Vector3 projectileStartPos = targetWorldPos + new Vector3(0, projectileDropHeight, 0);
        GameObject projectile = null;
        
        if (skillProjectilePrefab != null)
        {
            projectile = Instantiate(skillProjectilePrefab, projectileStartPos, Quaternion.identity);
            
            // Animate the projectile falling
            float startTime = Time.time;
            float journeyLength = projectileDropHeight;
            
            
            while (projectile != null && Time.time - startTime < journeyLength / projectileDropSpeed)
            {
                float distanceCovered = (Time.time - startTime) * projectileDropSpeed;
                float fractionOfJourney = distanceCovered / journeyLength;
                
                // Calculate new position with slight wobble for a more natural fall
                float wobbleX = Mathf.Sin(fractionOfJourney * 6) * 0.1f * (1 - fractionOfJourney);
                float wobbleY = Mathf.Cos(fractionOfJourney * 6) * 0.1f * (1 - fractionOfJourney);
                Vector3 newPosition = Vector3.Lerp(projectileStartPos, targetWorldPos, fractionOfJourney) 
                    + new Vector3(wobbleX, wobbleY, 0);
                
                // Update projectile position
                if (projectile != null)
                {
                    projectile.transform.position = newPosition;
                }
                
                yield return null;
            }
            
            // Ensure final position
            if (projectile != null)
            {
                projectile.transform.position = targetWorldPos;
            }
            
            AudioManager.Instance?.PlayPulseFallSFX();
            
            // Create impact effect
            if (impactEffectPrefab != null)
            {
                GameObject impactEffect = Instantiate(impactEffectPrefab, targetWorldPos, Quaternion.identity);
                Destroy(impactEffect, 2f); // Destroy after 2 seconds
            }
            
            // Destroy the projectile with a small delay
            if (projectile != null)
            {
                Destroy(projectile, 0.1f);
            }
        }
        
        // Wait for the impact delay before cracking tiles
        yield return new WaitForSeconds(impactDelay);
        
        // Update enemy tile positions before applying effects
        UpdateEnemyPositions();
        
        // Create a horizontal line of effect
        for (int x = targetPosition.x - horizontalRange; x <= targetPosition.x + horizontalRange; x++)
        {
            Vector2Int currentPos = new Vector2Int(x, targetPosition.y);
            
            // Check if the position is valid
            if (tileGrid.IsValidGridPosition(currentPos))
            {
                // Create a slight delay between each tile cracking for a spreading effect
                StartCoroutine(DelayedTileEffect(currentPos, Mathf.Abs(x - targetPosition.x) * 0.1f));
            }
        }
        
        // Start listening for movement events to check for cracked tiles
        StartCoroutine(MonitorCrackedTiles());
    }
    
    private void UpdateEnemyPositions()
    {
        // Clear the old list
        enemyTilePositions.Clear();
        
        // Find all enemies in the scene
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            // Get the grid position of the enemy
            Vector2Int enemyPos = tileGrid.GetGridPosition(enemy.transform.position);
            
            // Add to the list if not already present
            if (!enemyTilePositions.Contains(enemyPos))
            {
                enemyTilePositions.Add(enemyPos);
            }
        }
    }
    
    private IEnumerator DelayedTileEffect(Vector2Int position, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check if there's an enemy at this position
        bool enemyFound = enemyTilePositions.Contains(position);
        
        // CHANGED: Always crack the tile if there's an enemy on it
        if (enemyFound)
        {
            // Deal damage to enemy
            DealDamageAtPosition(position);
            
            // Crack the tile
            CrackTile(position);
        }
        else
        {
            // If no enemy, break the tile directly
            BreakTile(position);
        }
    }
    
    private void DealDamageAtPosition(Vector2Int position)
    {
        // Find all colliders at this world position
        Vector3 worldPos = tileGrid.GetWorldPosition(position);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos + new Vector3(0.5f, 0.5f, 0), 0.4f);
        
        foreach (Collider2D collider in colliders)
        {
            // Check if it's an enemy
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Deal damage
                enemy.TakeDamage(damageAmount);
                
                // Ensure this position is marked as an enemy position
                if (!enemyTilePositions.Contains(position))
                {
                    enemyTilePositions.Add(position);
                }
            }
        }
    }
    
    private void CrackTile(Vector2Int position)
    {
        if (tileGrid == null) return;
        
        // Directly call CrackTile method on the TileGrid
        tileGrid.CrackTile(position);
        
        // Add visual effect on top of the tile
        Vector3 worldPos = tileGrid.GetWorldPosition(position) + new Vector3(0.5f, 0.5f, 0);
        if (crackedTileEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(crackedTileEffectPrefab, worldPos, Quaternion.identity);
            effectInstance.transform.SetParent(transform); // Parent to this skill object
        }
        
        // Store this position as cracked
        if (!crackedTilePositions.Contains(position))
        {
            crackedTilePositions.Add(position);
        }
    }
    
    private IEnumerator MonitorCrackedTiles()
    {
        // Keep checking as long as this skill object exists
        while (true)
        {
            // Update enemy positions
            UpdateEnemyPositions();
            
            // Check player movement - ONLY for non-enemy tiles
            PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
            foreach (PlayerMovement player in players)
            {
                Vector2Int playerPos = tileGrid.GetGridPosition(player.transform.position);
                
                // Only check if player is NOT on an enemy tile
                if (!enemyTilePositions.Contains(playerPos))
                {
                    CheckEntityPosition(player.gameObject, true);
                }
            }
            
            // Check all enemies too
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                CheckEntityPosition(enemy.gameObject, false);
            }
            
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }
    
    private void CheckEntityPosition(GameObject entity, bool isPlayer)
    {
        Vector2Int gridPos = tileGrid.GetGridPosition(entity.transform.position);
        
        // If entity is on a cracked tile, check if they move off it
        if (crackedTilePositions.Contains(gridPos))
        {
            // Use a coroutine to check if the entity moved from this position
            StartCoroutine(CheckIfMoved(entity, gridPos));
        }
    }
    
    private IEnumerator CheckIfMoved(GameObject entity, Vector2Int startPosition)
    {
        // Wait a moment to see if the entity moves
        yield return new WaitForSeconds(0.3f);
        
        // Check current position
        Vector2Int currentPos = tileGrid.GetGridPosition(entity.transform.position);
        
        // If the entity moved from a cracked tile, break it
        if (startPosition != currentPos && crackedTilePositions.Contains(startPosition))
        {
            BreakTile(startPosition);
        }
    }
    
    private void BreakTile(Vector2Int position)
    {
        if (tileGrid == null) return;
        
        // Don't break the tile if there's an enemy on it
        if (enemyTilePositions.Contains(position))
        {
            return;
        }
        
        // Remove from cracked tiles list
        crackedTilePositions.Remove(position);
        
        // Directly call BreakTile method on the TileGrid
        tileGrid.BreakTile(position);
        
        // Add visual effect for broken tile
        Vector3 worldPos = tileGrid.GetWorldPosition(position) + new Vector3(0.5f, 0.5f, 0);
        if (brokenTileEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(brokenTileEffectPrefab, worldPos, Quaternion.identity);
            // We don't parent this one since it's permanent
        }
        
        // Find and destroy any cracked tile effects at this position
        foreach (Transform child in transform)
        {
            if (Vector3.Distance(child.position, worldPos) < 0.1f)
            {
                Destroy(child.gameObject);
            }
        }
    }
}