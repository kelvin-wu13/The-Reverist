using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private TileGrid tileGrid;

    private bool isMoving = false;
    private Vector2Int currentGridPosition = new Vector2Int(0, 0);

    private void Start()
    {
        // Initialize player position
        transform.position = tileGrid.GetWorldPosition(currentGridPosition);
    }

    private void Update()
    {
        if (!isMoving)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                TryMove(Vector2Int.up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                TryMove(Vector2Int.down);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                TryMove(Vector2Int.left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                TryMove(Vector2Int.right);
            }
        }
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

        Vector3 startPos = transform.position;
        Vector3 endPos = tileGrid.GetWorldPosition(targetGridPosition);

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
    }
}