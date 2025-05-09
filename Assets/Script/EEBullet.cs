using UnityEngine;

namespace SkillSystem
{
    public class EEBullet : MonoBehaviour
    {
        [Header("Bullet Properties")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private string targetTag = "Enemy";
        
        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitEffect;
        
        private Rigidbody2D rb;
        
        private void Awake()
        {
            // Get or add rigidbody component
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f; // No gravity
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            
            // Add collider if not present
            if (GetComponent<Collider2D>() == null)
            {
                CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.1f;
            }
        }
        
        private void Start()
        {
            // Destroy after lifetime
            Destroy(gameObject, lifetime);
        }
        
        public void Initialize(Vector2 direction, float speed, int damage, float stunTime)
        {
            this.damage = damage;
            this.stunDuration = stunTime;
            
            // Set velocity
            rb.linearVelocity = direction.normalized * speed;
            
            // Rotate to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.eulerAngles = new Vector3(0, 0, angle);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if we hit an object with the target tag
            if (other.CompareTag(targetTag))
            {
                // Try to get enemy component
                var enemy = other.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // Apply damage
                    enemy.TakeDamage(damage);
                    
                    // Stun the enemy
                    enemy.Stun(stunDuration);
                }
                
                // Spawn hit effect if available
                if (hitEffect != null)
                {
                    Instantiate(hitEffect, transform.position, Quaternion.identity);
                }
                
                // Destroy bullet
                Destroy(gameObject);
            }
        }
        
        // Optional method to stop the bullet from the outside (e.g., hitting walls)
        public void Stop(bool createEffect = true)
        {
            rb.linearVelocity = Vector2.zero;
            
            // Disable collider
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            
            // Disable sprite renderer
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            
            // Stop trail
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }
            
            // Spawn hit effect if available
            if (createEffect && hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy after a short delay to allow effects to finish
            Destroy(gameObject, 0.5f);
        }
    }
}