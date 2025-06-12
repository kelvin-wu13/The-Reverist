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
            FindTileGrid();
            FindPlayerStats();
        }

        private void FindTileGrid()
        {
            if (tileGrid == null)
            {
                // Find the TileGrid in the scene
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("WQSkill: Could not find TileGrid in the scene!");
                }
            }
        }

        private void FindPlayerStats()
        {
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
                if (playerStats == null)
                {
                    Debug.LogWarning("WQSkill: Could not find PlayerStats in the scene. Mana consumption will not work correctly.");
                }
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            Debug.Log($"Executing WQ skill at grid position {targetPosition}");

            if (tileGrid == null) tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("WQSkill: Could not find TileGrid in the scene!");
                return;
            }

            if (playerStats == null)
            {
                playerStats = casterTransform.GetComponent<PlayerStats>();
                if (playerStats == null)
                {
                    playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
                    if (playerStats == null)
                    {
                        Debug.Log("QuickSlash : Can't find player stats");
                        return;
                    }
                }
            }

            if (!playerStats.TryUseMana(manaCost))
            {
                Debug.Log($"QuickSlash: Not enough mana! Required: {manaCost}, Current: {playerStats.CurrentMana}");
                return;
            }

            // Always face right
            Vector2Int playerGridPos = tileGrid.GetGridPosition(casterTransform.position);

            // Hit 3 vertical tiles in front (x + 1)
            List<Vector2Int> damageGridPositions = new List<Vector2Int>
            {
                new Vector2Int(playerGridPos.x + 1, playerGridPos.y - 1),
                new Vector2Int(playerGridPos.x + 1, playerGridPos.y),
                new Vector2Int(playerGridPos.x + 1, playerGridPos.y + 1)
            };

            // Get Y offset from PlayerMovement (e.g. 1)
            float yOffset = 0f;
            PlayerMovement move = casterTransform.GetComponent<PlayerMovement>();
            if (move != null) yOffset = move.GetPositionOffset().y;

            List<Vector2> damageWorldPositions = new List<Vector2>();
            foreach (Vector2Int gridPos in damageGridPositions)
            {
                if (tileGrid.IsValidGridPosition(gridPos))
                {
                    Vector3 basePos = tileGrid.GetWorldPosition(gridPos);
                    Vector3 tileCenter = basePos + new Vector3(tileGrid.GetTileWidth(), tileGrid.GetTileHeight()) * 0.5f;
                    tileCenter += new Vector3(0, yOffset, 0);
                    damageWorldPositions.Add(tileCenter);
                }
            }

            foreach (Vector2 pos in damageWorldPositions)
            {
                Debug.DrawLine(casterTransform.position, pos, Color.red, 1f);

                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, effectRadius);
                foreach (Collider2D collider in hitColliders)
                {
                    if (collider.CompareTag("Enemy"))
                    {
                        Enemy enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(damageAmount);
                            Debug.Log($"WQ Skill hit enemy: {collider.name} for {damageAmount} damage");
                        }
                        else
                        {
                            Debug.LogWarning($"Enemy tag on {collider.name} but no Enemy script");
                        }
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

                    // Optional: force transition to idle
                    animator.Play("PlayerIdle"); // replace "Idle" with your real idle state name

                    Debug.Log("QuickSlash: Reset melee animation state");
                }
            }
        }
    }
}