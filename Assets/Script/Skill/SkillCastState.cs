using UnityEngine;

namespace SkillSystem
{
    // Base State class for the State Pattern
    public abstract class SkillCastState
    {
        protected SkillCast skillCast;

        public SkillCastState(SkillCast skillCast)
        {
            this.skillCast = skillCast;
        }

        public abstract void EnterState();
        public abstract void Update();
        public abstract void ProcessInput(SkillType skillType);
        public abstract void ExitState();
    }

    // Idle state - waiting for first skill input
    public class IdleState : SkillCastState
    {
        public IdleState(SkillCast skillCast) : base(skillCast) { }

        public override void EnterState()
        {
            if (skillCast.showDebugLogs) Debug.Log("Entered Idle State");
        }

        public override void Update()
        {
            // Nothing to update in idle state
        }

        public override void ProcessInput(SkillType skillType)
        {
            // First skill input received, transition to WaitingForSecond state
            skillCast.firstSkill = skillType;
            skillCast.ChangeState(skillCast.waitingState);
            
            if (skillCast.showDebugLogs) Debug.Log($"First skill selected: {skillType}");
        }

        public override void ExitState()
        {
            // Nothing special to do when exiting idle state
        }
    }

    // Waiting state - first skill input received, waiting for second input
    public class WaitingForSecondState : SkillCastState
    {
        public WaitingForSecondState(SkillCast skillCast) : base(skillCast) { }

        public override void EnterState()
        {
            if (skillCast.showDebugLogs) Debug.Log("Entered Waiting State");
        }

        public override void Update()
        {
            // No time limit anymore, will wait indefinitely
        }

        public override void ProcessInput(SkillType skillType)
        {
            // Second skill input received, cast the skill
            SkillCombination combination = skillCast.GetSkillCombination(skillCast.firstSkill, skillType);
            skillCast.CastSkill(combination);
            
            // Return to idle state
            skillCast.ChangeState(skillCast.idleState);
        }

        public override void ExitState()
        {
            // Reset first skill when exiting waiting state
            skillCast.firstSkill = SkillType.None;
        }
    }
}