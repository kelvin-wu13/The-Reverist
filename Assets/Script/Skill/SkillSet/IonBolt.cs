using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SkillSystem
{
    public class IonBolt : Skill
    {
        [Header("IonBolt Skill Settings")]
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private int explosionTileRadius = 1;
        [SerializeField] private int damage = 20;
        [SerializeField] private int explosionDamage = 15;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private float explosionEffectDuration = 1.0f;
        [SerializeField] public float manaCost = 1.5f;
        [SerializeField] public float cooldownDuration = 1.5f;

        [Header("Manual Speed Adjustment")]
        [SerializeField] private float speedMultiplier = 1.0f;

        private GameObject activeProjectile;
        private bool isProjectileFired = false;
        private bool isOnCooldown = false;
        private Vector2Int currentGridPosition;

        private TileGrid tileGrid;
        private PlayerMovement playerMovement;
        private PlayerStats playerStats;

        private Transform firePoint;

        private void Awake()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            if (tileGrid == null) tileGrid = FindObjectOfType<TileGrid>();
            if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();

            // Attempt to find firePoint under player
            if (firePoint == null && playerMovement != null)
            {
                Transform t = playerMovement.transform.Find("FirePoint");
                if (t != null) firePoint = t;
            }
        }

        private void Update()
        {
            if (isProjectileFired && activeProjectile != null)
            {
                float adjustedSpeed = projectileSpeed * speedMultiplier;
                activeProjectile.transform.Translate(Vector3.right * adjustedSpeed * Time.deltaTime, Space.World);

                Vector2Int newGridPosition = currentGridPosition;
                newGridPosition.x = Mathf.RoundToInt(tileGrid.GetGridPosition(activeProjectile.transform.position).x);

                if (newGridPosition.x > currentGridPosition.x)
                {
                    currentGridPosition.x = newGridPosition.x;
                    HandleHitAtTile(currentGridPosition);
                    CheckIfPastRightmostGrid();
                }
            }
        }

        public override void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            FindReferences();

            if (isOnCooldown)
            {
                Debug.Log("IonBolt is on cooldown!");
                return;
            }

            if (playerStats == null || playerStats.TryUseMana(Mathf.CeilToInt(manaCost)))
            {
                FireProjectile();
                StartCoroutine(StartCooldown());
            }
            else
            {
                Debug.Log("IonBolt: Not enough mana to cast!");
            }
        }

        private IEnumerator StartCooldown()
        {
            isOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            isOnCooldown = false;
            Debug.Log("IonBolt cooldown finished!");
        }

        private void FireProjectile()
        {
            Vector3 spawnPos = playerMovement.transform.position;

            if (firePoint != null)
            {
                spawnPos = firePoint.position;
            }

            // Correct the spawn position by removing playerMovement's visual Y offset
            spawnPos.y -= playerMovement.GetPositionOffset().y;

            currentGridPosition = tileGrid.GetGridPosition(spawnPos);

            Vector3 centeredSpawn = tileGrid.GetCenteredWorldPosition(currentGridPosition);

            activeProjectile = Instantiate(projectilePrefab, centeredSpawn, Quaternion.identity);
            AudioManager.Instance?.PlayIonBoltSFX();

            isProjectileFired = true;

            Debug.Log($"IonBolt: Fired at grid {currentGridPosition} at speed {projectileSpeed * speedMultiplier}");
        }

        private void HandleHitAtTile(Vector2Int gridPosition)
        {
            if (!IsEnemyTilePosition(gridPosition)) return;

            DamageEnemiesOnTile(gridPosition);

            ExplodeAtGridPosition(gridPosition);

            Destroy(activeProjectile);
            isProjectileFired = false;
        }

        private void DamageEnemiesOnTile(Vector2Int gridPos)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemy.transform.position);

                if (enemyGridPos == gridPos)
                {
                    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.TakeDamage(damage);
                        Debug.Log($"IonBolt: Hit enemy at tile {gridPos} for {damage} damage");
                    }
                }
            }
        }

        private void ExplodeAtGridPosition(Vector2Int centerGridPosition)
        {
            Debug.Log($"IonBolt: Explosion at grid position {centerGridPosition}");

            Vector3 worldPosition = tileGrid.GetWorldPosition(centerGridPosition);

            if (explosionEffectPrefab != null)
            {
                GameObject explosionEffect = Instantiate(explosionEffectPrefab, worldPosition, Quaternion.identity);
                Destroy(explosionEffect, explosionEffectDuration);
            }

            List<Vector2Int> affectedTiles = GetAffectedTiles(centerGridPosition, explosionTileRadius);

            foreach (Vector2Int tilePos in affectedTiles)
            {
                if (tilePos == centerGridPosition) continue;

                DamageEnemiesOnExplosionTile(tilePos);
            }
        }

        private void DamageEnemiesOnExplosionTile(Vector2Int gridPos)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            foreach (GameObject enemy in enemies)
            {
                Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemy.transform.position);

                if (enemyGridPos == gridPos)
                {
                    Enemy enemyComponent = enemy.GetComponent<Enemy>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.TakeDamage(explosionDamage);
                        Debug.Log($"IonBolt: Explosion dealt {explosionDamage} damage to enemy at tile {gridPos}");
                    }
                }
            }
        }

        private void CheckIfPastRightmostGrid()
        {
            if (currentGridPosition.x >= tileGrid.gridWidth || currentGridPosition.x > tileGrid.gridWidth - 1)
            {
                Destroy(activeProjectile);
                isProjectileFired = false;
            }
        }

        private bool IsEnemyTilePosition(Vector2Int gridPosition)
        {
            return tileGrid.IsValidGridPosition(gridPosition) &&
                   gridPosition.x >= tileGrid.gridWidth / 2;
        }

        private List<Vector2Int> GetAffectedTiles(Vector2Int centerPos, int radius)
        {
            List<Vector2Int> affectedTiles = new List<Vector2Int>();

            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                for (int yOffset = -radius; yOffset <= radius; yOffset++)
                {
                    Vector2Int tilePos = new Vector2Int(centerPos.x + xOffset, centerPos.y + yOffset);

                    if (tileGrid.IsValidGridPosition(tilePos))
                    {
                        affectedTiles.Add(tilePos);
                    }
                }
            }
            return affectedTiles;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier);
            Debug.Log($"IonBolt: Speed multiplier set to {speedMultiplier}");
        }

        public float GetEffectiveSpeed()
        {
            return projectileSpeed * speedMultiplier;
        }
    }
}
