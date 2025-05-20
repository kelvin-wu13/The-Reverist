using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

namespace SkillSystem
{
    public class SkillCast : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCrosshair crosshair;
        [SerializeField] private TileGrid tileGrid;
        
        [Header("Skill Settings")]
        [Header("QQ")]
        [SerializeField] public GameObject IonBoltPrefab;
        [Header("QE")]
        [SerializeField] private GameObject PulseFallPrefab;
        [Header("QW")]
        [SerializeField] private GameObject PlasmaSurgePrefab;
        [Header("EE")]
        [SerializeField] private GameObject GridLockPrefab;
        [Header("EQ")]
        [SerializeField] private GameObject WilloWispPrefab;
        [Header("EW")]
        [SerializeField] private GameObject MagneticPullPrefab;
        [Header("WW")]
        [SerializeField] private GameObject SwiftStrikePrefab;
        [Header("WQ")]
        [SerializeField] private GameObject QuickSlashPrefab;
        [Header("WE")]
        [SerializeField] private GameObject KineticShovePrefab;
        
        [Header("UI Feedback")]
        public bool showDebugLogs = true;
        
        // State machine variables
        [HideInInspector] public SkillType firstSkill = SkillType.None;
        
        // State instances
        [HideInInspector] public IdleState idleState;
        [HideInInspector] public WaitingForSecondState waitingState;
        private SkillCastState currentState;
        
        // For UI display
        public SkillType CurrentFirstSkill => firstSkill;
        public bool IsWaitingForSecondSkill => currentState == waitingState;
        
        // Cooldown tracking dictionary - Maps skill combinations to their cooldown end times
        private Dictionary<SkillCombination, float> skillCooldowns = new Dictionary<SkillCombination, float>();
        
        // Dictionary to store cooldown durations for each skill
        private Dictionary<SkillCombination, float> cooldownDurations = new Dictionary<SkillCombination, float>();
        
        private void Awake()
        {
            // Initialize states
            idleState = new IdleState(this);
            waitingState = new WaitingForSecondState(this);
            
            // Initialize cooldown durations for all skills
            InitializeCooldownDurations();
        }
        
