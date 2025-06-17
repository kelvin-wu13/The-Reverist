using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private TileGrid tileGrid;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private readonly int isMovingParam = Animator.StringToHash("IsMoving");
    private readonly int directionXParam = Animator.StringToHash("DirectionX");
    private readonly int directionYParam = Animator.StringToHash("DirectionY");

    [Header("Position Offset")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0.5f, 0.5f);

    [Header("Animation Settings")]
    [SerializeField] private bool smoothDirectionTransition = true;
    [SerializeField] private float directionSmoothTime = 0.1f;

    private Vector2 currentAnimDirection = Vector2.zero;
    private Vector2 targetAnimDirection = Vector2.zero;
    private Vector2 directionVelocity = Vector2.zero;

    private bool isMoving = false;
    private bool canMove = true; // ✅ NEW: Movement toggle

    private Vector2Int currentGridPosition = new Vector2Int(0, 0);
    private Vector2Int lastDirection = Vector2Int.down;

    public Vector2Int GetCurrentGridPosition() => currentGridPosition;
    public Vector2 GetPositionOffset() => positionOffset;

    private void Start()
    {
        transform.position = GetAdjustedWorldPosition(currentGridPosition);

        if (animator == null)
            animator = GetComponent<Animator>();

        targetAnimDirection = Vector2.down;
        currentAnimDirection = Vector2.down;
        UpdateAnimationParameters(false);
    }

    private void Update()
    {
        if (canMove)
        {
            HandleInput();
            UpdateAnimationDirection();
            UpdateAnimationParameters(isMoving);
        }
    }

    private void HandleInput()
    {
        if (!isMoving)
        {
            Vector2Int moveDirection = Vector2Int.zero;

            if (Input.GetKeyDown(KeyCode.UpArrow)) moveDirection = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.DownArrow)) moveDirection = Vector2Int.down;
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) moveDirection = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow)) moveDirection = Vector2Int.right;

            if (moveDirection != Vector2Int.zero && CanMove(moveDirection))
            {
                lastDirection = moveDirection;
                targetAnimDirection = new Vector2(moveDirection.x, moveDirection.y);
                TryMove(moveDirection);
            }
        }
    }

    private void UpdateAnimationDirection()
    {
        currentAnimDirection = smoothDirectionTransition
            ? Vector2.SmoothDamp(currentAnimDirection, targetAnimDirection, ref directionVelocity, directionSmoothTime)
            : targetAnimDirection;

        UpdateAnimationParameters(isMoving);
    }

    private bool CanMove(Vector2Int direction)
    {
        Vector2Int targetGridPosition = currentGridPosition + direction;
        return tileGrid.IsValidPlayerPosition(targetGridPosition);
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetGridPosition = currentGridPosition + direction;
        if (tileGrid.IsValidPlayerPosition(targetGridPosition))
            StartCoroutine(Move(targetGridPosition));
    }

    private IEnumerator Move(Vector2Int targetGridPosition)
    {
        isMoving = true;
        animator.SetBool(isMovingParam, true);

        Vector3 startPos = transform.position;
        Vector3 endPos = GetAdjustedWorldPosition(targetGridPosition);

        float elapsedTime = 0;
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / moveDuration;
            transform.position = Vector3.Lerp(startPos, endPos, percent);
            yield return null;
        }

        transform.position = endPos;
        currentGridPosition = targetGridPosition;
        isMoving = false;
        animator.SetBool(isMovingParam, false);

        targetAnimDirection = Vector2.zero;
        UpdateAnimationDirection();
    }

    private void UpdateAnimationParameters(bool moving)
    {
        if (animator != null)
        {
            animator.SetBool(isMovingParam, moving);
            animator.SetFloat(directionXParam, currentAnimDirection.x);
            animator.SetFloat(directionYParam, currentAnimDirection.y);
        }
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction != Vector2Int.zero)
        {
            lastDirection = direction;
            targetAnimDirection = new Vector2(direction.x, direction.y);
        }
    }

    public Vector2Int GetFacingDirection() => lastDirection;
    public bool IsMoving() => isMoving;

    private Vector3 GetAdjustedWorldPosition(Vector2Int gridPosition)
    {
        Vector3 basePosition = tileGrid.GetWorldPosition(gridPosition);
        float tileWidth = tileGrid.GetTileWidth();
        float tileHeight = tileGrid.GetTileHeight();

        Vector3 offset = new Vector3(
            positionOffset.x * tileWidth,
            positionOffset.y * tileHeight,
            0f
        );

        return basePosition + offset;
    }
    
    public void SetCanMove(bool state)
    {
        canMove = state;
    }
}
