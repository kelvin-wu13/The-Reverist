using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem
{
    public class SkillCast : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCrosshair crosshair;
        [SerializeField] private TileGrid tileGrid;

        [Header("Skill Prefabs")]
        [SerializeField] public GameObject IonBoltPrefab;
        [SerializeField] private GameObject PulseFallPrefab;
        [SerializeField] private GameObject PlasmaSurgePrefab;
        [SerializeField] private GameObject GridLockPrefab;
        [SerializeField] private GameObject WilloWispPrefab;
        [SerializeField] private GameObject MagneticPullPrefab;
        [SerializeField] private GameObject SwiftStrikePrefab;
        [SerializeField] private GameObject QuickSlashPrefab;
        [SerializeField] private GameObject KineticShovePrefab;

        [Header("UI Feedback")]
        public bool showDebugLogs = true;

        private SkillType firstSkill = SkillType.None;
        private ComboTracker comboTracker;

        private Dictionary<SkillCombination, float> skillCooldowns = new();
        private Dictionary<SkillCombination, float> cooldownDurations = new();

        private void Awake()
        {
            InitializeCooldownDurations();
        }

        private void Start()
        {
            crosshair ??= FindObjectOfType<PlayerCrosshair>();
            tileGrid ??= FindObjectOfType<TileGrid>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                CancelSkillInput();
                return;
            }

            SkillType input = SkillType.None;

            if (Input.GetKeyDown(KeyCode.Q)) input = SkillType.Q;
            else if (Input.GetKeyDown(KeyCode.W)) input = SkillType.W;
            else if (Input.GetKeyDown(KeyCode.E)) input = SkillType.E;

            if (input == SkillType.None) return;

            // First skill input
            if (firstSkill == SkillType.None)
            {
                firstSkill = input;
                if (showDebugLogs) Debug.Log($"First skill input: {firstSkill}");
            }
            else
            {
                // Second skill input → Cast combo
                SkillCombination combo = GetSkillCombination(firstSkill, input);
                CastSkill(combo);
                firstSkill = SkillType.None;
            }
        }

        private void TriggerMeleeAnimation(SkillCombination combo, Animator animator)
        {
            if (animator == null) return;

            // Cancel all current animations first
            animator.SetBool("IsShooting", false);
            
            // Reset all triggers to ensure clean state
            animator.ResetTrigger("QuickSlash");
            animator.ResetTrigger("SwiftStrike");
            
            // Set isMelee to true
            animator.SetBool("isMelee", true);

            // Small delay to ensure state changes are processed
            StartCoroutine(SetTriggerWithDelay(combo, animator));
        }

        private System.Collections.IEnumerator SetTriggerWithDelay(SkillCombination combo, Animator animator)
        {
            yield return new WaitForEndOfFrame(); // Wait one frame
            
            // Trigger specific melee animation based on combo
            switch (combo)
            {
                case SkillCombination.WQ: // QuickSlash
                    animator.SetTrigger("QuickSlash");
                    if (showDebugLogs) Debug.Log("Triggered QuickSlash animation");
                    break;
                    
                case SkillCombination.WW: // SwiftStrike
                    animator.SetTrigger("SwiftStrike");
                    if (showDebugLogs) Debug.Log("Triggered SwiftStrike animation");
                    break;
            }
        }

        private void CancelSkillInput()
        {
            if (firstSkill != SkillType.None)
            {
                if (showDebugLogs) Debug.Log("Skill input canceled.");
                firstSkill = SkillType.None;
            }
        }

        private SkillCombination GetSkillCombination(SkillType first, SkillType second)
        {
            if (first == SkillType.Q && second == SkillType.Q) return SkillCombination.QQ;
            if (first == SkillType.Q && second == SkillType.E) return SkillCombination.QE;
            if (first == SkillType.Q && second == SkillType.W) return SkillCombination.QW;

            if (first == SkillType.E && second == SkillType.E) return SkillCombination.EE;
            if (first == SkillType.E && second == SkillType.Q) return SkillCombination.EQ;
            if (first == SkillType.E && second == SkillType.W) return SkillCombination.EW;

            if (first == SkillType.W && second == SkillType.W) return SkillCombination.WW;
            if (first == SkillType.W && second == SkillType.Q) return SkillCombination.WQ;
            if (first == SkillType.W && second == SkillType.E) return SkillCombination.WE;

            return SkillCombination.None;
        }

        private bool IsSkillOnCooldown(SkillCombination combo)
        {
            if (combo == SkillCombination.None) return true;

            if (skillCooldowns.TryGetValue(combo, out float endTime))
            {
                if (Time.time < endTime)
                {
                    float remaining = endTime - Time.time;
                    if (showDebugLogs) Debug.Log($"Skill {combo} is on cooldown ({remaining:F1}s)");
                    return true;
                }
            }

            return false;
        }

        private void StartSkillCooldown(SkillCombination combo)
        {
            float duration = cooldownDurations.TryGetValue(combo, out float d) ? d : 0.5f;
            skillCooldowns[combo] = Time.time + duration;

            if (showDebugLogs)
                Debug.Log($"Started cooldown: {combo} → {duration}s");
        }

        private void CastSkill(SkillCombination combo)
        {
            if (IsSkillOnCooldown(combo)) return;

            GameObject prefab = combo switch
            {
                SkillCombination.QQ => IonBoltPrefab,
                SkillCombination.QE => PulseFallPrefab,
                SkillCombination.QW => PlasmaSurgePrefab,
                SkillCombination.EE => GridLockPrefab,
                SkillCombination.EQ => WilloWispPrefab,
                SkillCombination.EW => MagneticPullPrefab,
                SkillCombination.WW => SwiftStrikePrefab,
                SkillCombination.WQ => QuickSlashPrefab,
                SkillCombination.WE => KineticShovePrefab,
                _ => null
            };

            if (prefab == null)
            {
                if (showDebugLogs) Debug.LogWarning($"No prefab for {combo}");
                return;
            }

            Vector3 worldPos = crosshair.GetTargetWorldPosition();
            Vector2Int gridPos = crosshair.GetTargetGridPosition();

            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
            Skill skill = instance.GetComponent<Skill>();

            if (skill != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player")?.transform ?? transform;
                skill.Initialize(gridPos, combo, player);

                if (showDebugLogs)
                    Debug.Log($"Cast {combo} at {gridPos}");

                StartSkillCooldown(combo);
            }

            if (comboTracker == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    comboTracker = player.GetComponent<ComboTracker>();
                }
            }

            // Handle animations based on skill type
            Animator animator = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Animator>();
            if (animator != null)
            {
                // Check if it's a melee skill
                if (combo == SkillCombination.WQ || combo == SkillCombination.WW)
                {
                    TriggerMeleeAnimation(combo, animator);
                }
                else
                {
                    // For ranged skills, use the existing shooting animation
                    animator.SetBool("isMelee", false);
                    animator.SetBool("IsShooting", true);
                }
            }

            if (comboTracker != null)
            {
                comboTracker.TriggerCombo();
            }
        }

        private void InitializeCooldownDurations()
        {
            float defaultCD = 0.5f;

            foreach (SkillCombination combo in System.Enum.GetValues(typeof(SkillCombination)))
            {
                if (combo != SkillCombination.None)
                    cooldownDurations[combo] = defaultCD;
            }

            if (IonBoltPrefab?.GetComponent<IonBolt>() is IonBolt ionBolt)
                cooldownDurations[SkillCombination.QQ] = ionBolt.cooldownDuration;

            if (PlasmaSurgePrefab?.GetComponent<PlasmaSurge>() is PlasmaSurge plasmaSurge)
                cooldownDurations[SkillCombination.QW] = plasmaSurge.cooldownDuration;

            if (PlasmaSurgePrefab?.GetComponent<PulseFall>() is PulseFall pulseFall)
                cooldownDurations[SkillCombination.QE] = pulseFall.cooldownDuration;

            if (GridLockPrefab?.GetComponent<GridLock>() is GridLock gridLock)
                cooldownDurations[SkillCombination.EE] = gridLock.cooldownDuration;

            if (WilloWispPrefab?.GetComponent<WilloWisp>() is WilloWisp willoWisp)
                cooldownDurations[SkillCombination.EQ] = willoWisp.cooldownDuration;

            if (MagneticPullPrefab?.GetComponent<MagneticPull>() is MagneticPull magneticPull)
                cooldownDurations[SkillCombination.EW] = magneticPull.cooldownDuration;

            if (QuickSlashPrefab?.GetComponent<QuickSlash>() is QuickSlash quickSlash)
                cooldownDurations[SkillCombination.WQ] = quickSlash.cooldownDuration;

            if (SwiftStrikePrefab?.GetComponent<SwiftStrike>() is SwiftStrike swiftStrike)
                cooldownDurations[SkillCombination.WW] = swiftStrike.cooldownDuration;

            if (KineticShovePrefab?.GetComponent<KineticShove>() is KineticShove kineticShove)
                cooldownDurations[SkillCombination.WE] = kineticShove.cooldownDuration;

            foreach (var combo in cooldownDurations.Keys)
                skillCooldowns[combo] = 0f;
        }

        public float GetRemainingCooldown(SkillCombination combo)
        {
            return skillCooldowns.TryGetValue(combo, out float end) ? Mathf.Max(0f, end - Time.time) : 0f;
        }

        public float GetCooldownDuration(SkillCombination combo)
        {
            return cooldownDurations.TryGetValue(combo, out float d) ? d : 0.5f;
        }
    }
}
