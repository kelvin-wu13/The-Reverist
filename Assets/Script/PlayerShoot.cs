using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private int bulletDamage = 10;

    [Header("Grid Reference")]
    [SerializeField] private TileGrid tileGrid;
    
    [Header("Animation")]
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
        // Check if cooldown has passed
        if (Time.time - lastShootTime < shootCooldown)
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
            bullet.Initialize(direction, bulletSpeed, bulletDamage, tileGrid);
        }
        else
        {
            Debug.LogError("Bullet prefab does not have a Bullet component!");
        }
    }
}