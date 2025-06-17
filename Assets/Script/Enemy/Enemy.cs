using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private GameObject stunEffectPrefab;


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

    [Header("Position Offset")]
    [SerializeField] private Vector3 stunEffectOffset = new Vector3(0, 1.2f, 0); // You can tweak Y
    [SerializeField] private Vector2 positionOffset = Vector2.zero;

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
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        transform.position = targetPosition;
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        moveTimer = moveInterval;
        shootTimer = Random.Range(0f, shootInterval);

        if (animator != null)
        {
            animator.SetTrigger("Spawn");
        }

        AudioManager.Instance?.PlayEnemySpawnSFX();
        StartCoroutine(RandomMovement());
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

        if (!isDying && !isBeingPulled)
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

    private void LateUpdate()
    {
        MatchCameraRotation();
    }

    private void MatchCameraRotation()
    {
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Reset rotation (2D style)
            // If 3D look-at-camera: transform.forward = Camera.main.transform.forward;
        }
    }

    public TileGrid GetTileGrid() => tileGrid;
    public bool IsMoving() => isMoving;
    public bool IsBeingPulled() => isBeingPulled;

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

    public void CompletePull(Vector2Int newGridPosition, Vector3 finalPosition)
    {
        currentGridPosition = newGridPosition;
        targetGridPosition = newGridPosition;
        ReserveGridPosition(currentGridPosition);
        tileGrid.SetTileOccupied(currentGridPosition, true);
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        isBeingPulled = false;
    }

    private IEnumerator RandomMovement()
    {
        while (!isDying)
        {
            yield return new WaitForSeconds(moveInterval);
            if (!isMoving && !isStunned && !isAfterPush && !isBeingPulled)
                TryMove();
        }
    }

    private void TryMove()
    {
        if (isBeingPulled)
        {
            isMoving = false;
            return;
        }

        Vector2Int[] prioritizedDirections = new Vector2Int[]
        {
            Vector2Int.left,
            Vector2Int.down,
            Vector2Int.up,
            Vector2Int.right
        };

        foreach (Vector2Int direction in prioritizedDirections)
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
                Vector3 basePosition = tileGrid.GetWorldPosition(targetGridPosition);
                targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
                isMoving = true;
                return;
            }
        }
    }

    private void ShootAtPlayer()
    {
        if (bulletPrefab == null) return;

        Vector2 direction = Vector2.left;
        GameObject bulletObj = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();

        if (bullet == null)
            bullet = bulletObj.AddComponent<EnemyBullet>();

        if (bullet != null)
        {
            bullet.Initialize(direction, bulletSpeed, bulletDamage, tileGrid);

            if (animator != null)
                animator.SetTrigger("Attack");

            AudioManager.Instance?.PlayEnemyShootSFX();
        }
        else
        {
            Debug.LogError("Enemy: Failed to setup bullet");
            Destroy(bulletObj);
        }
    }

    private void ReserveGridPosition(Vector2Int position) => reservedPositions[position] = gameObject;

    private void ReleaseGridPosition(Vector2Int position)
    {
        if (reservedPositions.ContainsKey(position) && reservedPositions[position] == gameObject)
            reservedPositions.Remove(position);
    }

    private bool IsPositionReserved(Vector2Int position) =>
        reservedPositions.ContainsKey(position) && reservedPositions[position] != gameObject;

    private bool IsPositionOccupied(Vector2Int gridPosition)
    {
        Vector3 worldPosition = tileGrid.GetWorldPosition(gridPosition);
        float checkRadius = 0.4f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, checkRadius, obstacleLayer);
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject != gameObject)
                return true;
        }
        return false;
    }

    private void ShuffleDirections()
    {
        for (int i = directions.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;
        currentHealth -= damage;
        StartCoroutine(FlashColor());
        if (currentHealth <= 0) Die();
    }

    private IEnumerator FlashColor()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (isStunned)
            spriteRenderer.color = stunnedColor;
        else if (isAfterPush)
            spriteRenderer.color = pushedColor;
        else
            spriteRenderer.color = originalColor;
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

        if (animator != null)
        {
            animator.SetTrigger("Death");
            StartCoroutine(DelayedDeath());
        }
        else
        {
            FinalizeDeath();
        }

        AudioManager.Instance?.PlayEnemyDeathSFX();
    }

    private IEnumerator DelayedDeath()
    {
        yield return new WaitForSeconds(1f); // Adjust based on your animation length
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
            Vector3 effectPos = transform.position + stunEffectOffset;
            GameObject stunVFX = Instantiate(stunEffectPrefab, effectPos, Quaternion.identity, transform);
            Destroy(stunVFX, duration);
        }

        yield return new WaitForSeconds(duration);

        isStunned = false;

        if (spriteRenderer != null && !isDying && !isAfterPush && !isBeingPulled)
            spriteRenderer.color = originalColor;
    }


    public void SetPositionOffset(Vector2 newOffset)
    {
        positionOffset = newOffset;
        if (!isMoving && !isBeingPulled)
        {
            Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
            targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
            transform.position = targetPosition;
        }
    }

    public Vector2 GetPositionOffset() => positionOffset;

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
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
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
        Vector3 basePosition = tileGrid.GetWorldPosition(currentGridPosition);
        targetPosition = basePosition + new Vector3(positionOffset.x, positionOffset.y, 0);
        transform.position = targetPosition;
    }
}
