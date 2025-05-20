using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class MagneticPull : Skill
    {
        [SerializeField] private float pushDuration = 0.5f;
        [SerializeField] private float postPullStunDuration = 1f;
        [SerializeField] private bool preserveExactYPosition = true; // Flag to control Y position preservation

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            Debug.Log($"Magnetic Pull Skill activated at {targetPosition}");

            TileGrid tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("TileGrid not found in the scene!");
                return;
            }

            List<GameObject> pullTargets = new List<GameObject>();
            List<Vector2Int> currentPositions = new List<Vector2Int>();  // Store current positions
            List<Vector2Int> targetGridPositions = new List<Vector2Int>();
            List<Vector2> offsets = new List<Vector2>();
            List<bool> validPulls = new List<bool>();
            
            // Track which positions are already targeted to prevent multiple enemies from being pulled to the same tile
            HashSet<Vector2Int> targetedPositions = new HashSet<Vector2Int>();

            // First pass: collect all potential pulls
            List<PullCandidate> pullCandidates = new List<PullCandidate>();
            
            // Scan the grid for enemies
            for (int x = 0; x < tileGrid.gridWidth; x++)
            {
                for (int y = 0; y < tileGrid.gridHeight; y++)
                {
                    Vector2Int currentGridPos = new Vector2Int(x, y);

                    if (tileGrid.IsValidGridPosition(currentGridPos) &&
                        tileGrid.grid[x, y] == TileType.Enemy)
                    {
                        FindPullCandidates(currentGridPos, tileGrid, pullCandidates);
                    }
                }
            }
            
            // Sort candidates by x position (leftmost first) to prioritize enemies closer to player side
            pullCandidates.Sort((a, b) => a.currentPosition.x.CompareTo(b.currentPosition.x));
            
            // Second pass: assign valid pull targets, preventing duplicates
            foreach (var candidate in pullCandidates)
            {
                // Skip candidates that are already physically occupied
                // THIS IS THE KEY FIX - we're now checking isPhysicallyOccupied before considering the target
                if (candidate.isPhysicallyOccupied)
                {
                    Debug.Log($"Enemy at {candidate.currentPosition} cannot pull to {candidate.targetPosition} - target is physically occupied");
                    continue;
                }
                
                // Check if the target position is already targeted by another pull
                if (targetedPositions.Contains(candidate.targetPosition))
                {
                    // Skip this candidate or mark as invalid pull
                    pullTargets.Add(candidate.enemy.gameObject);
                    currentPositions.Add(candidate.currentPosition);
                    targetGridPositions.Add(candidate.targetPosition); // We'll use this, but the pull won't happen
                    offsets.Add(candidate.offset);
                    validPulls.Add(false); // Mark as invalid
                    
                    Debug.Log($"Enemy at {candidate.currentPosition} cannot pull to {candidate.targetPosition} - position already targeted");
                }
                else
                {
                    // This is a valid pull
                    pullTargets.Add(candidate.enemy.gameObject);
                    currentPositions.Add(candidate.currentPosition);
                    targetGridPositions.Add(candidate.targetPosition);
                    offsets.Add(candidate.offset);
                    validPulls.Add(true);
                    
                    // Reserve this target position
                    targetedPositions.Add(candidate.targetPosition);
                    
                    Debug.Log($"Enemy at {candidate.currentPosition} will pull to {candidate.targetPosition}");
                }
            }

            // Prepare enemies for pulling
            for (int i = 0; i < pullTargets.Count; i++)
            {
                if (pullTargets[i] != null && validPulls[i])
                {
                    Enemy enemy = pullTargets[i].GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.PrepareForPull(targetGridPositions[i]);
                    }
                }
            }

            // Execute the pull animation for each valid target
            for (int i = 0; i < pullTargets.Count; i++)
            {
                if (pullTargets[i] == null) continue;

                Enemy enemy = pullTargets[i].GetComponent<Enemy>();
                if (enemy == null) continue;

                if (validPulls[i])
                {
                    StartCoroutine(PullAnimation(enemy, currentPositions[i], targetGridPositions[i], offsets[i]));
                }
                else
                {
                    Debug.Log("Pull invalid — enemy will be stunned.");
                    enemy.Stun(postPullStunDuration);
                }
            }
        }

        // Struct to hold pull candidate information
        private struct PullCandidate
        {
            public Enemy enemy;
            public Vector2Int currentPosition;
            public Vector2Int targetPosition;
            public Vector2 offset;
            public bool isPhysicallyOccupied;
        }

        private void FindPullCandidates(
            Vector2Int gridPos,
            TileGrid tileGrid,
            List<PullCandidate> candidates)
        {
            // Pull direction is strictly LEFT with NO change in Y position
            Vector2Int targetGridPos = new Vector2Int(gridPos.x - 1, gridPos.y); // Explicitly preserve Y

            // Check if target position is valid
            if (!tileGrid.IsValidGridPosition(targetGridPos) ||
                tileGrid.grid[targetGridPos.x, targetGridPos.y] == TileType.Broken ||
                tileGrid.grid[targetGridPos.x, targetGridPos.y] == TileType.Player)
            {
                return;
            }

            // Find enemies at the current position
            Vector3 currentWorldPos = tileGrid.GetWorldPosition(gridPos);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(currentWorldPos, 0.4f);

            foreach (Collider2D col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy == null) continue;

                Vector2 offset = enemy.GetPositionOffset();

                // Check if target position is already physically occupied
                Vector3 targetWorldPos = tileGrid.GetWorldPosition(targetGridPos) + new Vector3(offset.x, offset.y, 0);
                Collider2D[] overlaps = Physics2D.OverlapCircleAll(targetWorldPos, 0.3f);
                
                // Filter out the current enemy from the overlaps
                bool isPhysicallyOccupied = false;
                foreach (Collider2D overlap in overlaps)
                {
                    if (overlap != col && overlap.CompareTag("Enemy"))
                    {
                        isPhysicallyOccupied = true;
                        break;
                    }
                }

                // Create and add the pull candidate
                PullCandidate candidate = new PullCandidate
                {
                    enemy = enemy,
                    currentPosition = gridPos,
                    targetPosition = targetGridPos,
                    offset = offset,
                    isPhysicallyOccupied = isPhysicallyOccupied
                };
                
                // Add candidate to list only if target is an Enemy tile and NOT physically occupied
                if (tileGrid.grid[targetGridPos.x, targetGridPos.y] == TileType.Enemy && !isPhysicallyOccupied)
                {
                    candidates.Add(candidate);
                    Debug.Log($"Found valid pull candidate at {gridPos} to {targetGridPos}, occupied: {isPhysicallyOccupied}");
                }
                else if (isPhysicallyOccupied)
                {
                    Debug.Log($"Skipping physically occupied target at {targetGridPos}");
                }
            }
        }

        private IEnumerator PullAnimation(Enemy enemy, Vector2Int currentPos, Vector2Int targetGridPos, Vector2 offset)
        {
            // Get current position to ensure we start from exactly where the enemy is
            Vector3 start = enemy.transform.position;
            
            // Calculate the target end position
            TileGrid tileGrid = enemy.GetTileGrid();
            Vector3 target = tileGrid.GetWorldPosition(targetGridPos);
            
            // If preserveExactYPosition is true, keep the same Y as the starting position
            Vector3 end;
            if (preserveExactYPosition) {
                end = new Vector3(
                    target.x + offset.x,
                    start.y, // Keep the exact same Y position
                    0
                );
            } else {
                end = new Vector3(
                    target.x + offset.x,
                    target.y + offset.y, 
                    0
                );
            }
            
            Debug.Log($"Pull animation from {start} to {end}");

            float elapsed = 0f;
            while (elapsed < pushDuration)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / pushDuration);
                enemy.transform.position = Vector3.Lerp(start, end, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            enemy.transform.position = end;
            enemy.ApplyPushEffect(targetGridPos, end);
            enemy.Stun(postPullStunDuration);
        }
    }
}