using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[StructLayout(LayoutKind.Explicit, Size = 20)]
public struct BleControllerState : IInputStateTypeInfo
{
    #region Format
    public static FourCC Format => new FourCC('B', 'L', 'E', 'C');
    public FourCC format => Format;
    #endregion

    #region State Fields
    [InputControl(name = "rell", layout = "Button", bit = 0)]
    [InputControl(name = "throw", layout = "Button", bit = 1)]
    [InputControl(name = "cancelCast", layout = "Button", bit = 4)]
    [InputControl(name = "switchBait", layout = "Button", bit = 5)]
    [InputControl(name = "anchor", layout = "Button", bit = 6)]
    [InputControl(name = "interact", layout = "Button", bit = 7)]
    [InputControl(name = "toggleFishing", layout = "Button", bit = 8)]
    [InputControl(name = "boost", layout = "Button", bit = 9)]
    [InputControl(name = "pause", layout = "Button", bit = 10)]
    [InputControl(name = "toggleReady", layout = "Button", bit = 11)]
    [FieldOffset(0)]
    public uint buttons;

    [InputControl(name = "aim", layout = "Vector2")]
    [FieldOffset(4)]
    public Vector2 aim;

    [InputControl(name = "drive", layout = "Axis")]
    [FieldOffset(12)]
    public float drive;

    [InputControl(name = "castSpeed", layout = "Axis")]
    [FieldOffset(16)]
    public float castSpeed;
    #endregion
}

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
[InputControlLayout(stateType = typeof(BleControllerState), displayName = "BLE Controller")]
public class BleControllerDevice : InputDevice
{
    #region Properties
    public static BleControllerDevice current { get; private set; }

    public ButtonControl rell { get; private set; }
    public ButtonControl throwButton { get; private set; }
    public Vector2Control aim { get; private set; }
    public ButtonControl cancelCast { get; private set; }
    public ButtonControl switchBait { get; private set; }
    public AxisControl drive { get; private set; }
    public ButtonControl anchor { get; private set; }
    public ButtonControl interact { get; private set; }
    public ButtonControl toggleFishing { get; private set; }
    public ButtonControl boost { get; private set; }
    public ButtonControl pause { get; private set; }
    public ButtonControl toggleReady { get; private set; }
    public AxisControl castSpeed { get; private set; }
    #endregion

    #region Private Variables
    private BleControllerState _state;
    #endregion

    #region Initialization
    static BleControllerDevice() => RegisterLayout();

    public static void RegisterLayout() => InputSystem.RegisterLayout<BleControllerDevice>();

    public static BleControllerDevice Init()
    {
        RegisterLayout();
        current = InputSystem.GetDevice<BleControllerDevice>() ?? InputSystem.AddDevice<BleControllerDevice>();
        return current;
    }
    #endregion

    #region InputDevice Overrides
    protected override void FinishSetup()
    {
        base.FinishSetup();
        this.rell = this.GetChildControl<ButtonControl>("rell");
        this.throwButton = this.GetChildControl<ButtonControl>("throw");
        this.aim = this.GetChildControl<Vector2Control>("aim");
        this.cancelCast = this.GetChildControl<ButtonControl>("cancelCast");
        this.switchBait = this.GetChildControl<ButtonControl>("switchBait");
        this.drive = this.GetChildControl<AxisControl>("drive");
        this.anchor = this.GetChildControl<ButtonControl>("anchor");
        this.interact = this.GetChildControl<ButtonControl>("interact");
        this.toggleFishing = this.GetChildControl<ButtonControl>("toggleFishing");
        this.boost = this.GetChildControl<ButtonControl>("boost");
        this.pause = this.GetChildControl<ButtonControl>("pause");
        this.toggleReady = this.GetChildControl<ButtonControl>("toggleReady");
        this.castSpeed = this.GetChildControl<AxisControl>("castSpeed");
    }

    protected override void OnAdded() => current = this;

    protected override void OnRemoved()
    {
        if (current == this) current = null;
    }
    #endregion

    #region Public Funcs
    public void ApplyPacket(byte[] buf, int size)
    {
        if (buf == null || size < 1) return;

        byte header = buf[0];
        int type = header & 0b11;
        int map = (header >> 2) & 0b11;
        int action = (header >> 4) & 0xF;

        if (type <= 1)
        {
            int bit = GetButtonBit(map, action);
            if (bit < 0)
            {
                Debug.LogWarning($"[BLE] Unknown button packet: map={map}, action={action}");
                return;
            }

            uint mask = 1u << bit;
            if (type == 1) this._state.buttons |= mask;
            else this._state.buttons &= ~mask;
        }
        else if (type == 2)
        {
            if (size < 3)
            {
                Debug.LogWarning($"[BLE] Invalid 1D packet: size={size}, map={map}, action={action}");
                return;
            }

            if (map == 2 && action == 0)
            {
                this._state.drive = ReadNormalizedInt16(buf, 1);
            }
            else if (map == 1 && action == 8)
            {
                this._state.castSpeed = ReadNormalizedInt16(buf, 1);
            }
            else
            {
                Debug.LogWarning($"[BLE] Invalid 1D packet: size={size}, map={map}, action={action}");
                return;
            }
        }
        else
        {
            if (size < 5 || map != 1 || action != 2)
            {
                Debug.LogWarning($"[BLE] Invalid 2D packet: size={size}, map={map}, action={action}");
                return;
            }

            this._state.aim = new Vector2(ReadNormalizedInt16(buf, 1), ReadNormalizedInt16(buf, 3));
        }

        InputSystem.QueueStateEvent(this, this._state);
    }
    #endregion

    #region Private Funcs
    private static float ReadNormalizedInt16(byte[] buf, int offset)
    {
        short raw = (short)(buf[offset] | (buf[offset + 1] << 8));
        return Mathf.Clamp(raw / 32767f, -1f, 1f);
    }

    private static int GetButtonBit(int map, int action) => (map, action) switch
    {
        (1, 0) => 0,  // FishingRod: Rell
        (1, 1) => 1,  // FishingRod: Throw
        (1, 5) => 4,  // FishingRod: CancelCast
        (1, 6) => 5,  // FishingRod: SwitchBait
        (1, 7) => 11, // FishingRod: ToggleReady
        (2, 1) => 6,  // Boat: Anchor
        (2, 2) => 7,  // Boat: Interact
        (2, 3) => 8,  // Boat: ToggleFishing
        (2, 4) => 9,  // Boat: Boost
        (3, 0) => 10, // Global: Pause
        _ => -1
    };
    #endregion
}
