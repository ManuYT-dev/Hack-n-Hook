using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameInit
{
    #region Public Funcs
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        if (SceneManager.GetActiveScene().name == "UnitTests")
            return;

        DefaultSettings.Init();
        Controlls.Init();
        BlePacketReceiver.Init();
        Application.quitting += GameInit.Closing;
    }

    public static void Closing()
    {
        Application.quitting -= GameInit.Closing;
        Controlls.Kill();
    }
    #endregion
}
