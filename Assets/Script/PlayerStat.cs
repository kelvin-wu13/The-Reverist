using System.Collections;
using System.Collections.Generic;
using SkillSystem;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats Configuration")]
    [SerializeField] private Stats stats;
    
    [Header("Mana Regeneration")]
    [SerializeField] private float manaRegenAmount = 1f;  // Amount of mana to regenerate
    [SerializeField] private float manaRegenInterval = 0.5f;  // Time between mana regeneration in seconds
    
    [Header("Death Animation")]
    [SerializeField] private Animator playerAnimator;  // Reference to player's Animator
    [SerializeField] private string deathAnimationTrigger = "Death";  // Name of death animation trigger
    [SerializeField] private float deathAnimationDuration = 2f;  // How long to wait before destroying/respawning
    [SerializeField] private bool useDeathAnimation = true;  // Toggle death animation on/off
    
    [Header("Current Values")]
    [SerializeField] private int currentHealth;
    [SerializeField] private float currentMana;  // Changed to float for fractional mana
    
    // Current stats
    private float manaRegenTimer;
    private bool isDead = false;  // Track if player is dead to prevent multiple death calls
    
    // Properties for easy access
    public int CurrentHealth { 
        get => currentHealth; 
        private set => currentHealth = value; 
    }
    public int MaxHealth => stats ? stats.MaxHealth : 0;
    public float CurrentMana {  // Changed to float
        get => currentMana; 
        private set => currentMana = value; 
    }
    public int MaxMana => stats ? stats.MaxMana : 0;
    public bool IsDead => isDead;  // Public property to check if player is dead
    
    private void Start()
    {
        // Auto-assign animator if not set
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }
        
        // Validate that we have stats
        if (stats == null)
        {
            Debug.LogError("Stats scriptable object not assigned to PlayerStats!");
            return;
        }
        
        // Initialize stats if they're zero or invalid
        if (currentHealth <= 0 || currentHealth > stats.MaxHealth || 
            currentMana < 0 || currentMana > stats.MaxMana)
        {
            ResetToMaxStats();
        }
    }

    /// Resets health and mana to their maximum values based on the Stats scriptable object
    public void ResetToMaxStats()
    {
        if (stats == null) return;

        currentHealth = stats.MaxHealth;
        currentMana = stats.MaxMana;
        isDead = false;  // Reset death state

        // Re-enable components that might have been disabled
        PlayerShoot shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent != null)
        {
            shootComponent.enabled = true;
        }
        
        PlayerMovement moveComponent = GetComponent<PlayerMovement>();
        if (moveComponent != null)
        {
            moveComponent.enabled = true;
        }

        SkillCast skillComponent = GetComponent<SkillCast>();
        if (skillComponent != null)
        {
            skillComponent.enabled = true;
        }
    }
    
    private void Update()
    {
        if (stats == null || isDead) return;  // Don't regenerate mana if dead
        
        // Only regenerate mana if not at max
        if (currentMana < stats.MaxMana)
        {
            // Increment timer
            manaRegenTimer += Time.deltaTime;
            
            // Check if it's time to regenerate mana
            if (manaRegenTimer >= manaRegenInterval)
            {
                // Add the regenerated mana
                currentMana = Mathf.Min(stats.MaxMana, currentMana + manaRegenAmount);
                
                // Reset timer, but keep leftover time for next cycle
                manaRegenTimer -= manaRegenInterval;
            }
        }
    }
    
    // Deals damage to the player and updates health UI
    public void TakeDamage(int damage)
    {
        if (stats == null || isDead) return;  // Don't take damage if already dead
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Check if player died
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // Called when player health reaches zero
    private void Die()
    {
        if (isDead) return;  // Prevent multiple death calls
        
        isDead = true;
        
        // Disable player controls immediately
        PlayerShoot shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent != null)
        {
            shootComponent.enabled = false;
        }

        PlayerMovement moveComponent = GetComponent<PlayerMovement>();
        if (moveComponent != null)
        {
            moveComponent.enabled = false;
        }

        SkillCast skillComponent = GetComponent<SkillCast>();
        if (skillComponent != null)
        {
            skillComponent.enabled = false;
        }
        
        // Start death sequence
        StartCoroutine(DeathSequence());
    }
    
    // Handles the death animation sequence
    private IEnumerator DeathSequence()
    {
        // Play death animation if enabled and animator is available
        if (useDeathAnimation && playerAnimator != null)
        {
            // Check if the death trigger parameter exists
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
                
                // Wait for the death animation to complete
                yield return new WaitForSeconds(deathAnimationDuration);
            }
            else
            {
                Debug.LogWarning($"Death animation trigger '{deathAnimationTrigger}' not found in Animator!");
                // Still wait a bit for visual feedback
                yield return new WaitForSeconds(1f);
            }
        }
        else
        {
            // If no animation, still wait a moment for visual feedback
            yield return new WaitForSeconds(0.5f);
        }
        
        // Handle post-death logic (you can customize this)
        HandlePostDeath();
    }
    
    protected virtual void HandlePostDeath()
    {
        // Default behavior: destroy the game object
        // You can change this to respawn, show game over screen, etc.
        Destroy(gameObject);
        
        // Alternative examples:
        // Respawn();
        // GameManager.Instance.ShowGameOverScreen();
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// Respawns the player (resets stats and position)
    /// </summary>
    public void Respawn()
    {
        // Reset to spawn position (you'll need to implement this based on your game)
        // transform.position = spawnPoint.position;
        
        // Reset stats
        ResetToMaxStats();
        
        // Reset animator state if needed
        if (playerAnimator != null)
        {
            playerAnimator.Rebind();
            playerAnimator.Update(0f);
        }
    }

    // Attempts to use mana and returns whether successful
    public bool TryUseMana(float amount)  // Changed to float
    {
        if (stats == null || isDead) return false;  // Can't use mana if dead
        
        // Check if we have enough mana
        if (currentMana >= amount)
        {
            currentMana -= amount;
            return true;
        }
        
        return false;
    }
    
    // Restores mana by the specified amount
    public void RestoreMana(float amount)  // Changed to float
    {
        if (stats == null || isDead) return;  // Can't restore mana if dead
        
        currentMana = Mathf.Min(stats.MaxMana, currentMana + amount);
    }
    
    // Get current health percentage (0-1)
    public float GetHealthPercentage()
    {
        if (stats == null) return 0f;
        return (float)currentHealth / stats.MaxHealth;
    }
    
    // Get current mana percentage (0-1)
    public float GetManaPercentage()
    {
        if (stats == null) return 0f;
        return currentMana / stats.MaxMana;
    }
}