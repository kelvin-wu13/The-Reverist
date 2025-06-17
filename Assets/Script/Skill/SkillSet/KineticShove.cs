using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class KineticShove : Skill
    {
        [Header("Skill Properties")]
        [SerializeField] private int damageAmount = 25;
        [SerializeField] private float knockbackForce = 1f;
        [SerializeField] private float manaCost = 1.5f;
        [SerializeField] public float cooldownDuration = 2f;
        [SerializeField] private float stunDuration = 2f; // Duration to stun enemy when collision occurs

        private TileGrid tileGrid;
        private PlayerStats playerStats;

        private void Awake()
        {
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("WESkill: Could not find TileGrid component!");
            }

            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.Log("KineticShove: Cant find PlayerStat");
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Check mana cost
            if (playerStats == null || !playerStats.TryUseMana(manaCost))
            {
                Debug.Log("Not enough mana to cast KineticShove!");
                return;
            }

            AudioManager.Instance?.PlayKineticShoveSFX();
            
            Vector2Int casterPosition = tileGrid.GetGridPosition(casterTransform.position);
            Vector2Int direction = Vector2Int.right;

            List<Vector2Int> hitPositions = new List<Vector2Int>
            {
                casterPosition + direction + Vector2Int.up,
                casterPosition + direction,
                casterPosition + direction + Vector2Int.down,
                casterPosition + direction * 2 + Vector2Int.up,
                casterPosition + direction * 2,
                casterPosition + direction * 2 + Vector2Int.down
            };

            foreach (Vector2Int hitPos in hitPositions)
            {
                if (tileGrid.IsValidGridPosition(hitPos))
                {
                    Enemy enemy = FindEnemyAtPosition(hitPos);

                    if (enemy != null)
                    {
                        enemy.TakeDamage(damageAmount);
                        ProcessKnockbackOrStun(enemy, hitPos, direction);
                    }
                }
            }
        }

        private Enemy FindEnemyAtPosition(Vector2Int gridPos)
        {
            Vector3 worldPos = tileGrid.GetWorldPosition(gridPos);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.4f);

            foreach (Collider2D collider in colliders)
            {
                Enemy enemy = collider.GetComponent<Enemy>();
                if (enemy != null) return enemy;
            }

            return null;
        }

        private void ProcessKnockbackOrStun(Enemy enemy, Vector2Int currentPos, Vector2Int direction)
        {
            Vector2Int knockbackPos = currentPos + direction;

            // Check if knockback is possible
            bool canKnockback = CanKnockbackToPosition(knockbackPos);

            if (canKnockback)
            {
                // Apply normal knockback
                ApplyKnockback(enemy, currentPos, knockbackPos);
                Debug.Log($"Enemy at {currentPos} knocked back to {knockbackPos}");
            }
            else
            {
                // Enemy hits obstacle - apply stun instead
                ApplyCollisionStun(enemy, currentPos, knockbackPos);
                Debug.Log($"Enemy at {currentPos} stunned due to collision with obstacle at {knockbackPos}");
            }
        }

        private bool CanKnockbackToPosition(Vector2Int knockbackPos)
        {
            // Only check for enemies and objects, not tile types or bounds
            
            // Check if there's another enemy at the knockback position
            Enemy obstacleEnemy = FindEnemyAtPosition(knockbackPos);
            if (obstacleEnemy != null)
            {
                return false; // Enemy collision - will cause stun
            }

            // Check if the tile is occupied by other entities (objects, not tiles)
            if (tileGrid.IsTileOccupied(knockbackPos))
            {
                return false; // Object collision - will cause stun
            }

            // If destination is free (even if out of bounds or broken tile), allow knockback
            return true;
        }

        private void ApplyCollisionStun(Enemy enemy, Vector2Int currentPos, Vector2Int blockedPos)
        {
            // Apply stun effect to the enemy
            enemy.Stun(stunDuration);

            // Create collision effects
            Vector3 collisionWorldPos = tileGrid.GetWorldPosition(currentPos);

            // Optional: Create a brief visual indication of the blocked movement
            StartCoroutine(ShowCollisionFeedback(enemy));
        }

        private IEnumerator ShowCollisionFeedback(Enemy enemy)
        {
            if (enemy == null) yield break;

            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            if (renderer == null) yield break;

            Color originalColor = renderer.color;
            
            // Flash between original color and a collision color (like orange/red)
            Color collisionColor = Color.red;
            float flashDuration = 0.1f;
            int flashCount = 3;

            for (int i = 0; i < flashCount; i++)
            {
                renderer.color = collisionColor;
                yield return new WaitForSeconds(flashDuration);
                renderer.color = originalColor;
                yield return new WaitForSeconds(flashDuration);
            }
        }

        private void ApplyKnockback(Enemy enemy, Vector2Int startPos, Vector2Int endPos)
        {
            Vector3 startWorldPos = tileGrid.GetWorldPosition(startPos);
            Vector3 endWorldPos = tileGrid.GetWorldPosition(endPos);
            StartCoroutine(SmoothKnockback(enemy, startWorldPos, endWorldPos, startPos, endPos));
        }

        private IEnumerator SmoothKnockback(Enemy enemy, Vector3 startPos, Vector3 endPos, Vector2Int startGridPos, Vector2Int endGridPos)
        {
            if (enemy == null) yield break;

            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            Color originalColor = Color.white;

            if (renderer != null)
            {
                originalColor = renderer.color;
                renderer.color = Color.yellow;
            }

            // Preserve the enemy's position offset
            Vector2 positionOffset = enemy.GetPositionOffset();

            float elapsedTime = 0;
            float duration = 0.2f;

            while (elapsedTime < duration)
            {
                if (enemy == null) yield break;

                float t = elapsedTime / duration;
                Vector3 arcPoint = Vector3.Lerp(startPos, endPos, t);
                arcPoint.y += Mathf.Sin(t * Mathf.PI) * 0.2f;

                // Add position offset during movement
                arcPoint.x += positionOffset.x;
                arcPoint.y += positionOffset.y;

                enemy.transform.position = arcPoint;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (enemy != null)
            {
                // Update the enemy's grid position tracking with offset maintained
                enemy.SetPositionWithOffset(endGridPos);

                if (renderer != null)
                    renderer.color = originalColor;
            }
        }
    }
}