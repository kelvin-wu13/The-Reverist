using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathSceneInput : MonoBehaviour
{
    private bool canContinue = false;

    void Start()
    {
        // Optional: delay input for dramatic effect
        Invoke(nameof(EnableContinue), 1.5f);
    }

    void EnableContinue()
    {
        canContinue = true;
    }

    void Update()
    {
        if (canContinue && Input.anyKeyDown)
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
