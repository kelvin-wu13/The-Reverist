using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void CharacterSelectDemo()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void SkillQTrainingDemo()
    {
        SceneManager.LoadSceneAsync(2);
    }
}