        private void InitializeCooldownDurations()
        {
            // Default cooldown of 0.5 seconds for most skills
            float defaultCooldown = 0.5f;
            
            // Set default cooldowns for all skill combinations
            foreach (SkillCombination combo in System.Enum.GetValues(typeof(SkillCombination)))
            {
                if (combo != SkillCombination.None)
                {
                    cooldownDurations[combo] = defaultCooldown;
                }
            }

            // Get cooldown durations from skill prefabs when available
            //Get PlasmaSurge cooldown
            if (PlasmaSurgePrefab != null)
            {
                PlasmaSurge plasmaSurgeSkill = PlasmaSurgePrefab.GetComponent<PlasmaSurge>();
                if (plasmaSurgeSkill != null)
                {
                    cooldownDurations[SkillCombination.QW] = plasmaSurgeSkill.cooldownDuration;
                    if (showDebugLogs) Debug.Log($"Set PlasmaSurge cooldown to {plasmaSurgeSkill.cooldownDuration} seconds from prefab");
                }
            }
            
            // Get IonBolt cooldown from prefab
            if (IonBoltPrefab != null)
            {
                IonBolt ionBoltSkill = IonBoltPrefab.GetComponent<IonBolt>();
                if (ionBoltSkill != null)
                {
                    cooldownDurations[SkillCombination.QQ] = ionBoltSkill.cooldownDuration;
                    if (showDebugLogs) Debug.Log($"Set IonBolt cooldown to {ionBoltSkill.cooldownDuration} seconds from prefab");
                }
            }
            
            //Get GridLock cooldown
            if (GridLockPrefab != null)
            {
                GridLock gridLockSkill = GridLockPrefab.GetComponent<GridLock>();
                if (gridLockSkill != null)
                {
                    cooldownDurations[SkillCombination.EE] = gridLockSkill.cooldownDuration;
                    if (showDebugLogs) Debug.Log($"Set GridLock cooldown to {gridLockSkill.cooldownDuration} seconds from prefab");
                }
            }
            
            // // Get KineticShove cooldown from prefab
            // if (KineticShovePrefab != null)
            // {
            //     KineticShove kineticShoveSkill = KineticShovePrefab.GetComponent<KineticShove>();
            //     if (kineticShoveSkill != null)
            //     {
            //         cooldownDurations[SkillCombination.WE] = kineticShoveSkill.cooldownDuration;
            //         if (showDebugLogs) Debug.Log($"Set KineticShove cooldown to {kineticShoveSkill.cooldownDuration} seconds from prefab");
            //     }
            // }

            // // Get PulseFall cooldown from prefab
            // if (PulseFallPrefab != null)
            // {
            //     PulseFall pulseFallSkill = PulseFallPrefab.GetComponent<PulseFall>();
            //     if (ionBoltSkill != null)
            //     {
            //         cooldownDurations[SkillCombination.QE] = pulseFallSkill.cooldownDuration;
            //         if (showDebugLogs) Debug.Log($"Set PulseFall cooldown to {pulseFallSkill.cooldownDuration} seconds from prefab");
            //     }
            // }

            // // Get QuickSlash cooldown from prefab
            // if (QuickSlashPrefab != null)
            // {
            //     QuickSlash quickSlashSkill = QuickSlashPrefab.GetComponent<QuickSlash>();
            //     if (quickSlashSkill != null)
            //     {
            //         cooldownDurations[SkillCombination.WQ] = quickSlashSkill.cooldownDuration;
            //         if (showDebugLogs) Debug.Log($"Set QuickSlash cooldown to {quickSlashSkill.cooldownDuration} seconds from prefab");
            //     }
            // }

            // // Get SwiftStrike cooldown from prefab
            // if (SwiftStrikePrefab != null)
            // {
            //     SwiftStrike swiftStrikeSkill = SwiftStrikePrefab.GetComponent<SwiftStrike>();
            //     if (swiftStrikeSkill != null)
            //     {
            //         cooldownDurations[SkillCombination.WW] = swiftStrikeSkill.cooldownDuration;
            //         if (showDebugLogs) Debug.Log($"Set SwiftStrike cooldown to {swiftStrikeSkill.cooldownDuration} seconds from prefab");
            //     }
            // }
            
            // // Get WilloWisp cooldown from prefab
            // if (WilloWispPrefab != null)
            // {
            //     WilloWisp willoWispSkill = WilloWispPrefab.GetComponent<WilloWisp>();
            //     if (willoWispSkill != null)
            //     {
            //         cooldownDurations[SkillCombination.EQ] = willoWispSkill.cooldownDuration;
            //         if (showDebugLogs) Debug.Log($"Set WilloWisp cooldown to {willoWispSkill.cooldownDuration} seconds from prefab");
            //     }
            // }

            // // Get MagneticPull cooldown from prefab
            // if (MagneticPullPrefab != null)
            // {
            //     MagneticPull magneticPullSkill = MagneticPullPrefab.GetComponent<MagneticPull>();
            //     if (magneticPullSkill != null)
            //     {
            //         cooldownDurations[SkillCombination.EW] = magneticPullSkill.cooldownDuration;
            //         if (showDebugLogs) Debug.Log($"Set MagneticPull cooldown to {magneticPullSkill.cooldownDuration} seconds from prefab");
            //     }
            // }

            // You can add more specific cooldowns for other skills here
            // following the same pattern as more skills get implemented

            // Initialize the cooldown tracking dictionary with zero times
            foreach (SkillCombination combo in System.Enum.GetValues(typeof(SkillCombination)))
            {
                if (combo != SkillCombination.None)
                {
                    skillCooldowns[combo] = 0f;
                }
            }
        }
        
        private void Start()
        {
            if (crosshair == null)
            {
                crosshair = FindObjectOfType<PlayerCrosshair>();
                if (crosshair == null)
                {
                    Debug.LogError("SkillCast: Could not find PlayerCrosshair in the scene!");
                }
            }
            
            if (tileGrid == null)
            {
                tileGrid = FindObjectOfType<TileGrid>();
                if (tileGrid == null)
                {
                    Debug.LogError("SkillCast: Could not find TileGrid in the scene!");
                }
            }
            
            // Start in idle state
            ChangeState(idleState);
        }
        
