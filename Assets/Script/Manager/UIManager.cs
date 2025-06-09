using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Health UI Elements")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthFillImage;
    
    [Header("Mana UI Elements")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private Image manaFillImage;
    
    [Header("UI Colors")]
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color manaColor = Color.blue;
    [SerializeField] private Color lowHealthColor = Color.yellow;
    [SerializeField] private Color criticalHealthColor = new Color(1f, 0.3f, 0f); // Orange-red
    
    [Header("Health Thresholds")]
    [SerializeField] private float lowHealthThreshold = 0.5f;
    [SerializeField] private float criticalHealthThreshold = 0.25f;
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableSmoothTransitions = true;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private bool enableHealthFlashing = true;
    [SerializeField] private float flashSpeed = 3f;
    
    [Header("Update Settings")]
    [SerializeField] private float updateFrequency = 0.1f; // How often to check for changes (in seconds)
    
    // Private variables
    private PlayerStats playerStats;
    private float targetHealthValue;
    private float targetManaValue;
    private float currentHealthDisplay;
    private float currentManaDisplay;
    private bool isFlashingHealth;
    private float flashTimer;
    private float updateTimer;
    
    // Cached values to detect changes
    private int lastHealth = -1;
    private float lastMana = -1f;
    private int lastMaxHealth = -1;
    private int lastMaxMana = -1;
    private bool wasPlayerDead = false;
    
    // Cached original colors
    private Color originalHealthColor;
    private Color originalManaColor;
    
    private void Start()
    {
        InitializeUI();
        FindAndConnectToPlayerStats();
    }
    
    private void Update()
    {
        // Update timer for checking player stats changes
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateFrequency)
        {
            CheckForPlayerStatsChanges();
            updateTimer = 0f;
        }
        
        if (enableSmoothTransitions)
        {
            HandleSmoothTransitions();
        }
        
        if (enableHealthFlashing && isFlashingHealth)
        {
            HandleHealthFlashing();
        }
    }
    
    private void InitializeUI()
    {
        // Cache original colors
        originalHealthColor = healthColor;
        originalManaColor = manaColor;
        
        // Set initial colors
        SetHealthBarColor(healthColor);
        SetManaBarColor(manaColor);
        
        // Initialize slider values
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
        
        if (manaSlider != null)
        {
            manaSlider.minValue = 0f;
            manaSlider.maxValue = 1f;
            manaSlider.value = 1f;
        }
        
        // Initialize display values
        currentHealthDisplay = 1f;
        currentManaDisplay = 1f;
        targetHealthValue = 1f;
        targetManaValue = 1f;
    }
    
    private void FindAndConnectToPlayerStats()
    {
        // Try to find PlayerStats component in the scene
        playerStats = FindObjectOfType<PlayerStats>();
        
        if (playerStats == null)
        {
            Debug.LogError("UIManager: PlayerStats component not found in the scene!");
            return;
        }
        
        Debug.Log("UIManager: Successfully connected to PlayerStats");
        
        // Initialize UI with current values
        InitializeWithPlayerStats();
    }
    
    private void InitializeWithPlayerStats()
    {
        if (playerStats == null) return;
        
        // Set initial values
        lastHealth = playerStats.CurrentHealth;
        lastMana = playerStats.CurrentMana;
        lastMaxHealth = playerStats.MaxHealth;
        lastMaxMana = playerStats.MaxMana;
        wasPlayerDead = playerStats.IsDead;
        
        // Update UI immediately
        OnHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
        OnManaChanged(playerStats.CurrentMana, playerStats.MaxMana);
    }
    
    private void CheckForPlayerStatsChanges()
    {
        if (playerStats == null) return;
        
        // Check for health changes
        if (playerStats.CurrentHealth != lastHealth || playerStats.MaxHealth != lastMaxHealth)
        {
            OnHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
            lastHealth = playerStats.CurrentHealth;
            lastMaxHealth = playerStats.MaxHealth;
        }
        
        // Check for mana changes
        if (!Mathf.Approximately(playerStats.CurrentMana, lastMana) || playerStats.MaxMana != lastMaxMana)
        {
            OnManaChanged(playerStats.CurrentMana, playerStats.MaxMana);
            lastMana = playerStats.CurrentMana;
            lastMaxMana = playerStats.MaxMana;
        }
        
        // Check for death state changes
        if (playerStats.IsDead != wasPlayerDead)
        {
            if (playerStats.IsDead)
            {
                OnPlayerDeath();
            }
            else
            {
                OnPlayerRespawn();
            }
            wasPlayerDead = playerStats.IsDead;
        }
    }
    
    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (maxHealth == 0) return;
        
        float healthPercentage = (float)currentHealth / maxHealth;
        
        if (enableSmoothTransitions)
        {
            targetHealthValue = healthPercentage;
        }
        else
        {
            UpdateHealthSlider(healthPercentage);
        }
        
        UpdateHealthText(currentHealth, maxHealth);
        UpdateHealthColor(healthPercentage);
    }
    
    private void OnManaChanged(float currentMana, int maxMana)
    {
        if (maxMana == 0) return;
        
        float manaPercentage = currentMana / maxMana;
        
        if (enableSmoothTransitions)
        {
            targetManaValue = manaPercentage;
        }
        else
        {
            UpdateManaSlider(manaPercentage);
        }
        
        UpdateManaText(currentMana, maxMana);
    }
    
    private void OnPlayerDeath()
    {
        // Stop any ongoing animations
        isFlashingHealth = false;
        
        // You can add death UI effects here
        Debug.Log("UIManager: Player has died!");
        
        // Example: Flash health bar red rapidly
        StartCoroutine(DeathFlashEffect());
    }
    
    private void OnPlayerRespawn()
    {
        // Stop death effects
        StopAllCoroutines();
        isFlashingHealth = false;
        
        // Reset UI colors
        SetHealthBarColor(originalHealthColor);
        
        Debug.Log("UIManager: Player has respawned!");
    }
    
    private void UpdateHealthSlider(float value)
    {
        currentHealthDisplay = value;
        if (healthSlider != null)
        {
            healthSlider.value = value;
        }
    }
    
    private void UpdateManaSlider(float value)
    {
        currentManaDisplay = value;
        if (manaSlider != null)
        {
            manaSlider.value = value;
        }
    }
    
    private void UpdateHealthText(int current, int max)
    {
        if (healthText != null)
        {
            healthText.text = $"{current}/{max}";
        }
    }
    
    private void UpdateManaText(float current, int max)
    {
        if (manaText != null)
        {
            // Show one decimal place for mana since it can be fractional
            manaText.text = $"{current:F1}/{max}";
        }
    }
    
    private void UpdateHealthColor(float healthPercentage)
    {
        Color targetColor;
        
        if (healthPercentage <= criticalHealthThreshold)
        {
            targetColor = criticalHealthColor;
            if (enableHealthFlashing)
            {
                isFlashingHealth = true;
            }
        }
        else if (healthPercentage <= lowHealthThreshold)
        {
            targetColor = lowHealthColor;
            isFlashingHealth = false;
        }
        else
        {
            targetColor = originalHealthColor;
            isFlashingHealth = false;
        }
        
        if (!isFlashingHealth)
        {
            SetHealthBarColor(targetColor);
        }
    }
    
    private void SetHealthBarColor(Color color)
    {
        if (healthFillImage != null)
        {
            healthFillImage.color = color;
        }
    }
    
    private void SetManaBarColor(Color color)
    {
        if (manaFillImage != null)
        {
            manaFillImage.color = color;
        }
    }
    
    private void HandleSmoothTransitions()
    {
        // Smooth health transition
        if (!Mathf.Approximately(currentHealthDisplay, targetHealthValue))
        {
            float newValue = Mathf.Lerp(currentHealthDisplay, targetHealthValue, transitionSpeed * Time.deltaTime);
            UpdateHealthSlider(newValue);
        }
        
        // Smooth mana transition
        if (!Mathf.Approximately(currentManaDisplay, targetManaValue))
        {
            float newValue = Mathf.Lerp(currentManaDisplay, targetManaValue, transitionSpeed * Time.deltaTime);
            UpdateManaSlider(newValue);
        }
    }
    
    private void HandleHealthFlashing()
    {
        flashTimer += Time.deltaTime * flashSpeed;
        float flashIntensity = (Mathf.Sin(flashTimer) + 1f) * 0.5f; // Normalize to 0-1
        
        Color flashColor = Color.Lerp(criticalHealthColor, Color.white, flashIntensity * 0.3f);
        SetHealthBarColor(flashColor);
    }
    
    private IEnumerator DeathFlashEffect()
    {
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float intensity = Mathf.Sin(elapsed * 20f); // Fast flashing
            Color flashColor = intensity > 0 ? Color.red : Color.black;
            SetHealthBarColor(flashColor);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    /// <summary>
    /// Manually set UI references if not assigned in inspector
    /// </summary>
    public void SetUIReferences(Slider healthSlider, Slider manaSlider, 
                               TextMeshProUGUI healthText = null, TextMeshProUGUI manaText = null,
                               Image healthFill = null, Image manaFill = null)
    {
        this.healthSlider = healthSlider;
        this.manaSlider = manaSlider;
        this.healthText = healthText;
        this.manaText = manaText;
        this.healthFillImage = healthFill;
        this.manaFillImage = manaFill;
        
        InitializeUI();
    }
    
    /// <summary>
    /// Force refresh the UI with current player stats
    /// </summary>
    public void RefreshUI()
    {
        if (playerStats != null)
        {
            OnHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
            OnManaChanged(playerStats.CurrentMana, playerStats.MaxMana);
        }
    }
    
    /// <summary>
    /// Enable or disable smooth transitions
    /// </summary>
    public void SetSmoothTransitions(bool enabled)
    {
        enableSmoothTransitions = enabled;
    }
    
    /// <summary>
    /// Enable or disable health flashing when critical
    /// </summary>
    public void SetHealthFlashing(bool enabled)
    {
        enableHealthFlashing = enabled;
        if (!enabled)
        {
            isFlashingHealth = false;
        }
    }
    
    /// <summary>
    /// Set how frequently the UI checks for player stat changes
    /// </summary>
    public void SetUpdateFrequency(float frequency)
    {
        updateFrequency = Mathf.Max(0.01f, frequency); // Minimum 0.01 seconds
    }
    
    /// <summary>
    /// Manually assign a PlayerStats reference
    /// </summary>
    public void SetPlayerStats(PlayerStats stats)
    {
        playerStats = stats;
        InitializeWithPlayerStats();
    }
}