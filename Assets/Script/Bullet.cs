using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private TileGrid tileGrid;

    [SerializeField] private float fadeOutTime = 0.1f;
    [SerializeField] private GameObject hitEffectPrefab;

    private Vector2Int currentGridPosition;
    private bool isDestroying = false;

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
            CheckForEnemyHit(currentGridPosition);
            CheckIfPastRightmostGrid();
        }
    }

    private void CheckForEnemyHit(Vector2Int gridPosition)
    {
        if (!IsEnemyTilePosition(gridPosition)) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Vector2Int enemyGridPos = tileGrid.GetGridPosition(enemy.transform.position);

            if (enemyGridPos == gridPosition)
            {
                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                if (enemyComponent != null)
                {
                    enemyComponent.TakeDamage(damage);
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

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

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
