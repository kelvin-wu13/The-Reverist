using UnityEngine;

public class ComboTracker : MonoBehaviour
{
    private Animator animator;
    private int currentComboIndex = 0;
    private float timeElapsed;
    [SerializeField] private float waitTime = 1.5f;
    [SerializeField] private int comboAmount = 3;

    private readonly int ComboIndex = Animator.StringToHash("ComboIndex");
    // Removed IsShooting - PlayerShoot now handles this

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed >= waitTime)
        {
            currentComboIndex = 0;
            animator.SetInteger(ComboIndex, 0);
        }
    }

    public void TriggerCombo()
    {
        timeElapsed = 0f;
        currentComboIndex++;
        if (currentComboIndex > comboAmount)
            currentComboIndex = 1;

        animator.SetInteger(ComboIndex, currentComboIndex);
        // Removed IsShooting control - PlayerShoot handles animation timing
    }

    public int GetCurrentComboIndex()
    {
        return currentComboIndex;
    }

    // This method is no longer needed but keeping it for compatibility
    public void ResetShootingFlag()
    {
        // Method kept for backwards compatibility but does nothing
        // Animation control is now handled by PlayerShoot
    }
}