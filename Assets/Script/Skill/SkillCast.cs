using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class SkillCast : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCrosshair crosshair;
        [SerializeField] private TileGrid tileGrid;
        
        [Header("Skill Settings")]
        [SerializeField] public GameObject skillQQPrefab;
        [SerializeField] private GameObject skillQEPrefab;
        [SerializeField] private GameObject skillQWPrefab;
        [SerializeField] private GameObject skillEEPrefab;
        [SerializeField] private GameObject skillEQPrefab;
        [SerializeField] private GameObject skillEWPrefab;
        [SerializeField] private GameObject skillWWPrefab;
        [SerializeField] private GameObject skillWQPrefab;
        [SerializeField] private GameObject skillWEPrefab;
        
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
            
            // Check for skill inputs
            if (Input.GetKeyDown(KeyCode.Q))
            {
                currentState.ProcessInput(SkillType.Q);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                currentState.ProcessInput(SkillType.E);
            }
            else if (Input.GetKeyDown(KeyCode.W))
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
        
        public void CastSkill(SkillCombination combination)
        {
            if (crosshair == null) return;
            
            Vector3 targetPosition = crosshair.GetTargetWorldPosition();
            Vector2Int targetGridPos = crosshair.GetTargetGridPosition();
            
            GameObject skillPrefab = null;
            string skillName = "Unknown";
            
            switch (combination)
            {
                case SkillCombination.QQ:
                    skillPrefab = skillQQPrefab;
                    skillName = "Q+Q Skill";
                    break;
                case SkillCombination.QE:
                    skillPrefab = skillQEPrefab;
                    skillName = "Q+E Skill";
                    break;
                case SkillCombination.QW:
                    skillPrefab = skillQWPrefab;
                    skillName = "Q+W Skill";
                    break;
                case SkillCombination.EE:
                    skillPrefab = skillEEPrefab;
                    skillName = "E+E Skill";
                    break;
                case SkillCombination.EQ:
                    skillPrefab = skillEQPrefab;
                    skillName = "E+Q Skill";
                    break;
                case SkillCombination.EW:
                    skillPrefab = skillEWPrefab;
                    skillName = "E+W Skill";
                    break;
                case SkillCombination.WW:
                    skillPrefab = skillWWPrefab;
                    skillName = "W+W Skill";
                    break;
                case SkillCombination.WQ:
                    skillPrefab = skillWQPrefab;
                    skillName = "W+Q Skill";
                    break;
                case SkillCombination.WE:
                    skillPrefab = skillWEPrefab;
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
    }
}