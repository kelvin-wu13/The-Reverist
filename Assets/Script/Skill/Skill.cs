using UnityEngine;

namespace SkillSystem
{
    public class Skill : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5.0f;
        [SerializeField] private ParticleSystem skillEffect;
        
        private Vector2Int targetGridPosition;
        private SkillCombination skillType;
        
        public virtual void Initialize(Vector2Int gridPos, SkillCombination type, Transform caster)
        {
            targetGridPosition = gridPos;
            skillType = type;
            
            // Start lifetime countdown
            Destroy(gameObject, lifetime);
            
            // Apply skill effects based on type
            ExecuteSkillEffect(gridPos, caster);

            // Play particle effect if assigned
            if (skillEffect != null)
            {
                skillEffect.Play();
            }
        }

         // Virtual method to be overridden by specific skill implementations
        public virtual void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {
            // Default implementation does nothing
            Debug.Log($"Base skill effect for {skillType} at {targetPosition}");
        }
        
        // private void ApplySkillEffects()
        // {
        //     // Different effects based on skill type
        //     switch (skillType)
        //     {
        //         case SkillCombination.QQ:
        //             // Example: Single target damage
        //             break;
        //         case SkillCombination.QE:
        //             // Example: Area damage
        //             break;
        //         case SkillCombination.QW:
        //             // Example: Status effect
        //             break;
        //         case SkillCombination.EE:
        //             // Example: Movement or utility
        //             break;
        //         case SkillCombination.EQ:
        //             // Example: Single target damage
        //             break;
        //         case SkillCombination.EW:
        //             // Example: Area damage
        //             break;
        //         case SkillCombination.WW:
        //             // Example: Status effect
        //             break;
        //         case SkillCombination.WQ:
        //             // Example: Movement or utility
        //             break;
        //         case SkillCombination.WE:
        //             // Example: Movement or utility
        //             break;
        //     }
        // }
    }
}