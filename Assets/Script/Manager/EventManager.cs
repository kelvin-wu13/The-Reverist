using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

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
    public UnityEvent<string> OnSkillSelected;

    [Header("UI References")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private GameObject skillPopupPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject bossButton;
    [SerializeField] private GameObject skillDescButton;
    [SerializeField] private GameObject SkillPopupButton;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueTrigger dialogueTrigger;

    [Header("Game Flow Settings")]
    [SerializeField] private float delayBetweenSteps = 1f;

    private enum GameState { Dialog, SkillQ, BattleQ, FinalDialog, TrainingRoom }

    private GameState currentState = GameState.Dialog;
    private bool battleOver = false;
    private bool skillPopupOpen = false;
    private bool waitingForSkillPopupExit = false;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        skillPopupPanel.SetActive(false);

        OnGameStart?.Invoke();

        if (dialogueManager != null)
            dialogueManager.OnDialogueFinished.AddListener(ProceedAfterDialogue);

        StartGameFlow();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSkillPopupFromButton();
        }
    }

    public void StartGameFlow()
    {
        if (dialogueTrigger != null)
            dialogueTrigger.TriggerDialogue();
    }

    public void ProceedAfterDialogue()
    {
        StartCoroutine(ShowSkillPopup("Q"));
    }

    private IEnumerator ShowSkillPopup(string skillName)
    {
        currentState = GameState.SkillQ;
        Time.timeScale = 0f;

        OnSkillPopupShow?.Invoke();
        OnSkillSelected?.Invoke(skillName);

        skillPopupPanel.SetActive(true);
        skillPopupOpen = true;
        waitingForSkillPopupExit = true;

        // ONLY wait for skill popup close button (ToggleSkillPopupFromButton)
        yield return new WaitUntil(() => waitingForSkillPopupExit == false);

        skillPopupPanel.SetActive(false);
        skillPopupOpen = false;
        Time.timeScale = 1f;

        OnSkillPopupHide?.Invoke();

        yield return new WaitForSecondsRealtime(delayBetweenSteps);
        yield return StartBattle();
    }

    private IEnumerator StartBattle()
    {
        battleOver = false;
        currentState = GameState.BattleQ;
        FindObjectOfType<SkillSystem.SkillCast>().SetAllowedSkillSet("Q");

        OnBattleStart?.Invoke();
        SkillPopupButton.SetActive(true);

        yield return new WaitUntil(() => battleOver);

        SkillPopupButton.SetActive(false);
        OnBattleEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }

    public void ToggleSkillPopupFromButton()
    {
        // Handle different behavior based on current game state
        switch (currentState)
        {
            case GameState.SkillQ:
                // During initial skill popup phase - close popup and continue game flow
                if (skillPopupOpen && waitingForSkillPopupExit)
                {
                    skillPopupOpen = false;
                    skillPopupPanel.SetActive(false);
                    Time.timeScale = 1f;
                    waitingForSkillPopupExit = false;
                }
                break;

            case GameState.BattleQ:
                // During battle - DON'T allow skill popup to open from random clicks
                // Only allow it to close if it's already open
                if (skillPopupOpen)
                {
                    skillPopupOpen = false;
                    skillPopupPanel.SetActive(false);
                    Time.timeScale = 1f;
                }
                break;

            default:
                // For other states, maintain original behavior
                skillPopupOpen = !skillPopupOpen;
                skillPopupPanel.SetActive(skillPopupOpen);
                Time.timeScale = skillPopupOpen ? 0f : 1f;
                break;
        }
    }

    // Separate method specifically for opening skill popup during battle
    public void OpenSkillPopupDuringBattle()
    {
        if (currentState == GameState.BattleQ && !skillPopupOpen)
        {
            skillPopupOpen = true;
            skillPopupPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // ensure game isn't paused
        SceneManager.LoadScene(1); // assumes main menu is Scene 1
    }

    public void MarkBattleOver() => battleOver = true;
}