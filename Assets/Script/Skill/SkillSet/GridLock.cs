using System.Collections;
using UnityEngine;

namespace SkillSystem
{
    public class GridLock : Skill
    {
        [Header("Projectile Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 5f;
        [SerializeField] private int bulletDamage = 15;
        [SerializeField] private float enemyStunDuration = 3f;
        [SerializeField] public float cooldownDuration = 2.5f;
        [SerializeField] private float manaCost = 1.5f;

        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugPath = true;

        private TileGrid tileGrid;
        private bool isOnCooldown = false;
        private float cooldownTimer = 0f;
        private PlayerStats playerStats;
        private PlayerShoot playerShoot;
        private PlayerMovement playerMovement; // FIX: Added reference to PlayerMovement

        private void Awake()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
            }

            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }

            if (playerShoot == null)
            {
                playerShoot = FindObjectOfType<PlayerShoot>();
            }

            // FIX: Find the PlayerMovement component to get accurate grid position
            if (playerMovement == null)
            {
                playerMovement = FindObjectOfType<PlayerMovement>();
            }
        }

        private void Update()
        {
            if (isOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    isOnCooldown = false;
                    cooldownTimer = 0f;
                }
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            base.ExecuteSkillEffect(targetPosition, casterTransform);

            FindReferences();

            if (!CanCastSkill() || bulletPrefab == null || tileGrid == null || playerShoot == null || playerMovement == null) return;

            Transform firePoint = playerShoot.GetBulletSpawnPoint();
            if (firePoint == null)
            {
                return;
            }

            Vector3 spawnPosition = firePoint.position;

            // FIX: Get the player's logical grid position from PlayerMovement, just like PlayerShoot.cs does.
            Vector2Int spawnGridPos = playerMovement.GetCurrentGridPosition();

            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

            EEBullet bulletScript = bullet.GetComponent<EEBullet>();
            if (bulletScript != null)
            {
                // FIX: Call the new, unified Initialize method with all correct parameters.
                bulletScript.Initialize(Vector2.right, bulletSpeed, bulletDamage, enemyStunDuration, tileGrid, spawnGridPos);

                AudioManager.Instance?.PlayGridLockSFX();

                ConsumeResources();
                StartCooldown();

                Debug.Log($"GridLock: Fired from logical grid position {spawnGridPos}");
            }
            else
            {
                Destroy(bullet);
            }
        }

        public bool CanCastSkill()
        {
            if (isOnCooldown)
            {
                return false;
            }

            if (playerStats != null && !playerStats.TryUseMana(0))
            {
                return false;
            }

            return true;
        }

        private void ConsumeResources()
        {
            if (playerStats != null)
            {
                playerStats.TryUseMana(manaCost);
            }
        }

        private void StartCooldown()
        {
            isOnCooldown = true;
            cooldownTimer = cooldownDuration;
        }
    }
}