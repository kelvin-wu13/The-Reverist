using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeathSceneManager : MonoBehaviour
{
    public float delayBeforeDeathScene = 2f;
    private bool isDead = false;

    public void HandlePlayerDeath()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player died. Waiting before showing death scene...");
        StartCoroutine(LoadDeathSceneAfterDelay());
    }

    private IEnumerator LoadDeathSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeDeathScene);
        SceneManager.LoadScene("DeathScene");
    }
}
