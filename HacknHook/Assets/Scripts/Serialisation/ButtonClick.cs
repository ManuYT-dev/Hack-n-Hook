using Newtonsoft.Json.Bson;
using UnityEngine;

public class ButtonClick : MonoBehaviour
{
    public void ButtonLoadClick()
    {
        if (!SerializeGame.Load())
        {
            MainMenueButtons buttons = new MainMenueButtons();
            buttons.NewGameButtonPressed();
        }
    }
}
