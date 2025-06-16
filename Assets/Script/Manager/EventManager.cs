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

    private enum GameState { Dialog, SkillQ, BattleQ, SkillW, BattleW, SkillE, BattleE, FinalDialog, TrainingRoom }

    private GameState currentState = GameState.Dialog;
    private int currentSkillIndex = 0;
    private string[] skillNames = { "Q", "W", "E" };

    private bool battleOver = false;

    private void Start()
    {
        OnGameStart?.Invoke();
        StartGameFlow();
    }

    public void StartGameFlow()
    {
        StartCoroutine(GameFlowSequence());
    }

    private IEnumerator GameFlowSequence()
    {
        yield return StartCoroutine(ShowDialog());

        for (int i = 0; i < skillNames.Length; i++)
        {
            currentSkillIndex = i;
            yield return StartCoroutine(ShowSkillPopup(skillNames[i]));
            yield return StartCoroutine(StartBattle());
        }

        yield return StartCoroutine(ShowFinalDialog());
        ShowTrainingRoom();
    }

    private IEnumerator ShowDialog()
    {
        currentState = GameState.Dialog;
        OnDialogStart?.Invoke();
        if (dialogPanel != null) dialogPanel.SetActive(true);

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        if (dialogPanel != null) dialogPanel.SetActive(false);
        OnDialogEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }

    private IEnumerator ShowSkillPopup(string skillName)
    {
        switch (skillName)
        {
            case "Q": currentState = GameState.SkillQ; break;
            case "W": currentState = GameState.SkillW; break;
            case "E": currentState = GameState.SkillE; break;
        }

        OnSkillPopupShow?.Invoke();
        OnSkillSelected?.Invoke(skillName);
        if (skillPopupPanel != null) skillPopupPanel.SetActive(true);

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        if (skillPopupPanel != null) skillPopupPanel.SetActive(false);
        OnSkillPopupHide?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }

    private IEnumerator StartBattle()
    {
        battleOver = false;

        switch (currentSkillIndex)
        {
            case 0:
                currentState = GameState.BattleQ;
                FindObjectOfType<SkillSystem.SkillCast>().SetAllowedSkillSet("Q");
                break;
            case 1:
                currentState = GameState.BattleW;
                FindObjectOfType<SkillSystem.SkillCast>().SetAllowedSkillSet("QW");
                break;
            case 2:
                currentState = GameState.BattleE;
                FindObjectOfType<SkillSystem.SkillCast>().SetAllowedSkillSet("QWE");
                break;
        }

        OnBattleStart?.Invoke();
        if (battlePanel != null) battlePanel.SetActive(true);

        yield return new WaitUntil(() => battleOver);

        if (battlePanel != null) battlePanel.SetActive(false);
        OnBattleEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }

    private IEnumerator ShowFinalDialog()
    {
        currentState = GameState.FinalDialog;
        OnDialogStart?.Invoke();
        if (dialogPanel != null) dialogPanel.SetActive(true);

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        if (dialogPanel != null) dialogPanel.SetActive(false);
        OnDialogEnd?.Invoke();
        yield return new WaitForSeconds(delayBetweenSteps);
    }

    private void ShowTrainingRoom()
    {
        currentState = GameState.TrainingRoom;
        OnTrainingRoomShow?.Invoke();

        if (trainingRoomPanel != null) trainingRoomPanel.SetActive(true);
        if (bossButton != null) { bossButton.SetActive(true); OnBossButtonShow?.Invoke(); }
        if (skillDescButton != null) { skillDescButton.SetActive(true); OnSkillDescButtonShow?.Invoke(); }
    }

    public void MarkBattleOver() => battleOver = true;

    public void RestartGameFlow()
    {
        currentState = GameState.Dialog;
        currentSkillIndex = 0;
        HideAllPanels();
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

    public void SkipToTrainingRoom()
    {
        StopAllCoroutines();
        HideAllPanels();
        ShowTrainingRoom();
    }
}
