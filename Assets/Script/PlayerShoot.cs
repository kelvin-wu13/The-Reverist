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

    private float lastShootTime;
    private bool isHoldingFireButton = false;
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

        // Get the player's current grid position
        Vector2Int playerGridPos = tileGrid.GetGridPosition(transform.position);
        
        // Shoot in the forward direction (assuming the player is facing right)
        ShootBullet(Vector2.right);
        
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