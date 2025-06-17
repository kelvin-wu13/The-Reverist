using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuNavigator : MonoBehaviour
{
    public Button[] buttons;
    private int currentIndex = -1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Navigate(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Navigate(1);
        }
    }

    void Navigate(int direction)
    {
        // If nothing selected yet, select first
        if (currentIndex == -1 || EventSystem.current.currentSelectedGameObject == null)
        {
            currentIndex = 0;
            buttons[currentIndex].Select();
            return;
        }

        // Move index with wraparound
        currentIndex = (currentIndex + direction + buttons.Length) % buttons.Length;
        buttons[currentIndex].Select();
    }
}