        private void Update()
        {
            // Let the current state handle updates
            currentState.Update();

            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift) || UnityEngine.Input.GetKeyDown(KeyCode.RightShift))
            {
                CancelSkillInput();
            } 
            // Check for skill inputs
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
            {
                currentState.ProcessInput(SkillType.Q);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                currentState.ProcessInput(SkillType.E);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.W))
            {
                currentState.ProcessInput(SkillType.W);
            }
        }
        
        public void ChangeState(SkillCastState newState)
        {
            if (currentState != null)
            {
                currentState.ExitState();
            }
            
            currentState = newState;
            currentState.EnterState();
        }
        
        public SkillCombination GetSkillCombination(SkillType first, SkillType second)
        {
            //Q SKILL COMBINATION
            if (first == SkillType.Q && second == SkillType.Q) return SkillCombination.QQ;
            if (first == SkillType.Q && second == SkillType.E) return SkillCombination.QE;
            if (first == SkillType.Q && second == SkillType.W) return SkillCombination.QW;

            //E SKILL COMBINATION
            if (first == SkillType.E && second == SkillType.E) return SkillCombination.EE;
            if (first == SkillType.E && second == SkillType.Q) return SkillCombination.EQ;
            if (first == SkillType.E && second == SkillType.W) return SkillCombination.EW;
            
            //W SKILL COMBINATION
            if (first == SkillType.W && second == SkillType.W) return SkillCombination.WW;
            if (first == SkillType.W && second == SkillType.Q) return SkillCombination.WQ;
            if (first == SkillType.W && second == SkillType.E) return SkillCombination.WE;

            return SkillCombination.None;
        }
        
        // Check if a skill is on cooldown
        public bool IsSkillOnCooldown(SkillCombination combination)
        {
            if (combination == SkillCombination.None) return false;
            
            // If the current time is less than the stored cooldown end time, the skill is on cooldown
            if (skillCooldowns.ContainsKey(combination) && Time.time < skillCooldowns[combination])
            {
                float remainingCooldown = skillCooldowns[combination] - Time.time;
                if (showDebugLogs) Debug.Log($"Skill {combination} is on cooldown for {remainingCooldown:F1} more seconds");
                return true;
            }
            
            return false;
        }
        
        // Put a skill on cooldown
        private void StartSkillCooldown(SkillCombination combination)
        {
            if (combination == SkillCombination.None) return;
            
            // Get the cooldown duration for this skill
            float duration = 0.5f; // Default
            if (cooldownDurations.ContainsKey(combination))
            {
                duration = cooldownDurations[combination];
            }
            
            // Set the time when cooldown will end
            skillCooldowns[combination] = Time.time + duration;
            
            if (showDebugLogs) Debug.Log($"Skill {combination} put on cooldown for {duration} seconds");
        }
        
        public void CastSkill(SkillCombination combination)
        {
            if (crosshair == null) return;
            
            // Check if the skill is on cooldown first
            if (IsSkillOnCooldown(combination))
            {
                if (showDebugLogs) Debug.Log($"Cannot cast {combination} - Skill is on cooldown!");
                return;
            }
            
            Vector3 targetPosition = crosshair.GetTargetWorldPosition();
            Vector2Int targetGridPos = crosshair.GetTargetGridPosition();
            
            GameObject skillPrefab = null;
            string skillName = "Unknown";
            
            switch (combination)
            {
                case SkillCombination.QQ:
                    skillPrefab = IonBoltPrefab;
                    skillName = "Q+Q Skill";
                    break;
                case SkillCombination.QE:
                    skillPrefab = PulseFallPrefab;
                    skillName = "Q+E Skill";
                    break;
                case SkillCombination.QW:
                    skillPrefab = PlasmaSurgePrefab;
                    skillName = "Q+W Skill (PlasmaSurge)";
                    break;
                case SkillCombination.EE:
                    skillPrefab = GridLockPrefab;
                    skillName = "E+E Skill";
                    break;
                case SkillCombination.EQ:
                    skillPrefab = WilloWispPrefab;
                    skillName = "E+Q Skill";
                    break;
                case SkillCombination.EW:
                    skillPrefab = MagneticPullPrefab;
                    skillName = "E+W Skill";
                    break;
                case SkillCombination.WW:
                    skillPrefab = SwiftStrikePrefab;
                    skillName = "W+W Skill";
                    break;
                case SkillCombination.WQ:
                    skillPrefab = QuickSlashPrefab;
                    skillName = "W+Q Skill";
                    break;
                case SkillCombination.WE:
                    skillPrefab = KineticShovePrefab;
                    skillName = "W+E Skill";
                    break;
            }
            
            if (skillPrefab != null)
            {
                GameObject skillInstance = Instantiate(skillPrefab, targetPosition, Quaternion.identity);
                
                // If your skill prefabs have a Skill component, you can pass additional data
                Skill skillComponent = skillInstance.GetComponent<Skill>();
                if (skillComponent != null)
                {
                    //Find Player transform as caster
                    Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
                    if (playerTransform == null)
                    {
                        Debug.Log("Could not find player transform");
                        playerTransform = transform;
                    }

                    skillComponent.Initialize(targetGridPos, combination, playerTransform);

                    if (showDebugLogs) Debug.Log($"Cast {skillName} at position {targetGridPos}");
                    
                    // Start the cooldown for this skill
                    StartSkillCooldown(combination);
                }
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"No prefab assigned for skill combination: {combination}");
            }
        }
        
        // Optional: Method to cancel current skill input
        public void CancelSkillInput()
        {
            if (currentState == waitingState)
            {
                if (showDebugLogs) Debug.Log("Skill input canceled");
                ChangeState(idleState);
            }
        }
        
        // Optional: Method to get remaining cooldown time for UI
        public float GetRemainingCooldown(SkillCombination combination)
        {
            if (combination == SkillCombination.None) return 0f;
            
            if (skillCooldowns.ContainsKey(combination))
            {
                float remainingTime = skillCooldowns[combination] - Time.time;
                return remainingTime > 0 ? remainingTime : 0f;
            }
            
            return 0f;
        }
        
        // Helper method to get cooldown duration for a skill combination
        public float GetCooldownDuration(SkillCombination combination)
        {
            if (combination == SkillCombination.None) return 0f;
            
            if (cooldownDurations.ContainsKey(combination))
            {
                return cooldownDurations[combination];
            }
            
            return 0.5f; // Default cooldown duration
        }
    }
}