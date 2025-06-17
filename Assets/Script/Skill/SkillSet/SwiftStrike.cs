using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SkillSystem
{
    public class SwiftStrike : Skill
    {
        [Header("Skill Properties")]
        [SerializeField] public float cooldownDuration = 3.0f;
        [SerializeField] public float manaCost = 15.0f;

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
        private PlayerStats playerStats;
        private PlayerCrosshair playerCrosshair; // Add reference to crosshair

        private void Start()
        {
            // Get tile grid reference
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("SwiftStrike: Could not find TileGrid in the scene!");
                Destroy(gameObject);
                return;
            }

            // Find PlayerStats component
            if (playerTransform != null)
            {
                playerStats = playerTransform.GetComponent<PlayerStats>();
                if (playerStats == null)
                {
                    playerStats = playerTransform.GetComponentInChildren<PlayerStats>();
                }
            }

            if (playerStats == null)
            {
                playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
            }

            // Find PlayerCrosshair component
            playerCrosshair = FindObjectOfType<PlayerCrosshair>();
            if (playerCrosshair == null)
            {
                Debug.LogWarning("SwiftStrike: Could not find PlayerCrosshair in the scene!");
            }

            // Check if player has enough mana before executing the skill
            if (playerStats != null)
            {
                if (playerStats.TryUseMana(manaCost))
                {
                    Debug.Log($"SwiftStrike: Used {manaCost} mana to cast skill");
                    // Freeze crosshair before executing the skill
                    if (playerCrosshair != null)
                    {
                        playerCrosshair.FreezeCrosshair();
                    }
                    // Execute the dash immediately
                    StartCoroutine(ExecuteDash());
                    AudioManager.Instance?.PlaySwiftStrikeSFX();
                }
                else
                {
                    Debug.LogWarning("SwiftStrike: Not enough mana to cast skill!");
                    Destroy(gameObject);
                }
            }
            if (skillEffect != null)
            {
                skillEffect.transform.position = transform.position;
                skillEffect.Play();
            }
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
            
            Debug.Log($"SwiftStrike: Stored original position at {originalPosition} (world: {originalWorldPosition})");
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
                Debug.LogWarning("SwiftStrike: Dash target position is invalid, trying to find a valid position nearby");
                
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
                
                // If still invalid, cancel the dash and unfreeze crosshair
                if (!IsValidDashPosition(dashPosition))
                {
                    Debug.LogWarning("SwiftStrike: Could not find a valid dash position, canceling skill");
                    if (playerCrosshair != null)
                    {
                        playerCrosshair.UnfreezeCrosshair();
                    }
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
            else
            {
                // Create a simple visual effect if no prefab is available
                CreateSimpleDashEffect(playerTransform.position, dashDuration);
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

        // Create a simple particle effect for dash if no prefab is available
        private void CreateSimpleDashEffect(Vector3 position, float duration)
        {
            GameObject dashEffectObj = new GameObject("DashEffect");
            dashEffectObj.transform.position = position;

            // Create trail renderer
            TrailRenderer trailRenderer = dashEffectObj.AddComponent<TrailRenderer>();
            trailRenderer.startWidth = 0.3f;
            trailRenderer.endWidth = 0.1f;
            trailRenderer.time = dashDuration * 2f;

            // Set material to default particle material
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            
            // Set trail color
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.blue, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trailRenderer.colorGradient = gradient;

            // Destroy after duration
            Destroy(dashEffectObj, duration + 1f);

            // Attach to player if possible
            if (playerTransform != null)
            {
                dashEffectObj.transform.SetParent(playerTransform);
            }
        }

        // New method to check if position is valid for dashing (allows enemy tiles but not broken tiles)
        private bool IsValidDashPosition(Vector2Int gridPosition)
        {
            // Check if position is valid and not a broken tile
            return tileGrid.IsValidGridPosition(gridPosition) && 
                tileGrid.grid[gridPosition.x, gridPosition.y] != TileType.Broken;
        }
        
        // New method to execute the WQ-style attack (vertical 3-tile pattern)
        private void ExecuteWQStyleAttack(Vector2Int _, Transform casterTransform)
        {
            if (tileGrid == null || casterTransform == null) return;

            Vector2Int playerGridPos = tileGrid.GetGridPosition(casterTransform.position);

            List<Vector2Int> damageGridPositions = new List<Vector2Int>
            {
                new Vector2Int(playerGridPos.x + 1, playerGridPos.y - 2),
                new Vector2Int(playerGridPos.x + 1, playerGridPos.y),
                new Vector2Int(playerGridPos.x + 1, playerGridPos.y -1)
            };

            float yOffset = 0f;
            PlayerMovement move = casterTransform.GetComponent<PlayerMovement>();
            if (move != null)
                yOffset = move.GetPositionOffset().y;

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
                            Debug.Log($"SwiftStrike hit enemy: {collider.name} for {damageAmount} damage");
                        }
                        else
                        {
                            Debug.LogWarning($"Enemy tag on {collider.name} but no Enemy script");
                        }
                    }
                }
            }
        }

        
        private IEnumerator ReturnToOriginalPosition()
        {
            if (playerTransform == null) yield break;
            
            Vector3 currentPosition = playerTransform.position;
            Vector3 returnPosition = originalWorldPosition; // Use the stored world position directly
            
            Debug.Log($"SwiftStrike: Returning to original position at {originalPosition} (world: {returnPosition})");
            
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

            //Reset melee animation before destroying
            ResetMeleeAnimation();
            
            // Unfreeze crosshair after skill completion
            if (playerCrosshair != null)
            {
                playerCrosshair.UnfreezeCrosshair();
            }
            
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
        
        private void ResetMeleeAnimation()
        {
            // Find the player's animator and reset melee state
            if (playerTransform != null)
            {
                Animator animator = playerTransform.GetComponent<Animator>();
                Debug.Log("Current animator state: " + animator.GetCurrentAnimatorStateInfo(0).IsName("YourMeleeState"));
                if (animator != null)
                {
                    animator.ResetTrigger("SwiftStrike"); // or SwiftStrike
                    animator.Play("Idle"); // Force state reset if needed
                    Debug.Log("SwiftStrike: Reset melee animation state");
                }
            }
        }

        // Modify your existing OnDestroy method to include animation reset
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
            
            // Reset melee animation state when skill is destroyed
            ResetMeleeAnimation();
            
            // Always unfreeze crosshair when skill is destroyed (safety measure)
            if (playerCrosshair != null && playerCrosshair.IsFrozen())
            {
                playerCrosshair.UnfreezeCrosshair();
                Debug.Log("SwiftStrike: Unfroze crosshair on destroy");
            }
        }
    }
}