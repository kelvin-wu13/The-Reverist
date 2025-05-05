using UnityEngine;
using System.Collections.Generic;

namespace SkillSystem
{
    public class WQSkill : Skill
    {
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private float effectRadius = 0.5f;
        
        private TileGrid tileGrid;

        private void Awake()
        {
            // Find the TileGrid in the scene
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("WQSkill: Could not find TileGrid in the scene!");
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
                    
                    // Visual effect - you could add a tile effect here
                    // tileGrid.CrackTile(gridPos);
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
        }
        
        // Visualization in the editor
        private void OnDrawGizmos()
        {
            // Draw gizmos even when not selected
            DrawSkillGizmos();
        }
        
        // Visualization when selected in the editor
        private void OnDrawGizmosSelected()
        {
            // Draw more prominent gizmos when selected
            DrawSkillGizmos();
        }
        
        private void DrawSkillGizmos()
        {
            if (transform == null)
                return;
                
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                    return;
            }
            
            Transform casterTransform = transform;
            Vector2 forwardDirection = casterTransform.right;
            
            // Get player's current grid position
            Vector2Int playerGridPos = tileGrid.GetGridPosition(casterTransform.position);
            
            // Calculate the target grid positions (1 tile in front of player)
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
            
            // Draw lines and circles for each valid grid position
            foreach (Vector2Int gridPos in damageGridPositions)
            {
                if (tileGrid.IsValidGridPosition(gridPos))
                {
                    Vector3 worldPos = tileGrid.GetWorldPosition(gridPos);
                    
                    // Draw forward line
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(casterTransform.position, worldPos);
                    
                    // Draw damage areas
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(worldPos, effectRadius);
                }
            }
            
            // Connect the vertical positions with lines if they are valid
            if (tileGrid.IsValidGridPosition(frontTile) && 
                tileGrid.IsValidGridPosition(new Vector2Int(frontTile.x, frontTile.y + 1)))
            {
                Gizmos.DrawLine(
                    tileGrid.GetWorldPosition(frontTile),
                    tileGrid.GetWorldPosition(new Vector2Int(frontTile.x, frontTile.y + 1))
                );
            }
            
            if (tileGrid.IsValidGridPosition(frontTile) && 
                tileGrid.IsValidGridPosition(new Vector2Int(frontTile.x, frontTile.y - 1)))
            {
                Gizmos.DrawLine(
                    tileGrid.GetWorldPosition(frontTile),
                    tileGrid.GetWorldPosition(new Vector2Int(frontTile.x, frontTile.y - 1))
                );
            }
        }
    }
}