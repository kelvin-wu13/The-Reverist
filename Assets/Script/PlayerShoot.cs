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
    private readonly int comboIndexParam = Animator.StringToHash("ComboIndex");
    [SerializeField] private float WaitTime = 1f;

    private float Time_elapsed;
    private float lastShootTime;
    private bool isHoldingFireButton = false;

    private bool isSkillAnimating = false;
    private float skillAnimationEndTime = 0f;

    private void Awake()
    {
        if (bulletSpawnPoint == null)
        {
            bulletSpawnPoint = transform.Find("FirePoint");
            if (bulletSpawnPoint == null)
            {
                bulletSpawnPoint = transform;
            }
        }
        if (tileGrid == null) tileGrid = FindObjectOfType<TileGrid>();
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (comboTracker == null) comboTracker = GetComponent<ComboTracker>();
    }

    private void Update()
    {
        if (isSkillAnimating && Time.time > skillAnimationEndTime)
        {
            isSkillAnimating = false;
        }

        isHoldingFireButton = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);

        if (isHoldingFireButton)
            TryShoot();

        bool isCurrentlyShooting = isHoldingFireButton || isSkillAnimating;
        animator.SetBool(isShootingParam, isCurrentlyShooting);

        if (comboTracker != null)
        {
            animator.SetInteger("ComboIndex", comboTracker.GetCurrentComboIndex());
        }

        UpdateShootTimer();
    }
    public void TriggerSkillAnimation(float duration)
    {
        isSkillAnimating = true;
        skillAnimationEndTime = Time.time + duration;

        if (comboTracker != null)
        {
            comboTracker.TriggerCombo();
        }
    }

    public Transform GetBulletSpawnPoint()
    {
        return bulletSpawnPoint;
    }

    private void TryShoot()
    {
        if (isSkillAnimating) return;
        if (stats == null) return;
        if (Time.time - lastShootTime < stats.ShootCooldown) return;

        ShootBulletFromCurrentTile();
        lastShootTime = Time.time;

        if (comboTracker != null)
        {
            comboTracker.TriggerCombo();
        }
    }

    private void UpdateShootTimer()
    {
        Time_elapsed += Time.deltaTime;
    }

    private void ShootBulletFromCurrentTile()
    {
        Vector2Int playerGridPos = playerMovement != null
            ? playerMovement.GetCurrentGridPosition()
            : tileGrid.GetGridPosition(transform.position);

        Vector2Int spawnGridPos = new Vector2Int(playerGridPos.x, playerGridPos.y);

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
