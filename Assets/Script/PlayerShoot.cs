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
    [SerializeField] private PlayerMovement playerMovement;
    
    // Animation parameter hash for better performance
    private readonly int ComboIndex = Animator.StringToHash("ComboIndex");
    private readonly int isShootingParam = Animator.StringToHash("IsShooting");
    [SerializeField] private int comboAmount = 3;

    private float Time_elapsed;
    [SerializeField] private float WaitTime = 1f;
    private int currentComboIndex = 0;
    private float lastShootTime;
    private bool isHoldingFireButton = false;
    private Vector2 shootDirection = Vector2.right;
    private ComboTracker comboTracker;

    private void Awake()
    {   
        if (bulletSpawnPoint == null)
        {
            bulletSpawnPoint = transform;
        }
        
        if (tileGrid == null)
        {
            tileGrid = FindObjectOfType<TileGrid>();
        }
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (comboTracker == null)
        {
            comboTracker = GetComponent<ComboTracker>();
        }
        
        if (stats == null)
        {
            Debug.LogError("Stats scriptable object not assigned to PlayerShoot!");
        }
    }

    private void Update()
    {
        // Handle shooting input 
        isHoldingFireButton = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);

        if (isHoldingFireButton)
        {
            TryShoot();
        }
        
        // Always update the animator with current input state
        // The animator will handle transitions based on IsShooting parameter
        animator.SetBool(isShootingParam, isHoldingFireButton);

        UpdateShootTimer();
    }
    public Transform GetBulletSpawnPoint()
    {
        return bulletSpawnPoint;
    }

    private void TryShoot()
    {
        if (stats == null) return;

        if (Time.time - lastShootTime < stats.ShootCooldown)
        {
            return;
        }

        if (comboTracker != null)
        {
            comboTracker.TriggerCombo();
        }

        ShootBulletFromCurrentTile();
        lastShootTime = Time.time;
    }

    // This method can be called by Animation Events at the end of shoot animation
    public void OnShootAnimationEnd()
    {
        // Optional: Add any cleanup logic here
        // The animator will handle the transition automatically
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
        // Get the tile the player is currently on (used for logic, not spawning)
        Vector2Int currentGridPosition = playerMovement != null
            ? playerMovement.GetCurrentGridPosition()
            : tileGrid.GetGridPosition(transform.position);

        // World position of the bullet spawn (based on FirePoint)
        Vector3 spawnPosition = bulletSpawnPoint.position;

        // Instantiate bullet at FirePoint position
        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        Time_elapsed = 0;

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            // Still use tile-based grid logic for things like homing, AoE, or stacking effects
            bullet.Initialize(Vector2.right, stats.BulletSpeed, stats.BulletDamage, tileGrid);
        }
        else
        {
            Debug.LogError("Bullet prefab does not have a Bullet component!");
        }
    }
}