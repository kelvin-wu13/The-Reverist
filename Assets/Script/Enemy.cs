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
    [SerializeField] private LayerMask obstacleLayer; // Layer for collision detection

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private Color stunnedColor = Color.blue;

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
        targetPosition = transform.position;
        
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
        if (isMoving && !isStunned)
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
        if (!isStunned && !isDying)
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

    private IEnumerator RandomMovement()
    {
        while (!isDying)
        {
            // Wait for the move interval
            yield return new WaitForSeconds(moveInterval);
            
            if (!isMoving && !isStunned)
            {
                // Try to move in a random direction
                TryMove();
            }
        }
    }
    
    private void TryMove()
    {
        // Shuffle the directions array for random movement
        ShuffleDirections();
        
        // Try each direction until a valid move is found
        foreach (Vector2Int direction in directions)
        {
            Vector2Int newPosition = currentGridPosition + direction;
            
            // Check if the new position is valid (is within grid, is an enemy tile, and is not reserved)
            if (tileGrid.IsValidGridPosition(newPosition) && 
                tileGrid.grid[newPosition.x, newPosition.y] == TileType.Enemy && 
                !IsPositionReserved(newPosition))
            {
                // Release our current position reservation
                ReleaseGridPosition(currentGridPosition);
                
                // Reserve the new position
                ReserveGridPosition(newPosition);
                
                // Update target position
                targetGridPosition = newPosition;
                targetPosition = tileGrid.GetWorldPosition(targetGridPosition);
                isMoving = true;
                
                return; // Successfully moved
            }
        }
        
        // If we reach here, no valid move was found
        Debug.Log("Enemy: No valid move found from current position");
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
        spriteRenderer.color = isStunned ? stunnedColor : originalColor;
    }

    private void Die()
    {
        isDying = true;

        StopAllCoroutines(); // Stop the movement coroutine

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
        if (spriteRenderer != null)
            spriteRenderer.color = stunnedColor;

        //Wait for stun duration
        yield return new WaitForSeconds(duration);

        //Remove Stun effect
        isStunned = false;

        //Restore original color
        if (spriteRenderer != null && !isDying)
            spriteRenderer.color = originalColor;
    }
}