using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class EEBullet : MonoBehaviour
    {
        [Header("Bullet Properties")]
        [SerializeField] private int damage = 15;
        [SerializeField] private float tileMoveDuration = 0.3f; // Time to traverse one tile
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private string targetTag = "Enemy";
        
        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private float fadeOutTime = 0.2f;
        [SerializeField] private bool showDebugInfo = true;
        
        // Grid movement properties
        private TileGrid tileGrid;
        private Vector2Int currentGridPosition;
        private Vector2Int targetGridPosition;
        private bool isMoving = false;
        private bool isDestroying = false;
        
        private void Awake()
        {
            // Add collider if not present
            if (GetComponent<Collider2D>() == null)
            {
                CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.25f;
            }
        }
        
        public void InitializeGridBased(Vector2Int startPosition, float movementSpeed, int bulletDamage, float enemyStunDuration, TileGrid grid)
        {
            this.tileGrid = grid;
            this.currentGridPosition = startPosition;
            this.damage = bulletDamage;
            this.stunDuration = enemyStunDuration;
            
            // Convert speed (tiles/second) to duration (seconds/tile)
            if (movementSpeed > 0)
                this.tileMoveDuration = 1f / movementSpeed;
            
            // Snap to the grid position
            transform.position = tileGrid.GetWorldPosition(currentGridPosition);
            
            // Start grid-based movement
            StartGridMovement();
        }
        
        private void StartGridMovement()
        {
            if (tileGrid == null) return;
            
            StartCoroutine(MoveAlongGrid());
        }

        private IEnumerator MoveAlongGrid()
        {
            // Continue moving until we hit something or go off-grid
            while (!isDestroying)
            {
                // Calculate next grid position (one tile to the right)
                Vector2Int nextPosition = currentGridPosition + Vector2Int.right;

                // Check if the next position is valid
                if (!tileGrid.IsValidGridPosition(nextPosition))
                {
                    // We've reached the edge of the grid, destroy the bullet
                    if (showDebugInfo) Debug.Log("Bullet reached grid boundary at " + nextPosition);
                    DestroyBullet();
                    yield break;
                }

                // Move to the next position first, then check for enemies
                isMoving = true;
                targetGridPosition = nextPosition;

                Vector3 startPos = transform.position;
                Vector3 endPos = tileGrid.GetWorldPosition(targetGridPosition);

                float elapsedTime = 0;
                while (elapsedTime < tileMoveDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float percent = elapsedTime / tileMoveDuration;
                    transform.position = Vector3.Lerp(startPos, endPos, percent);
                    yield return null;
                }

                // Ensure we land exactly on the target position
                transform.position = endPos;
                currentGridPosition = targetGridPosition;
                isMoving = false;

                // Now check if there's an enemy at our current tile
                GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
                foreach (GameObject enemy in enemies)
                {
                    Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemy.transform.position);
                    if (enemyGridPos == currentGridPosition)
                    {
                        Enemy enemyComponent = enemy.GetComponent<Enemy>();
                        if (enemyComponent != null)
                        {
                            enemyComponent.TakeDamage(damage);
                            enemyComponent.Stun(stunDuration);

                            if (hitEffect != null)
                            {
                                Instantiate(hitEffect, transform.position, Quaternion.identity);
                            }

                            DestroyBullet();
                            yield break;
                        }
                    }
                }
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
        
        private IEnumerator FadeOutAndDestroy()
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
        
        // Draw gizmos to visualize the bullet's path
        private void OnDrawGizmos()
        {
            if (tileGrid != null && showDebugInfo && !isDestroying)
            {
                // Draw a circle at the current position
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
                
                // Draw a circle at the next position
                Vector2Int nextPos = currentGridPosition + Vector2Int.right;
                if (tileGrid.IsValidGridPosition(nextPos))
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(tileGrid.GetWorldPosition(nextPos), 0.2f);
                }
            }
        }
    }
}