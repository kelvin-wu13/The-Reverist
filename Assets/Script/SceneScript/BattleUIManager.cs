using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    public GameObject dialogPanel;
    public Text dialogText;

    public GameObject popupPanel;
    public Text popupText;

    public void ShowDialog(string message)
    {
        dialogPanel.SetActive(true);
        dialogText.text = message;
    }

    public void ShowSkillPopup(string message)
    {
        popupPanel.SetActive(true);
        popupText.text = message;
    }

    public void HideAllUI()
    {
        dialogPanel.SetActive(false);
        popupPanel.SetActive(false);
    }
}
