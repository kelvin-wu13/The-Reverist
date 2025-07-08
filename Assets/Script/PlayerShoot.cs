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

    private ComboTracker comboTracker;

    private readonly int isShootingParam = Animator.StringToHash("IsShooting");
    [SerializeField] private float WaitTime = 1f;

    private float Time_elapsed;
    private float lastShootTime;
    private bool isHoldingFireButton = false;

    private void Awake()
    {
        // If not manually assigned, find FirePoint in the scene
        if (bulletSpawnPoint == null)
        {
            bulletSpawnPoint = transform.Find("FirePoint");
            if (bulletSpawnPoint == null)
            {
                Debug.LogWarning("FirePoint not found! Defaulting to player transform.");
                bulletSpawnPoint = transform;
            }
        }

        if (tileGrid == null)
            tileGrid = FindObjectOfType<TileGrid>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        if (comboTracker == null)
        comboTracker = GetComponent<ComboTracker>();

        if (stats == null)
            Debug.LogError("Stats scriptable object not assigned to PlayerShoot!");
    }

    private void Update()
    {
        isHoldingFireButton = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);

        if (isHoldingFireButton)
            TryShoot();

        // Always update Animator with IsShooting and current ComboIndex
        animator.SetBool(isShootingParam, isHoldingFireButton);

        // Also update ComboIndex here, based on ComboTracker
        if (comboTracker != null)
        {
            animator.SetInteger("ComboIndex", comboTracker.GetCurrentComboIndex());
        }

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
            return;

        ShootBulletFromCurrentTile();
        lastShootTime = Time.time;

        if (comboTracker != null)
            comboTracker.TriggerCombo();
    }

    private void UpdateShootTimer()
    {
        Time_elapsed += Time.deltaTime;
    }

    private void ShootBulletFromCurrentTile()
    {
        // Get current player grid position (logical row)
        Vector2Int playerGridPos = playerMovement != null
            ? playerMovement.GetCurrentGridPosition()
            : tileGrid.GetGridPosition(transform.position);

        Vector2Int spawnGridPos = new Vector2Int(playerGridPos.x, playerGridPos.y);

        // Use FirePoint world position
        Vector3 spawnPosition = bulletSpawnPoint.position;

        GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        Time_elapsed = 0;

        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Initialize(Vector2.right, stats.BulletSpeed, stats.BulletDamage, tileGrid, spawnGridPos);
            AudioManager.Instance?.PlayBasicShootSFX();
        }
        else
        {
            Debug.LogError("Bullet prefab does not have a Bullet component!");
        }
    }
}
