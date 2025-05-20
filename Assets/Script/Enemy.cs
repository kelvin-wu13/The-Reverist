using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDying = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveInterval = 2f;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private bool isStunned = false;
    [SerializeField] private bool isAfterPush = false; // New flag for post-push/pull state
    [SerializeField] private bool isBeingPulled = false; // Flag to track when enemy is being pulled
    [SerializeField] private float postPushDelay = 1f; // Delay after being pushed/pulled
    [SerializeField] private LayerMask obstacleLayer; // Layer for collision detection
    
    [Header("Position Offset")]
    [SerializeField] private Vector2 positionOffset = Vector2.zero; // Added position offset

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private Color stunnedColor = Color.blue;
    [SerializeField] private Color pushedColor = new Color(1f, 0.5f, 0f); // Orange color for pushed state
    [SerializeField] private Color pulledColor = new Color(0.5f, 0f, 1f); // Purple color for pulled state

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootInterval = 3f;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootChance = 0.7f; // Chance to shoot when timer is up

    private TileGrid tileGrid;
    private Vector2Int currentGridPosition;
    private Vector2Int targetGridPosition; // Added to track where enemy is headed
    private Vector3 targetPosition;
    private Color originalColor;
    private float moveTimer;
    private float shootTimer;
    
    // Static dictionary to track which grid positions are reserved
    private static Dictionary<Vector2Int, GameObject> reservedPositions = new Dictionary<Vector2Int, GameObject>();

    // Possible movement directions
    private Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        tileGrid = FindObjectOfType<TileGrid>();

        if(tileGrid == null)
        {
            Debug.LogError("Enemy: Could not find TileGrid");
        }
        
        // Create shoot point if not assigned
        if(shootPoint == null)
        {
            shootPoint = transform;
        }
    }

    private void Start()
    {
        // Initialize Health
        currentHealth = maxHealth;

        currentGridPosition = tileGrid.GetGridPosition(transform.position);
        targetGridPosition = currentGridPosition; // Initialize as current position
        
        // Apply position offset to target position
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        
        // Set initial position with offset
        transform.position = targetPosition;
        
        // Reserve the current position
        ReserveGridPosition(currentGridPosition);

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
            
        // Initialize move timer
        moveTimer = moveInterval;
        
        // Initialize shoot timer with a random offset so all enemies don't shoot at once
        shootTimer = Random.Range(0f, shootInterval);
        
        // Start movement coroutine
        StartCoroutine(RandomMovement());
    }
    
    private void Update()
    {
        // Handle movement
        if (isMoving && !isStunned && !isAfterPush && !isBeingPulled)
        {
            // Move towards target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            // Check if we've reached the target position
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                transform.position = targetPosition;
                
                // Update current position and ensure it's reserved
                currentGridPosition = targetGridPosition;
                
                // No need to reserve again as we maintain our reservation throughout movement
            }
        }
        
        // Handle shooting
        if (!isDying && !isBeingPulled)
        {
            shootTimer -= Time.deltaTime;
            
            if (shootTimer <= 0)
            {
                // Reset the shoot timer
                shootTimer = shootInterval;
                
                // Random chance to shoot
                if (Random.value <= shootChance)
                {
                    ShootAtPlayer();
                }
            }
        }
    }
    
    public TileGrid GetTileGrid()
    {
        return tileGrid;
    }


    private IEnumerator RandomMovement()
    {
        while (!isDying)
        {
            // Wait for the move interval
            yield return new WaitForSeconds(moveInterval);

            if (!isMoving && !isStunned && !isAfterPush && !isBeingPulled)
            {
                // Try to move in a random direction
                TryMove();
            }
        }
    }
    
    private void TryMove()
    {
        // Cancel movement if the enemy is being pulled
        if (isBeingPulled)
        {
            Debug.Log("Enemy movement canceled due to being pulled.");
            isMoving = false;
            return;
        }

        // Define movement priority (favoring leftward movement first)
        Vector2Int[] prioritizedDirections = new Vector2Int[]
        {
            Vector2Int.left,   // Prefer moving toward the player zone
            Vector2Int.down,
            Vector2Int.up,
            Vector2Int.right   // Last priority: advancing deeper into enemy zone
        };

        // Try to find a valid movement direction
        foreach (Vector2Int direction in prioritizedDirections)
        {
            Vector2Int newPosition = currentGridPosition + direction;

            bool isValid = tileGrid.IsValidGridPosition(newPosition);
            bool isEnemyTile = isValid && tileGrid.grid[newPosition.x, newPosition.y] == TileType.Enemy;
            bool isReserved = IsPositionReserved(newPosition);

            if (isValid && isEnemyTile && !isReserved)
            {
                // Cancel reservation at current position
                ReleaseGridPosition(currentGridPosition);

                // Reserve the new position
                ReserveGridPosition(newPosition);

                // Update movement data
                targetGridPosition = newPosition;
                Vector3 basePosition = tileGrid.GetWorldPosition(targetGridPosition);
                targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);

                isMoving = true;
                return;
            }
        }

        // No available move found
        Debug.Log("Enemy: No valid prioritized move found from current position");
    }
    
    private void ShootAtPlayer()
    {
        if (bulletPrefab == null) return;
        
        // Always shoot straight to the left (toward player side)
        Vector2 direction = Vector2.left;
        
        // Create the bullet
        GameObject bulletObj = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        
        if (bullet != null)
        {
            // Initialize bullet with parameters
            bullet.Initialize(direction, bulletSpeed, bulletDamage, tileGrid);
            
            // Play shoot sound/effect if needed
            PlayShootEffect();
        }
        else
        {
            // If there's no EnemyBullet component, try to add one
            bullet = bulletObj.AddComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.Initialize(direction, bulletSpeed, bulletDamage, tileGrid);
                PlayShootEffect();
            }
            else
            {
                Debug.LogError("Enemy: Failed to add EnemyBullet component to bullet prefab");
                Destroy(bulletObj);
            }
        }
    }
    
    private void PlayShootEffect()
    {
        // Flash the sprite when shooting
        StartCoroutine(ShootFlash());
        
        // You could play a sound effect or particle effect here
    }
    
    private IEnumerator ShootFlash()
    {
        if (spriteRenderer == null) yield break;
        
        // Store the original color
        Color originalFlashColor = spriteRenderer.color;
        
        // Set to yellow for shooting flash
        spriteRenderer.color = Color.yellow;
        
        // Short flash duration
        yield return new WaitForSeconds(0.05f);
        
        // Return to original color
        spriteRenderer.color = originalFlashColor;
    }
    
    // Reserve a grid position for this enemy
    private void ReserveGridPosition(Vector2Int position)
    {
        reservedPositions[position] = gameObject;
    }
    
    // Release a grid position reservation
    private void ReleaseGridPosition(Vector2Int position)
    {
        if (reservedPositions.ContainsKey(position) && reservedPositions[position] == gameObject)
        {
            reservedPositions.Remove(position);
        }
    }
    
    // Check if a grid position is already reserved
    private bool IsPositionReserved(Vector2Int position)
    {
        return reservedPositions.ContainsKey(position) && reservedPositions[position] != gameObject;
    }
    
    // Physical collision check (can be used in addition to reservation system)
    private bool IsPositionOccupied(Vector2Int gridPosition)
    {
        // Convert grid position to world position
        Vector3 worldPosition = tileGrid.GetWorldPosition(gridPosition);
        
        // Create a small overlap circle to check for collisions
        float checkRadius = 0.4f; // Adjust based on your entity size
        
        // Check for overlapping colliders
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, checkRadius, obstacleLayer);
        
        // If we found any colliders that aren't our own, the position is occupied
        foreach (Collider2D collider in colliders)
        {
            // Skip our own collider
            if (collider.gameObject != gameObject)
            {
                return true; // Position is occupied
            }
        }
        
        return false; // Position is free
    }
    
    private void ShuffleDirections()
    {
        // Simple Fisher-Yates shuffle
        for (int i = directions.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;

        currentHealth -= damage;

        StartCoroutine(FlashColor());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashColor()
    {
        // Don't proceed if no sprite renderer available
        if (spriteRenderer == null) yield break;
        
        // Change to hit color
        spriteRenderer.color = hitColor;
        
        // Wait for flash duration
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Change back to original color or stunned color if currently stunned
        if (isStunned)
            spriteRenderer.color = stunnedColor;
        else if (isAfterPush)
            spriteRenderer.color = pushedColor;
        else if (isBeingPulled)
            spriteRenderer.color = pulledColor;
        else
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDying = true;

        StopAllCoroutines(); // Stop the movement coroutine
        
        // Make sure to release grid position when dying
        ReleaseGridPosition(currentGridPosition);

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        Destroy(gameObject);
    }

    public void Stun(float duration)
    {
        if (isDying) return;
        StartCoroutine(ApplyStun(duration));
    }

    private IEnumerator ApplyStun(float duration)
    {
        isStunned = true;

        //Visual feedback
        if (spriteRenderer != null && !isBeingPulled)
            spriteRenderer.color = stunnedColor;

        //Wait for stun duration
        yield return new WaitForSeconds(duration);

        //Remove Stun effect
        isStunned = false;

        //Restore original color
        if (spriteRenderer != null && !isDying && !isAfterPush && !isBeingPulled)
            spriteRenderer.color = originalColor;
    }
    
    // Method to set position offset at runtime
    public void SetPositionOffset(Vector2 newOffset)
    {
        positionOffset = newOffset;
        
        // Update current position with new offset if not moving
        if (!isMoving && !isBeingPulled)
        {
            Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
            targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
            transform.position = targetPosition;
        }
    }
    
    // Method to get current position offset
    public Vector2 GetPositionOffset()
    {
        return positionOffset;
    }

    // Modified method to prepare for pull
    public void PrepareForPull(Vector2Int targetGridPos)
    {
        // Release current grid position reservation immediately
        ReleaseGridPosition(currentGridPosition);
        
        // Set being pulled flag
        isBeingPulled = true;
        
        // Update visual feedback for pulled state
        if (spriteRenderer != null && !isDying)
            spriteRenderer.color = pulledColor;
            
        // Stop movement
        isMoving = false;
        StopCoroutine(nameof(RandomMovement));
    }

    // New method to handle push/pull effects
    public void ApplyPushEffect(Vector2Int newGridPosition, Vector3 newWorldPosition)
    {
        // Stop current movement
        StopAllCoroutines(); // Stop all coroutines including RandomMovement
        isMoving = false;
        
        // Reset flags
        isBeingPulled = false;
        
        // Update grid positions
        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        
        // Reserve the new grid position
        ReserveGridPosition(currentGridPosition);
        
        // Set the target position with the existing offset
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        
        // Apply post-push delay
        StartCoroutine(PostPushDelay());
    }
    
    // Method to set position with offset (for use with KineticShove)
    public void SetPositionWithOffset(Vector2Int newGridPosition)
    {
        // Stop current movement
        StopAllCoroutines(); // Stop all coroutines including RandomMovement
        isMoving = false;
        
        // Update grid positions
        ReleaseGridPosition(currentGridPosition);
        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        ReserveGridPosition(currentGridPosition);
        
        // Set the target position with the existing offset
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        transform.position = targetPosition; // Immediately set position
        
        // Apply post-push delay
        StartCoroutine(PostPushDelay());
    }
    
    // Add delay after being pushed/pulled
    private IEnumerator PostPushDelay()
    {
        isAfterPush = true;
        isBeingPulled = false;

        // Visual feedback for pushed state
        if (spriteRenderer != null && !isDying && !isStunned)
            spriteRenderer.color = pushedColor;

        // Wait for the delay period
        yield return new WaitForSeconds(postPushDelay);

        // Remove push effect
        isAfterPush = false;

        // Restore original color if not stunned
        if (spriteRenderer != null && !isDying && !isStunned)
            spriteRenderer.color = originalColor;

        // Restart the movement coroutine
        StartCoroutine(RandomMovement());
    }
}