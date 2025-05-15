using UnityEngine;

namespace SkillSystem
{
    public class EEBullet : MonoBehaviour
    {
        [Header("Bullet Properties")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private string targetTag = "Enemy";
        
        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private float fadeOutTime = 0.1f;
        
        // Reference to tile grid for position checks
        private TileGrid tileGrid;
        private Vector2Int currentGridPosition;
        private bool isDestroying = false;
        
        private void Awake()
        {
            // Get reference to the TileGrid in the scene
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("TileGrid not found in scene!");
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
            // Set initial grid position
            if (tileGrid != null)
            {
                currentGridPosition = tileGrid.GetGridPosition(transform.position);
            }
            
            // Destroy after lifetime as a fallback
            Destroy(gameObject, lifetime);
        }
        
        private void Update()
        {
            if (isDestroying) return;
            
            // Move the bullet straight to the right
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
            
            if (tileGrid != null)
            {
                // Check if we've moved to a new grid position
                Vector2Int newGridPosition = tileGrid.GetGridPosition(transform.position);
                
                // If we changed grid cells, check for hits and boundaries
                if (newGridPosition != currentGridPosition)
                {
                    currentGridPosition = newGridPosition;
                    
                    // Check if bullet has gone past the rightmost grid
                    CheckIfPastRightmostGrid();
                }
            }
        }
        
        public void Initialize(Vector2 direction, float bulletSpeed, int bulletDamage, float stunTime)
        {
            // Only use the speed from parameters, direction will always be right
            this.speed = bulletSpeed;
            this.damage = bulletDamage;
            this.stunDuration = stunTime;
            
            // Set rotation to face right
            transform.rotation = Quaternion.identity;
        }
        
        private void CheckIfPastRightmostGrid()
        {
            // If we're beyond the rightmost enemy grid column, destroy the bullet
            if (currentGridPosition.x >= tileGrid.gridWidth)
            {
                DestroyBullet();
                return;
            }
            
            // If we're in an invalid position, destroy the bullet
            if (!tileGrid.IsValidGridPosition(currentGridPosition))
            {
                DestroyBullet();
                return;
            }
            
            // Check if we've passed the rightmost enemy column
            // In your grid setup, enemies occupy the right half of the grid
            if (currentGridPosition.x > tileGrid.gridWidth - 1)
            {
                DestroyBullet();
            }
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
                DestroyBullet();
            }
        }
        
        private void DestroyBullet()
        {
            if (isDestroying) return;
            isDestroying = true;
            
            // Disable collider if present
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            
            // Start fade-out animation
            StartCoroutine(FadeOutAndDestroy());
        }
        
        private System.Collections.IEnumerator FadeOutAndDestroy()
        {
            float startAlpha = 1f;
            float elapsedTime = 0;
            
            // Get sprite renderer (if any)
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Fade out the sprite
            while (elapsedTime < fadeOutTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutTime);
                
                // Apply alpha if sprite renderer exists
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = alpha;
                    spriteRenderer.color = color;
                }
                
                yield return null;
            }
            
            // Destroy the gameObject
            Destroy(gameObject);
        }
    }
}