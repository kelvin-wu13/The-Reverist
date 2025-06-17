using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    [Header("Skill Events")]
    public UnityEvent<string> OnSkillSelected;

    [Header("UI References")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private GameObject skillPopupPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject bossButton;
    [SerializeField] private GameObject openSkillPopupButton;
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
        Debug.Log("Game started.");
        OnGameStart?.Invoke();

        StartGameFlow();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && currentState == GameState.BattleQ)
        {
            ToggleSkillPopupFromButton();
        }
    }

    public void StartGameFlow()
    {
        Debug.Log("Starting game flow...");
        if (dialogueTrigger != null)
        {
            Debug.Log("Triggering opening dialogue...");
            dialogueTrigger.TriggerDialogue();
        }
    }

    public void ProceedAfterDialogue()
    {
        Debug.Log("Dialogue ended. Showing skill popup.");
        ShowSkillPopup();
    }

    public void ShowSkillPopup()
    {
        if (currentState != GameState.SkillQ && currentState != GameState.Dialog)
        {
            Debug.LogWarning("ShowSkillPopup blocked - not in Dialog or SkillQ state. Current state: " + currentState);
            return;
        }
        
        StartCoroutine(ShowSkillPopupCoroutine("Q"));
    }

    public void ConfirmSkillPopup()
    {
        if (currentState == GameState.SkillQ && waitingForSkillPopupExit && skillPopupOpen)
        {
            Debug.Log("ConfirmSkillPopup called by button.");
            waitingForSkillPopupExit = false; // this will allow the coroutine to proceed
        }
    }

    private IEnumerator ShowSkillPopupCoroutine(string skillName)
    {
        Debug.Log("Entering ShowSkillPopupCoroutine...");
        currentState = GameState.SkillQ;
        Time.timeScale = 0f;

        OnSkillPopupShow?.Invoke();
        OnSkillSelected?.Invoke(skillName);

        skillPopupPanel.SetActive(true);
        skillPopupOpen = true;
        waitingForSkillPopupExit = true;

        Debug.Log("Skill popup opened. Waiting for player to close it manually...");

        yield return new WaitUntil(() => waitingForSkillPopupExit == false);

        skillPopupPanel.SetActive(false);
        skillPopupOpen = false;
        Time.timeScale = 1f;

        Debug.Log("Skill popup closed. Proceeding to StartBattle.");
        OnSkillPopupHide?.Invoke();

        yield return new WaitForSecondsRealtime(delayBetweenSteps);
        yield return StartBattle();
    }

    private IEnumerator StartBattle()
    {
        Debug.Log("Starting battle phase...");
        battleOver = false;
        currentState = GameState.BattleQ;

        var skillCaster = FindObjectOfType<SkillSystem.SkillCast>();
        if (skillCaster != null)
        {
            skillCaster.SetAllowedSkillSet("Q");
            Debug.Log("Set Q skills for battle.");
        }

        OnBattleStart?.Invoke();

        battlePanel.SetActive(true);
        openSkillPopupButton.SetActive(true);

        EventSystem.current.SetSelectedGameObject(null); // Important

        yield return new WaitUntil(() => battleOver);

        Debug.Log("Battle ended.");
        battlePanel.SetActive(false);
        openSkillPopupButton.SetActive(false);
        OnBattleEnd?.Invoke();

        yield return new WaitForSecondsRealtime(delayBetweenSteps);
    }

    // Modified to only allow button-controlled opening/closing
    public void ToggleSkillPopupFromButton()
    {
        if (currentState == GameState.SkillQ)
        {
            // Only allow closing the popup to proceed to battle during initial state
            if (waitingForSkillPopupExit && skillPopupOpen)
            {
                Debug.Log("Initial skill popup closed via SkillPopupButton. Proceeding to battle...");
                skillPopupPanel.SetActive(false);
                skillPopupOpen = false;
                Time.timeScale = 1f;
                waitingForSkillPopupExit = false;
                return;
            }

            Debug.Log("Blocked: Skill popup is already closed or not waiting.");
            return;
        }

        if (currentState == GameState.BattleQ)
        {
            // Allow normal pause/unpause toggle in battle phase
            skillPopupOpen = !skillPopupOpen;
            skillPopupPanel.SetActive(skillPopupOpen);
            Time.timeScale = skillPopupOpen ? 0f : 1f;
            Debug.Log("Battle phase popup toggled. New state: " + (skillPopupOpen ? "OPEN" : "CLOSED"));
            return;
        }

        Debug.Log("Popup toggle blocked in current state: " + currentState);
    }


    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }

    public void MarkBattleOver()
    {
        Debug.Log("MarkBattleOver called.");
        battleOver = true;
    }
}