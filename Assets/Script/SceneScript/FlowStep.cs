using UnityEngine;
using UnityEngine.Events;

public class FlowStep : MonoBehaviour
{
    public UnityEvent onStepStart;

    public void ExecuteStep()
    {
        onStepStart?.Invoke();
    }
}
