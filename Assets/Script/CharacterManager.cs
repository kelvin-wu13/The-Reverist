using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterManager : MonoBehaviour
{
    public CharacterDatabase characterDB;

    public TMP_Text nameText;
    public SpriteRenderer artworkSprite;

    private int selectedOption = 0;

    void Start()
    {
        UpdateCharacter(selectedOption);
    }

    public void NextOption() 
    {
        selectedOption++;

        if (selectedOption >= characterDB.CharacterCount)
        {
            selectedOption = 0;
        }

        UpdateCharacter(selectedOption);
    }

    public void BackOption()
    {
        selectedOption--;

        if (selectedOption < 0)
        {
            selectedOption = characterDB.CharacterCount - 1;
        }

        UpdateCharacter(selectedOption);
    }

    public void SelectCharacter(int index)
    {
        selectedOption = index;
        UpdateCharacter(selectedOption);
    }

    private void UpdateCharacter(int selectedOption)
    {
        Character character = characterDB.GetCharacter(selectedOption);
        artworkSprite.sprite = character.characterSprite;
        nameText.text = character.characterName;
    }
}
