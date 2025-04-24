using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDying = false;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;

    private TileGrid tileGrid;
    private Vector2Int currentgridPosition;
    private Vector3 targetPosition;
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            originalColor = spriteRenderer.color;

        tileGrid = FindObjectOfType<TileGrid>();

        if(tileGrid == null)
        {
            Debug.Log("EnemyDummy: Could not find TileGrid");
        }
    }

    private void Start()
    {
        // Initialize Health
        currentHealth = maxHealth;

        currentgridPosition = tileGrid.GetGridPosition(transform.position);
        targetPosition = transform.position;
    }

    public void TakeDamage(int damage)
    {
        if (isDying) return;

        currentHealth -= damage;

        StartCoroutine(FlashColor());

        if (currentHealth <= 0)
        {
            Die();
        }

    }

    private IEnumerator FlashColor()
    {
        // Don't proceed if no sprite renderer available
        if (spriteRenderer == null) yield break;
        
        // Change to hit color
        spriteRenderer.color = hitColor;
        
        // Wait for flash duration
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Change back to original color
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDying = true;

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        Destroy(gameObject);
    }
}
