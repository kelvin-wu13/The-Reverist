using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace SkillSystem
{
    public class PlasmaSurge : Skill
    {
        [Header("Plasma Surge Skill Settings")]
        [SerializeField] private int damage = 30;
        [SerializeField] private float laserWidth = 0.3f;
        [SerializeField] private float laserMaxDistance = 20f;
        [SerializeField] private float laserDuration = 0.8f;
        [SerializeField] private Color laserColor = Color.blue;
        [SerializeField] private GameObject laserPrefab;
        [SerializeField] private float manaCost = 1.5f; // Mana cost for this Skill
        [SerializeField] public float cooldownDuration = 1.5f; // Cooldown duration in seconds - make public so SkillCast can access it
        
        // Add isOnCooldown variable
        private bool isOnCooldown = false;
        
        private TileGrid tileGrid;
        private LineRenderer laserLineRenderer;
        private GameObject activeLaser;
        private PlayerStats playerStats;
        
        private void Awake()
        {
            // Find the TileGrid in the scene
            FindTileGrid();
            
            // Find player stats
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogError("PlasmaSurge: Could not find PlayerStats in the scene!");
            }
        }

        private void FindTileGrid()
        {
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("PlasmaSurge: Could not find TileGrid in the scene!");
                }
            }
        }

        // Override the parent class's method
        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Make sure TileGrid is initialized
            FindTileGrid();

            // Check if skill is on cooldown
            if (isOnCooldown)
            {
                Debug.Log("PlasmaSurge is on cooldown!");
                return;
            }

            // Check if player has enough mana
            if (playerStats != null && playerStats.TryUseMana(manaCost))
            {
                // Get the player's current grid position
                PlayerMovement playerMovement = casterTransform.GetComponent<PlayerMovement>();
                Vector2Int playerPosition = Vector2Int.zero;
                
                if (playerMovement != null)
                {
                    playerPosition = playerMovement.GetCurrentGridPosition();
                }
                else
                {
                    // Fallback to target position if playerMovement component not found
                    playerPosition = targetPosition;
                }
                
                // Fire laser from the player position to the right
                FireHorizontalLaser(casterTransform.position, playerPosition);
                
                // Start the cooldown
                StartCoroutine(StartCooldown());
            }
            else
            {
                Debug.Log("Not enough mana to cast PlasmaSurge!");
            }
        }

        private IEnumerator StartCooldown()
        {
            isOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            isOnCooldown = false;
            Debug.Log("PlasmaSurge cooldown finished!");
        }

        private void FireHorizontalLaser(Vector3 startPosition, Vector2Int playerGridPosition)
        {
            // Ensure TileGrid is available
            if (tileGrid == null)
            {
                Debug.LogError("PlasmaSurge: TileGrid reference is null when trying to fire laser!");
                return;
            }
            
            // Calculate the right direction based on the grid
            Vector3 direction = Vector3.right;
            
            // Create a laser GameObject if needed
            if (activeLaser == null)
            {
                if (laserPrefab != null)
                {
                    activeLaser = Instantiate(laserPrefab, startPosition, Quaternion.identity);
                }
            }
            
            // Get or add LineRenderer component
            if (laserLineRenderer == null)
            {
                laserLineRenderer = activeLaser.GetComponent<LineRenderer>();
                if (laserLineRenderer == null)
                {
                    laserLineRenderer = activeLaser.AddComponent<LineRenderer>();
                    SetupLaserLineRenderer(laserLineRenderer);
                }
            }
            
            // Set laser start point (at player position)
            Vector3 laserStartPosition = tileGrid.GetWorldPosition(playerGridPosition);
            laserLineRenderer.SetPosition(0, laserStartPosition);
            
            // Calculate end point - maximum distance to the right
            Vector3 endPosition = laserStartPosition + (direction * laserMaxDistance);
            laserLineRenderer.SetPosition(1, endPosition);
            
            // Process hits along the row to the right of the player
            ProcessRowHits(playerGridPosition, direction);
            
            // Make the laser visible
            laserLineRenderer.enabled = false;
            
            // Start the laser duration countdown
            StartCoroutine(DeactivateLaserAfterDuration());
            
            Debug.Log($"PlasmaSurge: Fired horizontal laser across row at position Y={playerGridPosition.y}");
        }
        
        private void ProcessRowHits(Vector2Int playerGridPosition, Vector3 direction)
        {
            Vector3 startWorldPos = tileGrid.GetWorldPosition(playerGridPosition);

            RaycastHit2D[] hits = Physics2D.RaycastAll(startWorldPos, direction, laserMaxDistance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            HashSet<Collider2D> hitObjects = new HashSet<Collider2D>();

            foreach (RaycastHit2D hit in hits)
            {
                if (hitObjects.Contains(hit.collider)) continue;
                hitObjects.Add(hit.collider);

                // Use the collider's transform position for accurate grid mapping
                Vector2Int hitGridPos = tileGrid.GetGridPosition(hit.collider.transform.position);
                if (hitGridPos.y != playerGridPosition.y) continue; // ✅ Skip if not same row

                if (hit.collider.CompareTag("Enemy"))
                {
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage);
                        Debug.Log($"PlasmaSurge: Dealt {damage} to enemy at {hitGridPos}");
                    }
                }
                else if (hit.collider.CompareTag("Obstacle"))
                {
                    IDestructible destructible = hit.collider.GetComponent<IDestructible>();
                    if (destructible != null)
                    {
                        destructible.TakeDamage(damage);
                        Debug.Log($"PlasmaSurge: Dealt {damage} to obstacle at {hitGridPos}");
                    }
                }
            }
            ProcessTilesInRow(playerGridPosition);
        }

        
        private void ProcessTilesInRow(Vector2Int playerGridPosition)
        {
            // Determine the width of the grid (this will need to be available from your TileGrid class)
            // For now, assuming a method or property exists to get grid dimensions
            int gridWidth = GetGridWidth();
            
            // Process all tiles to the right of the player's position
            for (int x = playerGridPosition.x + 1; x < gridWidth; x++)
            {
                Vector2Int tilePosition = new Vector2Int(x, playerGridPosition.y);
                
                // Check if there's an enemy or destructible object at this tile
                // You'll need a method in your TileGrid or a system to track objects on tiles
                CheckTileForTargets(tilePosition);
            }
        }
        
        private int GetGridWidth()
        {
            return tileGrid != null ? tileGrid.gridWidth : 0;
        }
        
        private void CheckTileForTargets(Vector2Int tilePosition)
        {
            Vector3 worldPosition = tileGrid.GetWorldPosition(tilePosition);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.2f); // ✅ Smaller radius
        }

        private void SetupLaserLineRenderer(LineRenderer lineRenderer)
        {
            // Configure the LineRenderer component
            lineRenderer.positionCount = 2; // Start and end points
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;
            lineRenderer.enabled = false; // Start disabled
        }
        
        
        private IEnumerator DeactivateLaserAfterDuration()
        {
            // Wait for the laser duration
            yield return new WaitForSeconds(laserDuration);
            
            // Disable and destroy the laser
            if (laserLineRenderer != null)
            {
                laserLineRenderer.enabled = false;
            }
            
            if (activeLaser != null)
            {
                Destroy(activeLaser);
                activeLaser = null;
                laserLineRenderer = null;
            }
        }
        
        // Override OnDestroy to clean up any remaining laser objects
        private void OnDestroy()
        {
            if (activeLaser != null)
            {
                Destroy(activeLaser);
            }
        }
        
        // Visualize the laser in the editor
        private void OnDrawGizmosSelected()
        {
            // Draw a line representing the potential laser path
            Gizmos.color = laserColor;
            Vector3 direction = Vector3.right;
            Gizmos.DrawLine(transform.position, transform.position + direction * laserMaxDistance);
        }
    }
    
    // Interface for objects that can take damage (like obstacles)
    public interface IDestructible
    {
        void TakeDamage(int damage);
    }
}