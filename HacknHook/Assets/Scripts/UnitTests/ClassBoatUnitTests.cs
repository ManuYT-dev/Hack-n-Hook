using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public class ClassBoatUnitTests : MonoBehaviour
{
    #region Private Variables
    private readonly List<string> _results = new();
    private readonly List<UnityEngine.Object> _createdObjects = new();
    private int _passed;
    private int _failed;
    private Vector2 _scrollPos;
    private bool _isRunning;
    #endregion

    #region Unity Funcs
    private IEnumerator Start()
    {
        // Wait two frames so InputUnitTests (which runs after one frame) finishes before boat tests touch Controlls.
        yield return null;
        yield return null;
        yield return this.RunAllTestsCoroutine();
    }

    private void OnDestroy()
    {
        this.CleanupCreatedObjects();
    }

    private void OnGUI()
    {
        float colWidth = Mathf.Max(180f, (Screen.width - 64f) / 3f);
        float x = 16f + colWidth + 16f;
        GUILayout.BeginArea(new Rect(x, 16f, colWidth, Screen.height - 32f));
        GUIStyle headerStyle = new(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = this._failed == 0 ? Color.green : Color.red;
        GUILayout.Label($"ClassBoat: {this._passed} passed, {this._failed} failed", headerStyle);
        this._scrollPos = GUILayout.BeginScrollView(this._scrollPos);
        foreach (string line in this._results)
        {
            GUILayout.Label(line);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    #endregion

    #region Public Funcs
    [ContextMenu("Re-run ClassBoat Unit Tests")]
    public void RunAllTests()
    {
        if (this._isRunning || !Application.isPlaying)
        {
            return;
        }

        this.StopAllCoroutines();
        this.StartCoroutine(this.RunAllTestsCoroutine());
    }
    #endregion

    #region Test Runner
    private IEnumerator RunAllTestsCoroutine()
    {
        this._isRunning = true;
        this._results.Clear();
        this._passed = 0;
        this._failed = 0;

        Controlls.Init();
        ReadOnlyArray<InputDevice>? previousDevices = Controlls.inputActions.devices;
        var prevBg = InputSystem.settings.backgroundBehavior;
        var prevEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;

        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

        Keyboard vKeyboard = InputSystem.AddDevice<Keyboard>("ClassBoatTestKeyboard");
        Gamepad vGamepad = InputSystem.AddDevice<Gamepad>("ClassBoatTestGamepad");

        try
        {
            Controlls.inputActions.devices = new ReadOnlyArray<InputDevice>(
                new InputDevice[] { vKeyboard, vGamepad }
            );
            ResetDevices(vKeyboard, vGamepad);

            this.RunInitializationTests();
            this.RunAnchorTests(vKeyboard, vGamepad);
            this.RunFishingModeTests(vKeyboard);
            this.RunMovementAndBoostTests(vKeyboard, vGamepad);
            this.RunTurningAndVisualTests(vKeyboard);
            this.RunBoundaryAndMaxDistanceTests(vKeyboard);
            this.RunInteractTests(vKeyboard);
            this.RunEnableDisableLifecycleTests(vKeyboard);

            yield return this.RunUpdateAndStartFrameTest(vKeyboard);
        }
        finally
        {
            this.CleanupCreatedObjects();
            ResetDevices(vKeyboard, vGamepad);
            Controlls.inputActions.devices = previousDevices;
            InputSystem.RemoveDevice(vKeyboard);
            InputSystem.RemoveDevice(vGamepad);
            Controlls.inputActions.Enable();
            InputSystem.settings.backgroundBehavior = prevBg;
            InputSystem.settings.editorInputBehaviorInPlayMode = prevEditorInput;
            InputSystem.Update();
            this._isRunning = false;
        }

        string summary = $"[ClassBoatUnitTests] Finished: {this._passed} passed, {this._failed} failed.";
        if (this._failed == 0)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogError(summary);
        }
    }
    #endregion

    #region Initialization & Properties Tests
    private void RunInitializationTests()
    {
        // 1. Default inspector values and initial property state
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("DefaultBoat");
            bool ok =
                boat.BoatData == null &&
                string.IsNullOrEmpty(boat.BoatName) &&
                Mathf.Approximately(boat.Speed, 5f) &&
                Mathf.Approximately(boat.BoostedSpeed, 10f) &&
                Mathf.Approximately(boat.Acceleration, 15f) &&
                Mathf.Approximately(boat.TurnSpeed, 8f) &&
                Mathf.Approximately(boat.BuyPrice, 0f) &&
                boat.MaxDistance == 0 &&
                Mathf.Approximately(boat.CurrentVelocity, 0f) &&
                Mathf.Approximately(boat.CurrentSpeed, 0f) &&
                boat.FacingDirection == 1 &&
                !boat.IsAnchored &&
                !boat.IsFishing &&
                !boat.IsBoosting &&
                !boat.IsMoving &&
                !boat.IsAtMaxDistance &&
                Mathf.Approximately(boat.DistanceFromOrigin, 0f);

            this.Record("Default inspector stats and initial property state", ok,
                $"speed={boat.Speed}, boost={boat.BoostedSpeed}, accel={boat.Acceleration}, turn={boat.TurnSpeed}, facing={boat.FacingDirection}");
        });

        // 2. Awake() auto-applies boatData and resolves child SpriteRenderer
        this.RunIsolated(() =>
        {
            Sprite sprite = this.CreateTestSprite();
            ScriptableBoat data = this.CreateScriptableBoat("Sloop", 7.5f, 14f, 20f, 12f, 250f, 120, sprite);

            GameObject go = this.Track(new GameObject("AwakeBoat"));
            go.SetActive(false);

            GameObject child = new("Visual");
            child.transform.SetParent(go.transform, false);
            SpriteRenderer childRenderer = child.AddComponent<SpriteRenderer>();

            ClassBoat boat = go.AddComponent<ClassBoat>();
            SetPrivateField(boat, "boatData", data);
            go.SetActive(true);

            bool ok =
                boat.BoatData == data &&
                boat.BoatName == "Sloop" &&
                Mathf.Approximately(boat.Speed, 7.5f) &&
                Mathf.Approximately(boat.BoostedSpeed, 14f) &&
                Mathf.Approximately(boat.Acceleration, 20f) &&
                Mathf.Approximately(boat.TurnSpeed, 12f) &&
                Mathf.Approximately(boat.BuyPrice, 250f) &&
                boat.MaxDistance == 120 &&
                childRenderer.sprite == sprite;

            this.Record("Awake applies ScriptableBoat and resolves child SpriteRenderer", ok,
                $"name={boat.BoatName}, speed={boat.Speed}, spriteAssigned={childRenderer.sprite == sprite}");
        });

        // 3. Init(ScriptableBoat) updates stats, root SpriteRenderer, and supports re-initialization
        this.RunIsolated(() =>
        {
            Sprite spriteA = this.CreateTestSprite();
            Sprite spriteB = this.CreateTestSprite();
            ScriptableBoat dataA = this.CreateScriptableBoat("Cutter", 6f, 11f, 12f, 9f, 100f, 80, spriteA);
            ScriptableBoat dataB = this.CreateScriptableBoat("Galleon", 9f, 18f, 25f, 6f, 900f, 300, spriteB);

            ClassBoat boat = this.CreateBoat("InitBoat", addRootSpriteRenderer: true);
            SpriteRenderer sr = boat.GetComponent<SpriteRenderer>();

            boat.Init(dataA);
            bool firstOk =
                boat.BoatData == dataA &&
                boat.BoatName == "Cutter" &&
                Mathf.Approximately(boat.Speed, 6f) &&
                Mathf.Approximately(boat.BoostedSpeed, 11f) &&
                Mathf.Approximately(boat.Acceleration, 12f) &&
                Mathf.Approximately(boat.TurnSpeed, 9f) &&
                Mathf.Approximately(boat.BuyPrice, 100f) &&
                boat.MaxDistance == 80 &&
                sr.sprite == spriteA;

            boat.Init(dataB);
            bool secondOk =
                boat.BoatData == dataB &&
                boat.BoatName == "Galleon" &&
                Mathf.Approximately(boat.Speed, 9f) &&
                Mathf.Approximately(boat.BoostedSpeed, 18f) &&
                Mathf.Approximately(boat.Acceleration, 25f) &&
                Mathf.Approximately(boat.TurnSpeed, 6f) &&
                Mathf.Approximately(boat.BuyPrice, 900f) &&
                boat.MaxDistance == 300 &&
                sr.sprite == spriteB;

            this.Record("Init(ScriptableBoat) initializes and re-initializes stats & sprite", firstOk && secondOk,
                $"firstOk={firstOk}, secondOk={secondOk}, finalName={boat.BoatName}");
        });

        // 4. Init(null) preserves existing ScriptableBoat and stats without throwing
        this.RunIsolated(() =>
        {
            Sprite sprite = this.CreateTestSprite();
            ScriptableBoat data = this.CreateScriptableBoat("Skiff", 8f, 15f, 16f, 10f, 150f, 60, sprite);
            ClassBoat boat = this.CreateBoat("NullInitBoat", addRootSpriteRenderer: true);
            SpriteRenderer sr = boat.GetComponent<SpriteRenderer>();
            boat.Init(data);

            bool prevLogEnabled = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;
                boat.Init(null);
            }
            finally
            {
                Debug.unityLogger.logEnabled = prevLogEnabled;
            }

            bool ok =
                boat.BoatData == data &&
                boat.BoatName == "Skiff" &&
                Mathf.Approximately(boat.Speed, 8f) &&
                boat.MaxDistance == 60 &&
                sr.sprite == sprite;

            this.Record("Init(null) preserves existing ScriptableBoat and stats", ok,
                $"name={boat.BoatName}, speed={boat.Speed}");
        });

        // 5. DistanceFromOrigin clamps negative X to 0 and reports positive X
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("DistanceBoat");
            boat.transform.position = new Vector3(-4.5f, 0f, 0f);
            bool negClamped = Mathf.Approximately(boat.DistanceFromOrigin, 0f);

            boat.transform.position = new Vector3(14.25f, 2f, 0f);
            bool posExact = Mathf.Approximately(boat.DistanceFromOrigin, 14.25f);

            this.Record("DistanceFromOrigin clamps negative X and tracks positive X", negClamped && posExact,
                $"negClamped={negClamped}, pos={boat.DistanceFromOrigin}");
        });
    }
    #endregion

    #region Anchor Tests
    private void RunAnchorTests(Keyboard keyboard, Gamepad gamepad)
    {
        // 6. SetAnchored state changes, idempotency, and OnAnchorChanged
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("AnchorBoat");
            List<bool> events = new();
            boat.OnAnchorChanged += v => events.Add(v);

            boat.SetAnchored(false);
            bool noopFalse = !boat.IsAnchored && events.Count == 0;

            boat.SetAnchored(true);
            bool anchoredOk = boat.IsAnchored && events.Count == 1 && events[0];

            boat.SetAnchored(true);
            bool noopTrue = boat.IsAnchored && events.Count == 1;

            boat.SetAnchored(false);
            bool unanchoredOk = !boat.IsAnchored && events.Count == 2 && !events[1];

            this.Record("SetAnchored updates state, suppresses duplicates, fires OnAnchorChanged",
                noopFalse && anchoredOk && noopTrue && unanchoredOk,
                $"eventCount={events.Count}, anchored={boat.IsAnchored}");
        });

        // 7. ToggleAnchor flips state and emits OnAnchorChanged
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("ToggleAnchorBoat");
            List<bool> events = new();
            boat.OnAnchorChanged += v => events.Add(v);

            boat.ToggleAnchor();
            bool first = boat.IsAnchored;
            boat.ToggleAnchor();
            bool second = !boat.IsAnchored;

            bool ok = first && second && events.Count == 2 && events[0] && !events[1];
            this.Record("ToggleAnchor flips state and fires OnAnchorChanged", ok,
                $"events=[{string.Join(",", events)}]");
        });

        // 8. Input Boat.Anchor (Keyboard S and Gamepad East) triggers ToggleAnchor
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("InputAnchorBoat");
            int eventCount = 0;
            boat.OnAnchorChanged += _ => eventCount++;

            SendKeyboard(keyboard, new KeyboardState(Key.S));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            bool afterKey = boat.IsAnchored && eventCount == 1;

            SendGamepad(gamepad, new GamepadState(GamepadButton.East));
            InputSystem.Update();
            SendGamepad(gamepad, new GamepadState());
            InputSystem.Update();
            bool afterGamepad = !boat.IsAnchored && eventCount == 2;

            this.Record("Boat.Anchor input (Keyboard S & Gamepad East) toggles anchor", afterKey && afterGamepad,
                $"afterKey={afterKey}, afterGamepad={afterGamepad}");
        });

        // 9. Anchoring clears active boost and fires OnBoostChanged(false)
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("AnchorClearsBoostBoat");
            boat.Init(this.CreateScriptableBoat("BoostAnchor", 5f, 10f, 15f, 8f, 0f, 0, null));

            List<bool> boostEvents = new();
            boat.OnBoostChanged += v => boostEvents.Add(v);

            SendKeyboard(keyboard, new KeyboardState(Key.D, Key.LeftShift));
            InputSystem.Update();
            boat.StepMovement(0.05f);
            bool boostingBefore = boat.IsBoosting;

            boat.SetAnchored(true);
            bool clearedAfter = !boat.IsBoosting && boat.IsAnchored;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            bool ok = boostingBefore && clearedAfter && boostEvents.Count == 2 && boostEvents[0] && !boostEvents[1];
            this.Record("SetAnchored(true) clears active boost and fires OnBoostChanged(false)", ok,
                $"boostEvents=[{string.Join(",", boostEvents)}]");
        });
    }
    #endregion

    #region Fishing Mode & Input Switching Tests
    private void RunFishingModeTests(Keyboard keyboard)
    {
        // 10. SetFishingMode state, idempotency, OnFishingModeChanged, and input map switching
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("FishingBoat");
            List<bool> events = new();
            boat.OnFishingModeChanged += v => events.Add(v);

            boat.SetFishingMode(false);
            bool noopFalse = !boat.IsFishing && events.Count == 0;

            boat.SetFishingMode(true);
            bool enteredFishing =
                boat.IsFishing &&
                events.Count == 1 &&
                events[0] &&
                !Controlls.boatActions.Drive.enabled &&
                !Controlls.boatActions.Anchor.enabled &&
                !Controlls.boatActions.Interact.enabled &&
                !Controlls.boatActions.Boost.enabled &&
                Controlls.boatActions.ToggleFishing.enabled &&
                Controlls.fishingRodActions.enabled;

            boat.SetFishingMode(true);
            bool noopTrue = boat.IsFishing && events.Count == 1;

            boat.SetFishingMode(false);
            bool exitedFishing =
                !boat.IsFishing &&
                events.Count == 2 &&
                !events[1] &&
                !Controlls.fishingRodActions.enabled &&
                Controlls.boatActions.Drive.enabled &&
                Controlls.boatActions.Anchor.enabled &&
                Controlls.boatActions.Interact.enabled &&
                Controlls.boatActions.Boost.enabled &&
                Controlls.boatActions.ToggleFishing.enabled;

            this.Record("SetFishingMode switches action maps, suppresses duplicates, fires OnFishingModeChanged",
                noopFalse && enteredFishing && noopTrue && exitedFishing,
                $"entered={enteredFishing}, exited={exitedFishing}");
        });

        // 11. ToggleFishing via Boat.ToggleFishing input (Key.F) enters and exits fishing mode
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("ToggleFishingInputBoat");

            SendKeyboard(keyboard, new KeyboardState(Key.F));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            bool inFishing = boat.IsFishing && Controlls.fishingRodActions.enabled;

            SendKeyboard(keyboard, new KeyboardState(Key.F));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            bool outFishing = !boat.IsFishing && Controlls.boatActions.enabled && !Controlls.fishingRodActions.enabled;

            this.Record("Boat.ToggleFishing input (Key.F) toggles fishing mode on and off", inFishing && outFishing,
                $"inFishing={inFishing}, outFishing={outFishing}");
        });

        // 12. Fishing mode clears boost and blocks drive, anchor, and interact inputs
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("FishingBlocksBoat");
            boat.Init(this.CreateScriptableBoat("Fisher", 5f, 10f, 15f, 8f, 0f, 0, null));

            int interactCalls = 0;
            List<bool> boostEvents = new();
            boat.OnInteract += () => interactCalls++;
            boat.OnBoostChanged += v => boostEvents.Add(v);

            SendKeyboard(keyboard, new KeyboardState(Key.D, Key.LeftShift));
            InputSystem.Update();
            boat.StepMovement(0.05f);
            bool boostedFirst = boat.IsBoosting;

            boat.SetFishingMode(true);
            bool boostCleared = !boat.IsBoosting && boostEvents.Count == 2 && !boostEvents[1];

            // Try driving, anchoring, and interacting while fishing
            SendKeyboard(keyboard, new KeyboardState(Key.D, Key.S, Key.E));
            InputSystem.Update();
            boat.StepMovement(0.2f);
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            bool blockedOk =
                !boat.IsAnchored &&
                interactCalls == 0 &&
                !boat.IsBoosting &&
                Mathf.Approximately(boat.CurrentVelocity, 0f);

            this.Record("Fishing mode clears boost and blocks drive/anchor/interact inputs",
                boostedFirst && boostCleared && blockedOk,
                $"boostCleared={boostCleared}, anchored={boat.IsAnchored}, interactCalls={interactCalls}, vel={boat.CurrentVelocity:F3}");
        });
    }
    #endregion

    #region Movement, Boost & Deceleration Tests
    private void RunMovementAndBoostTests(Keyboard keyboard, Gamepad gamepad)
    {
        // 13. StepMovement ignores deltaTime <= 0
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("ZeroDtBoat");
            boat.Init(this.CreateScriptableBoat("NoDt", 6f, 12f, 20f, 8f, 0f, 0, null));

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();

            boat.StepMovement(0f);
            boat.StepMovement(-0.5f);

            bool ok =
                Mathf.Approximately(boat.CurrentVelocity, 0f) &&
                Mathf.Approximately(boat.transform.position.x, 0f) &&
                !boat.IsMoving;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            this.Record("StepMovement ignores deltaTime <= 0", ok,
                $"vel={boat.CurrentVelocity}, x={boat.transform.position.x}");
        });

        // 14. Deadzone filtering & proportional analog drive input
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("DeadzoneBoat");
            boat.Init(this.CreateScriptableBoat("Analog", 10f, 15f, 100f, 0f, 0f, 0, null));
            SetPrivateField(boat, "inputDeadzone", 0.3f);

            SendGamepad(gamepad, new GamepadState { leftStick = new Vector2(0.25f, 0f) });
            InputSystem.Update();
            boat.StepMovement(0.1f);
            bool ignoredBelowDeadzone = Mathf.Approximately(boat.CurrentVelocity, 0f) && !boat.IsMoving;

            SendGamepad(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0f) });
            InputSystem.Update();
            float expectedInput = Controlls.boatActions.Drive.ReadValue<float>();
            boat.StepMovement(0.1f);
            bool proportionalAboveDeadzone =
                expectedInput > 0.3f &&
                expectedInput < 0.9f &&
                Mathf.Abs(boat.CurrentVelocity - (expectedInput * 10f)) < 0.05f &&
                boat.IsMoving;

            SendGamepad(gamepad, new GamepadState());
            InputSystem.Update();

            this.Record("ReadDriveInput respects inputDeadzone and analog magnitude",
                ignoredBelowDeadzone && proportionalAboveDeadzone,
                $"belowIgnored={ignoredBelowDeadzone}, velAtHalfStick={boat.CurrentVelocity:F3}");
        });

        // 15. Acceleration, CurrentSpeed, IsMoving, and release deceleration
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("AccelBoat");
            boat.Init(this.CreateScriptableBoat("Runner", 8f, 16f, 10f, 8f, 0f, 0, null));

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();

            boat.StepMovement(0.5f); // 0 + 10 * 0.5 = 5
            bool midAccel =
                Mathf.Abs(boat.CurrentVelocity - 5f) < 0.001f &&
                Mathf.Abs(boat.CurrentSpeed - 5f) < 0.001f &&
                boat.IsMoving &&
                boat.transform.position.x > 0f;

            boat.StepMovement(0.5f); // capped at Speed = 8
            bool maxCruise = Mathf.Abs(boat.CurrentVelocity - 8f) < 0.001f;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            boat.StepMovement(0.5f); // 8 - 10 * 0.5 = 3
            bool midDecel = Mathf.Abs(boat.CurrentVelocity - 3f) < 0.001f && boat.IsMoving;

            boat.StepMovement(0.5f); // stops at 0
            bool stopped = Mathf.Approximately(boat.CurrentVelocity, 0f) && !boat.IsMoving;

            this.Record("Acceleration, CurrentSpeed, IsMoving, and release deceleration",
                midAccel && maxCruise && midDecel && stopped,
                $"midAccel={midAccel}, maxCruise={maxCruise}, midDecel={midDecel}, stopped={stopped}");
        });

        // 16. Zero acceleration snaps velocity immediately on drive and release
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("InstantVelBoat");
            boat.Init(this.CreateScriptableBoat("Instant", 7f, 14f, 0f, 0f, 0f, 0, null));

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(0.05f);
            bool instantDrive = Mathf.Approximately(boat.CurrentVelocity, 7f);

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            boat.StepMovement(0.05f);
            bool instantStop = Mathf.Approximately(boat.CurrentVelocity, 0f);

            this.Record("Zero acceleration applies instant target velocity and instant stop",
                instantDrive && instantStop,
                $"instantDrive={instantDrive}, instantStop={instantStop}");
        });

        // 17. Boost speed, OnBoostChanged deduplication, and fallback when boostedSpeed <= 0
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("BoostBoat");
            boat.Init(this.CreateScriptableBoat("Speedboat", 5f, 12f, 50f, 0f, 0f, 0, null));

            List<bool> boostEvents = new();
            boat.OnBoostChanged += v => boostEvents.Add(v);

            SendKeyboard(keyboard, new KeyboardState(Key.D, Key.LeftShift));
            InputSystem.Update();
            boat.StepMovement(0.2f); // 50 * 0.2 = 10
            boat.StepMovement(0.2f); // reaches boostedSpeed 12
            bool boostedOk =
                boat.IsBoosting &&
                boostEvents.Count == 1 &&
                boostEvents[0] &&
                Mathf.Approximately(boat.CurrentVelocity, 12f);

            // Release boost while still driving -> slows down toward normal speed
            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(0.1f); // 12 - 50*0.1 = 7
            boat.StepMovement(0.1f); // reaches normal speed 5
            bool unboostedOk =
                !boat.IsBoosting &&
                boostEvents.Count == 2 &&
                !boostEvents[1] &&
                Mathf.Approximately(boat.CurrentVelocity, 5f);

            // Fallback when boostedSpeed <= 0 uses normal speed
            boat.Init(this.CreateScriptableBoat("NoBoostCap", 6f, 0f, 100f, 0f, 0f, 0, null));
            SendKeyboard(keyboard, new KeyboardState(Key.D, Key.LeftShift));
            InputSystem.Update();
            boat.StepMovement(0.2f);
            bool fallbackOk = boat.IsBoosting && Mathf.Approximately(boat.CurrentVelocity, 6f);

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            this.Record("Boost increases speed to BoostedSpeed, deduplicates OnBoostChanged, falls back when <= 0",
                boostedOk && unboostedOk && fallbackOk,
                $"boosted={boostedOk}, unboosted={unboostedOk}, fallback={fallbackOk}");
        });

        // 18. Anchor deceleration multiplier (with normal acceleration and zero acceleration)
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("AnchorBrakeBoat");
            boat.Init(this.CreateScriptableBoat("Braker", 10f, 15f, 10f, 0f, 0f, 0, null));
            SetPrivateField(boat, "anchorDecelerationMultiplier", 2.5f);

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(1f); // v = 10
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            boat.SetAnchored(true);
            boat.StepMovement(0.2f); // 10 - (10 * 2.5 * 0.2) = 5
            bool multOk = Mathf.Abs(boat.CurrentVelocity - 5f) < 0.001f;

            // With acceleration == 0, anchor uses Max(speed, 10f) * multiplier
            boat.SetAnchored(false);
            boat.Init(this.CreateScriptableBoat("ZeroAccelBraker", 6f, 12f, 0f, 0f, 0f, 0, null));
            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(0.1f); // v = 6
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            boat.SetAnchored(true);
            boat.StepMovement(0.1f); // 6 - (Max(6,10) * 2.5 * 0.1) = 6 - 2.5 = 3.5
            bool zeroAccelAnchorOk = Mathf.Abs(boat.CurrentVelocity - 3.5f) < 0.001f;

            this.Record("Anchored deceleration applies anchorDecelerationMultiplier (incl. accel == 0 fallback)",
                multOk && zeroAccelAnchorOk,
                $"multOk={multOk}, zeroAccelAnchorOk={zeroAccelAnchorOk}, vel={boat.CurrentVelocity:F3}");
        });
    }
    #endregion

    #region Turning & Visual Rotation Tests
    private void RunTurningAndVisualTests(Keyboard keyboard)
    {
        // 19a. Turning around flips FacingDirection and uses Max(acceleration, turnSpeed) braking
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("TurnBrakeBoat");
            boat.Init(this.CreateScriptableBoat("Turner", 10f, 15f, 5f, 20f, 0f, 0, null));

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(2f); // v = 10

            // Reverse direction: turnBrakeRate = Max(5, 20) = 20
            SendKeyboard(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            boat.StepMovement(0.25f); // 10 - 20 * 0.25 = 5

            bool turningOk =
                boat.FacingDirection == -1 &&
                Mathf.Abs(boat.CurrentVelocity - 5f) < 0.001f;

            // Step until turned and accelerating in negative direction
            for (int i = 0; i < 10; i++)
            {
                boat.StepMovement(0.1f);
            }
            bool reversedOk = boat.CurrentVelocity < 0f && boat.FacingDirection == -1;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            this.Record("Turning around updates FacingDirection, brakes with Max(accel, turnSpeed), then reverses",
                turningOk && reversedOk,
                $"turningOk={turningOk}, reversedVel={boat.CurrentVelocity:F3}");
        });

        // 19b. Visual rotation modes: child localRotation, root rotation, root+child flipX, and facesLeftByDefault
        this.RunIsolated(() =>
        {
            // Mode 1: child SpriteRenderer -> localRotation
            GameObject childBoatGo = this.Track(new GameObject("ChildVisualBoat"));
            GameObject visualChild = new("SpriteChild");
            visualChild.transform.SetParent(childBoatGo.transform, false);
            visualChild.AddComponent<SpriteRenderer>();
            ClassBoat childBoat = childBoatGo.AddComponent<ClassBoat>();
            childBoat.Init(this.CreateScriptableBoat("ChildRot", 5f, 10f, 15f, 0f, 0f, 0, null)); // turnSpeed=0 snaps

            SendKeyboard(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            childBoat.StepMovement(0.05f);
            bool childRotOk = IsYawClose(visualChild.transform.localRotation, 180f);

            // Mode 2: root SpriteRenderer without children -> transform.rotation
            ClassBoat rootBoat = this.CreateBoat("RootVisualBoat", addRootSpriteRenderer: true);
            rootBoat.Init(this.CreateScriptableBoat("RootRot", 5f, 10f, 15f, 0f, 0f, 0, null));
            rootBoat.StepMovement(0.05f);
            bool rootRotOk = IsYawClose(rootBoat.transform.rotation, 180f);

            // Mode 3: root SpriteRenderer WITH an unrelated child -> flipX
            ClassBoat flipBoat = this.CreateBoat("FlipXBoat", addRootSpriteRenderer: true);
            SpriteRenderer flipSr = flipBoat.GetComponent<SpriteRenderer>();
            new GameObject("ExtraChild").transform.SetParent(flipBoat.transform, false);
            flipBoat.Init(this.CreateScriptableBoat("FlipX", 5f, 10f, 15f, 0f, 0f, 0, null));
            flipBoat.StepMovement(0.05f);
            bool flipXLeftOk = flipSr.flipX && IsYawClose(flipBoat.transform.rotation, 0f);

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            flipBoat.StepMovement(0.05f);
            bool flipXRightOk = !flipSr.flipX;

            // Mode 4: facesLeftByDefault == true
            GameObject leftBoatGo = this.Track(new GameObject("FacesLeftBoat"));
            leftBoatGo.SetActive(false);
            leftBoatGo.AddComponent<SpriteRenderer>();
            ClassBoat leftBoat = leftBoatGo.AddComponent<ClassBoat>();
            SetPrivateField(leftBoat, "facesLeftByDefault", true);
            leftBoatGo.SetActive(true);
            leftBoat.Init(this.CreateScriptableBoat("LeftDef", 5f, 10f, 15f, 0f, 0f, 0, null));

            bool initialLeftBoatYawOk = IsYawClose(leftBoat.transform.rotation, 180f); // facing +1 => 180
            SendKeyboard(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            leftBoat.StepMovement(0.05f);
            bool afterLeftDriveYawOk = IsYawClose(leftBoat.transform.rotation, 0f); // facing -1 => 0
            bool facesLeftOk = initialLeftBoatYawOk && afterLeftDriveYawOk;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            this.Record("Visual rotation handles child localRotation, root rotation, root+child flipX, and facesLeftByDefault",
                childRotOk && rootRotOk && flipXLeftOk && flipXRightOk && facesLeftOk,
                $"child={childRotOk}, root={rootRotOk}, flipX={flipXLeftOk && flipXRightOk}, facesLeft={facesLeftOk}");
        });
    }
    #endregion

    #region Boundary & MaxDistance Tests
    private void RunBoundaryAndMaxDistanceTests(Keyboard keyboard)
    {
        // 20. Left boundary clamps position.x >= 0 and zeroes negative velocity
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("LeftClampBoat");
            boat.Init(this.CreateScriptableBoat("LeftClamp", 10f, 15f, 100f, 0f, 0f, 0, null));
            boat.transform.position = new Vector3(0.2f, 0f, 0f);

            SendKeyboard(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            boat.StepMovement(0.1f); // would move left past 0

            bool ok =
                Mathf.Approximately(boat.transform.position.x, 0f) &&
                Mathf.Approximately(boat.CurrentVelocity, 0f) &&
                Mathf.Approximately(boat.DistanceFromOrigin, 0f);

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            this.Record("Left boundary clamps X >= 0 and zeroes negative velocity", ok,
                $"x={boat.transform.position.x}, vel={boat.CurrentVelocity}");
        });

        // 21. MaxDistance clamps X, zeroes velocity, sets IsAtMaxDistance, fires OnMaxDistanceReached once per arrival
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("MaxDistBoat");
            boat.Init(this.CreateScriptableBoat("Bound", 10f, 15f, 100f, 0f, 0f, 10, null));
            boat.transform.position = new Vector3(9.5f, 0f, 0f);

            int maxDistEvents = 0;
            boat.OnMaxDistanceReached += () => maxDistEvents++;

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(0.1f); // hits 10
            bool firstHit =
                Mathf.Approximately(boat.transform.position.x, 10f) &&
                Mathf.Approximately(boat.CurrentVelocity, 0f) &&
                boat.IsAtMaxDistance &&
                Mathf.Approximately(boat.DistanceFromOrigin, 10f) &&
                maxDistEvents == 1;

            // Stepping again at boundary must not re-fire OnMaxDistanceReached
            boat.StepMovement(0.1f);
            bool noRepeat = boat.IsAtMaxDistance && maxDistEvents == 1;

            // Back away clears IsAtMaxDistance
            SendKeyboard(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();
            boat.StepMovement(0.1f);
            bool cleared = !boat.IsAtMaxDistance && boat.transform.position.x < 10f;

            // Reach maxDistance again -> fires second time
            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(0.2f);
            boat.StepMovement(0.2f);
            bool secondHit = boat.IsAtMaxDistance && maxDistEvents == 2;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            this.Record("MaxDistance clamps X, zeroes velocity, toggles IsAtMaxDistance, fires OnMaxDistanceReached once per arrival",
                firstHit && noRepeat && cleared && secondHit,
                $"firstHit={firstHit}, noRepeat={noRepeat}, cleared={cleared}, secondHit={secondHit}, events={maxDistEvents}");
        });

        // 22. MaxDistance <= 0 allows unbounded positive X movement without firing OnMaxDistanceReached
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("UnboundedBoat");
            boat.Init(this.CreateScriptableBoat("Free", 10f, 15f, 100f, 0f, 0f, 0, null));
            boat.transform.position = new Vector3(19.5f, 0f, 0f);

            int maxDistEvents = 0;
            boat.OnMaxDistanceReached += () => maxDistEvents++;

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();
            boat.StepMovement(0.2f);

            bool ok = boat.transform.position.x > 20f && !boat.IsAtMaxDistance && maxDistEvents == 0;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            this.Record("MaxDistance <= 0 allows unbounded positive travel", ok,
                $"x={boat.transform.position.x:F2}, isAtMax={boat.IsAtMaxDistance}, events={maxDistEvents}");
        });
    }
    #endregion

    #region Interact & IInteractable Tests
    private void RunInteractTests(Keyboard keyboard)
    {
        int interactableLayer = LayerMask.NameToLayer("Interactable");

        // 23. Interact fires OnInteract even with no interactables present
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("EmptyInteractBoat");
            int onInteractCount = 0;
            boat.OnInteract += () => onInteractCount++;

            boat.Interact();

            this.Record("Interact() invokes OnInteract when no IInteractable is nearby", onInteractCount == 1,
                $"onInteractCount={onInteractCount}");
        });

        // 24. Physics2D.OverlapCircleAll path: closest wins, GetComponentInParent resolves child collider, ignores out-of-range and self/child
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("PhysicsInteractBoat");
            boat.transform.position = Vector3.zero;
            SetPrivateField(boat, "interactRadius", 3f);

            // Self & child interactable on Interactable layer must be ignored
            boat.gameObject.layer = interactableLayer;
            BoxCollider2D selfCol = boat.gameObject.AddComponent<BoxCollider2D>();
            selfCol.size = Vector2.one;
            TestInteractable selfTarget = boat.gameObject.AddComponent<TestInteractable>();

            GameObject boatChild = new("BoatChildInteractable");
            boatChild.transform.SetParent(boat.transform, false);
            boatChild.layer = interactableLayer;
            boatChild.AddComponent<BoxCollider2D>().size = Vector2.one;
            TestInteractable childTarget = boatChild.AddComponent<TestInteractable>();

            // Farther interactable inside radius (x = 2.5)
            TestInteractable farTarget = this.CreateColliderInteractable("FarTarget", new Vector3(2.5f, 0f, 0f), interactableLayer, parentTarget: false);
            // Closer parent interactable with collider on child (x = 1.0)
            TestInteractable closeTarget = this.CreateColliderInteractable("CloseParentTarget", new Vector3(1.0f, 0f, 0f), interactableLayer, parentTarget: true);
            // Out-of-range interactable (x = 5.0)
            TestInteractable outOfRange = this.CreateColliderInteractable("OutOfRangeTarget", new Vector3(5.0f, 0f, 0f), interactableLayer, parentTarget: false);

            Physics2D.SyncTransforms();
            boat.Interact();

            bool ok =
                closeTarget.CallCount == 1 &&
                closeTarget.LastBoat == boat &&
                farTarget.CallCount == 0 &&
                outOfRange.CallCount == 0 &&
                selfTarget.CallCount == 0 &&
                childTarget.CallCount == 0;

            this.Record("Interact() OverlapCircleAll picks closest parent/collider target and ignores self/children/out-of-range", ok,
                $"close={closeTarget.CallCount}, far={farTarget.CallCount}, out={outOfRange.CallCount}, self={selfTarget.CallCount}, child={childTarget.CallCount}");
        });

        // 25. Fallback FindObjectsByType<MonoBehaviour>() path when no Interactable-layer collider is hit
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("FallbackInteractBoat");
            boat.transform.position = Vector3.zero;
            SetPrivateField(boat, "interactRadius", 3f);

            GameObject selfChild = new("SelfChildFallback");
            selfChild.transform.SetParent(boat.transform, false);
            TestInteractable selfChildTarget = selfChild.AddComponent<TestInteractable>();

            TestInteractable farTarget = this.CreateFallbackInteractable("FarFallback", new Vector3(2.8f, 0f, 0f));
            TestInteractable closeTarget = this.CreateFallbackInteractable("CloseFallback", new Vector3(1.2f, 0f, 0f));
            TestInteractable outTarget = this.CreateFallbackInteractable("OutFallback", new Vector3(4.0f, 0f, 0f));

            Physics2D.SyncTransforms();
            boat.Interact();

            bool ok =
                closeTarget.CallCount == 1 &&
                closeTarget.LastBoat == boat &&
                farTarget.CallCount == 0 &&
                outTarget.CallCount == 0 &&
                selfChildTarget.CallCount == 0;

            this.Record("Interact() fallback FindObjectsByType resolves closest non-collider target and ignores self/out-of-range", ok,
                $"close={closeTarget.CallCount}, far={farTarget.CallCount}, out={outTarget.CallCount}, selfChild={selfChildTarget.CallCount}");
        });

        // 26. Boat.Interact input (Key.E) triggers Interact() when driving, ignored when fishing
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("InputInteractBoat");
            TestInteractable target = this.CreateFallbackInteractable("InputTarget", new Vector3(0.5f, 0f, 0f));
            int onInteractCount = 0;
            boat.OnInteract += () => onInteractCount++;

            SendKeyboard(keyboard, new KeyboardState(Key.E));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            bool normalOk = onInteractCount == 1 && target.CallCount == 1 && target.LastBoat == boat;

            boat.SetFishingMode(true);
            SendKeyboard(keyboard, new KeyboardState(Key.E));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            bool blockedWhileFishing = onInteractCount == 1 && target.CallCount == 1;

            this.Record("Boat.Interact input (Key.E) triggers Interact() when not fishing and ignores when fishing",
                normalOk && blockedWhileFishing,
                $"normalOk={normalOk}, blockedWhileFishing={blockedWhileFishing}");
        });
    }
    #endregion

    #region Lifecycle Tests
    private void RunEnableDisableLifecycleTests(Keyboard keyboard)
    {
        // 27a. OnDisable clears boost and unsubscribes boat input callbacks
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("DisableBoostBoat");
            boat.Init(this.CreateScriptableBoat("Disabler", 5f, 10f, 15f, 8f, 0f, 0, null));

            int boostOffCount = 0;
            boat.OnBoostChanged += b => { if (!b) boostOffCount++; };

            SendKeyboard(keyboard, new KeyboardState(Key.D, Key.LeftShift));
            InputSystem.Update();
            boat.StepMovement(0.05f);
            bool wasBoosting = boat.IsBoosting;

            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            boat.enabled = false;
            bool clearedBoost = !boat.IsBoosting && boostOffCount == 1;

            // Inputs while disabled must not change anchor or fishing
            SendKeyboard(keyboard, new KeyboardState(Key.S));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState(Key.F));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            bool ignoredInputs = !boat.IsAnchored && !boat.IsFishing;

            this.Record("OnDisable clears boost and unsubscribes boat action callbacks",
                wasBoosting && clearedBoost && ignoredInputs,
                $"wasBoosting={wasBoosting}, clearedBoost={clearedBoost}, ignoredInputs={ignoredInputs}");
        });

        // 27b. OnDisable restores boatActions when disabled during fishing; OnEnable re-applies fishing mode & callbacks
        this.RunIsolated(() =>
        {
            ClassBoat boat = this.CreateBoat("DisableFishingBoat");
            boat.SetFishingMode(true);
            bool fishingBeforeDisable = Controlls.fishingRodActions.enabled && !Controlls.boatActions.Drive.enabled;

            boat.enabled = false;
            bool restoredBoatControlsOnDisable = !Controlls.fishingRodActions.enabled && Controlls.boatActions.enabled;

            boat.enabled = true;
            bool reappliedFishingOnEnable = Controlls.fishingRodActions.enabled && !Controlls.boatActions.Drive.enabled;

            // ToggleFishing callback re-subscribed on OnEnable
            SendKeyboard(keyboard, new KeyboardState(Key.F));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            bool callbackRestored = !boat.IsFishing && Controlls.boatActions.enabled;

            this.Record("OnDisable restores boat controls from fishing and OnEnable re-applies input mode & callbacks",
                fishingBeforeDisable && restoredBoatControlsOnDisable && reappliedFishingOnEnable && callbackRestored,
                $"restored={restoredBoatControlsOnDisable}, reapplied={reappliedFishingOnEnable}, callbackRestored={callbackRestored}");
        });
    }

    private IEnumerator RunUpdateAndStartFrameTest(Keyboard keyboard)
    {
        ClassBoat boat = null;
        try
        {
            boat = this.CreateBoat("UpdateFrameBoat");
            boat.Init(this.CreateScriptableBoat("Updater", 8f, 16f, 40f, 8f, 0f, 100, null));

            SendKeyboard(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();

            // Wait two real frames so Start() and Update() execute
            yield return null;
            yield return null;

            bool ok = boat.transform.position.x > 0f && boat.CurrentVelocity > 0f && boat.IsMoving;
            this.Record("Update() advances movement across real Play Mode frames", ok,
                $"x={boat.transform.position.x:F4}, vel={boat.CurrentVelocity:F4}");
        }
        finally
        {
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();
            if (boat != null)
            {
                DestroyImmediate(boat.gameObject);
            }
        }
    }
    #endregion

    #region Helpers
    private void RunIsolated(Action testBody)
    {
        try
        {
            testBody();
        }
        catch (Exception ex)
        {
            this.Record("Unexpected exception in ClassBoat test", false, ex.ToString());
        }
        finally
        {
            this.CleanupCreatedObjects();
            Controlls.fishingRodActions.Disable();
            Controlls.inputActions.Enable();
        }
    }

    private T Track<T>(T obj) where T : UnityEngine.Object
    {
        if (obj != null)
        {
            this._createdObjects.Add(obj);
        }
        return obj;
    }

    private ClassBoat CreateBoat(string name, bool addRootSpriteRenderer = false)
    {
        GameObject go = this.Track(new GameObject(name));
        if (addRootSpriteRenderer)
        {
            go.AddComponent<SpriteRenderer>();
        }
        return go.AddComponent<ClassBoat>();
    }

    private ScriptableBoat CreateScriptableBoat(
        string boatName,
        float speed,
        float boostedSpeed,
        float acceleration,
        float turnSpeed,
        float buyPrice,
        int maxDistance,
        Sprite sprite)
    {
        ScriptableBoat data = this.Track(ScriptableObject.CreateInstance<ScriptableBoat>());
        SetPrivateField(data, "boatName", boatName);
        SetPrivateField(data, "speed", speed);
        SetPrivateField(data, "boostedSpeed", boostedSpeed);
        SetPrivateField(data, "acceleration", acceleration);
        SetPrivateField(data, "turnSpeed", turnSpeed);
        SetPrivateField(data, "buyPrice", buyPrice);
        SetPrivateField(data, "maxDistance", maxDistance);
        SetPrivateField(data, "sprite", sprite);
        return data;
    }

    private Sprite CreateTestSprite()
    {
        Texture2D tex = this.Track(new Texture2D(4, 4));
        Sprite sprite = this.Track(Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));
        return sprite;
    }

    private TestInteractable CreateColliderInteractable(string name, Vector3 position, int layer, bool parentTarget)
    {
        GameObject root = this.Track(new GameObject(name));
        root.transform.position = position;
        TestInteractable interactable = root.AddComponent<TestInteractable>();

        if (parentTarget)
        {
            GameObject child = new("ColliderChild");
            child.transform.SetParent(root.transform, false);
            child.layer = layer;
            child.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
        }
        else
        {
            root.layer = layer;
            root.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
        }

        return interactable;
    }

    private TestInteractable CreateFallbackInteractable(string name, Vector3 position)
    {
        GameObject go = this.Track(new GameObject(name));
        go.transform.position = position;
        return go.AddComponent<TestInteractable>();
    }

    private void CleanupCreatedObjects()
    {
        for (int i = this._createdObjects.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object obj = this._createdObjects[i];
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        this._createdObjects.Clear();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
        field.SetValue(target, value);
    }

    private static bool IsYawClose(Quaternion rotation, float expectedYaw) =>
        Quaternion.Angle(rotation, Quaternion.Euler(0f, expectedYaw, 0f)) < 0.1f;

    private static void SendKeyboard(Keyboard device, KeyboardState state) => InputSystem.QueueStateEvent(device, state);
    private static void SendGamepad(Gamepad device, GamepadState state) => InputSystem.QueueStateEvent(device, state);

    private static void ResetDevices(Keyboard keyboard, Gamepad gamepad)
    {
        if (keyboard != null) SendKeyboard(keyboard, new KeyboardState());
        if (gamepad != null) SendGamepad(gamepad, new GamepadState());
        InputSystem.Update();
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

    private sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        public int CallCount { get; private set; }
        public ClassBoat LastBoat { get; private set; }

        public void Interact(ClassBoat boat)
        {
            this.CallCount++;
            this.LastBoat = boat;
        }
    }
    #endregion
}
