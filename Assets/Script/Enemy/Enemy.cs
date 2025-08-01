using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Enemy : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private GameObject stunEffectPrefab;
    [SerializeField] private Vector3 stunEffectOffset = new Vector3(0, 1.2f, 0);

    [Header("Settings")]
    [SerializeField] public int maxHealth = 100;
    [SerializeField] public int currentHealth;
    [SerializeField] private bool isDying = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveInterval = 2f;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private bool isStunned = false;
    [SerializeField] private bool isAfterPush = false;
    [SerializeField] private bool isBeingPulled = false;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Offset Settings")]
    [SerializeField] private Vector2 baseOffset = new Vector2(0f, 1.6f);
    [SerializeField] private float xOffsetFalloffPerRow = 0.05f;
    [SerializeField] private float yOffsetFalloffPerRow = 0.1f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private Color stunnedColor = Color.blue;
    [SerializeField] private Color pushedColor = new Color(1f, 0.5f, 0f);

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float shootInterval = 3f;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float shootChance = 0.7f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private TileGrid tileGrid;
    private Vector2Int currentGridPosition;
    private Vector2Int targetGridPosition;
    private Vector3 targetPosition;
    private Color originalColor;
    private float moveTimer;
    private float shootTimer;
    private bool isTrainingScene = false;

    private bool canMoveAndShoot = false;
    private bool isDamageable = false;

    private static Dictionary<Vector2Int, GameObject> reservedPositions = new Dictionary<Vector2Int, GameObject>();

    private Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public static void ClearAllReservations() => reservedPositions.Clear();
    public static int GetReservationCount() => reservedPositions.Count;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        tileGrid = FindObjectOfType<TileGrid>();
        if (tileGrid == null)
            Debug.LogError("Enemy: Could not find TileGrid");

        if (shootPoint == null)
            shootPoint = transform;

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        EnemyManager.Instance?.RegisterEnemy(this);

        currentGridPosition = tileGrid.GetGridPosition(transform.position);
        targetGridPosition = currentGridPosition;
        targetPosition = GetAdjustedWorldPosition(currentGridPosition);
        transform.position = targetPosition;

        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        moveTimer = moveInterval;
        shootTimer = Random.Range(0f, shootInterval);

        animator?.SetTrigger("Spawn");
        StartCoroutine(RandomMovement());

        isTrainingScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Training");

        if (isTrainingScene)
        {
            StopAllCoroutines();
        }
    }

    private void Update()
    {
        if (isMoving && !isStunned && !isAfterPush && !isBeingPulled)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                transform.position = targetPosition;
                currentGridPosition = targetGridPosition;
                tileGrid.SetTileOccupied(currentGridPosition, true);
            }
        }

        if (canMoveAndShoot && !isDying && !isBeingPulled && !PlayerStats.IsPlayerDead)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0)
            {
                shootTimer = shootInterval;
                if (Random.value <= shootChance)
                    ShootAtPlayer();
            }
        }
    }

    public void SetBehavior(bool canMoveAndShoot, bool isDamageable)
    {
        this.canMoveAndShoot = canMoveAndShoot;
        this.isDamageable = isDamageable;

        if (this.canMoveAndShoot)
        {
            StartCoroutine(RandomMovement());
        }
        else
        {
            StopAllCoroutines();
        }
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
    }

    private IEnumerator RandomMovement()
    {
        while (!isDying)
        {
            yield return new WaitForSeconds(moveInterval);
            if (!PlayerStats.IsPlayerDead && !isMoving && !isStunned && !isAfterPush && !isBeingPulled)
                TryMove();
        }
    }

    private void TryMove()
    {
        List<Vector2Int> directions = new List<Vector2Int>
    {
        Vector2Int.left,
        Vector2Int.down,
        Vector2Int.up,
        Vector2Int.right
    };

        System.Random rng = new System.Random();
        int n = directions.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Vector2Int value = directions[k];
            directions[k] = directions[n];
            directions[n] = value;
        }
        foreach (Vector2Int direction in directions)
        {
            Vector2Int newPosition = currentGridPosition + direction;
            if (tileGrid.IsValidGridPosition(newPosition) &&
                tileGrid.grid[newPosition.x, newPosition.y] == TileType.Enemy &&
                !IsPositionReserved(newPosition) &&
                !tileGrid.IsTileOccupied(newPosition))
            {
                ReleaseGridPosition(currentGridPosition);
                tileGrid.SetTileOccupied(currentGridPosition, false);
                ReserveGridPosition(newPosition);
                tileGrid.SetTileOccupied(newPosition, true);
                targetGridPosition = newPosition;
                targetPosition = GetAdjustedWorldPosition(newPosition);
                isMoving = true;
                return;
            }
        }
    }

    public void CompletePull(Vector2Int newGridPosition, Vector3 finalPosition)
    {
        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        targetPosition = GetAdjustedWorldPosition(currentGridPosition);
        isBeingPulled = false;
    }

    public void ApplyPushEffect(Vector2Int newGridPosition, Vector3 newWorldPosition)
    {
        StopAllCoroutines();
        isMoving = false;
        isBeingPulled = false;
        tileGrid.SetTileOccupied(currentGridPosition, false);
        ReleaseGridPosition(currentGridPosition);

        if (tileGrid.IsTileOccupied(newGridPosition))
        {
            currentGridPosition = tileGrid.GetGridPosition(transform.position);
            ReserveGridPosition(currentGridPosition);
            tileGrid.SetTileOccupied(currentGridPosition, true);
            return;
        }

        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        targetPosition = GetAdjustedWorldPosition(currentGridPosition);
    }

    public void SetPositionWithOffset(Vector2Int newGridPosition)
    {
        StopAllCoroutines();
        isMoving = false;
        tileGrid.SetTileOccupied(currentGridPosition, false);
        ReleaseGridPosition(currentGridPosition);
        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        targetPosition = GetAdjustedWorldPosition(currentGridPosition);
        transform.position = targetPosition;
    }

    public void InterruptMovementForSkill()
    {
        if (isMoving && !isBeingPulled)
        {
            isMoving = false;
            transform.position = targetPosition;
            currentGridPosition = targetGridPosition;
            tileGrid.SetTileOccupied(currentGridPosition, true);
            ReserveGridPosition(currentGridPosition);
        }
    }

    public void PrepareForPull(Vector2Int targetGridPos)
    {
        isMoving = false;
        isBeingPulled = true;
        StopCoroutine(nameof(RandomMovement));
        ReleaseGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, false);
    }

    private void ShootAtPlayer()
    {
        if (bulletPrefab == null) return;

        GameObject bulletObj = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>() ?? bulletObj.AddComponent<EnemyBullet>();
        bullet.Initialize(Vector2.left, bulletSpeed, bulletDamage, tileGrid);

        animator?.SetTrigger("Attack");
        AudioManager.Instance?.PlayEnemyShootSFX();
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;

        if (!isDamageable)
        {
            StartCoroutine(FlashColor());
            return;
        }

        currentHealth -= damage;
        StartCoroutine(FlashColor());
        if (currentHealth <= 0) Die();
    }

    private IEnumerator FlashColor()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = isStunned ? stunnedColor : (isAfterPush ? pushedColor : originalColor);
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        StopAllCoroutines();
        ReleaseGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, false);

        foreach (Collider2D c in GetComponents<Collider2D>())
            c.enabled = false;

        animator?.SetTrigger("Death");
        StartCoroutine(DelayedDeath());
        AudioManager.Instance?.PlayEnemyDeathSFX();
    }

    private IEnumerator DelayedDeath()
    {
        yield return new WaitForSeconds(1f);
        FinalizeDeath();
    }

    private void FinalizeDeath()
    {
        EnemyManager.Instance?.UnregisterEnemy(this);
        EventManager.Instance?.CheckForBattleEnd();
        Destroy(gameObject);
    }

    public void Stun(float duration)
    {
        if (isDying) return;
        StartCoroutine(ApplyStun(duration));
    }

    private IEnumerator ApplyStun(float duration)
    {
        isStunned = true;
        if (spriteRenderer != null && !isBeingPulled)
            spriteRenderer.color = stunnedColor;

        if (stunEffectPrefab != null)
        {
            GameObject stunVFX = Instantiate(stunEffectPrefab, transform.position + stunEffectOffset, Quaternion.identity, transform);
            Destroy(stunVFX, duration);
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;
        if (spriteRenderer != null && !isDying && !isAfterPush && !isBeingPulled)
            spriteRenderer.color = originalColor;
    }

    private Vector3 GetAdjustedWorldPosition(Vector2Int gridPosition)
    {
        Vector3 basePos = tileGrid.GetCenteredWorldPosition(gridPosition);
        float dynamicX = baseOffset.x - (gridPosition.y * xOffsetFalloffPerRow);
        float dynamicY = baseOffset.y - (gridPosition.y * yOffsetFalloffPerRow);
        return basePos + new Vector3(dynamicX, dynamicY, 0f);
    }
    public Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition;
    }
    private void ReserveGridPosition(Vector2Int pos) => reservedPositions[pos] = gameObject;
    private void ReleaseGridPosition(Vector2Int pos)
    {
        if (reservedPositions.ContainsKey(pos) && reservedPositions[pos] == gameObject)
            reservedPositions.Remove(pos);
    }
    private bool IsPositionReserved(Vector2Int pos) => reservedPositions.ContainsKey(pos) && reservedPositions[pos] != gameObject;

    public TileGrid GetTileGrid() => tileGrid;
    public bool IsMoving() => isMoving;
    public bool IsBeingPulled() => isBeingPulled;
}
