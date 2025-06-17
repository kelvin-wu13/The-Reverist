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

            AudioManager.Instance?.PlayMagneticPullSFX();

            // Start the pull sequence with proper synchronization
            StartCoroutine(ExecutePullSequence());
        }

        private IEnumerator ExecutePullSequence()
        {
            // Step 1: Find all enemies and stop their movement
            List<Enemy> allEnemies = FindObjectsOfType<Enemy>().ToList();
            
            // Interrupt all enemy movements and prepare them for pull
            foreach (Enemy enemy in allEnemies)
            {
                enemy.InterruptMovementForSkill();
            }

            // Step 2: Wait for all enemies to finish their current movements
            yield return StartCoroutine(WaitForAllEnemiesToStop(allEnemies));

            // Step 3: Now execute the pull with all enemies in stable positions
            ExecutePullLogic();
        }

        private IEnumerator WaitForAllEnemiesToStop(List<Enemy> enemies)
        {
            float waitTime = 0f;
            
            while (waitTime < maxWaitTime)
            {
                bool allStopped = true;
                
                foreach (Enemy enemy in enemies)
                {
                    if (enemy != null && enemy.IsMoving())
                    {
                        allStopped = false;
                        break;
                    }
                }
                
                if (allStopped)
                {
                    Debug.Log("All enemies have stopped moving, proceeding with pull");
                    break;
                }
                
                waitTime += Time.deltaTime;
                yield return null;
            }
            
            if (waitTime >= maxWaitTime)
            {
                Debug.LogWarning("Timeout waiting for enemies to stop, proceeding anyway");
            }
        }

        private void ExecutePullLogic()
        {
            List<PullCandidate> pullCandidates = new List<PullCandidate>();

            for (int y = 0; y < tileGrid.gridHeight; y++)
            {
                for (int x = 0; x < tileGrid.gridWidth; x++)
                {
                    Vector2Int currentGridPos = new Vector2Int(x, y);
                    if (tileGrid.grid[x, y] == TileType.Enemy)
                    {
                        FindPullCandidates(currentGridPos, tileGrid, pullCandidates);
                    }
                }
            }

            // Sort from right to left (so back pulls resolve before front)
            pullCandidates.Sort((a, b) => b.currentPosition.x.CompareTo(a.currentPosition.x));

            Dictionary<Vector2Int, PullCandidate> positionToCandidate = new();
            foreach (var c in pullCandidates)
            {
                if (!positionToCandidate.ContainsKey(c.currentPosition))
                    positionToCandidate[c.currentPosition] = c;
            }

            HashSet<Vector2Int> reservedTargets = new HashSet<Vector2Int>();
            List<PullCandidate> validPulls = new List<PullCandidate>();

            foreach (var candidate in pullCandidates)
            {
                List<PullCandidate> chain = new();
                if (ResolvePullChain(candidate, positionToCandidate, reservedTargets, chain))
                {
                    foreach (var resolved in chain)
                    {
                        if (!reservedTargets.Contains(resolved.targetPosition))
                        {
                            reservedTargets.Add(resolved.targetPosition);
                            validPulls.Add(resolved);
                            Debug.Log($"[ChainPull] {resolved.enemy.name} → {resolved.targetPosition}");
                        }
                    }
                }
            }

            ExecuteValidPulls(validPulls);
        }

        private bool ResolvePullChain(PullCandidate current, Dictionary<Vector2Int, PullCandidate> map, HashSet<Vector2Int> reserved, List<PullCandidate> chain)
        {
            if (reserved.Contains(current.targetPosition))
                return false;

            // Resolve blocker recursively
            if (map.TryGetValue(current.targetPosition, out var blocker))
            {
                if (!ResolvePullChain(blocker, map, reserved, chain))
                    return false;
            }

            if (!IsPositionAvailableForPull(current.targetPosition, current.enemy))
                return false;

            chain.Add(current);
            return true;
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
            if (!tileGrid.IsValidGridPosition(targetPos))
                return false;

            TileType tileType = tileGrid.grid[targetPos.x, targetPos.y];

            if (tileType == TileType.Broken || tileType == TileType.EnemyBroken || tileType == TileType.PlayerBroken)
                return false;

            if (tileType == TileType.Player || tileType == TileType.PlayerCracked)
                return false;

            if (tileGrid.IsTileOccupied(targetPos))
                return false;

            Vector3 targetWorldPos = tileGrid.GetWorldPosition(targetPos);
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(targetWorldPos, 0.3f);

            foreach (Collider2D overlap in overlaps)
            {
                if (overlap.CompareTag("Enemy") && overlap.gameObject != excludeEnemy?.gameObject)
                {
                    Enemy otherEnemy = overlap.GetComponent<Enemy>();
                    if (otherEnemy != null && !otherEnemy.IsBeingPulled())
                        return false;
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
            Vector2Int targetGridPos = new Vector2Int(gridPos.x - 1, gridPos.y);

            // Only continue if target is in bounds
            if (!tileGrid.IsValidGridPosition(targetGridPos))
                return;

            // Loop through all enemies in the scene
            foreach (Enemy enemy in FindObjectsOfType<Enemy>())
            {
                if (enemy == null || enemy.IsBeingPulled()) continue;

                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemy.transform.position);

                if (enemyGridPos != gridPos) continue;

                Vector2 offset = enemy.GetPositionOffset();

                // Now check if THIS enemy can go to target
                if (!IsPositionAvailableForPull(targetGridPos, enemy))
                    continue;

                PullCandidate candidate = new PullCandidate
                {
                    enemy = enemy,
                    currentPosition = gridPos,
                    targetPosition = targetGridPos,
                    offset = offset
                };

                candidates.Add(candidate);
                Debug.Log($"Pull candidate at {gridPos} -> {targetGridPos}");
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