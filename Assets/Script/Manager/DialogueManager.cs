using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;

    private Queue<DialogueLine> lines = new Queue<DialogueLine>();

    public bool isDialogueActive = false;
    public float isTypingSpeed = 0.2f;
    public Animator animator;

    public UnityEvent OnDialogueFinished; // ⬅ Add this for callback

    private void Start()
    {
        if (Instance == null)
            Instance = this;
    }

    public void StartDialogue(Dialogue dialogue)
    {
        Time.timeScale = 0f; // ⏸ pause game
        isDialogueActive = true;

        animator.Play("show");
        lines.Clear();

        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }
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
        animator.Play("hide");
        Time.timeScale = 1f; // ▶ resume game

        OnDialogueFinished?.Invoke(); // 🔁 trigger next step
    }
}
