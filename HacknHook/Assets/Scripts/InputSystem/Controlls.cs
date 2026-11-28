public static class Controlls
{
    #region Public Fields
    public static readonly InputSystem_Actions inputActions = new();
    public static readonly InputSystem_Actions.FishingRodActions fishingRodActions = inputActions.FishingRod;
    public static readonly InputSystem_Actions.BoatActions boatActions = inputActions.Boat;
    public static readonly InputSystem_Actions.GlobalActions globalActions = inputActions.Global;
    #endregion

    #region Public Funcs
    public static void Init()
    {
        BleControllerDevice.Init();
        Controlls.inputActions.Enable();
    }

    public static void Kill()
    {
        BlePacketReceiver.Shutdown();
        Controlls.inputActions.Disable();
        Controlls.inputActions.Dispose();
    }
    #endregion
}
