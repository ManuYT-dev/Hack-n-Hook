using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public class ClassFishingRodUnitTests : MonoBehaviour
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
        // Wait until InputUnitTests and ClassBoatUnitTests finish before touching Controlls.
        yield return null;
        yield return null;
        yield return null;
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
        float colWidth = Mathf.Max(180f, (Screen.width - 80f) / 4f);
        float x = 16f + (colWidth + 16f) * 3f;
        GUILayout.BeginArea(new Rect(x, 16f, colWidth, Screen.height - 32f));
        GUIStyle headerStyle = new(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = this._failed == 0 ? Color.green : Color.red;
        GUILayout.Label($"FishingRod: {this._passed} passed, {this._failed} failed", headerStyle);
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
    [ContextMenu("Re-run ClassFishingRod Unit Tests")]
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

        Mouse vMouse = InputSystem.AddDevice<Mouse>("FishingRodTestMouse");
        Keyboard vKeyboard = InputSystem.AddDevice<Keyboard>("FishingRodTestKeyboard");
        BleControllerDevice ble = BleControllerDevice.current ?? BleControllerDevice.Init();

        try
        {
            Controlls.inputActions.devices = new ReadOnlyArray<InputDevice>(
                new InputDevice[] { vMouse, vKeyboard, ble }
            );
            ResetDevices(vMouse, vKeyboard, ble);

            this.RunInitializationAndVisualTests();
            this.RunReadyModeAndLifecycleTests(vMouse, vKeyboard, ble);
            this.RunCastSpeedSamplingAndPhysicsTests(vMouse, vKeyboard, ble);
            this.RunSingleFishBiteAndTrackingTests();
            this.RunReelingAndCancelTests(vMouse, vKeyboard, ble);

            yield return this.RunUpdateAndLateUpdateFrameTest(vMouse, vKeyboard, ble);
        }
        finally
        {
            this.CleanupCreatedObjects();
            Globals.ResetToDefaults();
            ResetDevices(vMouse, vKeyboard, ble);
            Controlls.inputActions.devices = previousDevices;
            InputSystem.RemoveDevice(vMouse);
            InputSystem.RemoveDevice(vKeyboard);
            Controlls.inputActions.Enable();
            InputSystem.settings.backgroundBehavior = prevBg;
            InputSystem.settings.editorInputBehaviorInPlayMode = prevEditorInput;
            InputSystem.Update();
            this._isRunning = false;
        }

        string summary = $"[ClassFishingRodUnitTests] Finished: {this._passed} passed, {this._failed} failed.";
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

    #region Initialization & Visuals Tests
    private void RunInitializationAndVisualTests()
    {
        // 1. Default state, runtime hierarchy, HookEmpty default sprite, and idle visibility
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("DefaultRod", new Vector3(10f, 20f, 0f));

            Transform hookTransform = rod.Visuals.HookTransform;
            SpriteRenderer hookRenderer = rod.Visuals.HookSpriteRenderer;
            LineRenderer lineRenderer = rod.Visuals.StringLineRenderer;

            bool ok =
                rod.CurrentState == RodState.Idle &&
                !rod.IsReady &&
                rod.HookedFish == null &&
                rod.ActiveBait == null &&
                rod.HookTipTransform != null &&
                Mathf.Approximately(rod.LastSpeedMultiplier, 1f) &&
                hookTransform != null &&
                hookRenderer != null &&
                hookRenderer.sprite != null &&
                hookRenderer.sprite.name.StartsWith("HookEmpty", StringComparison.Ordinal) &&
                lineRenderer != null &&
                lineRenderer.useWorldSpace &&
                lineRenderer.positionCount == 2 &&
                !hookTransform.gameObject.activeSelf &&
                !lineRenderer.enabled;

            this.Record("Default Idle state, HookEmpty sprite & LineRenderer setup", ok,
                $"state={rod.CurrentState}, sprite={hookRenderer?.sprite?.name}, lineEnabled={lineRenderer?.enabled}");
        });

        // 2. SetStringColor and ApplyStringColor update LineRenderer colors immediately
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("ColorRod");
            LineRenderer lineRenderer = rod.Visuals.StringLineRenderer;

            rod.ApplyStringColor(FishingLineType.Level2);
            Color goldExpected = new(1f, 0.84f, 0.2f, 1f);
            bool goldOk = ColorsApproxEqual(lineRenderer.startColor, goldExpected) && ColorsApproxEqual(lineRenderer.endColor, goldExpected);

            Color customColor = new(0.25f, 0.9f, 0.4f, 1f);
            rod.SetStringColor(customColor);
            bool customOk = ColorsApproxEqual(lineRenderer.startColor, customColor) && ColorsApproxEqual(lineRenderer.endColor, customColor);

            this.Record("SetStringColor & ApplyStringColor update LineRenderer colors", goldOk && customOk,
                $"goldOk={goldOk}, customOk={customOk}, startColor={lineRenderer.startColor}");
        });

        // 3. ScriptableString & ScriptableBait wiring drives AllowedMaxDepth, color & CurrentBaitFallSpeed
        this.RunIsolated(() =>
        {
            Color seafoam = new(0.2f, 0.85f, 0.75f, 1f);
            ScriptableFishingLine stringData = this.CreateScriptableString("DeepSilk", 420f, seafoam);
            ScriptableBait baitData = this.CreateScriptableBait("HeavyWorm", 85f, 250f, 1.4f, null);

            GameObject go = this.Track(new GameObject("ConfiguredRod"));
            go.SetActive(false);
            ClassFishingRod rod = go.AddComponent<ClassFishingRod>();
            SetPrivateField(rod, "equippedStringData", stringData);
            SetPrivateField(rod, "equippedBaitData", baitData);
            go.SetActive(true);

            LineRenderer lineRenderer = rod.Visuals.StringLineRenderer;
            bool awakeOk =
                rod.EquippedStringData == stringData &&
                rod.EquippedBaitData == baitData &&
                Mathf.Approximately(rod.AllowedMaxDepth, 420f) &&
                Mathf.Approximately(rod.CurrentBaitFallSpeed, 85f) &&
                ColorsApproxEqual(lineRenderer.startColor, seafoam);

            ScriptableFishingLine secondString = this.CreateScriptableString("SteelWire", 900f, Color.cyan);
            ScriptableBait secondBait = this.CreateScriptableBait("FastSpinner", 140f, 300f, 1.1f, null);
            rod.SetEquippedString(secondString);
            rod.SetEquippedBait(secondBait);

            bool setterOk =
                Mathf.Approximately(rod.AllowedMaxDepth, 900f) &&
                Mathf.Approximately(rod.CurrentBaitFallSpeed, 140f) &&
                ColorsApproxEqual(lineRenderer.startColor, Color.cyan);

            this.Record("ScriptableString & ScriptableBait configure depth, color & fallSpeed", awakeOk && setterOk,
                $"awakeOk={awakeOk}, setterOk={setterOk}, depth={rod.AllowedMaxDepth}, fallSpeed={rod.CurrentBaitFallSpeed}");
        });
    }
    #endregion

    #region Ready Mode & Lifecycle Tests
    private void RunReadyModeAndLifecycleTests(Mouse mouse, Keyboard keyboard, BleControllerDevice ble)
    {
        // 1. ToggleReady via Key.X toggles Idle <-> Ready; BLE (1,7) sets Ready on true and Idle on false (unless already cast)
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("ToggleReadyRod", new Vector3(5f, 10f, 0f));
            List<bool> readyEvents = new();
            List<RodState> stateEvents = new();
            rod.OnReadyModeChanged += v => readyEvents.Add(v);
            rod.OnStateChanged += s => stateEvents.Add(s);

            Transform hookTransform = rod.Visuals.HookTransform;
            LineRenderer lineRenderer = rod.Visuals.StringLineRenderer;

            // Key.X press -> Ready, second Key.X press -> Idle
            SendKeyboard(keyboard, new KeyboardState(Key.X));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            bool enteredReadyKey =
                rod.CurrentState == RodState.Ready &&
                rod.IsReady &&
                hookTransform.gameObject.activeSelf &&
                lineRenderer.enabled &&
                readyEvents.Count == 1 && readyEvents[0];

            SendKeyboard(keyboard, new KeyboardState(Key.X));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            bool exitedReadyKey =
                rod.CurrentState == RodState.Idle &&
                !rod.IsReady &&
                !hookTransform.gameObject.activeSelf &&
                !lineRenderer.enabled &&
                readyEvents.Count == 2 && !readyEvents[1];

            // BLE negative & below-minimum CastSpeed keep rod Idle; positive CastSpeed >= min auto-readies, casts, and 0 keeps Cast
            ble.ApplyPacket(MakeInt16Packet(true, 1, -4000), 3);
            InputSystem.Update();
            bool stayedIdleNeg = rod.CurrentState == RodState.Idle;

            ble.ApplyPacket(MakeInt16Packet(true, 1, 1000), 3);
            InputSystem.Update();
            bool stayedIdleBelowMin = rod.CurrentState == RodState.Idle;

            ble.ApplyPacket(MakeInt16Packet(true, 1, 6000), 3);
            InputSystem.Update();
            bool enteredReadyBle = rod.CurrentState == RodState.Ready && readyEvents.Count == 3 && readyEvents[2];

            rod.StepRod(0.05f);
            bool enteredSamplingBle = rod.CurrentState == RodState.SamplingCast;
            rod.StepRod(0.16f);
            ble.ApplyPacket(MakeInt16Packet(false, 1, 0), 3);
            InputSystem.Update();
            bool keptCastOnBleRelease = rod.CurrentState == RodState.Cast;

            this.Record("ToggleReady works from Key.X (toggle) and BLE CastSpeed (>= min positive)",
                enteredReadyKey && exitedReadyKey && stayedIdleNeg && stayedIdleBelowMin && enteredReadyBle && enteredSamplingBle && keptCastOnBleRelease,
                $"key={enteredReadyKey && exitedReadyKey}, ble={stayedIdleNeg && stayedIdleBelowMin && enteredReadyBle && enteredSamplingBle}, castKept={keptCastOnBleRelease}, states={stateEvents.Count}");
        });

        // 2. Cannot operate in Shop or when boat is not in Fishing mode; exiting Fishing resets to Idle
        this.RunIsolated(() =>
        {
            GameObject boatGo = this.Track(new GameObject("TestBoat"));
            ClassBoat boat = boatGo.AddComponent<ClassBoat>();
            Globals.boatInstance = boat;

            ClassFishingRod rod = this.CreateRod("BoatGatedRod");
            Controlls.fishingRodActions.Enable();

            // Boat starts with IsFishing == false
            rod.ToggleReady();
            bool blockedOutOfFishing = rod.CurrentState == RodState.Idle && !rod.CanOperateRod();

            // Enter boat fishing mode -> Ready allowed
            boat.SetFishingMode(true);
            rod.ToggleReady();
            bool allowedInFishing = rod.CurrentState == RodState.Ready && rod.CanOperateRod();

            // Leaving fishing mode resets active rod state to Idle
            boat.SetFishingMode(false);
            rod.StepRod(0.016f);
            bool resetOnExitFishing = rod.CurrentState == RodState.Idle;

            // Shop blocks operation and resets active rod state
            boat.SetFishingMode(true);
            rod.ToggleReady();
            Globals.isInShop = true;
            rod.StepRod(0.016f);
            bool resetInShop = rod.CurrentState == RodState.Idle && !rod.CanOperateRod();

            this.Record("Boat IsFishing and Globals.isInShop gate & reset rod state",
                blockedOutOfFishing && allowedInFishing && resetOnExitFishing && resetInShop,
                $"blocked={blockedOutOfFishing}, allowed={allowedInFishing}, exitReset={resetOnExitFishing}, shopReset={resetInShop}");
        });

        // 3. Globals.isPaused freezes rod progression without cancelling cast/hook
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("PausedRod", new Vector3(0f, 100f, 0f));
            SetPrivateField(rod.Physics, "airGravity", 10000f);
            SetPrivateField(rod.Physics, "baseCastDistance", 200f);

            rod.ExecuteCast(0.5f);
            rod.StepRod(0.3f); // Finish airborne arc to water surface and spawn/attach bait
            Vector3 castPos = rod.Visuals.HookTransform.position;

            Globals.isPaused = true;
            rod.StepRod(1.0f);
            bool stayedCastDuringPause =
                rod.CurrentState == RodState.Cast &&
                rod.ActiveBait != null &&
                Vector3.Distance(rod.Visuals.HookTransform.position, castPos) < 0.001f;

            Globals.isPaused = false;
            rod.StepRod(0.5f);
            bool resumedSinkingAfterUnpause =
                rod.CurrentState == RodState.Cast &&
                rod.Visuals.HookTransform.position.y < castPos.y;

            this.Record("Globals.isPaused freezes rod without cancelling active cast",
                stayedCastDuringPause && resumedSinkingAfterUnpause,
                $"paused={stayedCastDuringPause}, resumed={resumedSinkingAfterUnpause}");
        });
    }
    #endregion

    #region CastSpeed Sampling & +45° Airborne/Water Physics Tests
    private void RunCastSpeedSamplingAndPhysicsTests(Mouse mouse, Keyboard keyboard, BleControllerDevice ble)
    {
        // 1. CastSpeed below minThrowSpeed stays Ready; above minThrowSpeed samples peak over window & clamps [0.5, 1.5]
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("SamplingRod", new Vector3(0f, 200f, 0f));
            SetPrivateField(rod.Physics, "minThrowSpeed", 0.25f);
            SetPrivateField(rod.Physics, "maxThrowSpeed", 1.0f);
            SetPrivateField(rod.Physics, "castSampleDuration", 0.15f);
            SetPrivateField(rod.Physics, "baseCastDistance", 200f);
            SetPrivateField(rod.Physics, "airGravity", 10000f);

            rod.ToggleReady();

            // Mouse delta below threshold (10 * 0.02 = 0.20 < 0.25) -> remains Ready
            SendMouse(mouse, new MouseState { delta = new Vector2(10f, 0f) });
            InputSystem.Update();
            rod.StepRod(0.05f);
            bool belowThresholdStaysReady = rod.CurrentState == RodState.Ready;

            // Mouse delta crosses threshold (30 * 0.02 = 0.60 >= 0.25) -> SamplingCast
            SendMouse(mouse, new MouseState { delta = new Vector2(30f, 0f) });
            InputSystem.Update();
            rod.StepRod(0.05f);
            bool enteredSampling = rod.CurrentState == RodState.SamplingCast;

            // Higher peak during window (100 * 0.02 = 2.0 > 1.0 -> should clamp multiplier to 1.5x)
            SendMouse(mouse, new MouseState { delta = new Vector2(100f, 0f) });
            InputSystem.Update();
            rod.StepRod(0.05f);

            // Reset mouse and finish sampling window (0.15s total after entering SamplingCast)
            SendMouse(mouse, new MouseState { delta = Vector2.zero });
            InputSystem.Update();
            rod.StepRod(0.11f);

            float fastMult = rod.LastSpeedMultiplier;
            bool fastClampedOk =
                rod.CurrentState == RodState.Cast &&
                Mathf.Approximately(rod.LastPeakCastSpeed, 2.0f) &&
                Mathf.Approximately(fastMult, 1.5f);

            // Second cast via BLE CastSpeed (modus=1) right at minThrowSpeed (scaled 0.25 -> 0.5x)
            rod.CancelAndResetToIdle();
            rod.ToggleReady();

            short slowRaw = (short)Mathf.RoundToInt((0.25f / BleControllerDevice.CastSpeedScale) * 32767f);
            ble.ApplyPacket(MakeInt16Packet(true, 1, slowRaw), 3);
            InputSystem.Update();
            rod.StepRod(0.05f);
            rod.StepRod(0.16f);
            ble.ApplyPacket(MakeInt16Packet(false, 1, 0), 3);
            InputSystem.Update();

            float slowMult = rod.LastSpeedMultiplier;
            bool slowClampedOk =
                rod.CurrentState == RodState.Cast &&
                Mathf.Abs(slowMult - 0.5f) < 0.001f;

            this.Record("CastSpeed samples peak over window & clamps distance multiplier [0.5, 1.5]",
                belowThresholdStaysReady && enteredSampling && fastClampedOk && slowClampedOk,
                $"belowOk={belowThresholdStaysReady}, sampling={enteredSampling}, fastMult={fastMult:F3}, slowMult={slowMult:F5}");
        });

        // 2. Hook launches +45° airborne arc, enters water to spawn bait, sinks at scaled speed, clamps to String.MaxDepth
        this.RunIsolated(() =>
        {
            Vector3 origin = new(100f, 300f, 0f);
            ClassFishingRod rod = this.CreateRod("ArcAndDepthRod", origin);
            ScriptableFishingLine stringData = this.CreateScriptableString("TestString", 250f, Color.white);
            ScriptableBait baitData = this.CreateScriptableBait("SinkBait", 100f, 200f, 1f, this.CreateTestSprite());
            rod.SetEquippedString(stringData);
            rod.SetEquippedBait(baitData);
            SetPrivateField(rod.Physics, "baseCastDistance", 200f);
            SetPrivateField(rod.Physics, "airGravity", 600f);
            SetPrivateField(rod.Physics, "waterSurfaceOffsetY", 0f);
            SetPrivateField(rod.Physics, "waterSinkMultiplier", 0.25f);

            // Launch right (+1) at 1.0x speed -> hardcoded 45° velocity (vx == vy > 0)
            rod.ExecuteCast(Mathf.Lerp(0.25f, 1.0f, 0.5f));
            Vector2 launchVel = rod.Physics.Velocity;
            bool launchUp45Right =
                launchVel.x > 0f &&
                launchVel.y > 0f &&
                Mathf.Abs(launchVel.x - launchVel.y) < 0.01f;

            // Short airborne step stays above water with no bait spawned yet
            rod.StepRod(0.1f);
            Transform hookTransform = rod.Visuals.HookTransform;
            bool airbornePhaseOk =
                !rod.Physics.IsInWater &&
                rod.ActiveBait == null &&
                hookTransform.position.x > origin.x &&
                hookTransform.position.y > origin.y;

            // Step until hook hits waterY (~0.82s for dist=200, g=600) & spawns bait
            for (int i = 0; i < 60 && !rod.Physics.IsInWater; i++)
            {
                rod.StepRod(0.02f);
            }
            bool enteredWaterAndBaited =
                rod.Physics.IsInWater &&
                rod.ActiveBait != null &&
                rod.ActiveBait.IsAttachedToHook &&
                Mathf.Abs(hookTransform.position.y - origin.y) < 1f &&
                Mathf.Abs((hookTransform.position.x - origin.x) - 200f) < 10f;

            // Sink for 0.5s -> should descend at fallSpeed (100) * waterSinkMultiplier (0.25) * 0.5 = 12.5 units
            float yBeforeSink = hookTransform.position.y;
            rod.StepRod(0.5f);
            bool sankSlowlyInWater = Mathf.Abs((yBeforeSink - hookTransform.position.y) - 12.5f) < 0.5f;

            // Sink long enough to hit ScriptableString.MaxDepth (waterY - 250 = 50)
            rod.StepRod(12.0f);
            rod.StepRod(2.0f);
            float minY = origin.y - 250f;
            bool clampedAtStringMaxDepth = Mathf.Abs(hookTransform.position.y - minY) < 0.01f;

            // Mirror check when boat faces left (-1)
            GameObject boatGo = this.Track(new GameObject("FacingBoat"));
            ClassBoat boat = boatGo.AddComponent<ClassBoat>();
            SetPrivateField(boat, "facingDirection", -1);
            boat.SetFishingMode(true);
            Globals.boatInstance = boat;

            rod.CancelAndResetToIdle();
            rod.ExecuteCast(1.0f);
            Vector2 leftVel = rod.Physics.Velocity;
            bool launchUp45Left =
                leftVel.x < 0f &&
                leftVel.y > 0f &&
                Mathf.Abs(-leftVel.x - leftVel.y) < 0.01f;

            this.Record("Hook casts 45° air arc, enters water to spawn bait, sinks slowly to String.MaxDepth",
                launchUp45Right && airbornePhaseOk && enteredWaterAndBaited && sankSlowlyInWater && clampedAtStringMaxDepth && launchUp45Left,
                $"up45R={launchUp45Right}, air={airbornePhaseOk}, water={enteredWaterAndBaited}, sink={sankSlowlyInWater}, clamped={clampedAtStringMaxDepth}, up45L={launchUp45Left}");
        });
    }
    #endregion

    #region Single-Fish Bite & Cross-Parent Tracking Tests
    private void RunSingleFishBiteAndTrackingTests()
    {
        // Only one fish claims the bait across different parents; second fish stops hunting; hooked fish follows hook tip
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("HookBiteRod", new Vector3(0f, 300f, 0f));
            SetPrivateField(rod.Physics, "baseCastDistance", 200f);
            SetPrivateField(rod.Physics, "airGravity", 10000f);
            SetPrivateField(rod, "defaultBaitFallSpeed", 0f);
            rod.ExecuteCast(0.5f);
            rod.StepRod(0.3f);

            ClassBait bait = rod.ActiveBait;
            Vector2 baitWorld = bait.GetWorldPosition();

            // Place two fish in separate parent transforms near the bait
            ScriptableFish fishData = this.CreateScriptableFish("Barsch", FishType.Barsch, 100f, 150f, 25f, 300f, 500f);
            ClassFish fish1 = this.CreateFish("Fish_ParentA", "Chunk_1_1", new Vector3(50f, 25f, 0f), fishData, baitWorld + new Vector2(10f, 0f));
            ClassFish fish2 = this.CreateFish("Fish_ParentB", "Chunk_1_2", new Vector3(-40f, 80f, 0f), fishData, baitWorld + new Vector2(20f, 0f));

            // Suppress expected log spam from fish roaming/claiming
            bool prevLogEnabled = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;

                // Both start hunting bait
                InvokePrivateMethod(fish1, "CheckForBait");
                InvokePrivateMethod(fish2, "CheckForBait");

                // Fish 1 arrives first within stopDistance and claims
                fish1.transform.position = new Vector3(baitWorld.x, baitWorld.y, fish1.transform.position.z);
                InvokePrivateMethod(fish1, "UpdateMoving");

                // Fish 2 tries after Fish 1 claimed -> CheckForBait + TryClaim must reject Fish 2
                InvokePrivateMethod(fish2, "CheckForBait");
                bool secondClaimRejected = !bait.TryClaim(fish2);

                // Move hook tip elsewhere and verify Fish 1 follows in UpdateStuck
                Transform hookTransform = rod.Visuals.HookTransform;
                hookTransform.position = new Vector3(123f, 234f, hookTransform.position.z);
                InvokePrivateMethod(bait, "SyncPositionWithHook");
                InvokePrivateMethod(fish1, "UpdateStuck");

                bool singleHookOk =
                    rod.CurrentState == RodState.Hooked &&
                    rod.HookedFish == fish1 &&
                    bait.IsOccupied &&
                    bait.ClaimedByFish == fish1 &&
                    fish1.IsHooked &&
                    !fish2.IsHooked &&
                    secondClaimRejected;

                bool followOk = Vector2.Distance((Vector2)fish1.transform.position, new Vector2(123f, 234f)) < 0.05f;

                this.Record("Only first fish claims bait across parents & follows hook tip",
                    singleHookOk && followOk,
                    $"singleHook={singleHookOk}, follow={followOk}, rodState={rod.CurrentState}");
            }
            finally
            {
                Debug.unityLogger.logEnabled = prevLogEnabled;
            }
        });
    }
    #endregion

    #region Reeling, Completion Action & Cancel Tests
    private void RunReelingAndCancelTests(Mouse mouse, Keyboard keyboard, BleControllerDevice ble)
    {
        // 1. Reeling pulls hook back, pauses mid-reel to Hooked on release, and fires OnFishReeledIn before destroying fish
        this.RunIsolated(() =>
        {
            Vector3 origin = Vector3.zero;
            ClassFishingRod rod = this.CreateRod("ReelCatchRod", origin);
            SetPrivateField(rod.Physics, "baseCastDistance", 200f);
            SetPrivateField(rod.Physics, "airGravity", 10000f);
            SetPrivateField(rod.Physics, "reelSpeed", 100f);
            SetPrivateField(rod.Physics, "reelCompleteDistance", 25f);
            SetPrivateField(rod, "defaultBaitWeight", 2f); // effective reelSpeed = 100 / 2 = 50/s
            SetPrivateField(rod, "defaultBaitFallSpeed", 0f);

            bool callbackReceivedValidFish = false;
            ClassFish eventFish = null;
            int callbackCount = 0;
            rod.OnFishReeledIn += fish =>
            {
                callbackCount++;
                eventFish = fish;
                callbackReceivedValidFish = fish != null && fish.gameObject != null && fish.FishName == "Forelle";
            };

            rod.ExecuteCast(0.5f);
            rod.StepRod(0.3f);
            Transform hookTransform = rod.Visuals.HookTransform;
            hookTransform.position = new Vector3(100f, 0f, 0f);
            InvokePrivateMethod(rod.ActiveBait, "SyncPositionWithHook");

            ScriptableFish fishData = this.CreateScriptableFish("Forelle", FishType.Forelle, 80f, 120f, 15f, 200f, 400f);
            ClassFish fish = this.CreateFish("HookedForelle", "Pond", Vector3.zero, fishData, new Vector2(100f, 0f));

            bool prevLogEnabled = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;
                rod.ActiveBait.TryClaim(fish);
            }
            finally
            {
                Debug.unityLogger.logEnabled = prevLogEnabled;
            }

            // Hold Mouse.Right to reel for 1.0s (should move 50 units toward origin -> x ~ 50)
            SendMouse(mouse, new MouseState().WithButton(MouseButton.Right, true));
            InputSystem.Update();
            rod.StepRod(1.0f);
            bool reelingMovesTowardOrigin =
                rod.CurrentState == RodState.Reeling &&
                Mathf.Abs(hookTransform.position.x - 50f) < 0.5f;

            // Release Rell mid-reel -> returns cleanly to Hooked
            SendMouse(mouse, new MouseState());
            InputSystem.Update();
            rod.StepRod(0.016f);
            bool returnedToHookedOnRelease = rod.CurrentState == RodState.Hooked;

            // Reel via BLE Rell handcrank (modus=0, -1 direction) into reelCompleteDistance
            ble.ApplyPacket(MakeInt16Packet(true, 0, -1), 3);
            InputSystem.Update();
            rod.StepRod(1.1f);
            ble.ApplyPacket(MakeInt16Packet(false, 0, 0), 3);
            InputSystem.Update();

            bool completedOk =
                callbackCount == 1 &&
                callbackReceivedValidFish &&
                eventFish == fish &&
                rod.CurrentState == RodState.Idle &&
                rod.HookedFish == null &&
                rod.ActiveBait == null;

            this.Record("Reel-in pulls by weight, handles mid-release, and invokes OnFishReeledIn before cleanup",
                reelingMovesTowardOrigin && returnedToHookedOnRelease && completedOk,
                $"moving={reelingMovesTowardOrigin}, midRelease={returnedToHookedOnRelease}, completed={completedOk}");
        });

        // 2. Empty-hook reel-in does not invoke OnFishReeledIn, cleans up bait, and returns to Idle
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("EmptyReelRod", Vector3.zero);
            SetPrivateField(rod.Physics, "baseCastDistance", 100f);
            SetPrivateField(rod.Physics, "airGravity", 10000f);
            SetPrivateField(rod.Physics, "reelSpeed", 500f);
            SetPrivateField(rod.Physics, "reelCompleteDistance", 25f);

            int callbackCount = 0;
            rod.OnFishReeledIn += fish => callbackCount++;

            rod.ExecuteCast(0.5f);
            rod.StepRod(0.3f);
            bool hadBaitBeforeReel = rod.ActiveBait != null;

            SendMouse(mouse, new MouseState().WithButton(MouseButton.Right, true));
            InputSystem.Update();
            rod.StepRod(1.0f);
            SendMouse(mouse, new MouseState());
            InputSystem.Update();

            bool ok =
                hadBaitBeforeReel &&
                callbackCount == 0 &&
                rod.CurrentState == RodState.Idle &&
                rod.ActiveBait == null;

            this.Record("Empty-hook reel-in skips OnFishReeledIn, cleans up bait, and resets to Idle", ok,
                $"hadBait={hadBaitBeforeReel}, calls={callbackCount}, state={rod.CurrentState}");
        });

        // 3. CancelCast via Key.C during Hooked releases fish back to roaming and resets rod to Idle
        this.RunIsolated(() =>
        {
            ClassFishingRod rod = this.CreateRod("CancelRod", Vector3.zero);
            SetPrivateField(rod.Physics, "baseCastDistance", 100f);
            SetPrivateField(rod.Physics, "airGravity", 10000f);
            SetPrivateField(rod, "defaultBaitFallSpeed", 0f);
            rod.ExecuteCast(0.5f);
            rod.StepRod(0.3f);

            ScriptableFish fishData = this.CreateScriptableFish("Hecht", FishType.Hecht, 90f, 140f, 30f, 200f, 400f);
            ClassFish fish = this.CreateFish("ReleasedHecht", "Pond", Vector3.zero, fishData, Vector2.zero);

            bool prevLogEnabled = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;
                rod.ActiveBait.TryClaim(fish);
            }
            finally
            {
                Debug.unityLogger.logEnabled = prevLogEnabled;
            }

            SendKeyboard(keyboard, new KeyboardState(Key.C));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            bool keyCancelOk =
                rod.CurrentState == RodState.Idle &&
                rod.HookedFish == null &&
                rod.ActiveBait == null &&
                !fish.IsHooked;

            // BLE buttonPressed (id=3) also cancels Cast back to Idle
            rod.ExecuteCast(0.5f);
            ble.ApplyPacket(MakeButtonPacket(true, 3), 1);
            InputSystem.Update();
            ble.ApplyPacket(MakeButtonPacket(false, 3), 1);
            InputSystem.Update();

            bool bleCancelOk = rod.CurrentState == RodState.Idle;

            this.Record("CancelCast (Key.C & BLE buttonPressed) releases hooked fish and resets rod to Idle",
                keyCancelOk && bleCancelOk,
                $"keyCancel={keyCancelOk}, bleCancel={bleCancelOk}, fishHooked={fish.IsHooked}");
        });
    }
    #endregion

    #region Play Mode Frame Integration
    private IEnumerator RunUpdateAndLateUpdateFrameTest(Mouse mouse, Keyboard keyboard, BleControllerDevice ble)
    {
        ClassFishingRod rod = null;
        try
        {
            Vector3 origin = new(12f, 34f, 0f);
            rod = this.CreateRod("FrameIntegrationRod", origin);

            SendKeyboard(keyboard, new KeyboardState(Key.X));
            InputSystem.Update();
            SendKeyboard(keyboard, new KeyboardState());
            InputSystem.Update();

            // Wait two real frames so Start(), Update(), and LateUpdate() execute
            yield return null;
            yield return null;

            LineRenderer line = rod.Visuals.StringLineRenderer;
            Vector3 p0 = line.GetPosition(0);
            Vector3 p1 = line.GetPosition(1);

            bool ok =
                rod.CurrentState == RodState.Ready &&
                line.enabled &&
                Vector3.Distance(p0, origin) < 0.01f &&
                Vector3.Distance(p1, rod.HookTipTransform.position) < 0.01f;

            this.Record("Play Mode Start/Update/LateUpdate syncs Ready state & LineRenderer endpoints", ok,
                $"state={rod.CurrentState}, p0={p0}, p1={p1}");
        }
        finally
        {
            ResetDevices(mouse, keyboard, ble);
            if (rod != null)
            {
                DestroyImmediate(rod.gameObject);
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
            this.Record("Unexpected exception in ClassFishingRod test", false, ex.ToString());
        }
        finally
        {
            this.CleanupCreatedObjects();
            Globals.ResetToDefaults();
            Controlls.fishingRodActions.Disable();
            Controlls.inputActions.Enable();
            Controlls.fishingRodActions.Enable();
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

    private ClassFishingRod CreateRod(string name, Vector3? position = null)
    {
        GameObject go = this.Track(new GameObject(name));
        if (position.HasValue)
        {
            go.transform.position = position.Value;
        }
        ClassFishingRod rod = go.AddComponent<ClassFishingRod>();
        if (!Controlls.fishingRodActions.enabled)
        {
            Controlls.fishingRodActions.Enable();
        }
        return rod;
    }

    private ClassFish CreateFish(string name, string parentName, Vector3 parentWorldPos, ScriptableFish data, Vector2 worldPos)
    {
        GameObject parentGo = this.Track(new GameObject(parentName));
        parentGo.transform.position = parentWorldPos;

        GameObject fishGo = this.Track(new GameObject(name));
        fishGo.transform.SetParent(parentGo.transform, false);
        fishGo.AddComponent<RectTransform>();
        fishGo.AddComponent<SpriteRenderer>();

        Vector3 localPos = parentGo.transform.InverseTransformPoint(new Vector3(worldPos.x, worldPos.y, 0f));
        ClassFish fish = fishGo.AddComponent<ClassFish>();
        bool prevLogEnabled = Debug.unityLogger.logEnabled;
        try
        {
            Debug.unityLogger.logEnabled = false;
            fish.InitFish(data, localPos.x, localPos.y);
        }
        finally
        {
            Debug.unityLogger.logEnabled = prevLogEnabled;
        }

        return fish;
    }

    private ScriptableFishingLine CreateScriptableString(string name, float maxDepth, Color color)
    {
        ScriptableFishingLine data = this.Track(ScriptableObject.CreateInstance<ScriptableFishingLine>());
        SetPrivateField(data, "stringName", name);
        SetPrivateField(data, "maxDepth", maxDepth);
        SetPrivateField(data, "color", color);
        return data;
    }

    private ScriptableBait CreateScriptableBait(string name, float fallSpeed, float attractRadius, float weightMultiplier, Sprite sprite)
    {
        ScriptableBait data = this.Track(ScriptableObject.CreateInstance<ScriptableBait>());
        SetPrivateField(data, "baitName", name);
        SetPrivateField(data, "fallSpeed", fallSpeed);
        SetPrivateField(data, "attractRadius", attractRadius);
        SetPrivateField(data, "weightMultiplier", weightMultiplier);
        SetPrivateField(data, "sprite", sprite);
        return data;
    }

    private Sprite CreateTestSprite()
    {
        Texture2D tex = this.Track(new Texture2D(4, 4));
        Sprite sprite = this.Track(Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));
        return sprite;
    }

    private ScriptableFish CreateScriptableFish(
        string fishName,
        FishType fishType,
        float speed,
        float boostedSpeed,
        float defaultPrice,
        float roamRadius,
        float huntRadius)
    {
        ScriptableFish data = this.Track(ScriptableObject.CreateInstance<ScriptableFish>());
        SetPrivateField(data, "fishName", fishName);
        SetPrivateField(data, "fishType", fishType);
        SetPrivateField(data, "speed", speed);
        SetPrivateField(data, "boostedSpeed", boostedSpeed);
        SetPrivateField(data, "defaultPrice", defaultPrice);
        SetPrivateField(data, "roamRadius", roamRadius);
        SetPrivateField(data, "huntRadius", huntRadius);
        return data;
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

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
        return (T)field.GetValue(target);
    }

    private static void InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new MissingMethodException(target.GetType().Name, methodName);
        }
        method.Invoke(target, args);
    }

    private static bool ColorsApproxEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f
            && Mathf.Abs(a.g - b.g) < 0.01f
            && Mathf.Abs(a.b - b.b) < 0.01f
            && Mathf.Abs(a.a - b.a) < 0.01f;
    }

    private static byte[] MakeButtonPacket(bool active, byte modus) =>
        new[] { (byte)((active ? 1 : 0) | ((modus & 0b11) << 1)) };

    private static byte[] MakeInt16Packet(bool active, byte modus, short value) =>
        new[]
        {
            (byte)((active ? 1 : 0) | ((modus & 0b11) << 1)),
            (byte)(value & 0xFF),
            (byte)((value >> 8) & 0xFF)
        };

    private static void SendMouse(Mouse device, MouseState state) => InputSystem.QueueStateEvent(device, state);
    private static void SendKeyboard(Keyboard device, KeyboardState state) => InputSystem.QueueStateEvent(device, state);

    private static void ResetDevices(Mouse mouse, Keyboard keyboard, BleControllerDevice ble)
    {
        if (mouse != null)
        {
            SendMouse(mouse, new MouseState());
            InputSystem.ResetDevice(mouse, alsoResetDontResetControls: true);
        }
        if (keyboard != null)
        {
            SendKeyboard(keyboard, new KeyboardState());
        }
        if (ble != null)
        {
            ble.ApplyPacket(MakeInt16Packet(false, 0, 0), 3);
            ble.ApplyPacket(MakeInt16Packet(false, 1, 0), 3);
            ble.ApplyPacket(MakeButtonPacket(false, 3), 1);
        }
        Controlls.fishingRodActions.Rell.Reset();
        Controlls.fishingRodActions.Aim.Reset();
        Controlls.fishingRodActions.CastSpeed.Reset();
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
    #endregion
}
