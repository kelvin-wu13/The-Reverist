using UnityEngine;
using SkillSystem;

public class SkillHitDetector : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    public void OnSkillHitEnemy(SkillCombination skillCombo, Enemy hitEnemy)
    {
        if (hitEnemy == null) return;

        string comboString = skillCombo.ToString();

        if (showDebugLogs)
        {
            Debug.Log($"Skill {comboString} hit enemy at position {hitEnemy.GetCurrentGridPosition()}");
        }

        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnSkillHitEnemy(comboString);
        }
    }
    public static void ReportSkillHit(SkillCombination skillCombo, Enemy hitEnemy)
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnSkillHitEnemy(skillCombo.ToString());
        }
    }

    public static void ReportSkillHit(string skillComboString, Enemy hitEnemy)
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnSkillHitEnemy(skillComboString);
        }
    }
}