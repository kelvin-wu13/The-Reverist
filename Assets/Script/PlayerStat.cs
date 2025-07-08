using System.Collections;
using System.Collections.Generic;
using SkillSystem;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static bool IsPlayerDead = false;

    [Header("Stats Configuration")]
    [SerializeField] private Stats stats;

    [Header("Mana Regeneration")]
    [SerializeField] private float manaRegenAmount = 1f;
    [SerializeField] private float manaRegenInterval = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string deathAnimationTrigger = "Death";
    [SerializeField] private float deathAnimationDuration = 2f;
    [SerializeField] private bool useDeathAnimation = true;

    [SerializeField] private string spawnAnimationTrigger = "Spawn";
    [SerializeField] private float spawnAnimationDuration = 1.5f;

    [Header("Current Values")]
    [SerializeField] private int currentHealth;
    [SerializeField] private float currentMana;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;

    private Color originalColor;

    private float manaRegenTimer;
    private bool isDead = false;

    public int CurrentHealth
    {
        get => currentHealth;
        private set => currentHealth = value;
    }
    public int MaxHealth => stats ? stats.MaxHealth : 0;
    public float CurrentMana
    {
        get => currentMana;
        private set => currentMana = value;
    }
    public int MaxMana => stats ? stats.MaxMana : 0;
    public bool IsDead => isDead;

    private void Start()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (stats == null)
        {
            Debug.LogError("Stats scriptable object not assigned to PlayerStats!");
            return;
        }

        if (currentHealth <= 0 || currentHealth > stats.MaxHealth ||
            currentMana < 0 || currentMana > stats.MaxMana)
        {
            ResetToMaxStats();
        }
    }

    public void ResetToMaxStats()
    {
        if (stats == null) return;

        currentHealth = stats.MaxHealth;
        currentMana = stats.MaxMana;
        isDead = false;

        PlayerShoot shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent != null) shootComponent.enabled = true;

        PlayerMovement moveComponent = GetComponent<PlayerMovement>();
        if (moveComponent != null) moveComponent.enabled = true;

        SkillCast skillComponent = GetComponent<SkillCast>();
        if (skillComponent != null) skillComponent.enabled = true;

        if (playerAnimator != null)
        {
            bool hasTrigger = false;
            foreach (AnimatorControllerParameter param in playerAnimator.parameters)
            {
                if (param.name == spawnAnimationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasTrigger = true;
                    break;
                }
            }

            if (hasTrigger)
            {
                playerAnimator.SetTrigger(spawnAnimationTrigger);
            }
            else
            {
                Debug.LogWarning($"Spawn animation trigger '{spawnAnimationTrigger}' not found in Animator!");
            }
        }

        StartCoroutine(EnablePlayerAfterSpawn(spawnAnimationDuration));

        AudioManager.Instance?.PlayPlayerSpawnSFX();
    }

    private void Update()
    {
        if (stats == null || isDead) return;

        if (currentMana < stats.MaxMana)
        {
            manaRegenTimer += Time.deltaTime;

            if (manaRegenTimer >= manaRegenInterval)
            {
                currentMana = Mathf.Min(stats.MaxMana, currentMana + manaRegenAmount);
                manaRegenTimer -= manaRegenInterval;
            }
        }
    }

    private IEnumerator EnablePlayerAfterSpawn(float delay)
    {
        GetComponent<PlayerMovement>()?.SetCanMove(false);
        yield return new WaitForSeconds(delay);

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(spawnAnimationTrigger);
        }

        var movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SetCanMove(true);
            movement.ForceIdle();
        }
    }

    public void TakeDamage(int damage)
    {
        if (stats == null || isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        StartCoroutine(FlashColor());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashColor()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitColor;

        yield return new WaitForSeconds(hitFlashDuration);

        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        IsPlayerDead = true;

        PlayerShoot shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent != null) shootComponent.enabled = false;

        PlayerMovement moveComponent = GetComponent<PlayerMovement>();
        if (moveComponent != null) moveComponent.enabled = false;

        SkillCast skillComponent = GetComponent<SkillCast>();
        if (skillComponent != null) skillComponent.enabled = false;

        Enemy enemy = GetComponent<Enemy>();
        if (enemy != null) enemy.enabled = false;

        AudioManager.Instance?.PlayPlayerDeathSFX();

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (useDeathAnimation && playerAnimator != null)
        {
            bool hasTrigger = false;
            foreach (AnimatorControllerParameter param in playerAnimator.parameters)
            {
                if (param.name == deathAnimationTrigger && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasTrigger = true;
                    break;
                }
            }

            if (hasTrigger)
            {
                playerAnimator.SetTrigger(deathAnimationTrigger);
                yield return new WaitForSeconds(deathAnimationDuration);
            }
            else
            {
                Debug.LogWarning($"Death animation trigger '{deathAnimationTrigger}' not found in Animator!");
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        HandlePostDeath();
    }

    protected virtual void HandlePostDeath()
    {
        FindObjectOfType<DeathSceneManager>().HandlePlayerDeath();
    }

    public void Respawn()
    {
        IsPlayerDead = false;

        ResetToMaxStats();

        if (playerAnimator != null)
        {
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }
    }

    public bool TryUseMana(float amount)
    {
        if (stats == null || isDead) return false;

        if (currentMana >= amount)
        {
            currentMana -= amount;
            return true;
        }

        return false;
    }

    public void RestoreMana(float amount)
    {
        if (stats == null || isDead) return;

        currentMana = Mathf.Min(stats.MaxMana, currentMana + amount);
    }

    public float GetHealthPercentage()
    {
        if (stats == null) return 0f;
        return (float)currentHealth / stats.MaxHealth;
    }

    public float GetManaPercentage()
    {
        if (stats == null) return 0f;
        return currentMana / stats.MaxMana;
    }
}
