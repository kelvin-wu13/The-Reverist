using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField] private GameObject openSkillPopupButton;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueTrigger dialogueTrigger;

    [Header("Game Flow Settings")]
    [SerializeField] private float delayBetweenSteps = 1f;

    private enum GameState { Dialog, SkillQ, BattleQ, FinalDialog, TrainingRoom }

    private GameState currentState = GameState.Dialog;
    private bool battleOver = false;
    private bool skillPopupOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        OnGameStart?.Invoke();

        if (dialogueManager != null)
            dialogueManager.OnDialogueFinished.AddListener(ProceedAfterDialogue);

        StartGameFlow();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSkillPopupDuringBattle();
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

        // No text changes, just waits for key
        yield return new WaitForSecondsRealtime(0.2f); // 🔁 wait for input reset
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Mouse1));


        skillPopupPanel.SetActive(false);
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
        openSkillPopupButton.SetActive(true);

        yield return new WaitUntil(() => battleOver);

        openSkillPopupButton.SetActive(false);
        OnBattleEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }

    public void MarkBattleOver() => battleOver = true;

    public void ToggleSkillPopupDuringBattle()
    {
        skillPopupOpen = !skillPopupOpen;
        skillPopupPanel.SetActive(skillPopupOpen);
        Time.timeScale = skillPopupOpen ? 0f : 1f;
    }
}
