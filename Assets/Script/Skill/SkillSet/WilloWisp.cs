using System.Collections;
using UnityEngine;
using SkillSystem;

public class WilloWisp : Skill
{
    [Header("Skill Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private int maxShots = 3;
    [SerializeField] private float chargeTime = 1.0f; // Time it takes to charge an attack
    [SerializeField] public float cooldownDuration = 2.0f;
    [SerializeField] public float manaCost = 2.0f;

    [Header("Visual Effects")]
    [SerializeField] private Color attackFlashColor = Color.red;
    [SerializeField] private Color chargingColor = Color.yellow; // Color during charge
    [SerializeField] private float flashDuration = 0.1f;

    private TileGrid tileGrid;
    private PlayerStats playerStats;
    private Transform targetEnemy;
    private Vector2Int currentGridPosition;
    private Vector2Int targetPositionAtChargeStart;
    private int shotsFired = 0;
    private bool isMoving = false;
    private bool isSkillActive = true;
    private bool isCharging = false;

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

        // Find PlayerStats
        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("EQSkill: TileGrid not found!");
            return;
        }

        // Reset skill state
        shotsFired = 0;
        isMoving = false;
        isCharging = false;
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
        // Move mana check to the very beginning, before any other logic
        if (playerStats != null && !playerStats.TryUseMana(manaCost))
        {
            Debug.Log("Not enough mana to cast WilloWisp!");
            Destroy(gameObject); // Destroy the skill object if mana check fails
            yield break;
        }
        
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

            // Always check enemy's current position
            Vector2Int enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);

            // Always chase the enemy first before deciding to attack
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
            }

            // After movement, recheck if we're in the same position as the enemy
            // Enemy might have moved during our movement
            if (targetEnemy != null && targetEnemy.gameObject.activeInHierarchy)
            {
                enemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);

                if (currentGridPosition == enemyGridPos)
                {
                    // Record enemy position at charge start
                    targetPositionAtChargeStart = enemyGridPos;

                    // Only charge attack if we're in the same tile
                    Debug.Log("In same tile as enemy, charging attack");
                    yield return StartCoroutine(ChargeAttack());

                    // Perform the attack after charging
                    yield return StartCoroutine(PerformAttack());

                    // Check if we're done with all shots
                    if (shotsFired >= maxShots)
                    {
                        Debug.Log($"Reached maximum shots ({maxShots}), ending skill");
                        break;
                    }
                }
                else
                {
                    // Enemy not in the same tile - continue chase on next loop
                    Debug.Log("Enemy not in same tile after movement - continuing chase");
                }
            }
            else
            {
                // Target enemy is dead or inactive
                Debug.Log("Target enemy no longer valid after movement");
                continue;
            }

            // Small pause between action cycles
            yield return new WaitForSeconds(0.2f);
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

    private IEnumerator ChargeAttack()
    {
        if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy) 
        {
            Debug.Log("Target enemy is null or inactive, skipping charge");
            yield break;
        }

        isCharging = true;
        float chargeStartTime = Time.time;
        
        // Visual indicator for charging
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Color originalColor = Color.white;
        if (renderer != null)
        {
            originalColor = renderer.color;
            renderer.color = chargingColor;
        }
        
        Debug.Log($"Started charging attack. Enemy position at charge start: {targetPositionAtChargeStart}");
        
        // Wait for charge time
        yield return new WaitForSeconds(chargeTime);
        
        // Restore original color
        if (renderer != null)
        {
            renderer.color = originalColor;
        }
        
        isCharging = false;
        Debug.Log("Attack charge complete");
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

        // Check enemy's current position to see if they dodged
        Vector2Int currentEnemyGridPos = tileGrid.GetGridPosition(targetEnemy.position);
        
        // Always count as a shot fired when we attempt to attack
        shotsFired++;
        
        // Check if enemy moved during charge time (dodged)
        bool enemyDodged = currentEnemyGridPos != targetPositionAtChargeStart;
        
        if (enemyDodged)
        {
            Debug.Log($"Enemy dodged the attack! Moved from {targetPositionAtChargeStart} to {currentEnemyGridPos} during charge - Shot {shotsFired}/{maxShots} missed");
        }
        else
        {
            // Deal damage to enemy only if they didn't dodge
            Enemy enemyComponent = targetEnemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.TakeDamage(damageAmount);
                Debug.Log($"EQSkill dealt {damageAmount} damage to enemy at position {currentEnemyGridPos}. Shot {shotsFired}/{maxShots}");
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
            
            // Draw charge target position if charging
            if (isCharging && targetEnemy != null)
            {
                Vector3 targetPos = tileGrid.GetWorldPosition(targetPositionAtChargeStart);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetPos, 0.3f);
            }
        }
    }
}