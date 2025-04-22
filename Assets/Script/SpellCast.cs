using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpellType
{
    None,
    Q,
    W,
    E
}

public enum SpellCombination
{
    None,
    QQ, // Q pressed twice
    QE, // Q then E
    QW, // E then W
    WW, // W pressed twice
    WQ, // E then W
    WE, // E then W
    EE, // E pressed twice
    EQ,  // E then Q
    EW // E then W
}

// Base State class for the State Pattern
public abstract class SpellCastState
{
    protected SpellCast spellCast;

    public SpellCastState(SpellCast spellCast)
    {
        this.spellCast = spellCast;
    }

    public abstract void EnterState();
    public abstract void Update();
    public abstract void ProcessInput(SpellType spellType);
    public abstract void ExitState();
}

// Idle state - waiting for first spell input
public class IdleState : SpellCastState
{
    public IdleState(SpellCast spellCast) : base(spellCast) {}

    public override void EnterState()
    {
        if (spellCast.showDebugLogs) Debug.Log("Entered Idle State");
    }

    public override void Update()
    {
        // Nothing to update in idle state
    }

    public override void ProcessInput(SpellType spellType)
    {
        // First spell input received, transition to WaitingForSecond state
        spellCast.firstSpell = spellType;
        spellCast.ChangeState(spellCast.waitingState);
        
        if (spellCast.showDebugLogs) Debug.Log($"First spell selected: {spellType}");
    }

    public override void ExitState()
    {
        // Nothing special to do when exiting idle state
    }
}

// Waiting state - first spell input received, waiting for second input
public class WaitingForSecondState : SpellCastState
{
    public WaitingForSecondState(SpellCast spellCast) : base(spellCast) {}

    public override void EnterState()
    {
        if (spellCast.showDebugLogs) Debug.Log("Entered Waiting State");
    }

    public override void Update()
    {
        // No time limit anymore, will wait indefinitely
    }

    public override void ProcessInput(SpellType spellType)
    {
        // Second spell input received, cast the spell
        SpellCombination combination = spellCast.GetSpellCombination(spellCast.firstSpell, spellType);
        spellCast.CastSpell(combination);
        
        // Return to idle state
        spellCast.ChangeState(spellCast.idleState);
    }

    public override void ExitState()
    {
        // Reset first spell when exiting waiting state
        spellCast.firstSpell = SpellType.None;
    }
}

