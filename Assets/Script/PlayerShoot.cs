using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Stats stats;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private TileGrid tileGrid;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement; // Reference to player movement to get grid position
    
    // Animation parameter hash for better performance
    private readonly int ComboIndex = Animator.StringToHash("ComboIndex");
    private readonly int isShootingParam = Animator.StringToHash("IsShooting");
    [SerializeField] private int comboAmount = 3;

    private float Time_elapsed;
    [SerializeField] private float WaitTime = 1.5f;
    private int currentComboIndex = 0;
    private float lastShootTime;
    private bool isHoldingFireButton = false;
    private Vector2 shootDirection = Vector2.right; // Default shoot direction - always to the right
    
    private void Awake()
    {   
        // If no spawn point assigned, use the player position
        if (bulletSpawnPoint == null)
        {
            bulletSpawnPoint = transform;
        }
        
        // Try to find the TileGrid if not assigned in the inspector
        if (tileGrid == null)
        {
            tileGrid = FindObjectOfType<TileGrid>();
        }
        
        // Try to get animator if not assigned in the inspector
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Try to get player movement if not assigned in the inspector
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
        
        // Validate that we have stats
        if (stats == null)
        {
            Debug.LogError("Stats scriptable object not assigned to PlayerShoot!");
        }
    }

    private void Update()
    {
        // Handle shooting input
        if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space))
        {
            isHoldingFireButton = true;
        }
        else
        {
            isHoldingFireButton = false;
        }

        if (isHoldingFireButton)
        {
            TryShoot();
        }

        UpdateShootTimer();
        animator.SetBool(isShootingParam, isHoldingFireButton);
    }

    private void TryShoot()
    {
        // Make sure we have stats
        if (stats == null) return;

        // Check if cooldown has passed
        if (Time.time - lastShootTime < stats.ShootCooldown)
        {
            return;
        }

        currentComboIndex++;
        if (currentComboIndex > comboAmount)
        {
            currentComboIndex = 1;
        }

        // Play shooting animation
        if (animator != null)
        {
            animator.SetInteger(ComboIndex, currentComboIndex);
        }

        // Always shoot to the right (along x-axis)
        ShootBulletFromCurrentTile();

        // Update the last shoot time
        lastShootTime = Time.time;
    }

    private void UpdateShootTimer()
    {
        Time_elapsed += Time.deltaTime;
        if (Time_elapsed >= WaitTime)
        {
            currentComboIndex = 0;
        }
    }

    private void ShootBulletFromCurrentTile()
    {
        // Get the current grid position from the player movement component
        Vector2Int currentGridPosition = Vector2Int.zero;

        // If we have the player movement component, get the position from there
        if (playerMovement != null)
        {
            // Get the grid position from the PlayerMovement component
            // We need to add a public method to access this from PlayerMovement
            currentGridPosition = playerMovement.GetCurrentGridPosition();
        }
        else
        {
            // Fallback: calculate grid position from transform position
            currentGridPosition = tileGrid.GetGridPosition(transform.position);
        }

        // Calculate spawn position - use the right edge of the current tile
        Vector3 tileWorldPos = tileGrid.GetWorldPosition(currentGridPosition);
        float tileWidth = tileGrid.GetTileWidth();
        Vector3 spawnPosition = new Vector3(
            tileWorldPos.x + tileWidth,
            tileWorldPos.y + (tileGrid.GetTileHeight() / 2),
            0
        );

        // Create the bullet at the spawn position
        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        Time_elapsed = 0;

        // Get and configure the bullet component
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            // Always shoot straight to the right
            bullet.Initialize(Vector2.right, stats.BulletSpeed, stats.BulletDamage, tileGrid);
        }
        else
        {
            Debug.LogError("Bullet prefab does not have a Bullet component!");
        }
    }
}