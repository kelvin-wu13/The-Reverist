using System.Collections;
using UnityEngine;

namespace SkillSystem
{
    public class GridLock : Skill
    {
        [Header("Projectile Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 2f; // Speed in tiles per second
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
        
        private void Awake()
        {
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("TileGrid not found in scene!");
            }
            
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("PlayerStats not found in scene!");
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

            // Check if we can cast the skill
            if (!CanCastSkill() || bulletPrefab == null || tileGrid == null) return;
            
            // Get player's current grid position
            Vector2Int playerGridPos = tileGrid.GetGridPosition(casterTransform.position);
            
            // Create bullet at player's grid position
            Vector3 spawnPosition = tileGrid.GetWorldPosition(playerGridPos);
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

            // Get the bullet component
            EEBullet bulletScript = bullet.GetComponent<EEBullet>();
            if (bulletScript != null)
            {
                // Initialize the bullet with properties - will always fire to the right (east)
                bulletScript.InitializeGridBased(playerGridPos, bulletSpeed, bulletDamage, enemyStunDuration, tileGrid);
                
                if (showDebugPath)
                {
                    // Draw debug path from player position to the right edge of the grid
                    Debug.DrawLine(
                        tileGrid.GetWorldPosition(playerGridPos),
                        tileGrid.GetWorldPosition(new Vector2Int(tileGrid.gridWidth - 1, playerGridPos.y)),
                        Color.cyan, 1.0f);
                }
                
                // Skill has been executed, consume resources and start cooldown
                ConsumeResources();
                StartCooldown();
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
        
        // Additional method to set cooldown state - can be used by skill system if needed
        public void SetCooldownState(bool onCooldown, float remainingTime = 0f)
        {
            isOnCooldown = onCooldown;
            if (onCooldown)
            {
                cooldownTimer = remainingTime > 0f ? remainingTime : cooldownDuration;
            }
            else
            {
                cooldownTimer = 0f;
            }
        }
        
        // These methods help with UI feedback for cooldowns
        public float GetCooldownDuration()
        {
            return cooldownDuration;
        }
        
        public float GetRemainingCooldown()
        {
            return cooldownTimer;
        }
        
        public bool IsOnCooldown()
        {
            return isOnCooldown;
        }
        
        public float GetManaCost()
        {
            return manaCost;
        }
    }
}