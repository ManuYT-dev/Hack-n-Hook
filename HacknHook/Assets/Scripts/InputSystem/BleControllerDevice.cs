using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct BleControllerState : IInputStateTypeInfo
{
    #region Format
    public static FourCC Format => new FourCC('B', 'L', 'E', 'C');
    public FourCC format => Format;
    #endregion

    #region State Fields
    [InputControl(name = "toggleReady", layout = "Button", bit = 0)]
    [InputControl(name = "buttonPressed", layout = "Button", bit = 1)]
    [FieldOffset(0)]
    public uint buttons;

    [InputControl(name = "rell", layout = "Axis")]
    [FieldOffset(4)]
    public float rell;

    [InputControl(name = "castSpeed", layout = "Axis")]
    [FieldOffset(8)]
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

    public AxisControl rell { get; private set; }
    public ButtonControl toggleReady { get; private set; }
    public AxisControl castSpeed { get; private set; }
    public ButtonControl buttonPressed { get; private set; }
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
        this.rell = this.GetChildControl<AxisControl>("rell");
        this.toggleReady = this.GetChildControl<ButtonControl>("toggleReady");
        this.castSpeed = this.GetChildControl<AxisControl>("castSpeed");
        this.buttonPressed = this.GetChildControl<ButtonControl>("buttonPressed");
    }

    protected override void OnAdded() => current = this;

    protected override void OnRemoved()
    {
        if (current == this) current = null;
    }
    #endregion

    #region Constants
    private const int ModusRell = 0;      // 0b00
    private const int ModusCastSpeed = 1; // 0b01
    private const int ModusCancel = 3;    // 0b11

    public static float ReadyCastSpeedMinimum { get; set; } = 0.25f;
    public static float CastSpeedScale { get; set; } = 2.0f;
    #endregion

    #region Public Funcs
    public void ApplyPacket(byte[] buf, int size)
    {
        if (buf == null || size < 1) return;

        byte header = buf[0];
        bool active = (header & 0b1) != 0;
        int modus = (header >> 1) & 0b11;

        switch (modus)
        {
            case ModusRell:
                if (!this.TryReadAxisInt16(buf, size, active, normalized: false, out float rellValue)) return;
                this._state.rell = rellValue;
                break;

            case ModusCastSpeed:
                if (!this.TryReadAxisInt16(buf, size, active, normalized: true, out float rawCastSpeed)) return;
                float castSpeedValue = Mathf.Clamp(rawCastSpeed * CastSpeedScale, -1f, 1f);
                this._state.castSpeed = castSpeedValue;
                this.SetButtonBit(0, castSpeedValue >= ReadyCastSpeedMinimum);
                break;

            case ModusCancel:
                this.SetButtonBit(1, active);
                break;

            default:
                Debug.LogWarning($"[BLE] Unsupported modus: modus={modus}, header=0x{header:X2}");
                return;
        }

        InputSystem.QueueStateEvent(this, this._state);
    }
    #endregion

    #region Private Funcs
    private void SetButtonBit(int bit, bool active)
    {
        uint mask = 1u << bit;
        if (active) this._state.buttons |= mask;
        else this._state.buttons &= ~mask;
    }

    private bool TryReadAxisInt16(byte[] buf, int size, bool active, bool normalized, out float value)
    {
        if (!active)
        {
            value = 0f;
            return true;
        }

        if (size < 3)
        {
            Debug.LogWarning($"[BLE] Invalid int16 packet size: size={size}, header=0x{buf[0]:X2}");
            value = 0f;
            return false;
        }

        short raw = (short)(buf[1] | (buf[2] << 8));
        value = normalized ? Mathf.Clamp(raw / 32767f, -1f, 1f) : Mathf.Clamp(raw, -1f, 1f);
        return true;
    }
    #endregion
}
