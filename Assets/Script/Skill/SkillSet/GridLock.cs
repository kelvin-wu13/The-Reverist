using System.Collections;
using UnityEngine;

namespace SkillSystem
{
    public class GridLock : Skill
    {
        [Header("Projectile Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 5f; // Manually editable speed for EEBullet
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
        private PlayerShoot playerShoot; // Reference to get FirePoint
        
        private void Awake()
        {
            FindReferences();
        }
        
        private void FindReferences()
        {
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("TileGrid not found in scene!");
                }
            }
            
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
                if (playerStats == null)
                {
                    Debug.LogError("PlayerStats not found in scene!");
                }
            }
            
            if (playerShoot == null)
            {
                playerShoot = FindObjectOfType<PlayerShoot>();
                if (playerShoot == null)
                {
                    Debug.LogError("PlayerShoot not found in scene!");
                }
            }
        }
        
        private void Update()
        {
            // Handle cooldown timer if skill is on cooldown
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

            // Check if we can cast the skill
            if (!CanCastSkill() || bulletPrefab == null || tileGrid == null || playerShoot == null) return;
            
            // Get player's FirePoint position (same as regular bullets)
            Transform firePoint = playerShoot.GetBulletSpawnPoint();
            if (firePoint == null)
            {
                Debug.LogError("FirePoint not found on PlayerShoot!");
                return;
            }
            
            Vector3 spawnPosition = firePoint.position;
            
            // Create bullet at FirePoint position (same as regular bullets)
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

            // Get the bullet component
            EEBullet bulletScript = bullet.GetComponent<EEBullet>();
            if (bulletScript != null)
            {
                // Initialize the bullet with properties - will always fire to the right (east)
                bulletScript.InitializeGridBased(bulletSpeed, bulletDamage, enemyStunDuration, tileGrid);
                
                AudioManager.Instance?.PlayGridLockSFX();
                
                // Skill has been executed, consume resources and start cooldown
                ConsumeResources();
                StartCooldown();
                
                Debug.Log($"GridLock: Fired from FirePoint at world position {spawnPosition}");
            }
            else
            {
                Debug.LogWarning("Bullet prefab is missing EEBullet component!");
                Destroy(bullet);
            }
        }
        
        // Method to check if this skill can be cast
        public bool CanCastSkill()
        {
            // Check cooldown status
            if (isOnCooldown)
            {
                Debug.Log($"GridLock on cooldown for {cooldownTimer:F1} more seconds");
                return false;
            }
            
            // Check mana
            if (playerStats != null && !playerStats.TryUseMana(0)) // Just check without consuming
            {
                Debug.Log("Not enough mana to cast GridLock");
                return false;
            }
            
            return true;
        }
        
        // Method to consume resources when skill is cast
        private void ConsumeResources()
        {
            // Consume mana
            if (playerStats != null)
            {
                playerStats.TryUseMana(manaCost);
            }
        }
        
        // Start the cooldown for this skill
        private void StartCooldown()
        {
            isOnCooldown = true;
            cooldownTimer = cooldownDuration;
        }
    }
}