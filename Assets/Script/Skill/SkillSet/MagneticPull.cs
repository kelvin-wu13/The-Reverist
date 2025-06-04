using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SkillSystem
{
    public class MagneticPull : Skill
    {
        [SerializeField] private float pushDuration = 0.3f;
        [SerializeField] private bool preserveExactYPosition = true;
        [SerializeField] public float cooldownDuration = 2.0f;
        [SerializeField] public float manaCost = 2.0f;
        [SerializeField] private float maxWaitTime = 1.0f; // Maximum time to wait for enemies to stop moving

        private PlayerStats playerStats;
        private TileGrid tileGrid;

        private void Awake()
        {
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("MagneticPull: Could not find PlayerStats component!");
            }

            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("MagneticPull: Could not find TileGrid component!");
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Check mana cost
            if (playerStats != null && !playerStats.TryUseMana(manaCost))
            {
                Debug.Log("Not enough mana to cast MagneticPull!");
                return;
            }
            
            Debug.Log($"Magnetic Pull Skill activated at {targetPosition}");

            if (tileGrid == null)
            {
                Debug.LogError("TileGrid not found in the scene!");
                return;
            }

            // Clear all enemy position reservations at the start of skill execution
            Enemy.ClearAllReservations();
            Debug.Log("All enemy reservations cleared for MagneticPull skill");

            // Start the pull sequence with proper synchronization
            StartCoroutine(ExecutePullSequence());
        }
        
        private IEnumerator ExecutePullSequence()
        {
            Enemy.ClearAllReservations();
            EnemyManager.Instance?.InterruptAllEnemies();
            if (EnemyManager.Instance != null)
                yield return EnemyManager.Instance.WaitUntilAllStopped(maxWaitTime);
            ExecutePullLogic();
        }

        private void ExecutePullLogic()
        {
            List<PullCandidate> pullCandidates = new List<PullCandidate>();
            
            // Find all valid pull candidates
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
            
            // Sort candidates by X position (leftmost first)
            pullCandidates.Sort((a, b) => a.currentPosition.x.CompareTo(b.currentPosition.x));
            
            // Process candidates and resolve conflicts
            HashSet<Vector2Int> reservedTargets = new HashSet<Vector2Int>();
            List<PullCandidate> validPulls = new List<PullCandidate>();
            
            foreach (var candidate in pullCandidates)
            {
                // Check if target position is already reserved by another pull
                if (reservedTargets.Contains(candidate.targetPosition))
                {
                    Debug.Log($"Enemy at {candidate.currentPosition} cannot pull to {candidate.targetPosition} - already reserved");
                    continue;
                }
                
                // Double check that target is still available
                if (IsPositionAvailableForPull(candidate.targetPosition, candidate.enemy))
                {
                    reservedTargets.Add(candidate.targetPosition);
                    validPulls.Add(candidate);
                    Debug.Log($"Enemy at {candidate.currentPosition} will pull to {candidate.targetPosition}");
                }
                else
                {
                    Debug.Log($"Enemy at {candidate.currentPosition} cannot pull to {candidate.targetPosition} - position not available");
                }
            }

            // Execute all valid pulls simultaneously
            ExecuteValidPulls(validPulls);
        }

        private void ExecuteValidPulls(List<PullCandidate> validPulls)
        {
            // First, prepare all enemies for pull (clear their current positions)
            foreach (var pull in validPulls)
            {
                pull.enemy.PrepareForPull(pull.targetPosition);
            }

            // Then start all pull animations
            foreach (var pull in validPulls)
            {
                StartCoroutine(PullAnimation(pull.enemy, pull.currentPosition, pull.targetPosition, pull.offset));
            }
        }

        private bool IsPositionAvailableForPull(Vector2Int targetPos, Enemy excludeEnemy)
        {
            // Check grid validity
            if (!tileGrid.IsValidGridPosition(targetPos))
                return false;
                
            // Check tile type
            if (tileGrid.grid[targetPos.x, targetPos.y] != TileType.Enemy)
                return false;
                
            // Check if tile is broken or cracked
            TileType tileType = tileGrid.grid[targetPos.x, targetPos.y];
            if (tileType == TileType.EnemyBroken || tileType == TileType.Broken)
                return false;

            // Check for physical occupation by other enemies
            Vector3 targetWorldPos = tileGrid.GetWorldPosition(targetPos);
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(targetWorldPos, 0.3f);
            
            foreach (Collider2D overlap in overlaps)
            {
                if (overlap.CompareTag("Enemy") && overlap.gameObject != excludeEnemy.gameObject)
                {
                    Enemy otherEnemy = overlap.GetComponent<Enemy>();
                    if (otherEnemy != null && !otherEnemy.IsBeingPulled())
                    {
                        return false; // Position is occupied by a stable enemy
                    }
                }
            }

            return true;
        }

        private struct PullCandidate
        {
            public Enemy enemy;
            public Vector2Int currentPosition;
            public Vector2Int targetPosition;
            public Vector2 offset;
        }

        private void FindPullCandidates(Vector2Int gridPos, TileGrid tileGrid, List<PullCandidate> candidates)
        {
            // Pull direction is strictly LEFT (one tile)
            Vector2Int targetGridPos = new Vector2Int(gridPos.x - 1, gridPos.y);

            // Basic validation
            if (!tileGrid.IsValidGridPosition(targetGridPos))
                return;
                
            if (tileGrid.grid[targetGridPos.x, targetGridPos.y] != TileType.Enemy)
                return;

            // Find enemies at the current position
            Vector3 currentWorldPos = tileGrid.GetWorldPosition(gridPos);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(currentWorldPos, 0.4f);

            foreach (Collider2D col in colliders)
            {
                if (!col.CompareTag("Enemy")) continue;

                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy == null) continue;

                // Skip if enemy is already being processed for pull
                if (enemy.IsBeingPulled()) continue;

                Vector2 offset = enemy.GetPositionOffset();

                // Check if target position will be available
                if (IsPositionAvailableForPull(targetGridPos, enemy))
                {
                    PullCandidate candidate = new PullCandidate
                    {
                        enemy = enemy,
                        currentPosition = gridPos,
                        targetPosition = targetGridPos,
                        offset = offset
                    };
                    
                    candidates.Add(candidate);
                    Debug.Log($"Found valid pull candidate at {gridPos} to {targetGridPos}");
                }
            }
        }

        private IEnumerator PullAnimation(Enemy enemy, Vector2Int currentPos, Vector2Int targetGridPos, Vector2 offset)
        {
            // Get current position
            Vector3 start = enemy.transform.position;

            // Calculate target position
            Vector3 target = tileGrid.GetWorldPosition(targetGridPos);
            Vector3 end;
            
            if (preserveExactYPosition)
            {
                end = new Vector3(target.x + offset.x, start.y, 0);
            }
            else
            {
                end = new Vector3(target.x + offset.x, target.y + offset.y, 0);
            }

            Debug.Log($"Pull animation from {start} to {end}");

            // Smooth pull animation
            float elapsed = 0f;
            while (elapsed < pushDuration)
            {
                if (enemy == null) yield break; // Safety check
                
                float t = Mathf.SmoothStep(0, 1, elapsed / pushDuration);
                enemy.transform.position = Vector3.Lerp(start, end, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Finalize position
            if (enemy != null)
            {
                enemy.transform.position = end;
                enemy.CompletePull(targetGridPos, end);
            }
        }
    }
}