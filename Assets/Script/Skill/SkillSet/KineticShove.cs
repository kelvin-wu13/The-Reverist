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
        [SerializeField] private float stunDuration = 2f;

        private TileGrid tileGrid;
        private PlayerStats playerStats;

        private void Awake()
        {
            tileGrid = FindObjectOfType<TileGrid>();
            playerStats = FindObjectOfType<PlayerStats>();

            if (tileGrid == null)
                Debug.LogError("KineticShove: TileGrid not found!");

            if (playerStats == null)
                Debug.LogWarning("KineticShove: PlayerStats not found!");
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            if (playerStats == null || !playerStats.TryUseMana(manaCost))
            {
                Debug.Log("Not enough mana to cast KineticShove!");
                return;
            }

            AudioManager.Instance?.PlayKineticShoveSFX();

            Vector2Int casterPos = tileGrid.GetGridPosition(casterTransform.position);

            PlayerMovement movement = casterTransform.GetComponent<PlayerMovement>();
            Vector2Int dir = movement != null ? movement.GetFacingDirection() : Vector2Int.right;

            List<Vector2Int> hitPositions = new List<Vector2Int>
            {
                casterPos + dir,
                casterPos + dir + Vector2Int.up,
                casterPos + dir + Vector2Int.down,
                casterPos + dir * 2,
                casterPos + dir * 2 + Vector2Int.up,
                casterPos + dir * 2 + Vector2Int.down
            };

            DamageEnemiesOnTiles(hitPositions, dir);
        }

        private void DamageEnemiesOnTiles(List<Vector2Int> gridPositions, Vector2Int dir)
        {
            Enemy[] allEnemies = FindObjectsOfType<Enemy>();

            foreach (Vector2Int gridPos in gridPositions)
            {
                if (!tileGrid.IsValidGridPosition(gridPos)) continue;

                foreach (Enemy enemy in allEnemies)
                {
                    if (enemy == null) continue;

                    Vector2Int enemyGridPos = enemy.GetCurrentGridPosition();

                    if (enemyGridPos == gridPos)
                    {
                        enemy.TakeDamage(damageAmount);
                        ProcessKnockbackOrStun(enemy, gridPos, dir);
                        Debug.Log($"KineticShove HIT {enemy.name} at {enemyGridPos}");
                    }
                }
            }
        }

        private void ProcessKnockbackOrStun(Enemy enemy, Vector2Int currentPos, Vector2Int direction)
        {
            Vector2Int knockbackPos = currentPos + direction;

            if (CanKnockbackTo(knockbackPos))
            {
                ApplyKnockback(enemy, currentPos, knockbackPos);
                Debug.Log($"Enemy at {currentPos} knocked back to {knockbackPos}");
            }
            else
            {
                ApplyStun(enemy, currentPos, knockbackPos);
                Debug.Log($"Enemy at {currentPos} stunned due to obstacle at {knockbackPos}");
            }
        }

        private bool CanKnockbackTo(Vector2Int pos)
        {
            if (!tileGrid.IsValidGridPosition(pos)) return false;
            if (tileGrid.IsTileOccupied(pos)) return false;
            return true;
        }

        private void ApplyStun(Enemy enemy, Vector2Int fromPos, Vector2Int blockedPos)
        {
            enemy.Stun(stunDuration);
            StartCoroutine(ShowCollisionFeedback(enemy));
        }

        private IEnumerator ShowCollisionFeedback(Enemy enemy)
        {
            if (enemy == null) yield break;

            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            if (renderer == null) yield break;

            Color original = renderer.color;
            Color flashColor = Color.red;
            float duration = 0.1f;

            for (int i = 0; i < 3; i++)
            {
                renderer.color = flashColor;
                yield return new WaitForSeconds(duration);
                renderer.color = original;
                yield return new WaitForSeconds(duration);
            }
        }

        private void ApplyKnockback(Enemy enemy, Vector2Int start, Vector2Int end)
        {
            Vector3 startPos = tileGrid.GetCenteredWorldPosition(start);
            Vector3 endPos = tileGrid.GetCenteredWorldPosition(end);
            StartCoroutine(SmoothKnockback(enemy, startPos, endPos, start, end));
        }

        private IEnumerator SmoothKnockback(Enemy enemy, Vector3 start, Vector3 end, Vector2Int startGrid, Vector2Int endGrid)
        {
            if (enemy == null) yield break;

            SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
            Color original = renderer != null ? renderer.color : Color.white;

            if (renderer != null)
                renderer.color = Color.yellow;

            float duration = 0.2f;
            float time = 0f;

            while (time < duration)
            {
                if (enemy == null) yield break;

                float t = time / duration;
                Vector3 arcPos = Vector3.Lerp(start, end, t);
                arcPos.y += Mathf.Sin(t * Mathf.PI) * 0.2f;
                enemy.transform.position = arcPos;

                time += Time.deltaTime;
                yield return null;
            }

            if (enemy != null)
            {
                enemy.SetPositionWithOffset(endGrid);
                if (renderer != null)
                    renderer.color = original;
            }
        }
    }
}
