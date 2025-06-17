using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public FadeController fadeController;

    private void Awake()
    {
        if (fadeController == null)
            Debug.LogWarning("FadeController not assigned in MainMenu.");
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1f;
        fadeController.FadeOutAndLoadScene("0");
    }

    public void CharacterSelectDemo()
    {
        fadeController.FadeOutAndLoadScene("1"); // update name if needed
    }

    public void SkillArsenalTrainingDemo()
    {
        fadeController.FadeOutAndLoadScene("2"); // update name if needed
    }

    public void SkillSaberTrainingDemo()
    {
        fadeController.FadeOutAndLoadScene("3"); // update name if needed
    }

    public void SkillInterfereTrainingDemo()
    {
        fadeController.FadeOutAndLoadScene("4"); // update name if needed
    }
}
