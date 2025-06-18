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
        fadeController.FadeOutAndLoadScene("Main Menu Demo");
    }

    public void CharacterSelectDemo()
    {
        fadeController.FadeOutAndLoadScene("Character Select Demo"); // update name if needed
    }

    public void SkillArsenalDemo()
    {
        fadeController.FadeOutAndLoadScene("ArsenalDemo"); // update name if needed
    }

    public void SkillSaberDemo()
    {
        fadeController.FadeOutAndLoadScene("SaberDemo"); // update name if needed
    }

    public void SkillInterfereDemo()
    {
        fadeController.FadeOutAndLoadScene("InterfereDemo"); // update name if needed
    }

    public void TrainingSceneDemo()
    {
        fadeController.FadeOutAndLoadScene("TrainingScene"); // update name if needed
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
