using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    [Header("Game Flow Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnDialogStart;
    public UnityEvent OnDialogEnd;
    public UnityEvent OnSkillPopupShow;
    public UnityEvent OnSkillPopupHide;
    public UnityEvent OnBattleStart;
    public UnityEvent OnBattleEnd;
    public UnityEvent OnTrainingRoomShow;
    public UnityEvent OnBossButtonShow;
    public UnityEvent OnSkillDescButtonShow;
    
    [Header("Skill Events")]
    public UnityEvent<string> OnSkillSelected; // Pass skill name/ID
    
    [Header("UI References")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private GameObject skillPopupPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject trainingRoomPanel;
    [SerializeField] private GameObject bossButton;
    [SerializeField] private GameObject skillDescButton;
    
    [Header("Game Flow Settings")]
    [SerializeField] private float delayBetweenSteps = 1f;
    
    // Current game state
    private enum GameState
    {
        Dialog,
        SkillQ,
        BattleQ,
        SkillW,
        BattleW,
        SkillE,
        BattleE,
        FinalDialog,
        TrainingRoom
    }
    
    private GameState currentState = GameState.Dialog;
    private int currentSkillIndex = 0;
    private string[] skillNames = { "Q", "W", "E" };
    
    private void Start()
    {
        // Initialize the game flow
        OnGameStart?.Invoke();
        StartGameFlow();
    }
    
    public void StartGameFlow()
    {
        StartCoroutine(GameFlowSequence());
    }
    
    private IEnumerator GameFlowSequence()
    {
        // Initial Dialog
        yield return StartCoroutine(ShowDialog());
        
        // Skill Q -> Battle -> Skill W -> Battle -> Skill E -> Battle
        for (int i = 0; i < skillNames.Length; i++)
        {
            currentSkillIndex = i;
            yield return StartCoroutine(ShowSkillPopup(skillNames[i]));
            yield return StartCoroutine(StartBattle());
        }
        
        // Final Dialog
        yield return StartCoroutine(ShowFinalDialog());
        
        // Training Room with buttons
        ShowTrainingRoom();
    }
    
    private IEnumerator ShowDialog()
    {
        currentState = GameState.Dialog;
        OnDialogStart?.Invoke();
        
        // Show dialog panel
        if (dialogPanel != null)
            dialogPanel.SetActive(true);
        
        // Wait for dialog to finish (you can modify this to wait for user input)
        yield return new WaitForSeconds(3f); // Example: 3 seconds
        
        // Hide dialog panel
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        OnDialogEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }
    
    private IEnumerator ShowSkillPopup(string skillName)
    {
        // Update state based on skill
        switch (skillName)
        {
            case "Q": currentState = GameState.SkillQ; break;
            case "W": currentState = GameState.SkillW; break;
            case "E": currentState = GameState.SkillE; break;
        }
        
        OnSkillPopupShow?.Invoke();
        OnSkillSelected?.Invoke(skillName);
        
        // Show skill popup panel
        if (skillPopupPanel != null)
            skillPopupPanel.SetActive(true);
        
        // Wait for skill popup (you can modify this to wait for user input)
        yield return new WaitForSeconds(2f); // Example: 2 seconds
        
        // Hide skill popup panel
        if (skillPopupPanel != null)
            skillPopupPanel.SetActive(false);
        
        OnSkillPopupHide?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }
    
    private IEnumerator StartBattle()
    {
        // Update state based on current skill
        switch (currentSkillIndex)
        {
            case 0: currentState = GameState.BattleQ; break;
            case 1: currentState = GameState.BattleW; break;
            case 2: currentState = GameState.BattleE; break;
        }
        
        OnBattleStart?.Invoke();
        
        // Show battle panel
        if (battlePanel != null)
            battlePanel.SetActive(true);
        
        // Wait for battle to finish (you can modify this to wait for battle completion)
        yield return new WaitForSeconds(5f); // Example: 5 seconds
        
        // Hide battle panel
        if (battlePanel != null)
            battlePanel.SetActive(false);
        
        OnBattleEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }
    
    private IEnumerator ShowFinalDialog()
    {
        currentState = GameState.FinalDialog;
        OnDialogStart?.Invoke();
        
        // Show dialog panel
        if (dialogPanel != null)
            dialogPanel.SetActive(true);
        
        // Wait for final dialog to finish
        yield return new WaitForSeconds(3f); // Example: 3 seconds
        
        // Hide dialog panel
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        OnDialogEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }
    
    private void ShowTrainingRoom()
    {
        currentState = GameState.TrainingRoom;
        OnTrainingRoomShow?.Invoke();
        
        // Show training room panel
        if (trainingRoomPanel != null)
            trainingRoomPanel.SetActive(true);
        
        // Show boss button
        if (bossButton != null)
        {
            bossButton.SetActive(true);
            OnBossButtonShow?.Invoke();
        }
        
        // Show skill description button
        if (skillDescButton != null)
        {
            skillDescButton.SetActive(true);
            OnSkillDescButtonShow?.Invoke();
        }
    }
    
    // Public methods that can be called by buttons or other scripts
    public void OnBossButtonClicked()
    {
        Debug.Log("Boss button clicked!");
        // Add your boss logic here
    }
    
    public void OnSkillDescButtonClicked()
    {
        Debug.Log("Skill description button clicked!");
        // Add your skill description logic here
    }
    
    public void RestartGameFlow()
    {
        // Reset state and restart the flow
        currentState = GameState.Dialog;
        currentSkillIndex = 0;
        
        // Hide all panels
        HideAllPanels();
        
        // Start the flow again
        StartGameFlow();
    }
    
    private void HideAllPanels()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (skillPopupPanel != null) skillPopupPanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(false);
        if (trainingRoomPanel != null) trainingRoomPanel.SetActive(false);
        if (bossButton != null) bossButton.SetActive(false);
        if (skillDescButton != null) skillDescButton.SetActive(false);
    }
    
    // Getter for current state (useful for debugging or other scripts)
    // public GameState GetCurrentState()
    // {
    //     return currentState;
    // }
    
    // Method to force skip to training room (for testing)
    public void SkipToTrainingRoom()
    {
        StopAllCoroutines();
        HideAllPanels();
        ShowTrainingRoom();
    }
}