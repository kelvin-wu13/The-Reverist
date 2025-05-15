using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Player Configuration")]
    [SerializeField] private Stats stats;
    
    // Component references
    private PlayerStats playerStatsComponent;
    private PlayerShoot shootComponent;
    
    private void Awake()
    {
        // Validate stats
        if (stats == null)
        {
            Debug.LogError("Stats scriptable object not assigned to Player!");
        }
        
        // Get or add required components
        playerStatsComponent = GetComponent<PlayerStats>();
        if (playerStatsComponent == null)
        {
            playerStatsComponent = gameObject.AddComponent<PlayerStats>();
        }
        
        shootComponent = GetComponent<PlayerShoot>();
        if (shootComponent == null)
        {
            shootComponent = gameObject.AddComponent<PlayerShoot>();
        }
        
        // Set the scriptable object reference on all components
        // This ensures they all use the same stats object
        SetPlayerStatsOnAllComponents();
    }
    
    private void SetPlayerStatsOnAllComponents()
    {
        // Use reflection to set the playerStats field on all components
        // This is a clean way to ensure all components use the same stats
        
        // Get all components that might need the playerStats reference
        Component[] components = GetComponents<Component>();
        
        foreach (Component component in components)
        {
            // Find the stats field using reflection
            System.Reflection.FieldInfo field = component.GetType().GetField("stats", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic);
            
            // If the component has a stats field, set it
            if (field != null && field.FieldType == typeof(Stats))
            {
                field.SetValue(component, stats);
            }
        }
    }
    
    // Optional: Create an easy method to access components
    public PlayerStats GetPlayerStats() => playerStatsComponent;
    public PlayerShoot GetShootComponent() => shootComponent;
}