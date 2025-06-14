using UnityEngine;
using System.Collections.Generic;

namespace SkillSystem
{
    public class QuickSlash : Skill
    {
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private float effectRadius = 0.5f;
        [SerializeField] public float manaCost = 2f;
        [SerializeField] public float cooldownDuration = 2f;

        private TileGrid tileGrid;
        private PlayerStats playerStats;

        private void Awake()
        {
            tileGrid = FindObjectOfType<TileGrid>();
            playerStats = FindObjectOfType<PlayerStats>();
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            if (tileGrid == null || playerStats == null) return;
            if (!playerStats.TryUseMana(manaCost)) return;

            Vector2Int playerGridPos = tileGrid.GetGridPosition(casterTransform.position);

            // Match SwiftStrike's vertical forward line
            List<Vector2Int> damageGridPositions = new List<Vector2Int>
            {
                new Vector2Int(playerGridPos.x + 2, playerGridPos.y - 1), // front-up
                new Vector2Int(playerGridPos.x + 2, playerGridPos.y),     // front-middle
                new Vector2Int(playerGridPos.x + 2, playerGridPos.y - 2)  // front-down
            };


            float yOffset = 0f;
            PlayerMovement move = casterTransform.GetComponent<PlayerMovement>();
            if (move != null) yOffset = move.GetPositionOffset().y;

            foreach (Vector2Int gridPos in damageGridPositions)
            {
                if (!tileGrid.IsValidGridPosition(gridPos)) continue;

                Vector3 basePos = tileGrid.GetWorldPosition(gridPos);
                Vector3 center = basePos + new Vector3(tileGrid.GetTileWidth(), tileGrid.GetTileHeight()) * 0.5f;
                center += new Vector3(0, yOffset, 0);

                Debug.DrawLine(casterTransform.position, center, Color.red, 1f);

                Collider2D[] hits = Physics2D.OverlapCircleAll(center, effectRadius);
                foreach (Collider2D hit in hits)
                {
                    if (hit.CompareTag("Enemy") && hit.TryGetComponent(out Enemy enemy))
                    {
                        enemy.TakeDamage(damageAmount);
                        Debug.Log($"QuickSlash hit: {hit.name} for {damageAmount} damage");
                    }
                }
            }

            base.ExecuteSkillEffect(targetPosition, casterTransform);
            ResetMeleeAnimation();
        }

        private void ResetMeleeAnimation()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Animator animator = player.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.ResetTrigger("QuickSlash");
                    animator.Play("PlayerIdle");
                }
            }
        }
    }
}
