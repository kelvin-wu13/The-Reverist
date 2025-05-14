using System.Collections;
using UnityEngine;

namespace SkillSystem
{
    public class EQSkill : Skill
    {
        [Header("Skill Parameters")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private int damageAmount = 20;
        [SerializeField] private int maxShots = 3;
        [SerializeField] private float attackInterval = 0.5f;

        [Header("Visual Effects")]
        [SerializeField] private Color attackFlashColor = Color.red;
        [SerializeField] private float flashDuration = 0.1f;

        private TileGrid tileGrid;
        private Transform targetEnemy;
        private Vector2Int currentGridPosition;
        private int shotsFired = 0;
        private bool isMoving = false;
        private bool isSkillActive = true;

        public override void Initialize(Vector2Int gridPos, SkillCombination type, Transform caster)
        {
            base.Initialize(gridPos, type, caster);
            
            // Find TileGrid
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("EQSkill: TileGrid not found!");
                return;
            }

            // Reset skill state
            shotsFired = 0;
            isMoving = false;
            currentGridPosition = gridPos;

            Debug.Log($"EQSkill initialized at {gridPos} with maxShots={maxShots}");

            // Position the skill at the initial grid position
            transform.position = tileGrid.GetWorldPosition(gridPos);

            // Start finding and targeting enemies
            StartCoroutine(FindAndEngageEnemies());
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            Debug.Log($"EQSkill activated at {targetPosition}");
        }

        private IEnumerator FindAndEngageEnemies()
        {
            while (isSkillActive && shotsFired < maxShots)
            {
                // Debug logging to track progress
                Debug.Log($"EQSkill: Shots fired: {shotsFired}/{maxShots}");
                
                // Find nearest enemy if we don't have one or if current target is destroyed
                if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy)
                {
                    targetEnemy = FindNearestEnemy();
                    
                    // If no enemies found, wait and try again
                    if (targetEnemy == null)
                    {
                        Debug.Log("No enemies found, waiting before trying again");
                        yield return new WaitForSeconds(0.5f);
                        continue;
                    }
                }

                // Check if we need to move towards enemy
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);
                if (currentGridPosition != enemyGridPos)
                {
                    Debug.Log($"Moving towards enemy from {currentGridPosition} to {enemyGridPos}");
                    // Move towards enemy
                    yield return StartCoroutine(MoveTowardsEnemy());
                    
                    // Check if target was destroyed during movement
                    if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy)
                    {
                        Debug.Log("Enemy was destroyed during movement");
                        continue;
                    }
                    
                    // Recheck position after movement to ensure we're in the same tile
                    enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);
                    if (currentGridPosition != enemyGridPos)
                    {
                        Debug.Log("Enemy moved during our movement, continuing chase");
                        continue;
                    }
                }

                // Now in the same tile as enemy, perform attack
                Debug.Log("Performing attack");
                yield return StartCoroutine(PerformAttack());
                
                // Check if we're done with all shots
                if (shotsFired >= maxShots)
                {
                    Debug.Log($"Reached maximum shots ({maxShots}), ending skill");
                    break;
                }
                
                // Check if enemy is still alive
                if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy)
                {
                    // Target enemy is dead, find a new one on next loop iteration
                    Debug.Log("Target enemy destroyed, searching for new target");
                    continue;
                }
                
                // Check if enemy moved to a different tile
                enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);
                if (currentGridPosition != enemyGridPos)
                {
                    // Enemy moved to different tile - chase immediately without delay
                    Debug.Log("Enemy moved to a different tile - pursuing target");
                    continue;
                }

                // Small pause between attacks only if still on same tile as enemy
                yield return new WaitForSeconds(attackInterval);
            }

            // Skill complete
            Debug.Log($"EQSkill completed after firing {shotsFired}/{maxShots} shots");
            Destroy(gameObject);
        }

        private Transform FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            
            if (enemies.Length == 0) return null;

            Transform closestEnemy = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject enemyObj in enemies)
            {
                // Skip inactive enemies
                if (!enemyObj.activeInHierarchy) continue;
                
                float distance = Vector3.Distance(transform.position, enemyObj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemyObj.transform;
                }
            }

            return closestEnemy;
        }

        private IEnumerator MoveTowardsEnemy()
        {
            if (targetEnemy == null) yield break;
            
            isMoving = true;
            Vector2Int targetGridPos = tileGrid.GetGridPosition(targetEnemy.position);

            while (currentGridPosition != targetGridPos && targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
            {
                // Determine movement direction
                Vector2Int moveDirection = GetMoveDirection(currentGridPosition, targetGridPos);
                Vector2Int newGridPosition = currentGridPosition + moveDirection;

                // Move to new grid position
                transform.position = tileGrid.GetWorldPosition(newGridPosition);
                currentGridPosition = newGridPosition;

                Debug.Log($"Moved to position {currentGridPosition}");
                yield return new WaitForSeconds(1f / moveSpeed);

                // Recalculate target position in case enemy moves
                if (targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
                {
                    targetGridPos = tileGrid.GetGridPosition(targetEnemy.position);
                }
                else
                {
                    Debug.Log("Enemy became invalid during movement");
                    break;
                }
            }

            Debug.Log($"Movement complete, now at {currentGridPosition}");
            isMoving = false;
        }

        private Vector2Int GetMoveDirection(Vector2Int current, Vector2Int target)
        {
            // Simple grid movement towards target
            Vector2Int direction = Vector2Int.zero;
            
            if (current.x < target.x) direction.x = 1;
            else if (current.x > target.x) direction.x = -1;

            if (current.y < target.y) direction.y = 1;
            else if (current.y > target.y) direction.y = -1;

            return direction;
        }

        private IEnumerator PerformAttack()
        {
            if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy) 
            {
                Debug.Log("Target enemy is null or inactive, skipping attack");
                yield break;
            }

            // Visual attack feedback
            yield return StartCoroutine(VisualAttackFeedback());

            // Check if enemy is in the same grid tile
            Vector2Int enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);
            
            if (enemyGridPos == currentGridPosition)
            {
                // Deal damage to enemy
                Enemy enemyComponent = targetEnemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.TakeDamage(damageAmount);
                    shotsFired++;
                    Debug.Log($"EQSkill dealt {damageAmount} damage to enemy at position {enemyGridPos}. Shot {shotsFired}/{maxShots}");
                }
            }
        }

        private IEnumerator VisualAttackFeedback()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.color;
                renderer.color = attackFlashColor;
                yield return new WaitForSeconds(flashDuration);
                renderer.color = originalColor;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize current grid position in editor
            if (Application.isPlaying && tileGrid != null)
            {
                Vector3 centerPos = tileGrid.GetWorldPosition(currentGridPosition);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(centerPos, Vector3.one);
            }
        }
    }
}