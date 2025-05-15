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
    
    // Animation parameter hash for better performance
    private readonly int isShootingParam = Animator.StringToHash("IsShooting");

    private float lastShootTime;
    private bool isHoldingFireButton = false;
    private Vector2 shootDirection = Vector2.right; // Default shoot direction
    
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
        
        if(isHoldingFireButton)
        {
            TryShoot();
        }
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

        // Play shooting animation
        if (animator != null)
        {
            animator.SetTrigger(isShootingParam);
        }
        
        // Shoot the bullet in the current shoot direction
        ShootBullet(shootDirection);
        
        // Update the last shoot time
        lastShootTime = Time.time;
    }

    public void ShootBullet(Vector2 direction)
    {
        // Create the bullet
        GameObject bulletObject = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        
        // Get and configure the bullet component
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(direction, stats.BulletSpeed, stats.BulletDamage, tileGrid);
        }
        else
        {
            Debug.LogError("Bullet prefab does not have a Bullet component!");
        }
    }
}