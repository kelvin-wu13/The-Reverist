using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayDemo()
    {
        SceneManager.LoadSceneAsync(2);
    }
}
