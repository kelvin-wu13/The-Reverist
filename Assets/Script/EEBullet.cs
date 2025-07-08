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

        private Vector2 direction;
        private float speed;
        private TileGrid tileGrid;
        private Vector2Int currentGridPosition;
        private bool isDestroying = false;

        public void InitializeGridBased(float movementSpeed, int bulletDamage, float enemyStunDuration, TileGrid grid)
        {
            direction = Vector2.right;
            speed = movementSpeed;
            damage = bulletDamage;
            stunDuration = enemyStunDuration;
            tileGrid = grid;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            currentGridPosition = tileGrid.GetGridPosition(transform.position);
        }

        public void Initialize(Vector2 dir, float spd, int dmg, TileGrid grid)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            tileGrid = grid;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            currentGridPosition = tileGrid.GetGridPosition(transform.position);
        }

        private void Update()
        {
            if (isDestroying) return;

            transform.Translate(direction * speed * Time.deltaTime, Space.World);

            Vector2Int newGridPosition = tileGrid.GetGridPosition(transform.position);

            if (newGridPosition != currentGridPosition)
            {
                currentGridPosition = newGridPosition;
                
                // Only check for hit if there's an enemy on this tile
                if (CheckForEnemyOnTile(currentGridPosition))
                {
                    CheckForEnemyHit(currentGridPosition);
                }
                
                CheckIfPastRightmostGrid();
            }
        }

        private bool CheckForEnemyOnTile(Vector2Int gridPosition)
        {
            if (!IsEnemyTilePosition(gridPosition)) return false;

            GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
            foreach (GameObject enemy in enemies)
            {
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemy.transform.position);

                if (enemyGridPos == gridPosition)
                {
                    return true; // Enemy found on this tile
                }
            }
            return false; // No enemy found on this tile
        }

        private void CheckForEnemyHit(Vector2Int gridPosition)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
            foreach (GameObject enemy in enemies)
            {
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemy.transform.position);

                if (enemyGridPos == gridPosition)
                {
                    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.TakeDamage(damage);
                        enemyComponent.Stun(stunDuration);

                        SpawnHitEffect(transform.position);
                        DestroyBullet();
                        break;
                    }
                }
            }
        }

        private void SpawnHitEffect(Vector3 pos)
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, pos, Quaternion.identity);
            }
            else if (hitEffect != null)
            {
                Instantiate(hitEffect, pos, Quaternion.identity);
            }
        }

        private void CheckIfPastRightmostGrid()
        {
            if (currentGridPosition.x >= tileGrid.gridWidth || currentGridPosition.x > tileGrid.gridWidth - 1)
            {
                DestroyBullet();
            }
        }

        private bool IsEnemyTilePosition(Vector2Int gridPosition)
        {
            return tileGrid.IsValidGridPosition(gridPosition) && gridPosition.x >= tileGrid.gridWidth / 2;
        }

        private void DestroyBullet()
        {
            if (isDestroying) return;
            isDestroying = true;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            StartCoroutine(FadeOutAndDestroy());
        }

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