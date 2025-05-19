using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats Configuration")]
    [SerializeField] private Stats stats;
    
    [Header("Mana Regeneration")]
    [SerializeField] private float manaRegenAmount = 1f;  // Amount of mana to regenerate
    [SerializeField] private float manaRegenInterval = 0.5f;  // Time between mana regeneration in seconds
    
    [Header("Current Values")]
    [SerializeField] private int currentHealth;
    [SerializeField] private float currentMana;  // Changed to float for fractional mana
    
    // Events
    public UnityEvent<int, int> OnHealthChanged; // Current, Max
    public UnityEvent<float, int> OnManaChanged; // Current, Max (changed first parameter to float)
    public UnityEvent OnPlayerDeath;
    
    // Current stats
    private float manaRegenTimer;
    
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
    
    private void Start()
    {
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
        
        // Trigger initial UI updates
        OnHealthChanged?.Invoke(currentHealth, stats.MaxHealth);
        OnManaChanged?.Invoke(currentMana, stats.MaxMana);
    }
    
    /// <summary>
    /// Resets health and mana to their maximum values based on the Stats scriptable object
    /// </summary>
    public void ResetToMaxStats()
    {
        if (stats == null) return;
        
        currentHealth = stats.MaxHealth;
        currentMana = stats.MaxMana;
        
        // Notify UI
        OnHealthChanged?.Invoke(currentHealth, stats.MaxHealth);
        OnManaChanged?.Invoke(currentMana, stats.MaxMana);
    }
    
    private void Update()
    {
        if (stats == null) return;
        
        // Only regenerate mana if not at max
        if (currentMana < stats.MaxMana)
        {
            // Increment timer
            manaRegenTimer += Time.deltaTime;
            
            // Check if it's time to regenerate mana
            if (manaRegenTimer >= manaRegenInterval)
            {
                // Store previous mana value to check if it changes
                float previousMana = currentMana;
                
                // Add the regenerated mana
                currentMana = Mathf.Min(stats.MaxMana, currentMana + manaRegenAmount);
                
                // Reset timer, but keep leftover time for next cycle
                manaRegenTimer -= manaRegenInterval;
                
                // Only invoke event if mana actually changed
                if (!Mathf.Approximately(previousMana, currentMana))
                {
                    OnManaChanged?.Invoke(currentMana, stats.MaxMana);
                }
            }
        }
    }
    
    /// <summary>
    /// Deals damage to the player and updates health UI
    /// </summary>
    /// <param name="damage">Amount of damage to apply</param>
    public void TakeDamage(int damage)
    {
        if (stats == null) return;
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Notify listeners about health change
        OnHealthChanged?.Invoke(currentHealth, stats.MaxHealth);
        
        // Check if player died
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Heals the player by the specified amount
    /// </summary>
    /// <param name="amount">Amount of health to restore</param>
    // public void Heal(int amount)
    // {
    //     if (stats == null) return;
        
    //     currentHealth = Mathf.Min(stats.MaxHealth, currentHealth + amount);
        
    //     // Notify listeners about health change
    //     OnHealthChanged?.Invoke(currentHealth, stats.MaxHealth);
    // }
    
    /// <summary>
    /// Called when player health reaches zero
    /// </summary>
    private void Die()
    {
        // Trigger death event
        OnPlayerDeath?.Invoke();
        
        // Optional: Disable player controls, play death animation, etc.
        PlayerShoot shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent != null)
        {
            shootComponent.enabled = false;
        }
        Destroy(gameObject);
        // Optional: Restart level, show game over screen, etc.
        // Example:
        // StartCoroutine(GameOverSequence());
    }
    
    /// <summary>
    /// Attempts to use mana and returns whether successful
    /// </summary>
    /// <param name="amount">Amount of mana to consume</param>
    /// <returns>True if mana was successfully used</returns>
    public bool TryUseMana(float amount)  // Changed to float
    {
        if (stats == null) return false;
        
        // Check if we have enough mana
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, stats.MaxMana);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Restores mana by the specified amount
    /// </summary>
    /// <param name="amount">Amount of mana to restore</param>
    public void RestoreMana(float amount)  // Changed to float
    {
        if (stats == null) return;
        
        currentMana = Mathf.Min(stats.MaxMana, currentMana + amount);
        OnManaChanged?.Invoke(currentMana, stats.MaxMana);
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