using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private void Start()
    {
        this.pauseMenu = GameObject.Find("PauseMenu");
        this.pauseMenu.SetActive(false);
        this.paused = false;
        Controlls.globalActions.Enable();
        Controlls.globalActions.Pause.performed += ctx => this.Check();
    }
    public void ButtonSaveClick()
    {
        SerializeGame.Save();
    }

    public void ButtonMenuClick()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void ButtonExitClick()
    {
        Application.Quit();
    }

    public GameObject pauseMenu;

    private bool paused = false;

    private void Check()
    {
        if (this.paused)
            this.Resume();
        else
            this.Pause();
    }

    void Pause()
    {
        this.pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        this.paused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        this.pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        this.paused = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
