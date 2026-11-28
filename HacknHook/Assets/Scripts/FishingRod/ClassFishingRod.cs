using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RodState
{
    Idle,
    Ready,
    SamplingCast,
    Cast,
    Hooked,
    Reeling
}

public class ClassFishingRod : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private Transform rodOrigin;
    [SerializeField] private FishingRodVisuals visuals = new FishingRodVisuals();
    [SerializeField] private FishingHookPhysics physics = new FishingHookPhysics();

    [Header("Equipment Defaults")]
    [SerializeField] private ScriptableFishingLine equippedStringData;
    [SerializeField] private float defaultMaxDepth = 500f;
    [SerializeField] private ScriptableBait equippedBaitData;
    [SerializeField] private string defaultBaitName = "DefaultBait";
    [SerializeField] private float defaultBaitFallSpeed = 50f;
    [SerializeField] private float defaultAttractRadius = 200f;
    [SerializeField] private float defaultBaitWeight = 1f;
    [SerializeField] private bool destroyFishOnCatchComplete = true;
    #endregion

    #region Private Variables
    private RodState currentState = RodState.Idle;
    private ClassFish hookedFish;
    private ClassBait activeBait;
    private bool isQuitting;
    #endregion

    #region Events
    public event Action<ClassFish> OnFishReeledIn;
    public event Action<RodState> OnStateChanged;
    public event Action<bool> OnReadyModeChanged;
    #endregion

    #region Properties
    public RodState CurrentState => this.currentState;
    public bool IsReady => this.currentState == RodState.Ready || this.currentState == RodState.SamplingCast;
    public ClassFish HookedFish => this.hookedFish;
    public ClassBait ActiveBait => this.activeBait;
    public Transform HookTipTransform => this.visuals.HookTipTransform;
    public FishingRodVisuals Visuals => this.visuals;
    public FishingHookPhysics Physics => this.physics;
    public ScriptableFishingLine EquippedStringData => this.equippedStringData;
    public ScriptableBait EquippedBaitData => this.equippedBaitData;
    public float AllowedMaxDepth => this.equippedStringData != null ? Mathf.Max(0f, this.equippedStringData.Deapth) : Mathf.Max(0f, this.defaultMaxDepth);
    public float CurrentBaitFallSpeed => this.activeBait != null ? Mathf.Max(0f, this.activeBait.FallSpeed) : (this.equippedBaitData != null ? Mathf.Max(0f, this.equippedBaitData.FallSpeed) : Mathf.Max(0f, this.defaultBaitFallSpeed));
    public float LastPeakCastSpeed => this.physics.LastPeakCastSpeed;
    public float LastSpeedMultiplier => this.physics.LastSpeedMultiplier;
    #endregion

    #region Unity Funcs
    private void Awake()
    {
        if (hookCameraSwitcher == null)
        {
            hookCameraSwitcher = GetComponent<HookCameraSwitcher>();
        }

        if (hookCameraSwitcher == null)
        {
            Debug.LogError("HookCameraSwitcher fehlt auf diesem GameObject.", this);
        }

        if (this.rodOrigin == null)
        {
            this.rodOrigin = this.transform;
        }

        this.visuals.Initialize(this.transform, this.equippedStringData);
        this.currentState = RodState.Idle;
        this.visuals.PositionHookAtReady(this.GetRodOriginPosition());
        this.visuals.UpdateVisibility(this.currentState);
    }

    private void Start()
    {
        if (Globals.boatInstance == null && UnityEngine.Object.FindAnyObjectByType<ClassBoat>() == null)
        {
            if (!Controlls.inputActions.asset.enabled)
            {
                Controlls.inputActions.Enable();
            }

            if (!Controlls.fishingRodActions.enabled)
            {
                Controlls.fishingRodActions.Enable();
            }
        }
    }

    private void OnEnable()
    {
        Controlls.fishingRodActions.ToggleReady.performed += this.OnToggleReadyPerformed;
        Controlls.fishingRodActions.CancelCast.performed += this.OnCancelCastPerformed;
    }

    private void OnDisable()
    {
        Controlls.fishingRodActions.ToggleReady.performed -= this.OnToggleReadyPerformed;
        Controlls.fishingRodActions.CancelCast.performed -= this.OnCancelCastPerformed;
    }

    private void OnApplicationQuit() => this.isQuitting = true;

    private void OnDestroy()
    {
        this.OnFishReeledIn = null;
        this.OnStateChanged = null;
        this.OnReadyModeChanged = null;

        this.CleanupActiveBait();
        this.ReleaseHookedFish(releaseFromBait: !this.isQuitting);
        this.visuals.Cleanup(DestroyRuntimeObject);
    }

    private void Update() => this.StepRod(Time.deltaTime);

    private void LateUpdate()
    {
        if (this.currentState == RodState.Idle || this.IsReady)
        {
            this.visuals.PositionHookAtReady(this.GetRodOriginPosition());
        }

        this.visuals.SyncLine(this.GetRodOriginPosition(), this.currentState);
    }
    #endregion

    #region Public Funcs
    public bool CanOperateRod()
    {
        return !Globals.isPaused && !Globals.isInShop && (Globals.boatInstance == null || Globals.boatInstance.IsFishing);
    }

    public void ToggleReady()
    {
        if (!this.CanOperateRod())
        {
            return;
        }

        if (this.currentState == RodState.Idle)
        {
            this.physics.ResetState();
            this.visuals.PositionHookAtReady(this.GetRodOriginPosition());
            this.visuals.SetHookSprite(HookVisualState.Empty);
            this.SetState(RodState.Ready);
            this.OnReadyModeChanged?.Invoke(true);
        }
        else if (this.IsReady)
        {
            this.physics.ResetState();
            this.visuals.PositionHookAtReady(this.GetRodOriginPosition());
            this.visuals.SetHookSprite(HookVisualState.Empty);
            this.SetState(RodState.Idle);
            this.OnReadyModeChanged?.Invoke(false);
        }
    }

    public void StepRod(float deltaTime)
    {
        if (deltaTime < 0f || Globals.isPaused)
        {
            return;
        }

        if (!this.CanOperateRod())
        {
            if (this.currentState != RodState.Idle)
            {
                this.CancelAndResetToIdle();
            }

            return;
        }

        this.SyncHookedFishClaimState();

        switch (this.currentState)
        {
            case RodState.Ready:
            case RodState.SamplingCast:
                this.UpdateReadyAndSampling(deltaTime);
                break;
            case RodState.Cast:
            case RodState.Hooked:
            case RodState.Reeling:
                this.UpdateCastHookedAndReeling(deltaTime);
                break;
        }
    }

    public void ExecuteCast(float peakSpeed)
    {
        this.CleanupActiveBait();
        this.ReleaseHookedFish(releaseFromBait: true);

        bool wasReady = this.IsReady;
        int facingDirection = Globals.boatInstance != null && Globals.boatInstance.FacingDirection < 0 ? -1 : 1;

        this.physics.LaunchCast(this.visuals.HookTransform, this.GetRodOriginPosition(), peakSpeed, facingDirection);
        this.visuals.SetHookSprite(HookVisualState.Empty);
        this.SetState(RodState.Cast);

        if (wasReady)
        {
            this.OnReadyModeChanged?.Invoke(false);
        }

        Debug.Log($"[ClassFishingRod.ExecuteCast] Cast (peak={peakSpeed:F3}, mult={this.LastSpeedMultiplier:F2}, facing={facingDirection}).");
    }

    public void CancelAndResetToIdle()
    {
        if (this.currentState == RodState.Idle && this.activeBait == null && this.hookedFish == null)
        {
            return;
        }

        bool wasReady = this.IsReady;
        this.physics.ResetState();
        this.CleanupActiveBait();
        this.ReleaseHookedFish(releaseFromBait: true);

        this.visuals.PositionHookAtReady(this.GetRodOriginPosition());
        this.visuals.SetHookSprite(HookVisualState.Empty);
        this.SetState(RodState.Idle);

        if (wasReady)
        {
            this.OnReadyModeChanged?.Invoke(false);
        }
    }

    public void SetStringColor(Color color) => this.visuals.SetStringColor(color);
    public void ApplyStringColor(FishingLineType stringType) => this.visuals.ApplyStringColor(stringType);

    public void SetEquippedString(ScriptableFishingLine stringData)
    {
        this.equippedStringData = stringData;
        if (stringData != null)
        {
            this.visuals.SetStringColor(Color.white);
        }
    }

    public void SetEquippedBait(ScriptableBait baitData) => this.equippedBaitData = baitData;
    #endregion

    #region Private Funcs
    private void OnToggleReadyPerformed(InputAction.CallbackContext _) => this.ToggleReady();

    private void OnCancelCastPerformed(InputAction.CallbackContext _)
    {
        if (this.CanOperateRod())
        {
            this.CancelAndResetToIdle();
        }
    }

    private Vector3 GetRodOriginPosition() => this.rodOrigin != null ? this.rodOrigin.position : this.transform.position;

    private void UpdateReadyAndSampling(float deltaTime)
    {
        this.visuals.PositionHookAtReady(this.GetRodOriginPosition());

        float rawSpeed = Controlls.fishingRodActions.CastSpeed.ReadValue<float>();
        CastSamplingResult result = this.physics.StepSampling(this.currentState, rawSpeed, deltaTime, out float sampledPeak);

        if (result == CastSamplingResult.StartedSampling)
        {
            this.SetState(RodState.SamplingCast);
        }
        else if (result == CastSamplingResult.ReadyToCast)
        {
            this.ExecuteCast(sampledPeak);
        }
    }

    private void UpdateCastHookedAndReeling(float deltaTime)
    {
        bool isReeling = Controlls.fishingRodActions.Rell.IsPressed();
        Vector3 originPos = this.GetRodOriginPosition();

        if (isReeling)
        {
            this.physics.ClearVelocity();
            this.SetState(RodState.Reeling);
            if (this.physics.StepReeling(this.visuals.HookTransform, originPos, deltaTime, this.activeBait, this.hookedFish))
            {
                this.CompleteReelIn();
            }

            return;
        }

        if (this.currentState == RodState.Reeling)
        {
            if (this.hookedFish != null)
            {
                this.SetState(RodState.Hooked);
            }
            else
            {
                if (this.physics.IsInWater && this.activeBait == null)
                {
                    this.SpawnAndAttachBait();
                }

                this.SetState(RodState.Cast);
            }
        }

        if (this.currentState == RodState.Cast)
        {
            this.physics.StepAirborneAndWater(
                this.visuals.HookTransform,
                originPos,
                deltaTime,
                this.CurrentBaitFallSpeed,
                this.AllowedMaxDepth,
                this.SpawnAndAttachBait
            );
        }
    }

    private void SyncHookedFishClaimState()
    {
        if (this.activeBait != null && this.hookedFish == null && this.activeBait.ClaimedByFish != null)
        {
            this.HandleFishClaimed(this.activeBait.ClaimedByFish);
        }

        if (this.currentState != RodState.Hooked && this.currentState != RodState.Reeling)
        {
            return;
        }

        bool hadFishRef = !object.ReferenceEquals(this.hookedFish, null);
        bool fishInvalid = (hadFishRef && this.hookedFish == null)
            || (this.hookedFish != null && this.activeBait != null && this.activeBait.ClaimedByFish != this.hookedFish)
            || (this.currentState == RodState.Hooked && this.hookedFish == null);

        if (!fishInvalid)
        {
            return;
        }

        this.hookedFish = null;
        this.visuals.SetHookSprite(this.activeBait != null ? HookVisualState.Baited : HookVisualState.Empty);
        if (this.currentState == RodState.Hooked)
        {
            this.SetState(RodState.Cast);
        }
    }

    [SerializeField] private HookCameraSwitcher hookCameraSwitcher;

    private void SpawnAndAttachBait()
    {
        this.CleanupActiveBait();

        GameObject baitObject = new GameObject("HookBait");
        baitObject.transform.SetParent(this.visuals.HookTipTransform, false);

        RectTransform baitRect = baitObject.AddComponent<RectTransform>();
        baitRect.localPosition = Vector3.zero;
        baitRect.localScale = Vector3.one;

        SpriteRenderer baitRenderer = baitObject.AddComponent<SpriteRenderer>();
        baitRenderer.sortingOrder = this.visuals.BaitSortingOrder;

        ClassBait bait = baitObject.AddComponent<ClassBait>();
        if (this.equippedBaitData != null)
        {
            bait.Init(this.equippedBaitData);
        }
        else
        {
            bait.Init(this.defaultBaitName, this.defaultAttractRadius, this.defaultBaitFallSpeed, this.defaultBaitWeight, null);
        }

        bait.AttachToHook(this.visuals.HookTipTransform);
        bait.OnFishClaimed += this.HandleFishClaimed;
        this.activeBait = bait;
        this.visuals.SetHookSprite(HookVisualState.Baited);
        this.activeBait = bait;
        this.visuals.SetHookSprite(HookVisualState.Baited);

        if (this.hookCameraSwitcher != null)
        {
            this.hookCameraSwitcher.HookWasThrown(baitObject.transform);
        }
    }

    private void HandleFishClaimed(ClassFish fish)
    {
        if (fish == null || this.hookedFish != null)
        {
            return;
        }

        if (this.currentState != RodState.Cast && this.currentState != RodState.Reeling)
        {
            return;
        }

        this.hookedFish = fish;
        this.visuals.SetHookSprite(HookVisualState.Fish);
        this.SetState(RodState.Hooked);
    }

    private void CompleteReelIn()
    {
        ClassFish caughtFish = this.hookedFish;
        try
        {
            if (caughtFish != null)
            {
                this.OnFishReeledIn?.Invoke(caughtFish);
            }
        }
        finally
        {
            this.CleanupActiveBait();
            if (caughtFish != null && this.hookedFish == caughtFish)
            {
                caughtFish.CompleteCatch(this.destroyFishOnCatchComplete);
            }

            this.hookedFish = null;
            this.physics.ResetState();
            this.visuals.PositionHookAtReady(this.GetRodOriginPosition());
            this.visuals.SetHookSprite(HookVisualState.Empty);
            this.SetState(RodState.Idle);
        }
    }

    private void CleanupActiveBait()
    {
        if (this.activeBait == null)
        {
            return;
        }

        this.activeBait.OnFishClaimed -= this.HandleFishClaimed;
        this.activeBait.ReleaseClaim();
        this.activeBait.DetachFromHook();

        GameObject baitGameObject = this.activeBait.gameObject;
        this.activeBait = null;
        if (baitGameObject != null)
        {
            baitGameObject.SetActive(false);
            DestroyRuntimeObject(baitGameObject);
        }
    }

    private void ReleaseHookedFish(bool releaseFromBait)
    {
        if (this.hookedFish != null)
        {
            if (releaseFromBait)
            {
                this.hookedFish.ReleaseFromHook();
            }

            this.hookedFish = null;
        }
    }

    private void SetState(RodState newState)
    {
        if (this.currentState == newState)
        {
            this.visuals.UpdateVisibility(this.currentState);
            return;
        }

        this.currentState = newState;
        this.visuals.UpdateVisibility(this.currentState);
        this.OnStateChanged?.Invoke(this.currentState);
    }

    private static void DestroyRuntimeObject(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(obj);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
    #endregion
}
