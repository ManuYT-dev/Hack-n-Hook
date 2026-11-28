using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadShopScenes : MonoBehaviour
{
    public void ChangeSceneToUtilShop()
    {
        SceneManager.LoadScene("Main Menu");
        Application.Quit();
    }

    public void ChangeSceneToBaitShop()
    {
        SceneManager.LoadScene("Main Menu");
        Application.Quit();
    }

    public void ChangeSceneToMotorShop()
    {
        SceneManager.LoadScene("Main Menu");
        Application.Quit();
    }
}
