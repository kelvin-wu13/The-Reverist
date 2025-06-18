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

    public void SkillArsenalDemo()
    {
        fadeController.FadeOutAndLoadScene("2"); // update name if needed
    }

    public void SkillSaberDemo()
    {
        fadeController.FadeOutAndLoadScene("3"); // update name if needed
    }

    public void SkillInterfereDemo()
    {
        fadeController.FadeOutAndLoadScene("4"); // update name if needed
    }

    public void TrainingSceneDemo()
    {
        fadeController.FadeOutAndLoadScene("5"); // update name if needed
    }
}
