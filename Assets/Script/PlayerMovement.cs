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
    
    private bool isMoving = false;
    private Vector2Int currentGridPosition = new Vector2Int(0, 0);
    private Vector2Int lastDirection = Vector2Int.down; // Default facing direction
    
    // Public getter for current grid position
    public Vector2Int GetCurrentGridPosition()
    {
        return currentGridPosition;
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
        UpdateAnimationParameters(Vector2Int.zero, false);
    }

    private void Update()
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
                TryMove(moveDirection);
            }
        }
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int targetGridPosition = currentGridPosition + direction;

        // Check if the target position is valid
        if (tileGrid.IsValidPlayerPosition(targetGridPosition))
        {
            // Update animation parameters before starting movement
            UpdateAnimationParameters(direction, true);
            StartCoroutine(Move(targetGridPosition));
        }
        else
        {
            // Just face the direction without moving
            UpdateAnimationParameters(direction, false);
        }
    }

    private IEnumerator Move(Vector2Int targetGridPosition)
    {
        isMoving = true;

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
        
        // Reset to idle state when movement is complete
        UpdateAnimationParameters(Vector2Int.zero, false);
    }
    
    private void UpdateAnimationParameters(Vector2Int direction, bool isMoving)
    {
        if (animator != null)
        {
            // If we received a zero direction but we're supposed to be moving,
            // use the last non-zero direction (for continuing an animation)
            if (direction == Vector2Int.zero && isMoving)
            {
                direction = lastDirection;
            }
            
            // Set animator parameters
            animator.SetBool(isMovingParam, isMoving);
            
            // Only update direction if we have a non-zero direction
            if (direction != Vector2Int.zero)
            {
                animator.SetFloat(directionXParam, direction.x);
                animator.SetFloat(directionYParam, direction.y);
            }
        }
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