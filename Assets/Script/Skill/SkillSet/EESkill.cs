using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class SimpleEESkill : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float bulletSpeed = 10f;
        [SerializeField] private int bulletDamage = 10;
        [SerializeField] private float enemyStunDuration = 3f;

        [Header("Casting Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private float cooldownDuration = 1f;
        [SerializeField] private KeyCode fireKey = KeyCode.Space;
        private bool canFire = true;
        
        private void Awake()
        {
            // If no fire point is set, use this object's position
            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Update()
        {
            // Fire with specified key or left mouse button
            if ((Input.GetKeyDown(fireKey) || Input.GetMouseButtonDown(0)) && canFire)
            {
                FireBullet();
            }
        }

        public void FireBullet()
        {
            if (!canFire || bulletPrefab == null) return;
            
            // Create bullet at fire point
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            
            // Get the bullet component
            EEBullet bulletScript = bullet.GetComponent<EEBullet>();
            if (bulletScript != null)
            {
                // Set direction based on fire point's right vector
                Vector2 direction = firePoint.right;
                
                // Initialize the bullet with properties
                bulletScript.Initialize(direction, bulletSpeed, bulletDamage, enemyStunDuration);
            }
            else
            {
                Debug.LogWarning("Bullet prefab is missing EEBullet component!");
            }
            
            // Start cooldown
            StartCoroutine(Cooldown());
        }
        
        private IEnumerator Cooldown()
        {
            canFire = false;
            yield return new WaitForSeconds(cooldownDuration);
            canFire = true;
        }
        
        // Optional: Fire in a specific direction (for custom aiming)
        public void FireInDirection(Vector2 direction)
        {
            if (!canFire || bulletPrefab == null) return;
            
            // Create bullet at fire point
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            
            // Get the bullet component
            EEBullet bulletScript = bullet.GetComponent<EEBullet>();
            if (bulletScript != null)
            {
                // Initialize the bullet with properties
                bulletScript.Initialize(direction, bulletSpeed, bulletDamage, enemyStunDuration);
            }
            
            // Start cooldown
            StartCoroutine(Cooldown());
        }
    }
}