public class SpellCast : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCrosshair crosshair;
    [SerializeField] private TileGrid tileGrid;
    
    [Header("Spell Settings")]
    [SerializeField] private GameObject spellQQPrefab;
    [SerializeField] private GameObject spellQEPrefab;
    [SerializeField] private GameObject spellQWPrefab;
    [SerializeField] private GameObject spellEEPrefab;
    [SerializeField] private GameObject spellEQPrefab;
    [SerializeField] private GameObject spellEWPrefab;
    [SerializeField] private GameObject spellWWPrefab;
    [SerializeField] private GameObject spellWQPrefab;
    [SerializeField] private GameObject spellWEPrefab;
    
    [Header("UI Feedback")]
    public bool showDebugLogs = true;
    
    // State machine variables
    [HideInInspector] public SpellType firstSpell = SpellType.None;
    
    // State instances
    [HideInInspector] public IdleState idleState;
    [HideInInspector] public WaitingForSecondState waitingState;
    private SpellCastState currentState;
    
    // For UI display
    public SpellType CurrentFirstSpell => firstSpell;
    public bool IsWaitingForSecondSpell => currentState == waitingState;
    
    private void Awake()
    {
        // Initialize states
        idleState = new IdleState(this);
        waitingState = new WaitingForSecondState(this);
    }
    
    private void Start()
    {
        if (crosshair == null)
        {
            crosshair = FindObjectOfType<PlayerCrosshair>();
            if (crosshair == null)
            {
                Debug.LogError("SpellCast: Could not find PlayerCrosshair in the scene!");
            }
        }
        
        if (tileGrid == null)
        {
            tileGrid = FindObjectOfType<TileGrid>();
            if (tileGrid == null)
            {
                Debug.LogError("SpellCast: Could not find TileGrid in the scene!");
            }
        }
        
        // Start in idle state
        ChangeState(idleState);
    }
    
    private void Update()
    {
        // Let the current state handle updates
        currentState.Update();
        
        // Check for spell inputs
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentState.ProcessInput(SpellType.Q);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            currentState.ProcessInput(SpellType.E);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            currentState.ProcessInput(SpellType.W);
        }
    }
    
    public void ChangeState(SpellCastState newState)
    {
        if (currentState != null)
        {
            currentState.ExitState();
        }
        
        currentState = newState;
        currentState.EnterState();
    }
    
    public SpellCombination GetSpellCombination(SpellType first, SpellType second)
    {
        //Q SPELL COMBINATION
        if (first == SpellType.Q && second == SpellType.Q) return SpellCombination.QQ;
        if (first == SpellType.Q && second == SpellType.E) return SpellCombination.QE;
        if (first == SpellType.Q && second == SpellType.W) return SpellCombination.QW;

        //E SPEL COMBINATION
        if (first == SpellType.E && second == SpellType.E) return SpellCombination.EE;
        if (first == SpellType.E && second == SpellType.Q) return SpellCombination.EQ;
        if (first == SpellType.E && second == SpellType.W) return SpellCombination.EW;
        
        //W SPELL COMBINATION
        if (first == SpellType.W && second == SpellType.W) return SpellCombination.WW;
        if (first == SpellType.W && second == SpellType.Q) return SpellCombination.WQ;
        if (first == SpellType.W && second == SpellType.E) return SpellCombination.WE;

        return SpellCombination.None;
    }
    
    public void CastSpell(SpellCombination combination)
    {
        if (crosshair == null) return;
        
        Vector3 targetPosition = crosshair.GetTargetWorldPosition();
        Vector2Int targetGridPos = crosshair.GetTargetGridPosition();
        
        GameObject spellPrefab = null;
        string spellName = "Unknown";
        
        switch (combination)
        {
            case SpellCombination.QQ:
                spellPrefab = spellQQPrefab;
                spellName = "Q+Q Spell";
                break;
            case SpellCombination.QE:
                spellPrefab = spellEEPrefab;
                spellName = "Q+E Spell";
                break;
            case SpellCombination.QW:
                spellPrefab = spellQEPrefab;
                spellName = "Q+W Spell";
                break;
            case SpellCombination.EE:
                spellPrefab = spellEQPrefab;
                spellName = "E+E Spell";
                break;
            case SpellCombination.EQ:
                spellPrefab = spellQQPrefab;
                spellName = "E+Q Spell";
                break;
            case SpellCombination.EW:
                spellPrefab = spellEEPrefab;
                spellName = "E+W Spell";
                break;
            case SpellCombination.WW:
                spellPrefab = spellQEPrefab;
                spellName = "W+W Spell";
                break;
            case SpellCombination.WQ:
                spellPrefab = spellEQPrefab;
                spellName = "W+Q Spell";
                break;
            case SpellCombination.WE:
                spellPrefab = spellEQPrefab;
                spellName = "W+E Spell";
                break;
        }
        
        if (spellPrefab != null)
        {
            GameObject spellInstance = Instantiate(spellPrefab, targetPosition, Quaternion.identity);
            
            // If your spell prefabs have a Spell component, you can pass additional data
            Spell spellComponent = spellInstance.GetComponent<Spell>();
            if (spellComponent != null)
            {
                spellComponent.Initialize(targetGridPos, combination);
            }
            
            if (showDebugLogs) Debug.Log($"Cast {spellName} at position {targetGridPos}");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning($"No prefab assigned for spell combination: {combination}");
        }
    }
    
    // Optional: Method to cancel current spell input
    public void CancelSpellInput()
    {
        if (currentState == waitingState)
        {
            if (showDebugLogs) Debug.Log("Spell input canceled");
            ChangeState(idleState);
        }
    }
}

// Optional helper class to be attached to spell prefabs
public class Spell : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.0f;
    [SerializeField] private ParticleSystem spellEffect;
    
    private Vector2Int targetGridPosition;
    private SpellCombination spellType;
    
    public void Initialize(Vector2Int gridPos, SpellCombination type)
    {
        targetGridPosition = gridPos;
        spellType = type;
        
        // Start lifetime countdown
        Destroy(gameObject, lifetime);
        
        // Apply spell effects based on type
        ApplySpellEffects();
    }
    
    private void ApplySpellEffects()
    {
        // Different effects based on spell type
        switch (spellType)
        {
            case SpellCombination.QQ:
                // Example: Single target damage
                break;
            case SpellCombination.QE:
                // Example: Area damage
                break;
            case SpellCombination.QW:
                // Example: Status effect
                break;
            case SpellCombination.EE:
                // Example: Movement or utility
                break;
            case SpellCombination.EQ:
                // Example: Single target damage
                break;
            case SpellCombination.EW:
                // Example: Area damage
                break;
            case SpellCombination.WW:
                // Example: Status effect
                break;
            case SpellCombination.WQ:
                // Example: Movement or utility
                break;
            case SpellCombination.WE:
                // Example: Movement or utility
                break;
        }
        
        // Play particle effect if assigned
        if (spellEffect != null)
        {
            spellEffect.Play();
        }
    }
}