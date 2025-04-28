using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class QESkill : Skill
    {
        [Header("QE Skill Settings")]
        [SerializeField] private int damage = 30;
        [SerializeField] private float laserWidth = 0.3f;
        [SerializeField] private float laserMaxDistance = 20f;
        [SerializeField] private float laserDuration = 0.8f;
        [SerializeField] private Color laserColor = Color.blue;
        [SerializeField] private GameObject laserPrefab;
        
        private TileGrid tileGrid;
        private LineRenderer laserLineRenderer;
        private GameObject activeLaser;
        
        private void Awake()
        {
            // Find the TileGrid in the scene
            FindTileGrid();
        }

        private void FindTileGrid()
        {
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("QESkill: Could not find TileGrid in the scene!");
                }
            }
        }

        // Override the parent class's method
        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Make sure TileGrid is initialized
            FindTileGrid();
            
            // Fire laser from the caster position in the direction of the target
            FireLaser(casterTransform.position, targetPosition);
        }

        private void FireLaser(Vector3 startPosition, Vector2Int targetGridPosition)
        {
            // Ensure TileGrid is available
            if (tileGrid == null)
            {
                Debug.LogError("QESkill: TileGrid reference is null when trying to fire laser!");
                return;
            }
            
            // Convert target grid position to world position
            Vector3 targetPosition = tileGrid.GetWorldPosition(targetGridPosition);
            
            // Calculate direction vector from start to target
            Vector3 direction = (targetPosition - startPosition).normalized;
            
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
            
            // Set laser start point
            laserLineRenderer.SetPosition(0, startPosition);
            
            // Calculate end point by raycasting to find the maximum distance
            RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, direction, laserMaxDistance);
            
            // Calculate the end position
            Vector3 endPosition = startPosition + direction * laserMaxDistance;
            laserLineRenderer.SetPosition(1, endPosition);
            
            // Process all hits along the laser path
            ProcessLaserHits(hits, startPosition, direction);
            
            // Make the laser visible
            laserLineRenderer.enabled = true;
            
            // Start the laser duration countdown
            StartCoroutine(DeactivateLaserAfterDuration());
            
            Debug.Log($"QE Skill: Fired laser toward {targetGridPosition}");
        }
        
        private GameObject CreateBasicLaser()
        {
            // Create a simple laser GameObject
            GameObject laser = new GameObject("QE_Laser");
            
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
        
        private void ProcessLaserHits(RaycastHit2D[] hits, Vector3 startPosition, Vector3 direction)
        {
            // Sort hits by distance from start position
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
                        Debug.Log($"QE Skill: Dealt {damage} damage to enemy at position {hit.point}");
                        
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
                        Debug.Log($"QE Skill: Dealt {damage} damage to obstacle at position {hit.point}");
                    }
                }
            }
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
            Vector3 direction = transform.up;
            Gizmos.DrawLine(transform.position, transform.position + direction * laserMaxDistance);
        }
    }
    
    // Interface for objects that can take damage (like obstacles)
    public interface IDestructible
    {
        void TakeDamage(int damage);
    }
}