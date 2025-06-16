using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowController : MonoBehaviour
{
    public List<FlowStep> steps = new List<FlowStep>();
    private int currentStep = 0;

    void Start()
    {
        StartCoroutine(RunFlow());
    }

    IEnumerator RunFlow()
    {
        while (currentStep < steps.Count)
        {
            steps[currentStep].ExecuteStep();

            // Wait for player to press Space to continue
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

            currentStep++;
        }
    }
}
