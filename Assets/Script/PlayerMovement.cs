using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private TileGrid tileGrid;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    // Animation parameter hash IDs for better performance
    private readonly int isMovingParam = Animator.StringToHash("IsMoving");
    private readonly int directionXParam = Animator.StringToHash("DirectionX");
    private readonly int directionYParam = Animator.StringToHash("DirectionY");
    
    [Header("Position Offset")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0.5f, 0.5f); // Offset to center character on tile

    [Header("Animation Settings")]
    [SerializeField] private bool smoothDirectionTransition = true;
    [SerializeField] private float directionSmoothTime = 0.1f;

    // Smooth direction transition variables
    private Vector2 currentAnimDirection = Vector2.zero;
    private Vector2 targetAnimDirection = Vector2.zero;
    private Vector2 directionVelocity = Vector2.zero;

    private bool isMoving = false;
    private Vector2Int currentGridPosition = new Vector2Int(0, 0);
    private Vector2Int lastDirection = Vector2Int.down; // Default facing direction
    
    // Public getter for current grid position
    public Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition;
    }
    
    // Public getter for Skill position
    public Vector2 GetPositionOffset()
    {
        return positionOffset;
    }

    
    private void Start()
    {
        // Initialize player position
        transform.position = GetAdjustedWorldPosition(currentGridPosition);

        // If animator wasn't assigned in inspector, try to get it
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Set initial animation state
        targetAnimDirection = Vector2.down;
        currentAnimDirection = Vector2.down;
        UpdateAnimationParameters(false);
    }

    private void Update()
    {
        HandleInput();
        UpdateAnimationDirection();
        UpdateAnimationParameters(true);
    }

    private void HandleInput()
    {
        if (!isMoving)
        {
            Vector2Int moveDirection = Vector2Int.zero;
            
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                moveDirection = Vector2Int.up;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                moveDirection = Vector2Int.down;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                moveDirection = Vector2Int.left;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                moveDirection = Vector2Int.right;
            }
            
            if (moveDirection != Vector2Int.zero)
            {
                lastDirection = moveDirection;
                targetAnimDirection = new Vector2(moveDirection.x, moveDirection.y);
                TryMove(moveDirection);
            }
        }
    }

    private void UpdateAnimationDirection()
    {
        if (smoothDirectionTransition)
        {
            //Smooth transition
            currentAnimDirection = Vector2.SmoothDamp(
                currentAnimDirection,
                targetAnimDirection,
                ref directionVelocity,
                directionSmoothTime
            );
        }
        else
        {
            //Instant direction change
            currentAnimDirection = targetAnimDirection;
        }
        //Update animator
        UpdateAnimationParameters(isMoving);
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetGridPosition = currentGridPosition + direction;

        // Check if the target position is valid
        if (tileGrid.IsValidPlayerPosition(targetGridPosition))
        {
            StartCoroutine(Move(targetGridPosition));
        }
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

        // 👉 Reset direction to idle (important!)
        targetAnimDirection = Vector2.zero;
        UpdateAnimationDirection(); // Immediately apply idle direction
    }


    
    private void UpdateAnimationParameters(bool moving)
    {
        if (animator != null)
        {
            // Set movement state
            animator.SetBool(isMovingParam, moving);
            
            // Set direction parameters for blend tree
            animator.SetFloat(directionXParam, currentAnimDirection.x);
            animator.SetFloat(directionYParam, currentAnimDirection.y);
        }
    }
    
    // Method to manually set facing direction (useful for other systems)
    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction != Vector2Int.zero)
        {
            lastDirection = direction;
            targetAnimDirection = new Vector2(direction.x, direction.y);
        }
    }
    
    // Method to get current facing direction
    public Vector2Int GetFacingDirection()
    {
        return lastDirection;
    }
    
    // Method to check if player is currently moving
    public bool IsMoving()
    {
        return isMoving;
    }
    
    // New method to calculate adjusted world position with offset
    private Vector3 GetAdjustedWorldPosition(Vector2Int gridPosition)
    {
        // Get the base position from TileGrid
        Vector3 basePosition = tileGrid.GetWorldPosition(gridPosition);

        // Add the offset to center the character on the tile
        // The offset is scaled by tile size
        float tileWidth = tileGrid.GetTileWidth();
        float tileHeight = tileGrid.GetTileHeight();

        Vector3 offset = new Vector3(
            positionOffset.x * tileWidth,
            positionOffset.y * tileHeight,
            0f
        );

        return basePosition + offset;
    }
}