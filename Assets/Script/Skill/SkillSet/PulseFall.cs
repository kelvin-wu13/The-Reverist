using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SkillSystem;

public class PulseFall : Skill
{
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
    
    private void Awake()
    {
        tileGrid = FindObjectOfType<TileGrid>();
        if (tileGrid == null)
        {
            Debug.LogError("QESkill: Could not find TileGrid in the scene!");
        }
        
        playerCrosshair = FindObjectOfType<PlayerCrosshair>();
        if (playerCrosshair == null)
        {
            Debug.LogError("QESkill: Could not find PlayerCrosshair in the scene!");
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && impactSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    // Override the ExecuteSkillEffect method from the base class
    public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
    {
        base.ExecuteSkillEffect(targetPosition, casterTransform);
        
        if (tileGrid == null) return;
        
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
            
            // Optional: Add a growing shadow beneath where the projectile will land
            GameObject shadow = new GameObject("ProjectileShadow");
            SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();

            // Create a simple circle sprite for the shadow
            shadowRenderer.sprite = CreateShadowSprite();
            shadow.transform.position = new Vector3(targetWorldPos.x, targetWorldPos.y, 0.1f);
            shadow.transform.localScale = Vector3.zero;
            
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
                
                // Grow the shadow as the projectile approaches
                if (shadow != null)
                {
                    float shadowScale = fractionOfJourney * 0.5f;
                    shadow.transform.localScale = new Vector3(shadowScale, shadowScale, 1);
                    
                    // Make shadow more transparent at the beginning and more opaque as it reaches the ground
                    Color shadowColor = shadowRenderer.color;
                    shadowColor.a = fractionOfJourney * 0.5f;
                    shadowRenderer.color = shadowColor;
                }
                
                yield return null;
            }
            
            // Ensure final position
            if (projectile != null)
            {
                projectile.transform.position = targetWorldPos;
            }
            
            // Destroy the shadow
            if (shadow != null)
            {
                Destroy(shadow);
            }
            
            // Play impact sound
            if (audioSource != null && impactSound != null)
            {
                audioSource.PlayOneShot(impactSound);
            }
            
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
    
    private bool CheckForEnemyAtPosition(Vector2Int position)
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
                return true;
            }
        }
        
        return false;
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
    
    // Helper method to create a simple shadow sprite
    private Sprite CreateShadowSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Bilinear;
        
        // Create a circular gradient for the shadow
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(size/2, size/2));
                float normalizedDistance = distanceFromCenter / (size/2);
                
                // Create a circular gradient that fades from black in center to transparent at edges
                float alpha = Mathf.Clamp01(1 - normalizedDistance);
                alpha = Mathf.Pow(alpha, 2); // Make the gradient more concentrated in the center
                
                texture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return sprite;
    }
}