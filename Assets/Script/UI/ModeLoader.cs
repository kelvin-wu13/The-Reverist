using System.Collections;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;

    //void Update()
    //{
    //    if(Input.GetMouseButtonDown(0))
    //    {
    //        LoadNextMode();

    //    }
    //}

    public void LoadNextMode(string sceneName)
    {
        //StartCoroutine(LoadMode(SceneManager.GetActiveScene().buildIndex + 1));
        StartCoroutine(LoadMode(sceneName));

    }

    IEnumerator LoadMode(string sceneName)
    {
        //Play Animation
        transition.SetTrigger("Start");

        //Wait
        yield return new WaitForSeconds(transitionTime);

        //Load Scene
        SceneManager.LoadScene(sceneName);

    }

}
