using UnityEngine;
using System.Collections.Generic;

namespace SkillSystem
{
    public class MagneticPull : Skill
    {
        [SerializeField] private float pushForce = 5f;
        [SerializeField] private float pushDuration = 0.5f;
        
        // Override the execute skill effect method
        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Log activation of the skill
            Debug.Log($"EW Skill activated at {targetPosition}");
            
            // Get reference to the TileGrid
            TileGrid tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("TileGrid not found in the scene!");
                return;
            }
            
            // Store pushable objects and their target positions
            List<GameObject> pushableObjects = new List<GameObject>();
            List<Vector3> targetPositions = new List<Vector3>();
            
            // Iterate through all tiles in the grid
            for (int x = 0; x < tileGrid.gridWidth; x++)
            {
                for (int y = 0; y < tileGrid.gridHeight; y++)
                {
                    Vector2Int currentGridPos = new Vector2Int(x, y);
                    
                    // Check if it's an enemy tile
                    if (tileGrid.IsValidGridPosition(currentGridPos) && 
                        tileGrid.grid[x, y] == TileType.Enemy)
                    {
                        // Find all pushable objects on this tile
                        FindPushableObjectsAtPosition(currentGridPos, tileGrid, pushableObjects, targetPositions);
                    }
                }
            }
            
            // Push all found objects
            for (int i = 0; i < pushableObjects.Count; i++)
            {
                if (pushableObjects[i] != null)
                {
                    StartPushAnimation(pushableObjects[i], targetPositions[i]);
                }
            }
        }
        
        private void FindPushableObjectsAtPosition(Vector2Int gridPos, TileGrid tileGrid, List<GameObject> objects, List<Vector3> targets)
        {
            // Get the world position of the current tile
            Vector3 tileWorldPos = tileGrid.GetWorldPosition(gridPos);
            
            // Calculate push direction (toward player side - always to the left in grid coordinates)
            Vector2Int pushDirection = new Vector2Int(-1, 0);
            
            // Calculate the target grid position
            Vector2Int targetGridPos = new Vector2Int(gridPos.x + pushDirection.x, gridPos.y + pushDirection.y);
            
            // Skip if target position is not valid or if it's a player tile
            if (!tileGrid.IsValidGridPosition(targetGridPos) || 
                tileGrid.grid[targetGridPos.x, targetGridPos.y] == TileType.Player)
            {
                return;
            }
            
            // Check for objects on the current tile
            Collider2D[] colliders = Physics2D.OverlapCircleAll(tileWorldPos, 0.4f);
            
            foreach (Collider2D col in colliders)
            {
                // Only push objects with Enemy or Obstacle tags
                if (!col.gameObject.CompareTag("Enemy") && !col.gameObject.CompareTag("Obstacle"))
                {
                    continue;
                }
                
                // Check if target position already has an object
                Vector3 targetWorldPos = tileGrid.GetWorldPosition(targetGridPos);
                if (IsPositionOccupied(targetWorldPos))
                {
                    // Skip this object as the target position is already occupied
                    Debug.Log($"Target position {targetGridPos} is already occupied. Cannot push object.");
                    continue;
                }
                
                // Add to our lists
                objects.Add(col.gameObject);
                targets.Add(targetWorldPos);
                
                // No tile cracking as requested
            }
        }
        
        // Helper method to check if a position is already occupied by an Enemy or Obstacle
        private bool IsPositionOccupied(Vector3 position)
        {
            // Use a smaller radius to check for occupation at the exact target position
            Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.3f);
            
            foreach (Collider2D col in colliders)
            {
                if (col.gameObject.CompareTag("Enemy") || col.gameObject.CompareTag("Obstacle"))
                {
                    // Found an enemy or obstacle at this position
                    return true;
                }
            }
            
            // No enemy or obstacle found at this position
            return false;
        }
        
        private void StartPushAnimation(GameObject obj, Vector3 targetPos)
        {
            // Start the push coroutine
            StartCoroutine(PushAnimation(obj, targetPos));
        }
        
        private System.Collections.IEnumerator PushAnimation(GameObject obj, Vector3 targetPos)
        {
            Vector3 startPos = obj.transform.position;
            float elapsed = 0;
            
            while (elapsed < pushDuration)
            {
                // Calculate the current position using smooth interpolation
                float t = elapsed / pushDuration;
                t = Mathf.SmoothStep(0, 1, t); // Smooth the movement
                
                obj.transform.position = Vector3.Lerp(startPos, targetPos, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure the object is exactly at the target position
            obj.transform.position = targetPos;
            
            // Trigger any effects that should happen after pushing
            TriggerPushEffects(obj);
        }
        
        private void TriggerPushEffects(GameObject obj)
        {
            // Here you can add effects that happen when an object is pushed
            // For example, damage to the object, particle effects, sound, etc.
            
            // Example: Play a particle effect
            ParticleSystem pushParticles = obj.GetComponent<ParticleSystem>();
            if (pushParticles != null)
            {
                pushParticles.Play();
            }
            
            // Example: Play a sound effect
            AudioSource pushSound = obj.GetComponent<AudioSource>();
            if (pushSound != null)
            {
                pushSound.Play();
            }
        }
    }
}