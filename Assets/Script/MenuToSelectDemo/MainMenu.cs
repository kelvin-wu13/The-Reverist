using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
    
    public void CharacterSelectDemo()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void SkillArsenalTrainingDemo()
    {
        SceneManager.LoadSceneAsync(2);
    }

    public void SkillSaberTrainingDemo()
    {
        SceneManager.LoadSceneAsync(3);
    }

    public void SkillInterfereTrainingDemo()
    {
        SceneManager.LoadSceneAsync(4);
    }
}
