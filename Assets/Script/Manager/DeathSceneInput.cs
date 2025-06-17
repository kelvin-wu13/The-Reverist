using UnityEngine;
using System.Collections;


public class DeathSceneInput : MonoBehaviour
{
    public FadeController fadeController;
    private bool canContinue = false;

    void Start()
    {
        fadeController.FadeIn();
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
            fadeController.FadeOutAndLoadScene("0");
        }
    }
}
