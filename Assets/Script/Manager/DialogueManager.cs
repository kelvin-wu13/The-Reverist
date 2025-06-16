// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class DialogueManager : MonoBehaviour
// {
//     public GameObject panel;
//     public Sprite icon;
//     public Text nameText;
//     public Text contentText;

//     [System.Serializable]
//     public class DialogueCharacter
//     {
//         public string name;
//         public Sprite icon;
//     }

//     [System.Serializable]
//     public class DialogueLine
//     {
//         public DialogueCharacter character;
//         [TextArea(2, 4)]
//         public string text;
//     }


//     [Header("Inspector Dialog")]
//     public List<DialogueLine> inspectorLines = new List<DialogueLine>();

//     private Queue<DialogueLine> lines = new Queue<DialogueLine>();

//     public void StartInspectorDialogue()
//     {
//         StartDialogue(inspectorLines);
//     }

//     public void StartDialogue(List<DialogueLine> dialog)
//     {
//         panel.SetActive(true);
//         lines.Clear();
//         foreach (var line in dialog)
//             lines.Enqueue(line);
//         DisplayNext();
//     }

//     public void DisplayNext()
//     {
//         if (lines.Count == 0)
//         {
//             panel.SetActive(false);
//             EventManager.Instance.ProceedAfterDialogue();
//             return;
//         }

//         DialogueLine line = lines.Dequeue();
//         nameText.text = line.speaker;
//         contentText.text = line.text;
//     }

//     void Update()
//     {
//         if (panel.activeSelf && Input.GetKeyDown(KeyCode.Space))
//             DisplayNext();
//     }
// }
