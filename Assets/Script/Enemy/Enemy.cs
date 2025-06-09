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
    [SerializeField] private bool isAfterPush = false;
    [SerializeField] private bool isBeingPulled = false;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Position Offset")]
    [SerializeField] private Vector2 positionOffset = Vector2.zero;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private Color stunnedColor = Color.blue;
    [SerializeField] private Color pushedColor = new Color(1f, 0.5f, 0f);

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootInterval = 3f;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootChance = 0.7f;

    private TileGrid tileGrid;
    private Vector2Int currentGridPosition;
    private Vector2Int targetGridPosition;
    private Vector3 targetPosition;
    private Color originalColor;
    private float moveTimer;
    private float shootTimer;

    private static Dictionary<Vector2Int, GameObject> reservedPositions = new Dictionary<Vector2Int, GameObject>();

    private Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    // Static method to clear all reservations - can be called by skills
    public static void ClearAllReservations()
    {
        reservedPositions.Clear();
        Debug.Log("All enemy position reservations cleared");
    }
    
    // Static method to get current reservations count (for debugging)
    public static int GetReservationCount()
    {
        return reservedPositions.Count;
    }

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
        currentHealth = maxHealth;
        currentGridPosition = tileGrid.GetGridPosition(transform.position);
        targetGridPosition = currentGridPosition;
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        transform.position = targetPosition;
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        moveTimer = moveInterval;
        shootTimer = Random.Range(0f, shootInterval);
        StartCoroutine(RandomMovement());
    }
    
    private void Update()
    {
        if (isMoving && !isStunned && !isAfterPush && !isBeingPulled)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                transform.position = targetPosition;
                currentGridPosition = targetGridPosition;
                tileGrid.SetTileOccupied(currentGridPosition, true);
            }
        }

        if (!isDying && !isBeingPulled)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0)
            {
                shootTimer = shootInterval;
                if (Random.value <= shootChance)
                    ShootAtPlayer();
            }
        }
    }

    
    public TileGrid GetTileGrid()
    {
        return tileGrid;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    // Method to check if enemy is being pulled
    public bool IsBeingPulled()
    {
        return isBeingPulled;
    }

    // Method to interrupt movement for skill execution
    public void InterruptMovementForSkill()
    {
        if (isMoving && !isBeingPulled)
        {
            // Stop current movement and snap to current target
            isMoving = false;
            transform.position = targetPosition;
            currentGridPosition = targetGridPosition;
            
            // Clear any reservations and update grid
            tileGrid.SetTileOccupied(currentGridPosition, true);
            ReserveGridPosition(currentGridPosition);
            
            Debug.Log($"Enemy movement interrupted at position {currentGridPosition}");
        }
    }

    // Modified PrepareForPull method
    public void PrepareForPull(Vector2Int targetGridPos)
    {
        // Stop all movement
        isMoving = false;
        isBeingPulled = true;
        
        // Stop the random movement coroutine
        StopCoroutine(nameof(RandomMovement));
        
        // Clear current position
        ReleaseGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, false);
        
        Debug.Log($"Enemy at {currentGridPosition} prepared for pull to {targetGridPos}");
    }

    // New method to complete the pull
    public void CompletePull(Vector2Int newGridPosition, Vector3 finalPosition)
    {
        // Update position data
        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        
        // Reserve new position
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        
        // Update target position with offset
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        
        // Clear pull state
        isBeingPulled = false;
        
        // Resume normal behavior
        StartCoroutine(RandomMovement());
        
        Debug.Log($"Enemy pull completed at {currentGridPosition}");
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
        if (isBeingPulled)
        {
            isMoving = false;
            return;
        }

        Vector2Int[] prioritizedDirections = new Vector2Int[]
        {
            Vector2Int.left,
            Vector2Int.down,
            Vector2Int.up,
            Vector2Int.right
        };

        foreach (Vector2Int direction in prioritizedDirections)
        {
            Vector2Int newPosition = currentGridPosition + direction;
            bool isValid = tileGrid.IsValidGridPosition(newPosition);
            bool isEnemyTile = isValid && tileGrid.grid[newPosition.x, newPosition.y] == TileType.Enemy;
            bool isReserved = IsPositionReserved(newPosition);
            bool isOccupied = tileGrid.IsTileOccupied(newPosition);

            if (isValid && isEnemyTile && !isReserved && !isOccupied)
            {
                ReleaseGridPosition(currentGridPosition);
                tileGrid.SetTileOccupied(currentGridPosition, false);

                ReserveGridPosition(newPosition);
                tileGrid.SetTileOccupied(newPosition, true);

                targetGridPosition = newPosition;
                Vector3 basePosition = tileGrid.GetWorldPosition(targetGridPosition);
                targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);

                isMoving = true;
                return;
            }
        }
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
        else
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDying = true;
        StopAllCoroutines();
        ReleaseGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, false);
        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;
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

    // Method to handle push/pull effects
    public void ApplyPushEffect(Vector2Int newGridPosition, Vector3 newWorldPosition)
    {
        StopAllCoroutines();
        isMoving = false;
        isBeingPulled = false;

        tileGrid.SetTileOccupied(currentGridPosition, false);
        ReleaseGridPosition(currentGridPosition);

        // Check again if the tile is still safe before committing pull
        if (tileGrid.IsTileOccupied(newGridPosition))
        {
            Debug.LogWarning("Tile became occupied during pull! Aborting pull.");
            // Optionally reset to previous position
            currentGridPosition = tileGrid.GetGridPosition(transform.position);
            ReserveGridPosition(currentGridPosition);
            tileGrid.SetTileOccupied(currentGridPosition, true);
            return;
        }

        // Proceed with pull
        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);


        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);

        StartCoroutine(RandomMovement()); // <-- This ensures movement resumes
    }

    
    // Method to set position with offset (for use with KineticShove)
    public void SetPositionWithOffset(Vector2Int newGridPosition)
    {
        StopAllCoroutines();
        isMoving = false;

        tileGrid.SetTileOccupied(currentGridPosition, false);
        ReleaseGridPosition(currentGridPosition);

        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;

        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);

        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        transform.position = targetPosition;
    }
}