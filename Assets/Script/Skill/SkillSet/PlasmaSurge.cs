using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class PlasmaSurge : Skill
    {
        [Header("Plasma Surge Settings")]
        [SerializeField] private int damage = 25;
        [SerializeField] private float hitRadius = 0.4f;
        [SerializeField] private float animDuration = 1f;

        [Header("Cooldown")]
        [SerializeField] public float cooldownDuration = 2f;
        [SerializeField] public float manaCost = 1.5f;

        [Header("References")]
        private PlayerShoot playerShoot;
        private TileGrid tileGrid;
        private Transform player;
        private PlayerMovement playerMovement;
        [SerializeField] private Transform spawnOffsetReference;
        [SerializeField] private Vector3 offsetFromReference = Vector3.zero;


        public override void Initialize(Vector2Int targetPosition, SkillCombination combo, Transform playerTransform)
        {
            base.Initialize(targetPosition, combo, playerTransform);

            player = playerTransform;
            playerShoot = player.GetComponent<PlayerShoot>();
            playerMovement = player.GetComponent<PlayerMovement>();
            tileGrid = FindObjectOfType<TileGrid>();

            // Check if enough mana
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null && !stats.TryUseMana(manaCost))
            {
                Debug.Log("Not enough mana to cast PlasmaSurge.");
                Destroy(gameObject); // Cancel skill
                return;
            }

            if (spawnOffsetReference != null)
                transform.position = spawnOffsetReference.position + offsetFromReference;
            else
                transform.position = GetFirepointPosition();

            Animator anim = GetComponent<Animator>();
            if (anim != null)
                anim.Play("LaserAnimation", 0, 0f);

            StartCoroutine(ExecutePlasmaSurge());
        }




        private Vector3 GetFirepointPosition()
        {
            if (playerShoot != null)
            {
                Transform bulletSpawnPoint = playerShoot.GetBulletSpawnPoint();
                if (bulletSpawnPoint != null)
                    return bulletSpawnPoint.position;
            }

            Transform firepoint = player.Find("FirePoint") ??
                                 player.Find("BulletSpawnPoint") ??
                                 player.Find("Firepoint") ??
                                 player.Find("SpawnPoint");

            return firepoint != null ? firepoint.position : player.position + Vector3.right * 0.5f;
        }

        private IEnumerator ExecutePlasmaSurge()
        {
            Vector2Int playerGridPos = playerMovement != null
                ? playerMovement.GetCurrentGridPosition()
                : tileGrid.GetGridPosition(player.position);

            float tileW = tileGrid.GetTileWidth();
            float tileH = tileGrid.GetTileHeight();
            Vector3 laserStartPos = tileGrid.GetWorldPosition(playerGridPos) + new Vector3(tileW * 0.5f, tileH * 0.5f, 0f);

            Vector3 laserEndPos = CalculateLaserEndPosition(laserStartPos, playerGridPos);

            DamageEnemiesInLaserPath(laserStartPos, laserEndPos, playerGridPos);
            yield return new WaitForSeconds(animDuration);
            Destroy(gameObject);
        }



        private Vector3 CalculateLaserEndPosition(Vector3 startPos, Vector2Int playerGridPos)
        {
            int rightmostColumn = tileGrid.gridWidth - 1;
            Vector2Int endGridPos = new Vector2Int(rightmostColumn, playerGridPos.y);
            Vector3 endWorldPos = tileGrid.GetWorldPosition(endGridPos);
            return new Vector3(endWorldPos.x + 1f, startPos.y, startPos.z);
        }

        private void DamageEnemiesInLaserPath(Vector3 laserStart, Vector3 laserEnd, Vector2Int playerGridPos)
        {
            CheckEnemiesByGridTiles(playerGridPos);
        }

        private void CheckEnemiesByGridTiles(Vector2Int playerGridPos)
        {
            for (int x = playerGridPos.x + 1; x < tileGrid.gridWidth; x++)
            {
                Vector2Int checkPos = new Vector2Int(x, playerGridPos.y);
                if (tileGrid.IsValidGridPosition(checkPos))
                    DamageEnemiesOnTile(checkPos);
            }
        }


        private void DamageEnemiesOnTile(Vector2Int gridPos)
        {
            float tileW = tileGrid.GetTileWidth();
            float tileH = tileGrid.GetTileHeight();
            Vector3 worldPos = tileGrid.GetWorldPosition(gridPos) + new Vector3(tileW * 0.5f, tileH * 0.5f, 0f);

            Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, hitRadius);

            foreach (Collider2D collider in colliders)
                if (collider.CompareTag("Enemy"))
                    DamageEnemyDirect(collider);

            float tileSize = tileGrid.GetTileHeight();
        }

        private void DamageEnemyDirect(Collider2D enemyCollider)
        {
            var enemy = enemyCollider.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(damage);
        }
    }
}
