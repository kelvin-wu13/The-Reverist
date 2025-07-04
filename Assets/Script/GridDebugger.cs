using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GridDebugger : MonoBehaviour
{
    private TileGrid tileGrid;
    
    void Start()
    {
        tileGrid = FindObjectOfType<TileGrid>();
        DebugAllGridPositions();
    }
    
    void DebugAllGridPositions()
    {
        if (tileGrid == null) return;
        
        Debug.Log("=== GRID POSITION DEBUG ===");
        Debug.Log($"Grid Size: {tileGrid.gridWidth} x {tileGrid.gridHeight}");
        
        for (int y = 0; y < tileGrid.gridHeight; y++)
        {
            for (int x = 0; x < tileGrid.gridWidth; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = tileGrid.GetWorldPosition(gridPos);
                
                string rowDescription = "";
                if (y == tileGrid.gridHeight - 1) rowDescription = " (TOP ROW)";
                else if (y == tileGrid.gridHeight - 2) rowDescription = " (SECOND FROM TOP)";
                else if (y == 1) rowDescription = " (SECOND FROM BOTTOM)";
                else if (y == 0) rowDescription = " (BOTTOM ROW)";
                
                Debug.Log($"Grid({x},{y}){rowDescription} -> World({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");
            }
        }
    }
    
    // void Update()
    // {
    //     // Press G to get current player position
    //     if (Input.GetKeyDown(KeyCode.G))
    //     {
    //         GameObject player = GameObject.FindGameObjectWithTag("Player");
    //         if (player != null && tileGrid != null)
    //         {
    //             Vector3 playerWorldPos = player.transform.position;
    //             Vector2Int playerGridPos = tileGrid.GetGridPosition(casterTransform.position);
                
    //             Debug.Log($"=== PLAYER POSITION CHECK ===");
    //             Debug.Log($"Player World Position: {playerWorldPos}");
    //             Debug.Log($"Player Grid Position: {playerGridPos}");
                
    //             string rowDescription = "";
    //             if (playerGridPos.y == tileGrid.gridHeight - 1) rowDescription = "TOP ROW";
    //             else if (playerGridPos.y == tileGrid.gridHeight - 2) rowDescription = "SECOND FROM TOP";
    //             else if (playerGridPos.y == 1) rowDescription = "SECOND FROM BOTTOM";
    //             else if (playerGridPos.y == 0) rowDescription = "BOTTOM ROW";
    //             else rowDescription = "MIDDLE ROW";
                
    //             Debug.Log($"Row Description: {rowDescription}");
                
    //             // Show what the expected world position should be for this grid position
    //             Vector3 expectedWorldPos = tileGrid.GetWorldPosition(playerGridPos);
    //             Debug.Log($"Expected World Position for Grid({playerGridPos.x},{playerGridPos.y}): {expectedWorldPos}");
    //             Debug.Log($"Position Difference: {Vector3.Distance(playerWorldPos, expectedWorldPos):F3} units");
    //         }
    //     }
    // }
}