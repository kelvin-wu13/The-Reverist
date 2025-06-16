// using System.Collections;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.UI;

// public class EventManager : MonoBehaviour
// {
//     public static EventManager Instance { get; private set; }

//     [Header("Game Flow Events")]
//     public UnityEvent OnGameStart;
//     public UnityEvent OnDialogStart;
//     public UnityEvent OnDialogEnd;
//     public UnityEvent OnSkillPopupShow;
//     public UnityEvent OnSkillPopupHide;
//     public UnityEvent OnBattleStart;
//     public UnityEvent OnBattleEnd;
//     public UnityEvent OnTrainingRoomShow;
//     public UnityEvent OnBossButtonShow;
//     public UnityEvent OnSkillDescButtonShow;

//     [Header("Skill Events")]
//     public UnityEvent<string> OnSkillSelected;

//     [Header("UI References")]
//     [SerializeField] private GameObject dialogPanel;
//     [SerializeField] private GameObject dialogTextObject;
//     [SerializeField] private GameObject skillPopupPanel;
//     [SerializeField] private GameObject skillPopupHowToText;
//     [SerializeField] private GameObject battlePanel;
//     [SerializeField] private GameObject trainingRoomPanel;
//     [SerializeField] private GameObject bossButton;
//     [SerializeField] private GameObject skillDescButton;
//     [SerializeField] private GameObject openSkillPopupButton;
//     [SerializeField] private DialogueManager dialogueManager;

//     [Header("Game Flow Settings")]
//     [SerializeField] private float delayBetweenSteps = 1f;

//     private enum GameState { Dialog, SkillQ, BattleQ, FinalDialog, TrainingRoom }

//     private GameState currentState = GameState.Dialog;
//     private bool battleOver = false;
//     private bool skillPopupOpen = false;

//     private void Awake()
//     {
//         if (Instance == null) Instance = this;
//         else Destroy(gameObject);
//     }

//     private void Start()
//     {
//         OnGameStart?.Invoke();
//         StartGameFlow();
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Escape))
//         {
//             ToggleSkillPopupDuringBattle();
//         }
//     }

//     public void StartGameFlow()
//     {
//         dialogueManager.StartInspectorDialogue();
//     }

//     public void ProceedAfterDialogue()
//     {
//         StartCoroutine(ShowSkillPopup("Q"));
//     }

//     private IEnumerator ShowSkillPopup(string skillName)
//     {
//         currentState = GameState.SkillQ;
//         Time.timeScale = 0f;

//         OnSkillPopupShow?.Invoke();
//         OnSkillSelected?.Invoke(skillName);

//         skillPopupPanel.SetActive(true);
//         SetSkillPopupText("Press Q twice (QQ), Q then W (QW), or Q then E (QE) to cast skills.");

//         yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

//         skillPopupPanel.SetActive(false);
//         Time.timeScale = 1f;
//         OnSkillPopupHide?.Invoke();
//         yield return new WaitForSecondsRealtime(delayBetweenSteps);

//         yield return StartBattle();
//     }

//     private IEnumerator StartBattle()
//     {
//         battleOver = false;
//         currentState = GameState.BattleQ;
//         FindObjectOfType<SkillSystem.SkillCast>().SetAllowedSkillSet("Q");

//         OnBattleStart?.Invoke();
//         battlePanel.SetActive(true);
//         openSkillPopupButton.SetActive(true);

//         yield return new WaitUntil(() => battleOver);

//         battlePanel.SetActive(false);
//         openSkillPopupButton.SetActive(false);
//         OnBattleEnd?.Invoke();
//         yield return new WaitForSeconds(delayBetweenSteps);
//     }

//     public void MarkBattleOver() => battleOver = true;

//     public void ToggleSkillPopupDuringBattle()
//     {
//         if (skillPopupOpen)
//         {
//             skillPopupPanel.SetActive(false);
//             Time.timeScale = 1f;
//             skillPopupOpen = false;
//         }
//         else
//         {
//             skillPopupPanel.SetActive(true);
//             Time.timeScale = 0f;
//             SetSkillPopupText("Use QQ, QW, or QE to activate different Q skills.");
//             skillPopupOpen = true;
//         }
//     }

//     public void SetVNDialog(string message)
//     {
//         if (dialogTextObject != null)
//         {
//             dialogPanel.SetActive(true);
//             dialogTextObject.GetComponent<Text>().text = message;
//         }
//     }

//     public void SetSkillPopupText(string message)
//     {
//         if (skillPopupHowToText != null)
//         {
//             skillPopupHowToText.SetActive(true);
//             skillPopupHowToText.GetComponent<Text>().text = message;
//         }
//     }
// }
