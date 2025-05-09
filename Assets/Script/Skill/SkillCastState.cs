using UnityEngine;

namespace SkillSystem
{
    // Base abstract class for all skill cast states
    public abstract class SkillCastState
    {
        protected SkillCast skillCast;
        
        public SkillCastState(SkillCast skillCast)
        {
            this.skillCast = skillCast;
        }
        
        public abstract void EnterState();
        public abstract void ExitState();
        public abstract void Update();
        public abstract void ProcessInput(SkillType inputType);
    }
    
    // Idle state - waiting for the first skill input
    public class IdleState : SkillCastState
    {
        public IdleState(SkillCast skillCast) : base(skillCast) { }
        
        public override void EnterState()
        {
            // Reset first skill when entering idle state
            skillCast.firstSkill = SkillType.None;
            
            if (skillCast.showDebugLogs) Debug.Log("Entered Idle State");
        }
        
        public override void ExitState()
        {
            // Nothing special to do when exiting idle state
        }
        
        public override void Update()
        {
            // Nothing to update in idle state
        }
        
        public override void ProcessInput(SkillType inputType)
        {
            if (inputType != SkillType.None)
            {
                // Store the first skill
                skillCast.firstSkill = inputType;
                
                // Transition to waiting state
                skillCast.ChangeState(skillCast.waitingState);
                
                if (skillCast.showDebugLogs) Debug.Log($"First skill input: {inputType}");
            }
        }
    }
    
    // Waiting state - waiting for the second skill input
    public class WaitingForSecondState : SkillCastState
    {
        private float timeLeftForSecondInput;
        private const float MAX_WAIT_TIME = 2.0f; // Time window to input second skill
        
        public WaitingForSecondState(SkillCast skillCast) : base(skillCast) { }
        
        public override void EnterState()
        {
            // Reset timer
            timeLeftForSecondInput = MAX_WAIT_TIME;
            
            if (skillCast.showDebugLogs) Debug.Log("Entered Waiting State");
        }
        
        public override void ExitState()
        {
            // Nothing special to do when exiting waiting state
        }
        
        public override void Update()
        {
            // Count down the time left for second input
            timeLeftForSecondInput -= Time.deltaTime;
            
            // If time runs out, go back to idle state
            if (timeLeftForSecondInput <= 0)
            {
                if (skillCast.showDebugLogs) Debug.Log("Timeout waiting for second skill input");
                skillCast.ChangeState(skillCast.idleState);
            }
        }
        
        public override void ProcessInput(SkillType inputType)
        {
            if (inputType != SkillType.None)
            {
                // Get the skill combination
                SkillCombination combination = skillCast.GetSkillCombination(skillCast.firstSkill, inputType);
                
                // Cast the skill
                skillCast.CastSkill(combination);
                
                if (skillCast.showDebugLogs) Debug.Log($"Second skill input: {inputType}, Combination: {combination}");
                
                // Return to idle state
                skillCast.ChangeState(skillCast.idleState);
            }
        }
    }
}