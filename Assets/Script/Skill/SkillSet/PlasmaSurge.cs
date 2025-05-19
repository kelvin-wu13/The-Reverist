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
                else
                {
                    activeLaser = CreateBasicLaser();
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
            laserLineRenderer.enabled = true;
            
            // Start the laser duration countdown
            StartCoroutine(DeactivateLaserAfterDuration());
            
            Debug.Log($"PlasmaSurge: Fired horizontal laser across row at position Y={playerGridPosition.y}");
        }
        
        private void ProcessRowHits(Vector2Int playerGridPosition, Vector3 direction)
        {
            // Get the start world position
            Vector3 startWorldPos = tileGrid.GetWorldPosition(playerGridPosition);
            
            // Cast a ray to find all objects in the path
            RaycastHit2D[] hits = Physics2D.RaycastAll(startWorldPos, direction, laserMaxDistance);
            
            // Sort hits by distance
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            // Create a HashSet to track which objects have already been hit
            HashSet<Collider2D> hitObjects = new HashSet<Collider2D>();
            
            foreach (RaycastHit2D hit in hits)
            {
                // Skip if we've already processed this object
                if (hitObjects.Contains(hit.collider))
                    continue;
                
                // Add to our tracking set
                hitObjects.Add(hit.collider);
                
                // Check for enemy tag
                if (hit.collider.CompareTag("Enemy"))
                {
                    // Get enemy component and apply damage
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage);
                        Debug.Log($"PlasmaSurge: Dealt {damage} damage to enemy at position {hit.point}");
                        
                        // Create a hit effect at the impact point
                        CreateImpactEffect(hit.point);
                    }
                }
                // Check for obstacle tag
                else if (hit.collider.CompareTag("Obstacle"))
                {
                    // Create a hit effect at the impact point
                    CreateImpactEffect(hit.point);
                    
                    // Get obstacle component and apply damage if it has one
                    IDestructible destructible = hit.collider.GetComponent<IDestructible>();
                    if (destructible != null)
                    {
                        destructible.TakeDamage(damage);
                        Debug.Log($"PlasmaSurge: Dealt {damage} damage to obstacle at position {hit.point}");
                    }
                }
            }
            
            // Also check grid tiles in the same row for enemies and obstacles
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
            // This is a placeholder. Your TileGrid should have a method to return its width
            // For example: return tileGrid.Width;
            return 100; // Default large value as fallback
        }
        
        private void CheckTileForTargets(Vector2Int tilePosition)
        {
            // Get the world position for this tile
            Vector3 worldPosition = tileGrid.GetWorldPosition(tilePosition);
            
            // Use a small overlap circle to detect objects at this tile
            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.4f); // Adjust radius as needed
            
            foreach (Collider2D collider in colliders)
            {
                // Check for enemy tag
                if (collider.CompareTag("Enemy"))
                {
                    Enemy enemy = collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage);
                        Debug.Log($"PlasmaSurge: Dealt {damage} damage to enemy at grid position {tilePosition}");
                        CreateImpactEffect(worldPosition);
                    }
                }
                // Check for obstacle tag
                else if (collider.CompareTag("Obstacle"))
                {
                    IDestructible destructible = collider.GetComponent<IDestructible>();
                    if (destructible != null)
                    {
                        destructible.TakeDamage(damage);
                        Debug.Log($"PlasmaSurge: Dealt {damage} damage to obstacle at grid position {tilePosition}");
                        CreateImpactEffect(worldPosition);
                    }
                }
            }
        }
        
        private GameObject CreateBasicLaser()
        {
            // Create a simple laser GameObject
            GameObject laser = new GameObject("PlasmaSurge_Laser");
            
            // Add LineRenderer component
            LineRenderer lineRenderer = laser.AddComponent<LineRenderer>();
            SetupLaserLineRenderer(lineRenderer);
            
            return laser;
        }
        
        private void SetupLaserLineRenderer(LineRenderer lineRenderer)
        {
            // Configure the LineRenderer component
            lineRenderer.positionCount = 2; // Start and end points
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = laserColor;
            lineRenderer.endColor = laserColor;
            lineRenderer.enabled = false; // Start disabled
        }
        
        private void CreateImpactEffect(Vector3 position)
        {
            // Create a simple impact effect
            GameObject impact = new GameObject("LaserImpact");
            impact.transform.position = position;
            
            // Add a particle system
            ParticleSystem particles = impact.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = laserColor;
            main.startSize = 0.5f;
            main.startSpeed = 2f;
            main.startLifetime = 0.3f;
            main.duration = 0.2f;
            
            // Emission module
            var emission = particles.emission;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst(0f, 15);
            emission.SetBurst(0, burst);
            
            // Shape module
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            // Auto-destroy after effect completes
            Destroy(impact, 1f);
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