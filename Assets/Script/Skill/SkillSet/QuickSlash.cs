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
                    Debug.LogError("QQSkill: Could not find TileGrid in the scene!");
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
                    Debug.LogWarning("QQSkill: Could not find PlayerStats in the scene. Mana consumption will not work correctly.");
                }
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Log the skill execution
            Debug.Log($"Executing WQ skill at grid position {targetPosition}");

            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("WQSkill: Could not find TileGrid in the scene!");
                    return;
                }
            }

            if (playerStats == null)
            {
                playerStats = casterTransform.GetComponent<PlayerStats>();
                if (playerStats == null)
                {
                    playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
                    if (playerStats == null)
                    {
                        Debug.Log("QuickSlash : Cant find player Stat");
                        return;
                    }
                }
            }

            //Check if player has enought mana
            if (!playerStats.TryUseMana(manaCost))
            {
                Debug.Log($"QuickSlash: Not enough mana! Required: {manaCost}, Current: {playerStats.CurrentMana}");
                return;
            }

            // Get the forward direction based on caster's facing direction (default is right)
            Vector2 forwardDirection = casterTransform.right;

            // Get player's current grid position
            Vector2Int playerGridPos = tileGrid.GetGridPosition(casterTransform.position);

            // Calculate the target grid positions (1 tile in front of player, vertically aligned)
            Vector2Int frontTile;

            // Determine which direction is "front" based on player's facing direction
            if (Mathf.Abs(forwardDirection.x) > Mathf.Abs(forwardDirection.y))
            {
                // Facing horizontally (right or left)
                frontTile = new Vector2Int(
                    playerGridPos.x + (forwardDirection.x > 0 ? 1 : -1),
                    playerGridPos.y
                );
            }
            else
            {
                // Facing vertically (up or down)
                frontTile = new Vector2Int(
                    playerGridPos.x,
                    playerGridPos.y + (forwardDirection.y > 0 ? 1 : -1)
                );
            }

            // Calculate the three vertical tiles
            List<Vector2Int> damageGridPositions = new List<Vector2Int>();
            damageGridPositions.Add(frontTile);
            damageGridPositions.Add(new Vector2Int(frontTile.x, frontTile.y + 1)); // Above
            damageGridPositions.Add(new Vector2Int(frontTile.x, frontTile.y - 1)); // Below

            // Convert grid positions to world positions for damage application
            List<Vector2> damageWorldPositions = new List<Vector2>();
            foreach (Vector2Int gridPos in damageGridPositions)
            {
                if (tileGrid.IsValidGridPosition(gridPos))
                {
                    damageWorldPositions.Add(tileGrid.GetWorldPosition(gridPos));
                }
            }

            // Apply damage to each position
            foreach (Vector2 pos in damageWorldPositions)
            {
                // Debug visualization during runtime
                Debug.DrawLine(casterTransform.position, pos, Color.red, 1f);

                // Find all colliders at this position
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pos, effectRadius);

                // Apply damage to any enemies found
                foreach (Collider2D collider in hitColliders)
                {
                    // Check if the hit object has the "Enemy" tag
                    if (collider.CompareTag("Enemy"))
                    {
                        // Look for Enemy component
                        Enemy enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(damageAmount);
                            Debug.Log($"WQ Skill hit enemy: {collider.name} for {damageAmount} damage");
                        }
                        else
                        {
                            Debug.LogWarning($"Object tagged as 'Enemy' {collider.name} found but has no Enemy component");
                        }
                    }
                }
            }
            // Call the base implementation if needed
            base.ExecuteSkillEffect(targetPosition, casterTransform);

            // Reset melee animation after a short delay
            StartCoroutine(ResetAnimationAfterDelay(1.0f));
        }

        private void ResetMeleeAnimation()
        {
            // Find the player's animator and reset melee state
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Animator animator = player.GetComponent<Animator>();
                Debug.Log("Current animator state: " + animator.GetCurrentAnimatorStateInfo(0).IsName("YourMeleeState"));
                if (animator != null)
                {
                    // Reset trigger and bool
                    animator.ResetTrigger("QuickSlash");
                    animator.SetBool("isMelee", false);
                    animator.Play("Idle");
                    Debug.Log("QuickSlash: Reset melee animation state");
                }
            }
        }

        private System.Collections.IEnumerator ResetAnimationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetMeleeAnimation();
        }
    }
}