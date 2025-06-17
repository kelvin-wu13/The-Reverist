using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    public static DialogueManager Instance;

    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    private Queue<DialogueLine> lines = new Queue<DialogueLine>();

    public bool isDialogueActive = false;
    public float isTypingSpeed = 0.2f;
    //public Animator animator;

    public UnityEvent OnDialogueFinished; // ⬅ Add this for callback

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Update()
    {
        if (isDialogueActive && (Input.GetKeyDown(KeyCode.Space)) || (Input.GetMouseButtonDown(0)))
        {
            DisplayNextDialogueLine();
        }
    } 

    public void StartDialogue(Dialogue dialogue)
    {
        Time.timeScale = 0f;
        isDialogueActive = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true); // 👈 auto show panel

        lines.Clear();
        foreach (DialogueLine line in dialogue.dialogueLines)
            lines.Enqueue(line);

        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = lines.Dequeue();

        characterIcon.sprite = currentLine.character.icon;
        characterName.text = currentLine.character.name;

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        dialogueArea.text = "";
        foreach (char letter in dialogueLine.text.ToCharArray())
        {
            dialogueArea.text += letter;
            yield return new WaitForSecondsRealtime(isTypingSpeed);
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false); // 👈 auto hide panel

        Time.timeScale = 1f;
        OnDialogueFinished?.Invoke();
    }
}
