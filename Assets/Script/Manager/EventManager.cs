using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("Game Objects")]
    [SerializeField] private GameObject player;

    [Header("Game Flow Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnSkillPopupShow;
    public UnityEvent OnSkillPopupHide;
    public UnityEvent OnBattleStart;
    public UnityEvent OnBattleEnd;
    public UnityEvent OnTrainingRoomShow;
    public UnityEvent OnBossButtonShow;

    [Header("Skill Events")]
    public UnityEvent<string> OnSkillSelected;

    [Header("UI References")]
    [SerializeField] private GameObject skillPopupPanel;
    [SerializeField] private GameObject openSkillPopupButton;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueTrigger dialogueTrigger;

    [Header("Game Flow Settings")]
    [SerializeField] private float delayBetweenSteps = 1f;

    private enum GameState { Dialog, Skill, Battle, FinalDialog, TrainingRoom }

    private TileGrid tileGrid;
    private GameState currentState = GameState.Dialog;
    private bool battleOver = false;
    private bool skillPopupOpen = false;
    private bool waitingForSkillPopupExit = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // <-- Add this line
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        tileGrid = FindObjectOfType<TileGrid>();

        if (player != null)
        {
            player.SetActive(false);
            Debug.Log("Player hidden at start.");
        }

        Debug.Log("Game started.");
        OnGameStart?.Invoke();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && currentState == GameState.Battle)
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
        Debug.Log("ShowSkillPopup() called manually or via button.");

        if (currentState != GameState.Skill && currentState != GameState.Dialog)
        {
            Debug.LogWarning("ShowSkillPopup blocked - not in Dialog or Skill state. Current state: " + currentState);
            return;
        }

        StartCoroutine(ShowSkillPopupCoroutine("Q"));
    }

    public void ConfirmSkillPopup()
    {
        if (currentState == GameState.Skill && waitingForSkillPopupExit && skillPopupOpen)
        {
            Debug.Log("ConfirmSkillPopup called by button.");
            waitingForSkillPopupExit = false; // this will allow the coroutine to proceed
        }
    }

    private IEnumerator ShowSkillPopupCoroutine(string skillName)
    {
        Debug.Log("Entering ShowSkillPopupCoroutine...");
        currentState = GameState.Skill;
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
        currentState = GameState.Battle;

        TileGrid tileGrid = FindObjectOfType<TileGrid>();
        if (tileGrid != null)
        {
            tileGrid.SetupInitialPositions();
            Debug.Log("TileGrid initial positions set.");
        }

        player.SetActive(true);
        Debug.Log("Player activated for battle.");

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ResetToMaxStats();
            Debug.Log("Player spawn stats and animation triggered.");
        }

        EnemySpawner enemySpawner = FindObjectOfType<EnemySpawner>();
        if (enemySpawner != null)
        {
            enemySpawner.SpawnEnemies();
            Debug.Log("Enemies spawned.");
        }

        OnBattleStart?.Invoke();

        openSkillPopupButton.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);

        yield return new WaitForSeconds(0.1f);
        CheckForBattleEnd();
    }

    // Modified to only allow button-controlled opening/closing
    public void ToggleSkillPopupFromButton()
    {
        if (currentState == GameState.Skill)
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

        if (currentState == GameState.Battle)
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

    public void MarkBattleOver()
    {
        Debug.Log("MarkBattleOver called.");
        battleOver = true;
    }

    public void CheckForBattleEnd()
    {
        if (EnemyManager.Instance == null) return;

        int remaining = EnemyManager.Instance.GetAllEnemies().Count;
        Debug.Log("Remaining enemies (via EnemyManager): " + remaining);

        if (remaining <= 0 && currentState == GameState.Battle)
        {
            Debug.Log("All enemies defeated. Ending battle...");
            StartCoroutine(HandleBattleEndWithDelay(2f));
        }
    }

    private IEnumerator HandleBattleEndWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("Marking battle over and invoking OnBattleEnd...");
        MarkBattleOver();
        OnBattleEnd?.Invoke();
    }
}