using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class WESkill : Skill
    {
        [Header("Skill Properties")]
        [SerializeField] private int damageAmount = 25;
        [SerializeField] private float knockbackForce = 1f;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private GameObject aoeIndicatorPrefab;
        [SerializeField] private float indicatorDuration = 0.5f;
        [SerializeField] private Color indicatorColor = new Color(0, 1, 1, 0.5f); // Cyan semi-transparent
        
        private TileGrid tileGrid;
        private List<GameObject> temporaryEffects = new List<GameObject>();

        private void Awake()
        {
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("WESkill: Could not find TileGrid component!");
            }
        }

        private void OnDestroy()
        {
            // Clean up any remaining indicators
            CleanupTemporaryEffects();
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Use the caster's position as the base for the skill instead of target position
            Vector2Int casterPosition = tileGrid.GetGridPosition(casterTransform.position);
            
            // Get the direction the caster is facing (assuming player is facing right towards enemies)
            Vector2Int direction = Vector2Int.right; // Default direction (towards enemy side)

            // Calculate the hit pattern:
            // Row 1: 1 tile in front (up, mid, down)
            // Row 2: 2 tiles in front (up, mid, down)
            List<Vector2Int> hitPositions = new List<Vector2Int>
            {
                // Row 1: 1 tile in front
                casterPosition + direction + Vector2Int.up,      // Front top
                casterPosition + direction,                      // Front mid
                casterPosition + direction + Vector2Int.down,    // Front down
                
                // Row 2: 2 tiles in front
                casterPosition + direction * 2 + Vector2Int.up,  // 2nd row top
                casterPosition + direction * 2,                  // 2nd row mid
                casterPosition + direction * 2 + Vector2Int.down // 2nd row down
            };

            // Show visual indicator for the skill area
            ShowAreaOfEffectIndicator(hitPositions);
            
            // Apply the skill effect to each position
            foreach (Vector2Int hitPos in hitPositions)
            {
                if (tileGrid.IsValidGridPosition(hitPos))
                {
                    // Find enemy at this position
                    Enemy enemy = FindEnemyAtPosition(hitPos);
                    
                    if (enemy != null)
                    {
                        // Apply damage to the enemy
                        enemy.TakeDamage(damageAmount);
                        
                        // Try to apply knockback
                        TryKnockback(enemy, hitPos, direction);
                        
                        // Spawn hit effect
                        if (hitEffect != null)
                        {
                            Instantiate(hitEffect, tileGrid.GetWorldPosition(hitPos), Quaternion.identity);
                        }
                        
                        // Play hit sound
                        if (hitSound != null)
                        {
                            AudioSource.PlayClipAtPoint(hitSound, tileGrid.GetWorldPosition(hitPos));
                        }
                    }
                }
            }
        }
        
        // Helper method to find an enemy at a grid position
        private Enemy FindEnemyAtPosition(Vector2Int gridPos)
        {
            Vector3 worldPos = tileGrid.GetWorldPosition(gridPos);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.4f);
            
            foreach (Collider2D collider in colliders)
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    return enemy;
                }
            }
            
            return null;
        }
        
        // Try to knockback an enemy
        private void TryKnockback(Enemy enemy, Vector2Int currentPos, Vector2Int direction)
        {
            // Calculate the knockback destination (1 tile behind the enemy in the same direction)
            Vector2Int knockbackPos = currentPos + direction;
            
            // Check if knockback position is valid
            if (!tileGrid.IsValidGridPosition(knockbackPos))
            {
                return; // Can't knockback out of bounds
            }
            
            // Check if the knockback destination is a broken tile
            if (tileGrid.grid[knockbackPos.x, knockbackPos.y] == TileType.Broken)
            {
                return; // Can't knockback into broken tiles
            }
            
            // Check if there's an obstacle at knockback position
            Enemy obstacleEnemy = FindEnemyAtPosition(knockbackPos);
            
            if (obstacleEnemy != null)
            {
                // There's another enemy blocking the knockback
                
                // Check if we can knockback both (check position behind obstacle)
                Vector2Int doubleKnockbackPos = knockbackPos + direction;
                
                if (!tileGrid.IsValidGridPosition(doubleKnockbackPos) || 
                    tileGrid.grid[doubleKnockbackPos.x, doubleKnockbackPos.y] == TileType.Broken ||
                    FindEnemyAtPosition(doubleKnockbackPos) != null)
                {
                    // Can't knockback both - there's no space behind obstacle
                    return;
                }
                
                // Both can be knocked back
                ApplyKnockback(obstacleEnemy, knockbackPos, doubleKnockbackPos);
                ApplyKnockback(enemy, currentPos, knockbackPos);
            }
            else
            {
                // No obstacle, can knockback freely
                ApplyKnockback(enemy, currentPos, knockbackPos);
            }
        }
        
        private void ApplyKnockback(Enemy enemy, Vector2Int startPos, Vector2Int endPos)
        {
            // Get the world positions
            Vector3 startWorldPos = tileGrid.GetWorldPosition(startPos);
            Vector3 endWorldPos = tileGrid.GetWorldPosition(endPos);

            // Force the enemy to this new position (fixed knockback issue)
            StartCoroutine(SmoothKnockback(enemy, startWorldPos, endWorldPos));
        }
        
        private IEnumerator SmoothKnockback(Enemy enemy, Vector3 startPos, Vector3 endPos)
        {
            if (enemy == null) yield break;
            
            // Visual feedback
            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            Color originalColor = Color.white;
            
            if (renderer != null)
            {
                originalColor = renderer.color;
                renderer.color = Color.yellow;
            }
            
            // Perform smooth movement
            float elapsedTime = 0;
            float duration = 0.2f; // Fast knockback
            
            while (elapsedTime < duration)
            {
                if (enemy == null) yield break; // Check if enemy still exists

                // Calculate position with slight arc for better visual
                float t = elapsedTime / duration;
                Vector3 arcPoint = Vector3.Lerp(startPos, endPos, t);
                arcPoint.y += Mathf.Sin(t * Mathf.PI) * 0.2f; // Small arc
                
                // Move the enemy
                enemy.transform.position = arcPoint;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we end at exactly the target position
            if (enemy != null)
            {
                enemy.transform.position = endPos;
                
                // Reset color
                if (renderer != null)
                {
                    renderer.color = originalColor;
                }
            }
        }
        
        // Visual indicator for area of effect
        private void ShowAreaOfEffectIndicator(List<Vector2Int> positions)
        {
            // Clean up any existing indicators
            CleanupTemporaryEffects();
            
            // Create new indicators for each position in the attack
            foreach (Vector2Int pos in positions)
            {
                if (tileGrid.IsValidGridPosition(pos))
                {
                    Vector3 worldPos = tileGrid.GetWorldPosition(pos);
                    
                    // Create indicator
                    GameObject indicator;
                    
                    if (aoeIndicatorPrefab != null)
                    {
                        // Use provided prefab
                        indicator = Instantiate(aoeIndicatorPrefab, worldPos, Quaternion.identity);
                    }
                    else
                    {
                        // Create a simple indicator if no prefab is provided
                        indicator = CreateSimpleIndicator(worldPos);
                    }
                    
                    temporaryEffects.Add(indicator);
                }
            }
            
            // Schedule cleanup
            StartCoroutine(CleanupIndicatorsAfterDelay());
        }
        
        private GameObject CreateSimpleIndicator(Vector3 position)
        {
            GameObject indicator = new GameObject("AOE_Indicator");
            indicator.transform.position = position;
            
            // Create a simple visual (sprite)
            SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = indicatorColor;
            renderer.sortingOrder = 1; // Above ground tiles
            
            return indicator;
        }
        
        private Sprite CreateSquareSprite()
        {
            // Create a simple square sprite at runtime
            Texture2D texture = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];
            
            for (int i = 0; i < colors.Length; i++)
            {
                // Create a square with outlined edge
                int x = i % 32;
                int y = i / 32;
                
                if (x < 2 || x >= 30 || y < 2 || y >= 30)
                {
                    colors[i] = Color.white; // Border
                }
                else
                {
                    colors[i] = new Color(1, 1, 1, 0.3f); // Semi-transparent fill
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }
        
        private IEnumerator CleanupIndicatorsAfterDelay()
        {
            yield return new WaitForSeconds(indicatorDuration);
            CleanupTemporaryEffects();
        }
        
        private void CleanupTemporaryEffects()
        {
            foreach (GameObject effect in temporaryEffects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            
            temporaryEffects.Clear();
        }
    }
}
