using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public class InputUnitTests : MonoBehaviour
{
    private readonly List<string> _results = new();
    private int _passed;
    private int _failed;
    private Vector2 _scrollPos;
    private System.Random _rng;

    private IEnumerator Start()
    {
        yield return null;
        this.RunAllTests();
    }

    [ContextMenu("Re-run Input Unit Tests")]
    public void RunAllTests()
    {
        this._results.Clear();
        this._passed = 0;
        this._failed = 0;
        this._rng = new System.Random();

        Controlls.Init();
        ReadOnlyArray<InputDevice>? previousDevices = Controlls.inputActions.devices;
        var prevBg = InputSystem.settings.backgroundBehavior;
        var prevEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;

        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;

        List<InputDevice> disabledRealDevices = new();
        foreach (InputDevice dev in InputSystem.devices)
        {
            if (dev is not BleControllerDevice)
            {
                InputSystem.DisableDevice(dev);
                InputSystem.ResetDevice(dev, alsoResetDontResetControls: true);
                disabledRealDevices.Add(dev);
            }
        }
        InputSystem.Update();

        try
        {
            this.RunLaptopTests(previousDevices);
            this.RunBluetoothTests(previousDevices);
        }
        finally
        {
            foreach (InputDevice dev in disabledRealDevices)
            {
                if (dev.added)
                {
                    InputSystem.EnableDevice(dev);
                }
            }

            InputSystem.settings.backgroundBehavior = prevBg;
            InputSystem.settings.editorInputBehaviorInPlayMode = prevEditorInput;
            InputSystem.Update();
        }

        string summary = $"[InputUnitTests] Finished: {this._passed} passed, {this._failed} failed.";
        if (this._failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    #region Part 1: Normal Inputs (Laptop: Keyboard, Mouse, Gamepad)

    private void RunLaptopTests(ReadOnlyArray<InputDevice>? previousDevices)
    {
        Mouse vMouse = InputSystem.AddDevice<Mouse>("UnitTestMouse");
        Keyboard vKeyboard = InputSystem.AddDevice<Keyboard>("UnitTestKeyboard");
        Gamepad vGamepad = InputSystem.AddDevice<Gamepad>("UnitTestGamepad");

        try
        {
            Controlls.inputActions.Disable();
            Controlls.inputActions.devices = new ReadOnlyArray<InputDevice>(
                new InputDevice[] { vMouse, vKeyboard, vGamepad }
            );
            Controlls.inputActions.Enable();
            InputSystem.ResetDevice(vMouse, alsoResetDontResetControls: true);
            Controlls.fishingRodActions.Aim.Reset();
            InputSystem.Update();

            this.RunKeyboardAndMouseTests(vMouse, vKeyboard);
            ResetLaptopDevices(vMouse, vKeyboard, vGamepad);
            InputSystem.ResetDevice(vMouse, alsoResetDontResetControls: true);
            Controlls.fishingRodActions.Aim.Reset();
            InputSystem.Update();
            this.RunGamepadTests(vGamepad);
        }
        finally
        {
            ResetLaptopDevices(vMouse, vKeyboard, vGamepad);
            Controlls.inputActions.Disable();
            Controlls.inputActions.devices = previousDevices;
            Controlls.inputActions.Enable();
            InputSystem.RemoveDevice(vMouse);
            InputSystem.RemoveDevice(vKeyboard);
            InputSystem.RemoveDevice(vGamepad);
            InputSystem.Update();
        }
    }

    private void RunKeyboardAndMouseTests(Mouse mouse, Keyboard keyboard)
    {
        var rod = Controlls.fishingRodActions;
        var boat = Controlls.boatActions;
        var global = Controlls.globalActions;

        this.TestButton("Mouse.Right -> FishingRod.Rell", rod.Rell,
            () => SendMouse(mouse, new MouseState().WithButton(MouseButton.Right, true)),
            () => SendMouse(mouse, new MouseState()));

        this.TestButton("Mouse.Left -> FishingRod.Throw", rod.Throw,
            () => SendMouse(mouse, new MouseState().WithButton(MouseButton.Left, true)),
            () => SendMouse(mouse, new MouseState()));

        Vector2 randomPos = new(this._rng.Next(50, 1920), this._rng.Next(50, 1080));
        this.TestVector2("Mouse.Position -> FishingRod.Aim", rod.Aim,
            () => SendMouse(mouse, new MouseState { position = randomPos }),
            () => SendMouse(mouse, new MouseState { position = Vector2.zero }),
            randomPos, 0.01f);

        this.TestKeyButton("Key.Space -> FishingRod.Strike", rod.Strike, keyboard, Key.Space);
        this.TestKeyButton("Key.LeftShift -> FishingRod.Slack", rod.Slack, keyboard, Key.LeftShift);
        this.TestKeyButton("Key.C -> FishingRod.CancelCast", rod.CancelCast, keyboard, Key.C);
        this.TestKeyButton("Key.Q -> FishingRod.SwitchBait", rod.SwitchBait, keyboard, Key.Q);

        this.TestAxis("Key.A -> Boat.Drive (-1)", boat.Drive,
            () => SendKeyboard(keyboard, new KeyboardState(Key.A)),
            () => SendKeyboard(keyboard, new KeyboardState()),
            v => Mathf.Approximately(v, -1f));
        this.TestAxis("Key.D -> Boat.Drive (+1)", boat.Drive,
            () => SendKeyboard(keyboard, new KeyboardState(Key.D)),
            () => SendKeyboard(keyboard, new KeyboardState()),
            v => Mathf.Approximately(v, 1f));
        this.TestAxis("Key.LeftArrow -> Boat.Drive (-1)", boat.Drive,
            () => SendKeyboard(keyboard, new KeyboardState(Key.LeftArrow)),
            () => SendKeyboard(keyboard, new KeyboardState()),
            v => Mathf.Approximately(v, -1f));
        this.TestAxis("Key.RightArrow -> Boat.Drive (+1)", boat.Drive,
            () => SendKeyboard(keyboard, new KeyboardState(Key.RightArrow)),
            () => SendKeyboard(keyboard, new KeyboardState()),
            v => Mathf.Approximately(v, 1f));

        this.TestKeyButton("Key.S -> Boat.Anchor", boat.Anchor, keyboard, Key.S);
        this.TestKeyButton("Key.E -> Boat.Interact", boat.Interact, keyboard, Key.E);
        this.TestKeyButton("Key.F -> Boat.ToggleFishing", boat.ToggleFishing, keyboard, Key.F);
        this.TestKeyButton("Key.LeftShift -> Boat.Boost", boat.Boost, keyboard, Key.LeftShift);
        this.TestKeyButton("Key.Escape -> Global.Pause", global.Pause, keyboard, Key.Escape);
    }

    private void RunGamepadTests(Gamepad gamepad)
    {
        var rod = Controlls.fishingRodActions;
        var boat = Controlls.boatActions;
        var global = Controlls.globalActions;

        this.TestGamepadButton("Gamepad.South -> FishingRod.Rell", rod.Rell, gamepad, GamepadButton.South);
        this.TestButton("Gamepad.RightTrigger -> FishingRod.Throw", rod.Throw,
            () => SendGamepad(gamepad, new GamepadState { rightTrigger = 1f }),
            () => SendGamepad(gamepad, new GamepadState()));

        Vector2 randomStick = new(this.RandomSign() * this._rng.Next(350, 950) / 1000f, this.RandomSign() * this._rng.Next(350, 950) / 1000f);
        this.TestVector2("Gamepad.RightStick -> FishingRod.Aim", rod.Aim,
            () => SendGamepad(gamepad, new GamepadState { rightStick = randomStick }),
            () => SendGamepad(gamepad, new GamepadState()),
            null, 0f, actual => actual.sqrMagnitude > 0.01f);

        this.TestGamepadButton("Gamepad.West -> FishingRod.Strike", rod.Strike, gamepad, GamepadButton.West);
        this.TestButton("Gamepad.LeftTrigger -> FishingRod.Slack", rod.Slack,
            () => SendGamepad(gamepad, new GamepadState { leftTrigger = 1f }),
            () => SendGamepad(gamepad, new GamepadState()));
        this.TestGamepadButton("Gamepad.East -> FishingRod.CancelCast", rod.CancelCast, gamepad, GamepadButton.East);
        this.TestGamepadButton("Gamepad.DpadRight -> FishingRod.SwitchBait", rod.SwitchBait, gamepad, GamepadButton.DpadRight);

        this.TestAxis("Gamepad.LeftStick.Left -> Boat.Drive (-1)", boat.Drive,
            () => SendGamepad(gamepad, new GamepadState { leftStick = new Vector2(-1f, 0f) }),
            () => SendGamepad(gamepad, new GamepadState()),
            v => Mathf.Approximately(v, -1f));
        this.TestAxis("Gamepad.LeftStick.Right -> Boat.Drive (+1)", boat.Drive,
            () => SendGamepad(gamepad, new GamepadState { leftStick = new Vector2(1f, 0f) }),
            () => SendGamepad(gamepad, new GamepadState()),
            v => Mathf.Approximately(v, 1f));

        this.TestGamepadButton("Gamepad.East -> Boat.Anchor", boat.Anchor, gamepad, GamepadButton.East);
        this.TestGamepadButton("Gamepad.South -> Boat.Interact", boat.Interact, gamepad, GamepadButton.South);
        this.TestGamepadButton("Gamepad.North -> Boat.ToggleFishing", boat.ToggleFishing, gamepad, GamepadButton.North);
        this.TestButton("Gamepad.LeftTrigger -> Boat.Boost", boat.Boost,
            () => SendGamepad(gamepad, new GamepadState { leftTrigger = 1f }),
            () => SendGamepad(gamepad, new GamepadState()));
        this.TestGamepadButton("Gamepad.Start -> Global.Pause", global.Pause, gamepad, GamepadButton.Start);
    }

    private void TestKeyButton(string label, InputAction action, Keyboard keyboard, Key key) =>
        this.TestButton(label, action,
            () => SendKeyboard(keyboard, new KeyboardState(key)),
            () => SendKeyboard(keyboard, new KeyboardState()));

    private void TestGamepadButton(string label, InputAction action, Gamepad gamepad, GamepadButton button) =>
        this.TestButton(label, action,
            () => SendGamepad(gamepad, new GamepadState(button)),
            () => SendGamepad(gamepad, new GamepadState()));

    private static void ResetLaptopDevices(Mouse mouse, Keyboard keyboard, Gamepad gamepad)
    {
        SendMouse(mouse, new MouseState());
        SendKeyboard(keyboard, new KeyboardState());
        SendGamepad(gamepad, new GamepadState());
        InputSystem.Update();
    }

    private static void SendMouse(Mouse device, MouseState state) => InputSystem.QueueStateEvent(device, state);
    private static void SendKeyboard(Keyboard device, KeyboardState state) => InputSystem.QueueStateEvent(device, state);
    private static void SendGamepad(Gamepad device, GamepadState state) => InputSystem.QueueStateEvent(device, state);

    #endregion

    #region Part 2: Bluetooth Inputs (BLE)

    private void RunBluetoothTests(ReadOnlyArray<InputDevice>? previousDevices)
    {
        BleControllerDevice ble = BleControllerDevice.current ?? BleControllerDevice.Init();

        try
        {
            Controlls.inputActions.Disable();
            Controlls.inputActions.devices = new ReadOnlyArray<InputDevice>(new InputDevice[] { ble });
            Controlls.inputActions.Enable();
            Controlls.fishingRodActions.Aim.Reset();
            InputSystem.Update();

            this.RunBleTests(ble);
        }
        finally
        {
            ResetBluetoothDevice(ble);
            Controlls.inputActions.Disable();
            Controlls.inputActions.devices = previousDevices;
            Controlls.inputActions.Enable();
            InputSystem.Update();
        }
    }

    private void RunBleTests(BleControllerDevice ble)
    {
        var rod = Controlls.fishingRodActions;
        var boat = Controlls.boatActions;
        var global = Controlls.globalActions;

        this.TestBleButton("BLE FishingRod.Rell", rod.Rell, ble, 1, 0, d => d.rell);
        this.TestBleButton("BLE FishingRod.Throw", rod.Throw, ble, 1, 1, d => d.throwButton);
        this.TestBleAim(ble, rod.Aim);
        this.TestBleButton("BLE FishingRod.Strike", rod.Strike, ble, 1, 3, d => d.strike);
        this.TestBleButton("BLE FishingRod.Slack", rod.Slack, ble, 1, 4, d => d.slack);
        this.TestBleButton("BLE FishingRod.CancelCast", rod.CancelCast, ble, 1, 5, d => d.cancelCast);
        this.TestBleButton("BLE FishingRod.SwitchBait", rod.SwitchBait, ble, 1, 6, d => d.switchBait);

        this.TestBleDrive(ble, boat.Drive);
        this.TestBleButton("BLE Boat.Anchor", boat.Anchor, ble, 2, 1, d => d.anchor);
        this.TestBleButton("BLE Boat.Interact", boat.Interact, ble, 2, 2, d => d.interact);
        this.TestBleButton("BLE Boat.ToggleFishing", boat.ToggleFishing, ble, 2, 3, d => d.toggleFishing);
        this.TestBleButton("BLE Boat.Boost", boat.Boost, ble, 2, 4, d => d.boost);

        this.TestBleButton("BLE Global.Pause", global.Pause, ble, 3, 0, d => d.pause);
    }

    private void TestBleButton(string label, InputAction action, BleControllerDevice ble, byte map, byte actionId, Func<BleControllerDevice, ButtonControl> control)
    {
        using var probe = new ActionProbe(action);

        ble.ApplyPacket(MakeButtonPacket(true, map, actionId), 1);
        InputSystem.Update();
        bool pressedOk = probe.Started == 1 && probe.Performed == 1 && control(ble).isPressed;

        ble.ApplyPacket(MakeButtonPacket(false, map, actionId), 1);
        InputSystem.Update();
        bool releasedOk = probe.Canceled == 1 && !control(ble).isPressed;

        this.Record(label, pressedOk && releasedOk,
            $"started={probe.Started}, performed={probe.Performed}, canceled={probe.Canceled}");
    }

    private void TestBleAim(BleControllerDevice ble, InputAction aimAction)
    {
        short rx = this.RandomNonZeroInt16();
        short ry = this.RandomNonZeroInt16();
        Vector2 expected = new(rx / 32767f, ry / 32767f);

        this.TestVector2($"BLE FishingRod.Aim random ({rx}, {ry})", aimAction,
            () => ble.ApplyPacket(Make2DPacket(1, 2, rx, ry), 5),
            () => ble.ApplyPacket(Make2DPacket(1, 2, 0, 0), 5),
            expected, 0.001f);

        this.TestVector2("BLE FishingRod.Aim min clamp (-32768, -32768)", aimAction,
            () => ble.ApplyPacket(Make2DPacket(1, 2, short.MinValue, short.MinValue), 5),
            () => ble.ApplyPacket(Make2DPacket(1, 2, 0, 0), 5),
            new Vector2(-1f, -1f), 0.001f);
    }

    private void TestBleDrive(BleControllerDevice ble, InputAction driveAction)
    {
        short r = this.RandomNonZeroInt16();
        float expected = r / 32767f;

        this.TestAxis($"BLE Boat.Drive random ({r})", driveAction,
            () => ble.ApplyPacket(Make1DPacket(2, 0, r), 3),
            () => ble.ApplyPacket(Make1DPacket(2, 0, 0), 3),
            v => Mathf.Abs(v - expected) < 0.001f);

        this.TestAxis("BLE Boat.Drive min clamp (-32768)", driveAction,
            () => ble.ApplyPacket(Make1DPacket(2, 0, short.MinValue), 3),
            () => ble.ApplyPacket(Make1DPacket(2, 0, 0), 3),
            v => Mathf.Approximately(v, -1f));
    }

    private static void ResetBluetoothDevice(BleControllerDevice ble)
    {
        for (byte actionId = 0; actionId <= 6; actionId++)
        {
            if (actionId == 2) ble.ApplyPacket(Make2DPacket(1, 2, 0, 0), 5);
            else ble.ApplyPacket(MakeButtonPacket(false, 1, actionId), 1);
        }
        ble.ApplyPacket(Make1DPacket(2, 0, 0), 3);
        for (byte actionId = 1; actionId <= 4; actionId++)
            ble.ApplyPacket(MakeButtonPacket(false, 2, actionId), 1);
        ble.ApplyPacket(MakeButtonPacket(false, 3, 0), 1);
        InputSystem.Update();
    }

    private short RandomNonZeroInt16() => (short)(this.RandomSign() * this._rng.Next(2000, 32768));

    private static byte[] MakeButtonPacket(bool pressed, byte map, byte action) =>
        new[] { (byte)((pressed ? 1 : 0) | ((map & 0b11) << 2) | ((action & 0xF) << 4)) };

    private static byte[] Make1DPacket(byte map, byte action, short value) =>
        new[]
        {
            (byte)(0b10 | ((map & 0b11) << 2) | ((action & 0xF) << 4)),
            (byte)(value & 0xFF),
            (byte)((value >> 8) & 0xFF)
        };

    private static byte[] Make2DPacket(byte map, byte action, short x, short y) =>
        new[]
        {
            (byte)(0b11 | ((map & 0b11) << 2) | ((action & 0xF) << 4)),
            (byte)(x & 0xFF),
            (byte)((x >> 8) & 0xFF),
            (byte)(y & 0xFF),
            (byte)((y >> 8) & 0xFF)
        };
    #endregion

    #region Shared Test Helpers & UI
    private void TestButton(string label, InputAction action, Action press, Action release)
    {
        using var probe = new ActionProbe(action);

        press();
        InputSystem.Update();
        bool activeOk = probe.Started == 1 && probe.Performed == 1;

        release();
        InputSystem.Update();
        bool releaseOk = probe.Canceled == 1;

        this.Record(label, activeOk && releaseOk,
            $"started={probe.Started}, performed={probe.Performed}, canceled={probe.Canceled}");
    }

    private void TestAxis(string label, InputAction action, Action activate, Action reset, Func<float, bool> verify)
    {
        using var probe = new ActionProbe(action);

        activate();
        InputSystem.Update();
        float actual = action.ReadValue<float>();
        bool activeOk = probe.Started == 1 && probe.Performed == 1 && verify(actual);

        reset();
        InputSystem.Update();
        bool resetOk = probe.Canceled == 1 && Mathf.Approximately(action.ReadValue<float>(), 0f);

        this.Record(label, activeOk && resetOk,
            $"val={actual:F3}, started={probe.Started}, performed={probe.Performed}, canceled={probe.Canceled}");
    }

    private void TestVector2(
        string label,
        InputAction action,
        Action activate,
        Action reset,
        Vector2? expected,
        float tolerance,
        Func<Vector2, bool> customCheck = null)
    {
        using var probe = new ActionProbe(action);

        activate();
        InputSystem.Update();
        Vector2 actual = action.ReadValue<Vector2>();
        bool valueOk = customCheck != null
            ? customCheck(actual)
            : expected.HasValue && Vector2.Distance(actual, expected.Value) <= tolerance;
        bool activeOk = probe.Started == 1 && probe.Performed == 1 && valueOk;

        reset();
        InputSystem.Update();
        bool resetOk = probe.Canceled == 1 && action.ReadValue<Vector2>() == Vector2.zero;

        this.Record(label, activeOk && resetOk,
            $"val={actual}, started={probe.Started}, performed={probe.Performed}, canceled={probe.Canceled}");
    }

    private void Record(string label, bool ok, string details)
    {
        string line = ok ? $"[PASS] {label} ({details})" : $"[FAIL] {label} ({details})";
        this._results.Add(line);
        if (ok)
        {
            this._passed++;
            Debug.Log(line);
        }
        else
        {
            this._failed++;
            Debug.LogError(line);
        }
    }

    private int RandomSign() => this._rng.Next(2) == 0 ? -1 : 1;

    private sealed class ActionProbe : IDisposable
    {
        private readonly InputAction _action;
        public int Started { get; private set; }
        public int Performed { get; private set; }
        public int Canceled { get; private set; }

        public ActionProbe(InputAction action)
        {
            this._action = action;
            this._action.started += this.OnStarted;
            this._action.performed += this.OnPerformed;
            this._action.canceled += this.OnCanceled;
        }

        private void OnStarted(InputAction.CallbackContext _) => this.Started++;
        private void OnPerformed(InputAction.CallbackContext _) => this.Performed++;
        private void OnCanceled(InputAction.CallbackContext _) => this.Canceled++;

        public void Dispose()
        {
            this._action.started -= this.OnStarted;
            this._action.performed -= this.OnPerformed;
            this._action.canceled -= this.OnCanceled;
        }
    }

    private void OnGUI()
    {
        float colWidth = Mathf.Max(180f, (Screen.width - 64f) / 3f);
        GUILayout.BeginArea(new Rect(16f, 16f, colWidth, Screen.height - 32f));
        GUIStyle headerStyle = new(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = this._failed == 0 ? Color.green : Color.red;
        GUILayout.Label($"Input: {this._passed} passed, {this._failed} failed", headerStyle);
        this._scrollPos = GUILayout.BeginScrollView(this._scrollPos);
        foreach (string line in this._results)
            GUILayout.Label(line);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    #endregion
}
