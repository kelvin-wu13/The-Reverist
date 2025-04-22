using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileMovement : MonoBehaviour
{
    [SerializeField] private bool isRepeatedMovement = false;
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private float tileSize = 5f;

    private bool isMoving = false;


    // Update is called once per frame
    private void Update()
    {
        if(!isMoving)
        {
            System.Func<KeyCode, bool> inputFunction;

            if(isRepeatedMovement)
            {
                inputFunction = Input.GetKey;
            }
            else
            {
                inputFunction = Input.GetKeyDown;
            }
  
            //if the input function is active
            if (inputFunction(KeyCode.W))
            {
                StartCoroutine(Move(Vector2.up));
            }
            else if (inputFunction(KeyCode.S))
            {
                StartCoroutine(Move(Vector2.down));
            }
            else if (inputFunction(KeyCode.A))
            {
                StartCoroutine(Move(Vector2.left));
            }
            else if (inputFunction(KeyCode.D))
            {
                StartCoroutine(Move(Vector2.right));
            }
        }
    }

    private IEnumerator Move(Vector2 direction)
    {
        //Record we are moving 
        isMoving = true;

        //Make a note where we are going
        Vector2 startPos = transform.position;
        Vector2 endPos = startPos + (direction * tileSize);

        //Move in the desired direction by taking the required time
        float elapsedTime = 0;
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / moveDuration;
            transform.position = Vector2.Lerp(startPos, endPos, percent);
            yield return null;
        }

        //Make sure to end up exactly where we want
        transform.position = endPos;

        //No longer mvoing
        isMoving = false;
    }
}
