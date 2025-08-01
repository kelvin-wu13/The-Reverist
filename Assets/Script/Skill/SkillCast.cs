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

        private SkillType[] skillQueue = new SkillType[2] { SkillType.None, SkillType.None };
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

            if (input != SkillType.None)
            {
                AddSkillToQueue(input);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                CastQueuedSkill();
            }
        }

        private void AddSkillToQueue(SkillType skill)
        {
            skillQueue[0] = skillQueue[1];
            skillQueue[1] = skill;
        }

        private void CancelSkillInput()
        {
            if (skillQueue[0] != SkillType.None || skillQueue[1] != SkillType.None)
            {
                skillQueue[0] = SkillType.None;
                skillQueue[1] = SkillType.None;
            }
        }

        private void CastQueuedSkill()
        {
            if (skillQueue[0] == SkillType.None || skillQueue[1] == SkillType.None)
            {
                return;
            }

            SkillCombination combo = GetSkillCombination(skillQueue[0], skillQueue[1]);

            if (IsSkillOnCooldown(combo))
            {
                if (showDebugLogs) Debug.Log($"Skill {combo} is on cooldown!");
                return;
            }

            CastSkill(combo);

            skillQueue[0] = SkillType.None;
            skillQueue[1] = SkillType.None;
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
                return Time.time < endTime;
            return false;
        }

        private void StartSkillCooldown(SkillCombination combo)
        {
            float duration = cooldownDurations.TryGetValue(combo, out float d) ? d : 0.5f;
            skillCooldowns[combo] = Time.time + duration;
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

            if (prefab == null) return;

            Vector3 worldPos = crosshair.GetTargetWorldPosition();
            Vector2Int gridPos = crosshair.GetTargetGridPosition();
            GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);

            Skill skill = instance.GetComponent<Skill>();
            if (skill != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player")?.transform ?? transform;
                skill.Initialize(gridPos, combo, player);
                StartSkillCooldown(combo);
            }

            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnSkillCast(combo.ToString());
            }

            if (comboTracker == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) comboTracker = player.GetComponent<ComboTracker>();
            }

            Animator animator = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Animator>();
            if (animator != null)
            {
                if (combo == SkillCombination.WQ || combo == SkillCombination.WW)
                    TriggerMeleeAnimation(combo, animator);
                else
                    animator.SetBool("IsShooting", true);
            }
        }

        private void TriggerMeleeAnimation(SkillCombination combo, Animator animator)
        {
            if (animator == null) return;
            animator.SetBool("IsShooting", false);
            animator.ResetTrigger("QuickSlash");
            animator.ResetTrigger("SwiftStrike");
            StartCoroutine(SetTriggerWithDelay(combo, animator));
        }

        private System.Collections.IEnumerator SetTriggerWithDelay(SkillCombination combo, Animator animator)
        {
            yield return new WaitForEndOfFrame();
            switch (combo)
            {
                case SkillCombination.WQ: animator.SetTrigger("QuickSlash"); break;
                case SkillCombination.WW: animator.SetTrigger("SwiftStrike"); break;
            }
        }

        private void InitializeCooldownDurations()
        {
            float defaultCD = 0.5f;
            foreach (SkillCombination combo in System.Enum.GetValues(typeof(SkillCombination)))
                if (combo != SkillCombination.None) cooldownDurations[combo] = defaultCD;

            foreach (var combo in cooldownDurations.Keys)
                skillCooldowns[combo] = 0f;
        }

        public SkillType[] GetSkillQueue()
        {
            return (SkillType[])skillQueue.Clone();
        }
        public bool IsQueueFull()
        {
            return skillQueue[0] != SkillType.None && skillQueue[1] != SkillType.None;
        }
    }
}