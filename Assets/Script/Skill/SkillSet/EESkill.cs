using System.Collections;
using UnityEngine;

namespace SkillSystem
{
    public class EESkill : Skill
    {
        [Header("Bullet Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 10f;
        [SerializeField] private int bulletDamage = 10;
        [SerializeField] private float enemyStunDuration = 3f;
        
        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            base.ExecuteSkillEffect(targetPosition, casterTransform);
            
            if (bulletPrefab != null)
            {
                // Create bullet at caster position
                GameObject bullet = Instantiate(bulletPrefab, casterTransform.position, Quaternion.identity);
                
                // Get the bullet component
                EEBullet bulletScript = bullet.GetComponent<EEBullet>();
                if (bulletScript != null)
                {
                    // Direction is now always to the right regardless of target position
                    Vector2 direction = Vector2.right;
                    
                    // Initialize the bullet with properties
                    bulletScript.Initialize(direction, bulletSpeed, bulletDamage, enemyStunDuration);
                }
                else
                {
                    Debug.LogWarning("Bullet prefab is missing EEBullet component!");
                }
            }
            else
            {
                Debug.LogWarning("No bullet prefab assigned for EE skill!");
            }
        }
    }
}