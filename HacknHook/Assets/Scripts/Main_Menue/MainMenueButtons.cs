using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenueButtons : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        Debug.Log("[ LOADED ] Loaded MainMenueButtons");
    }

    public void NewGameButtonPressed()
    {
        Debug.Log("[ BUTTONPRESSED ] New Game Button Pressed");
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void LoadGameButtonPressed()
    {
        Debug.Log("[ BUTTONPRESSED ] Load Game Button Pressed");
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
    public void OptionsButtonPressed()
    {
        Debug.Log("[ BUTTONPRESSED ] Options Button Pressed");
    }

    public void QuitGamePressed()
    {
        Debug.Log("[ BUTTONPRESSED ] Quit Game Button Pressed");
        Application.Quit();
    }

    // Update is called once per frame
    //void Update()
    //{
    //
    //}
}
