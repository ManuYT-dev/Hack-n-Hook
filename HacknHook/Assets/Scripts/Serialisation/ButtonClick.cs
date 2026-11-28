using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClick : MonoBehaviour
{
    public void ButtonLoadClick()
    {
        if (!SerializeGame.Load())
        {
            MainMenueButtons buttons = new MainMenueButtons();
            buttons.NewGameButtonPressed();
        }
        else
        {
            SceneManager.LoadScene("Game");
        }
    }

    public void ButtonSaveClick()
    {
        if (!SerializeGame.Save())
        {
            Debug.Log("Couldn't save.");
        }
        else
        {
            Debug.Log("Successfully saved game.");
        }
    }
}
