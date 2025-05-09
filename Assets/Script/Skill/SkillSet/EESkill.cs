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
                // Calculate direction from caster to target
                Vector3 worldTargetPos = new Vector3(targetPosition.x, targetPosition.y, 0);
                Vector2 direction = (worldTargetPos - casterTransform.position).normalized;
                
                // Create bullet at caster position
                GameObject bullet = Instantiate(bulletPrefab, casterTransform.position, Quaternion.identity);
                
                // Get the bullet component
                EEBullet bulletScript = bullet.GetComponent<EEBullet>();
                if (bulletScript != null)
                {
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