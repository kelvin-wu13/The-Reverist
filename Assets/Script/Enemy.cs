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

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private Color stunnedColor = Color.blue;

    private TileGrid tileGrid;
    private Vector2Int currentGridPosition;
    private Vector3 targetPosition;
    private Color originalColor;
    private float moveTimer;

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
    }

    private void Start()
    {
        // Initialize Health
        currentHealth = maxHealth;

        currentGridPosition = tileGrid.GetGridPosition(transform.position);
        targetPosition = transform.position;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
            
        // Initialize move timer
        moveTimer = moveInterval;
        
        // Start movement coroutine
        StartCoroutine(RandomMovement());
    }
    
    private void Update()
    {
        // Handle movement
        if (isMoving  && !isStunned)
        {
            // Move towards target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            // Check if we've reached the target position
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                transform.position = targetPosition;
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
            
            // Check if the new position is valid (is within grid and is an enemy tile)
            if (tileGrid.IsValidGridPosition(newPosition) && tileGrid.grid[newPosition.x, newPosition.y] == TileType.Enemy)
            {
                // Update grid position and set target
                currentGridPosition = newPosition;
                targetPosition = tileGrid.GetWorldPosition(currentGridPosition);
                isMoving = true;
                
                return; // Successfully moved
            }
        }
        
        // If we reach here, no valid move was found
        Debug.Log("Enemy: No valid move found from current position");
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