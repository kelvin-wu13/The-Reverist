using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SkillSystem
{
    public class WWSkill : Skill
    {
        [Header("Dash Settings")]
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private AnimationCurve dashCurve;
        [SerializeField] private GameObject dashEffectPrefab;
        
        [Header("Attack Settings")]
        [SerializeField] private int damageAmount = 10;
        [SerializeField] private float effectRadius = 0.5f;
        
        [Header("Return Settings")]
        [SerializeField] private float returnDelay = 0.5f;
        [SerializeField] private float returnDuration = 0.5f;
        [SerializeField] private AnimationCurve returnCurve;
        [SerializeField] private GameObject returnEffectPrefab;
        
        private Vector2Int targetPosition;
        private Transform playerTransform;
        private TileGrid tileGrid;
        private Vector2Int originalPosition;
        private Vector3 originalWorldPosition;
        private bool isDashing = false;
        
        private void Start()
        {
            // Get tile grid reference
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("WWSkill: Could not find TileGrid in the scene!");
                Destroy(gameObject);
                return;
            }
            
            // Execute the dash immediately
            StartCoroutine(ExecuteDash());
        }
        
        public override void Initialize(Vector2Int targetPos, SkillCombination skillType, Transform caster)
        {
            base.Initialize(targetPos, skillType, caster);
            this.targetPosition = targetPos;
            this.playerTransform = caster;
            
            // Store the original player position - important to do this BEFORE the dash occurs
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
            }
            originalPosition = tileGrid != null ? tileGrid.GetGridPosition(playerTransform.position) : Vector2Int.zero;
            originalWorldPosition = playerTransform.position;
            
            Debug.Log($"WWSkill: Stored original position at {originalPosition} (world: {originalWorldPosition})");
        }
        
        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // This method is called by the base class, but we're using our own implementation
            // Leave empty as we handle execution in our own coroutines
        }
        
        private IEnumerator ExecuteDash()
        {
            if (playerTransform == null || tileGrid == null) yield break;
            
            // Calculate position 1 tile behind the target position
            Vector2Int playerPos = tileGrid.GetGridPosition(playerTransform.position);
            Vector2Int direction = targetPosition - playerPos;
            
            // Normalize direction to get just the facing direction
            if (direction.x != 0) direction.x = direction.x / Mathf.Abs(direction.x);
            if (direction.y != 0) direction.y = direction.y / Mathf.Abs(direction.y);
            
            // Calculate the position 1 tile behind the target
            Vector2Int dashPosition = targetPosition - direction;
            
            // Modified validation: Allow movement into enemy grid but not into broken tiles
            if (!IsValidDashPosition(dashPosition))
            {
                Debug.LogWarning("WWSkill: Dash target position is invalid, trying to find a valid position nearby");
                
                // Try to find a valid position nearby
                List<Vector2Int> adjacentPositions = new List<Vector2Int>
                {
                    dashPosition + Vector2Int.up,
                    dashPosition + Vector2Int.down,
                    dashPosition + Vector2Int.left,
                    dashPosition + Vector2Int.right
                };
                
                foreach (Vector2Int pos in adjacentPositions)
                {
                    if (IsValidDashPosition(pos))
                    {
                        dashPosition = pos;
                        break;
                    }
                }
                
                // If still invalid, cancel the dash
                if (!IsValidDashPosition(dashPosition))
                {
                    Debug.LogWarning("WWSkill: Could not find a valid dash position, canceling skill");
                    Destroy(gameObject);
                    yield break;
                }
            }
            
            // Spawn dash effect if available
            if (dashEffectPrefab != null)
            {
                GameObject dashEffect = Instantiate(dashEffectPrefab, playerTransform.position, Quaternion.identity);
                Destroy(dashEffect, dashDuration + 0.5f);
            }
            
            // Store original position
            Vector3 startPosition = playerTransform.position;
            Vector3 targetWorldPosition = tileGrid.GetWorldPosition(dashPosition) + new Vector3(0.5f, 0.5f, 0);
            
            isDashing = true;
            
            // Perform the dash over time
            float elapsedTime = 0f;
            while (elapsedTime < dashDuration)
            {
                float t = dashCurve != null ? dashCurve.Evaluate(elapsedTime / dashDuration) : elapsedTime / dashDuration;
                playerTransform.position = Vector3.Lerp(startPosition, targetWorldPosition, t);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure player is at exactly the target position
            playerTransform.position = targetWorldPosition;
            
            // Get the forward direction based on caster's facing direction
            Vector2 forwardDirection = playerTransform.right;
            
            // Calculate the position 1 tile in front of the player after dashing
            Vector2Int frontTile;
            
            // Determine which direction is "front" based on player's facing direction
            if (Mathf.Abs(forwardDirection.x) > Mathf.Abs(forwardDirection.y))
            {
                // Facing horizontally (right or left)
                frontTile = new Vector2Int(
                    dashPosition.x + (forwardDirection.x > 0 ? 1 : -1),
                    dashPosition.y
                );
            }
            else
            {
                // Facing vertically (up or down)
                frontTile = new Vector2Int(
                    dashPosition.x,
                    dashPosition.y + (forwardDirection.y > 0 ? 1 : -1)
                );
            }
            
            // Execute the skill effect 1 tile in front of the player's dash position
            ExecuteWQStyleAttack(frontTile, playerTransform);
            
            // Wait for the specified delay before returning
            yield return new WaitForSeconds(returnDelay);
            
            // Return to original position
            yield return StartCoroutine(ReturnToOriginalPosition());
        }
        
        // New method to check if position is valid for dashing (allows enemy tiles but not broken tiles)
        private bool IsValidDashPosition(Vector2Int gridPosition)
        {
            // Check if position is valid and not a broken tile
            return tileGrid.IsValidGridPosition(gridPosition) && 
                tileGrid.grid[gridPosition.x, gridPosition.y] != TileType.Broken;
        }
        
        // New method to execute the WQ-style attack (vertical 3-tile pattern)
        private void ExecuteWQStyleAttack(Vector2Int frontTile, Transform casterTransform)
        {
            // Log the skill execution
            Debug.Log($"Executing WW skill at grid position {frontTile}");
            
            // Get the forward direction based on caster's facing direction (default is right)
            Vector2 forwardDirection = casterTransform.right;
            
            // Calculate the three vertical tiles centered on the frontTile
            //Tambahin lagi damage di playerPosition
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
                    
                    // Visual effect - you could add additional effects here
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
                            Debug.Log($"WW Skill hit enemy: {collider.name} for {damageAmount} damage");
                        }
                        else
                        {
                            Debug.LogWarning($"Object tagged as 'Enemy' {collider.name} found but has no Enemy component");
                        }
                    }
                }
            }
        }
        
        private IEnumerator ReturnToOriginalPosition()
        {
            if (playerTransform == null) yield break;
            
            // Spawn return effect if available
            if (returnEffectPrefab != null)
            {
                GameObject returnEffect = Instantiate(returnEffectPrefab, playerTransform.position, Quaternion.identity);
                Destroy(returnEffect, returnDuration + 0.5f);
            }
            
            Vector3 currentPosition = playerTransform.position;
            Vector3 returnPosition = originalWorldPosition; // Use the stored world position directly
            
            Debug.Log($"WWSkill: Returning to original position at {originalPosition} (world: {returnPosition})");
            
            // Create a more complex path for smoother animation
            List<Vector3> path = GenerateSmoothPath(currentPosition, returnPosition, 5);
            
            // Perform the return dash over time using the path for smoother movement
            float elapsedTime = 0f;
            while (elapsedTime < returnDuration)
            {
                float t = returnCurve != null ? returnCurve.Evaluate(elapsedTime / returnDuration) : elapsedTime / returnDuration;
                
                // Use the smooth path instead of a direct lerp
                playerTransform.position = EvaluatePathPosition(path, t);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure player is at exactly the return position
            playerTransform.position = returnPosition;
            isDashing = false;
            
            // Destroy this skill object after completion
            Destroy(gameObject, 0.5f);
        }
        
        // Generate a smooth path with intermediary points
        private List<Vector3> GenerateSmoothPath(Vector3 start, Vector3 end, int numPoints)
        {
            List<Vector3> path = new List<Vector3>();
            
            // Calculate the midpoint with a slight height offset for arc effect
            Vector3 mid = (start + end) / 2f;
            mid.y += 0.5f; // Add slight height to create an arc
            
            for (int i = 0; i <= numPoints; i++)
            {
                float t = i / (float)numPoints;
                
                // Quadratic Bezier curve
                Vector3 position = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * end;
                path.Add(position);
            }
            
            return path;
        }
        
        // Evaluate the position along the path based on t (0-1)
        private Vector3 EvaluatePathPosition(List<Vector3> path, float t)
        {
            if (path.Count == 0) return Vector3.zero;
            if (t <= 0) return path[0];
            if (t >= 1) return path[path.Count - 1];
            
            float indexFloat = t * (path.Count - 1);
            int index = Mathf.FloorToInt(indexFloat);
            float remainder = indexFloat - index;
            
            if (index >= path.Count - 1) return path[path.Count - 1];
            
            return Vector3.Lerp(path[index], path[index + 1], remainder);
        }
        
        // Override OnDestroy to handle cleanup
        private void OnDestroy()
        {
            // If the player was dashing when this was destroyed, ensure they can move again
            if (isDashing)
            {
                isDashing = false;
                
                // Make sure player is returned to original position if skill is interrupted
                if (playerTransform != null)
                {
                    playerTransform.position = originalWorldPosition;
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && tileGrid != null && playerTransform != null)
            {
                // Draw the dash path
                Gizmos.color = Color.blue;
                Vector3 dashTarget = tileGrid.GetWorldPosition(targetPosition) + new Vector3(0.5f, 0.5f, 0);
                Gizmos.DrawLine(playerTransform.position, dashTarget);
                
                // Draw the attack pattern (vertical 3-tile)
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                
                // Get player's forward direction
                Vector2 forwardDirection = playerTransform.right;
                
                // Get the dash position
                Vector2Int playerPos = tileGrid.GetGridPosition(playerTransform.position);
                Vector2Int direction = targetPosition - playerPos;
                if (direction.x != 0) direction.x = direction.x / Mathf.Abs(direction.x);
                if (direction.y != 0) direction.y = direction.y / Mathf.Abs(direction.y);
                Vector2Int dashPosition = targetPosition - direction;
                
                // Calculate position 1 tile in front of dash position
                Vector2Int frontTile;
                if (Mathf.Abs(forwardDirection.x) > Mathf.Abs(forwardDirection.y))
                {
                    // Facing horizontally (right or left)
                    frontTile = new Vector2Int(
                        dashPosition.x + (forwardDirection.x > 0 ? 1 : -1),
                        dashPosition.y
                    );
                }
                else
                {
                    // Facing vertically (up or down)
                    frontTile = new Vector2Int(
                        dashPosition.x,
                        dashPosition.y + (forwardDirection.y > 0 ? 1 : -1)
                    );
                }
                
                // Draw the three vertical tiles
                List<Vector2Int> attackPositions = new List<Vector2Int>();
                attackPositions.Add(frontTile);
                attackPositions.Add(new Vector2Int(frontTile.x, frontTile.y + 1));
                attackPositions.Add(new Vector2Int(frontTile.x, frontTile.y - 1));
                
                foreach (Vector2Int pos in attackPositions)
                {
                    if (tileGrid.IsValidGridPosition(pos))
                    {
                        Vector3 worldPos = tileGrid.GetWorldPosition(pos) + new Vector3(0.5f, 0.5f, 0);
                        Gizmos.DrawCube(worldPos, new Vector3(1f, 1f, 0.1f));
                        
                        // Draw effect radius
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(worldPos, effectRadius);
                    }
                }
                
                // Draw return path
                if (originalWorldPosition != Vector3.zero)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(dashTarget, originalWorldPosition);
                }
            }
        }
    }
}
