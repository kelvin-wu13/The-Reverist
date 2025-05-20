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
        [SerializeField] private float movementLockDuration = 1f; // Duration to lock movement after knockback

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

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
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
                        TryKnockback(enemy, hitPos, direction);

                        if (hitEffect != null)
                            Instantiate(hitEffect, tileGrid.GetWorldPosition(hitPos), Quaternion.identity);

                        if (hitSound != null)
                            AudioSource.PlayClipAtPoint(hitSound, tileGrid.GetWorldPosition(hitPos));
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

        private void TryKnockback(Enemy enemy, Vector2Int currentPos, Vector2Int direction)
        {
            Vector2Int knockbackPos = currentPos + direction;

            if (!tileGrid.IsValidGridPosition(knockbackPos)) return;
            if (tileGrid.grid[knockbackPos.x, knockbackPos.y] == TileType.Broken) return;

            Enemy obstacleEnemy = FindEnemyAtPosition(knockbackPos);

            if (obstacleEnemy != null)
            {
                Vector2Int doubleKnockbackPos = knockbackPos + direction;

                if (!tileGrid.IsValidGridPosition(doubleKnockbackPos) ||
                    tileGrid.grid[doubleKnockbackPos.x, doubleKnockbackPos.y] == TileType.Broken ||
                    FindEnemyAtPosition(doubleKnockbackPos) != null)
                    return;

                ApplyKnockback(obstacleEnemy, knockbackPos, doubleKnockbackPos);
                ApplyKnockback(enemy, currentPos, knockbackPos);
            }
            else
            {
                ApplyKnockback(enemy, currentPos, knockbackPos);
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

                // Note: The movement lock for 1 second is now handled in the SetPositionWithOffset method
            }
        }
    }
}