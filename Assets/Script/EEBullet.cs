using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class EEBullet : MonoBehaviour
    {
        [Header("Bullet Properties")]
        [SerializeField] private int damage = 15;
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private string targetTag = "Enemy";

        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float fadeOutTime = 0.2f;

        // Movement properties (using Bullet script approach)
        private Vector2 direction;
        private float speed;
        private TileGrid tileGrid;
        private Vector2Int currentGridPosition;
        private bool isDestroying = false;

        // Initialize for grid-based movement (GridLock skill)
        public void InitializeGridBased(float movementSpeed, int bulletDamage, float enemyStunDuration, TileGrid grid)
        {
            // Use the same initialization as regular Bullet script
            direction = Vector2.right; // Always move right for GridLock
            speed = movementSpeed;
            damage = bulletDamage;
            stunDuration = enemyStunDuration;
            tileGrid = grid;

            // Set rotation to face right (same as Bullet script)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Use the same position logic as Bullet script (bullet spawns at FirePoint)
            currentGridPosition = tileGrid.GetGridPosition(transform.position);
        }

        // Initialize for directional movement (like original Bullet script)
        public void Initialize(Vector2 dir, float spd, int dmg, TileGrid grid)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            tileGrid = grid;

            // Set rotation based on direction (from Bullet script)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            currentGridPosition = tileGrid.GetGridPosition(transform.position);
        }

        private void Update()
        {
            if (isDestroying) return;

            // Use the same movement logic as Bullet script
            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            // Use the same position adjustment as Bullet script for hit detection
            float tileCenterYOffset = tileGrid.GetTileHeight() * 0.5f;
            Vector3 adjusted = transform.position - new Vector3(0, tileCenterYOffset, 0);
            Vector2Int newGridPosition = tileGrid.GetGridPosition(adjusted);

            if (newGridPosition != currentGridPosition)
            {
                currentGridPosition = newGridPosition;
                CheckForEnemyHit(currentGridPosition);
                CheckIfPastRightmostGrid();
            }
        }

        // Use the same hit detection logic as Bullet script
        private void CheckForEnemyHit(Vector2Int gridPosition)
        {
            // Use Bullet script's enemy position validation
            if (!IsEnemyTilePosition(gridPosition)) return;

            GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
            foreach (GameObject enemy in enemies)
            {
                // Use same position adjustment as Bullet script
                Vector3 enemyAdjusted = enemy.transform.position - new Vector3(0, tileGrid.GetTileHeight() * 0.5f, 0);
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemyAdjusted);

                if (enemyGridPos == gridPosition)
                {
                    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.TakeDamage(damage);
                        
                        // Add stun effect for GridLock skill
                        enemyComponent.Stun(stunDuration);

                        // Use hit effect spawning logic from Bullet script
                        SpawnHitEffect(transform.position);
                        DestroyBullet();
                        break;
                    }
                }
            }
        }

        // Hit effect spawning from Bullet script
        private void SpawnHitEffect(Vector3 pos)
        {
            // Try to use the new hitEffectPrefab first (Bullet script style)
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, pos, Quaternion.identity);
            }
            // Fall back to ParticleSystem if available (original EEBullet style)
            else if (hitEffect != null)
            {
                Instantiate(hitEffect, pos, Quaternion.identity);
            }
        }

        // Boundary checking from Bullet script
        private void CheckIfPastRightmostGrid()
        {
            if (currentGridPosition.x >= tileGrid.gridWidth || currentGridPosition.x > tileGrid.gridWidth - 1)
            {
                DestroyBullet();
            }
        }

        // Enemy position validation from Bullet script
        private bool IsEnemyTilePosition(Vector2Int gridPosition)
        {
            return tileGrid.IsValidGridPosition(gridPosition) && gridPosition.x >= tileGrid.gridWidth / 2;
        }

        // Enhanced destroy method combining both scripts' approaches
        private void DestroyBullet()
        {
            if (isDestroying) return;
            isDestroying = true;

            // Disable collider (from both scripts)
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            StartCoroutine(FadeOutAndDestroy());
        }

        // Fade out logic from Bullet script
        private IEnumerator FadeOutAndDestroy()
        {
            float startAlpha = 1f;
            float elapsedTime = 0;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            while (elapsedTime < fadeOutTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutTime);
                if (sr != null)
                {
                    Color color = sr.color;
                    color.a = alpha;
                    sr.color = color;
                }
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}