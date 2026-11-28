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
    [Header("Origins & Visuals")]
    [SerializeField] private Transform rodOrigin;
    [SerializeField] private Transform hookTransform;
    [SerializeField] private Transform hookTipTransform;
    [SerializeField] private Vector2 hookTipLocalOffset = Vector2.zero;
    [SerializeField] private SpriteRenderer hookSpriteRenderer;
    [SerializeField] private Sprite hookEmptySprite;
    [SerializeField] private Sprite hookBaitedSprite;
    [SerializeField] private Sprite hookFishSprite;
    [SerializeField] private Vector3 hookScale = Vector3.one;
    [SerializeField] private string hookSortingLayerName = "Default";
    [SerializeField] private int hookSortingOrder = 15;
    [SerializeField] private bool hideHookWhenIdle = true;
    [SerializeField] private Vector2 readyHookOffset = new Vector2(0f, -1f);

    [Header("String / LineRenderer")]
    [SerializeField] private ScriptableString equippedStringData;
    [SerializeField] private LineRenderer stringLineRenderer;
    [SerializeField] private Color stringColor = Color.white;
    [SerializeField] private float stringWidth = 0.05f;
    [SerializeField] private string stringSortingLayerName = "Default";
    [SerializeField] private int stringSortingOrder = 14;
    [SerializeField] private float fallbackMaxDepth = 500f;

    [Header("Throw / Cast Speed Sampling")]
    [SerializeField] private float minThrowSpeed = 0.25f;
    [SerializeField] private float maxThrowSpeed = 1.0f;
    [SerializeField] private float castSampleDuration = 0.15f;
    [SerializeField] private bool onlyPositiveCastSpeed = true;
    [SerializeField] private float baseCastDistance = 240f;
    [SerializeField] private float castAngleDegrees = 45f;
    [SerializeField] private float castTravelSpeed = 600f;

    [Header("Reeling")]
    [SerializeField] private float reelSpeed = 500f;
    [SerializeField] private float reelCompleteDistance = 20f;
    [SerializeField] private bool destroyFishOnCatchComplete = true;

    [Header("Bait")]
    [SerializeField] private ScriptableBait equippedBaitData;
    [SerializeField] private string fallbackBaitName = "DefaultBait";
    [SerializeField] private float fallbackBaitFallSpeed = 50f;
    [SerializeField] private float fallbackAttractRadius = 200f;
    [SerializeField] private float fallbackBaitWeight = 1f;
    #endregion

    #region Private Variables
    private RodState currentState = RodState.Idle;
    private ClassFish hookedFish;
    private ClassBait activeBait;
    private float lastPeakCastSpeed;
    private float lastSpeedMultiplier = 1f;
    private float peakSampleSpeed;
    private float castSampleTimer;
    private bool isTravelingToCastTarget;
    private Vector3 castTravelTarget;
    private Material runtimeStringMaterial;
    private bool isQuitting;
    #endregion

    #region Events
    public event Action<ClassFish> OnFishReeledIn;
    public event Action<RodState> OnStateChanged;
    public event Action<bool> OnReadyModeChanged;
    #endregion

    #region Properties
    public RodState CurrentState
    {
        get => this.currentState;
        private set => this.currentState = value;
    }

    public bool IsReady => this.currentState == RodState.Ready || this.currentState == RodState.SamplingCast;
    public ClassFish HookedFish => this.hookedFish;
    public ClassBait ActiveBait => this.activeBait;
    public Transform HookTipTransform => this.hookTipTransform;
    public ScriptableString EquippedStringData => this.equippedStringData;
    public ScriptableBait EquippedBaitData => this.equippedBaitData;
    public float AllowedMaxDepth => this.equippedStringData != null ? Mathf.Max(0f, this.equippedStringData.MaxDepth) : Mathf.Max(0f, this.fallbackMaxDepth);
    public float CurrentBaitFallSpeed => this.activeBait != null ? Mathf.Max(0f, this.activeBait.FallSpeed) : (this.equippedBaitData != null ? Mathf.Max(0f, this.equippedBaitData.FallSpeed) : Mathf.Max(0f, this.fallbackBaitFallSpeed));
    public float LastPeakCastSpeed => this.lastPeakCastSpeed;
    public float LastSpeedMultiplier => this.lastSpeedMultiplier;
    #endregion

    #region Unity Funcs
    private void Awake()
    {
        if (this.rodOrigin == null)
        {
            this.rodOrigin = this.transform;
        }

        if (this.hookEmptySprite == null)
        {
            this.hookEmptySprite = this.LoadDefaultHookSprite();
        }

        if (this.equippedStringData != null)
        {
            this.stringColor = this.equippedStringData.ColorString;
        }

        this.EnsureHookHierarchy();
        this.EnsureStringLineRenderer();

        this.currentState = RodState.Idle;
        this.PositionHookAtReady();
        this.SetHookSprite(this.hookEmptySprite);
        this.UpdateVisualVisibility();
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

            Debug.Log($"[ClassFishingRod.Start] Standalone mode on {this.gameObject.name} - ensured FishingRod input actions are enabled.");
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

    private void OnApplicationQuit()
    {
        this.isQuitting = true;
    }

    private void OnDestroy()
    {
        this.OnFishReeledIn = null;
        this.OnStateChanged = null;
        this.OnReadyModeChanged = null;

        this.CleanupActiveBait();

        if (this.hookedFish != null)
        {
            if (!this.isQuitting)
            {
                this.hookedFish.ReleaseFromHook();
            }

            this.hookedFish = null;
        }

        if (this.runtimeStringMaterial != null)
        {
            this.DestroyRuntimeObject(this.runtimeStringMaterial);
            this.runtimeStringMaterial = null;
        }
    }

    private void Update()
    {
        this.StepRod(Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (this.currentState == RodState.Idle || this.IsReady)
        {
            this.PositionHookAtReady();
        }

        this.SyncLineRenderer();
    }
    #endregion

    #region Public Funcs
    /// <summary>
    /// Returns true when global UI state and boat fishing mode allow operating the fishing rod.
    /// </summary>
    public bool CanOperateRod()
    {
        return !Globals.isPaused && !Globals.isInShop && (Globals.boatInstance == null || Globals.boatInstance.IsFishing);
    }

    /// <summary>
    /// Toggles the rod between <see cref="RodState.Idle"/> and <see cref="RodState.Ready"/> (or exits <see cref="RodState.SamplingCast"/>).
    /// </summary>
    public void ToggleReady()
    {
        if (!this.CanOperateRod())
        {
            return;
        }

        if (this.currentState == RodState.Idle)
        {
            this.peakSampleSpeed = 0f;
            this.castSampleTimer = 0f;
            this.isTravelingToCastTarget = false;
            this.PositionHookAtReady();
            this.SetHookSprite(this.hookEmptySprite);
            this.SetState(RodState.Ready);
            this.OnReadyModeChanged?.Invoke(true);
            Debug.Log($"[ClassFishingRod.ToggleReady] {this.gameObject.name} entered Ready mode.");
        }
        else if (this.currentState == RodState.Ready || this.currentState == RodState.SamplingCast)
        {
            this.peakSampleSpeed = 0f;
            this.castSampleTimer = 0f;
            this.isTravelingToCastTarget = false;
            this.PositionHookAtReady();
            this.SetHookSprite(this.hookEmptySprite);
            this.SetState(RodState.Idle);
            this.OnReadyModeChanged?.Invoke(false);
            Debug.Log($"[ClassFishingRod.ToggleReady] {this.gameObject.name} exited Ready mode to Idle.");
        }
    }

    /// <summary>
    /// Advances rod state, cast sampling, casting travel, sinking, and reeling by <paramref name="deltaTime"/>.
    /// </summary>
    public void StepRod(float deltaTime)
    {
        if (deltaTime < 0f)
        {
            return;
        }

        if (Globals.isPaused)
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

        // Safety net in case a fish claimed the bait without our callback running.
        if (this.activeBait != null && this.hookedFish == null && this.activeBait.ClaimedByFish != null)
        {
            this.HandleFishClaimed(this.activeBait.ClaimedByFish);
        }

        if (this.currentState == RodState.Hooked || this.currentState == RodState.Reeling)
        {
            bool hadFishRef = !object.ReferenceEquals(this.hookedFish, null);
            bool fishDestroyed = hadFishRef && this.hookedFish == null;
            bool fishReleased = this.hookedFish != null && this.activeBait != null && this.activeBait.ClaimedByFish != this.hookedFish;

            if (fishDestroyed || fishReleased || (this.currentState == RodState.Hooked && this.hookedFish == null))
            {
                Debug.LogWarning($"[ClassFishingRod.StepRod] Hooked fish reference became invalid on {this.gameObject.name}; updating rod state.");
                this.hookedFish = null;
                this.SetHookSprite(this.activeBait != null ? this.GetHookBaitedSprite() : this.hookEmptySprite);
                if (this.currentState == RodState.Hooked)
                {
                    this.SetState(RodState.Cast);
                }
            }
        }

        switch (this.currentState)
        {
            case RodState.Idle:
                break;
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

    /// <summary>
    /// Executes a cast using the sampled <paramref name="peakSpeed"/>.
    /// </summary>
    public void ExecuteCast(float peakSpeed)
    {
        if (this.hookTransform == null)
        {
            this.EnsureHookHierarchy();
        }

        this.CleanupActiveBait();

        if (this.hookedFish != null)
        {
            this.hookedFish.ReleaseFromHook();
            this.hookedFish = null;
        }

        float t = Mathf.InverseLerp(this.minThrowSpeed, this.maxThrowSpeed, peakSpeed);
        float speedMultiplier = Mathf.Clamp(Mathf.Lerp(0.5f, 1.5f, t), 0.5f, 1.5f);
        float castDistance = this.baseCastDistance * speedMultiplier;

        this.lastPeakCastSpeed = peakSpeed;
        this.lastSpeedMultiplier = speedMultiplier;
        this.peakSampleSpeed = 0f;
        this.castSampleTimer = 0f;

        int facingDirection = this.GetFacingDirection();
        float upwardAngleDeg = Mathf.Abs(this.castAngleDegrees);
        float angleRad = upwardAngleDeg * Mathf.Deg2Rad;
        Vector2 castDirection = new Vector2(Mathf.Cos(angleRad) * facingDirection, Mathf.Sin(angleRad));
        if (castDirection.sqrMagnitude > 0.0001f)
        {
            castDirection.Normalize();
        }
        else
        {
            castDirection = new Vector2(facingDirection, 1f).normalized;
        }

        Vector3 originPos = this.GetRodOriginPosition();
        this.castTravelTarget = originPos + new Vector3(castDirection.x, castDirection.y, 0f) * castDistance;

        bool wasReady = this.IsReady;

        this.PositionHookAtReady();
        this.SetHookSprite(this.hookEmptySprite);
        this.isTravelingToCastTarget = true;
        this.SetState(RodState.Cast);

        if (wasReady)
        {
            this.OnReadyModeChanged?.Invoke(false);
        }

        Debug.Log($"[ClassFishingRod.ExecuteCast] Cast executed (peakSpeed={peakSpeed:F3}, mult={speedMultiplier:F2}, dist={castDistance:F1}, facing={facingDirection}, target=({this.castTravelTarget.x:F1}, {this.castTravelTarget.y:F1})).");

        if (this.castTravelSpeed <= 0f || Vector2.Distance(this.hookTransform.position, this.castTravelTarget) <= 0.01f)
        {
            this.hookTransform.position = this.castTravelTarget;
            this.isTravelingToCastTarget = false;
            this.SpawnAndAttachBait();
        }
    }

    /// <summary>
    /// Cancels any active ready/cast/hooked state, cleans up bait and hooked fish, and resets to <see cref="RodState.Idle"/>.
    /// </summary>
    public void CancelAndResetToIdle()
    {
        if (this.currentState == RodState.Idle && this.activeBait == null && this.hookedFish == null)
        {
            return;
        }

        bool wasReady = this.IsReady;

        this.isTravelingToCastTarget = false;
        this.peakSampleSpeed = 0f;
        this.castSampleTimer = 0f;

        this.CleanupActiveBait();

        if (this.hookedFish != null)
        {
            this.hookedFish.ReleaseFromHook();
            this.hookedFish = null;
        }

        this.PositionHookAtReady();
        this.SetHookSprite(this.hookEmptySprite);
        this.SetState(RodState.Idle);

        if (wasReady)
        {
            this.OnReadyModeChanged?.Invoke(false);
        }

        Debug.Log($"[ClassFishingRod.CancelAndResetToIdle] {this.gameObject.name} reset to Idle.");
    }

    /// <summary>
    /// Sets the fishing line color and immediately updates the <see cref="LineRenderer"/> colors.
    /// </summary>
    public void SetStringColor(Color color)
    {
        this.stringColor = color;
        this.ApplyStringLineColors(color);
    }

    /// <summary>
    /// Applies the visual color corresponding to the equipped <see cref="StringType"/>.
    /// </summary>
    public void ApplyStringColor(StringType stringType)
    {
        Color color = stringType switch
        {
            StringType.Cobweb => new Color(0.85f, 0.85f, 0.85f, 1f),
            StringType.String => Color.white,
            StringType.GoldString => new Color(1f, 0.84f, 0.2f, 1f),
            _ => Color.white
        };

        this.SetStringColor(color);
    }

    /// <summary>
    /// Assigns a <see cref="ScriptableString"/> at runtime, updating max depth and line color.
    /// </summary>
    public void SetEquippedString(ScriptableString stringData)
    {
        this.equippedStringData = stringData;
        if (stringData != null)
        {
            this.SetStringColor(stringData.ColorString);
        }
    }

    /// <summary>
    /// Assigns a <see cref="ScriptableBait"/> at runtime for subsequent casts.
    /// </summary>
    public void SetEquippedBait(ScriptableBait baitData)
    {
        this.equippedBaitData = baitData;
    }
    #endregion

    #region Private Funcs
    private void OnToggleReadyPerformed(InputAction.CallbackContext _) => this.ToggleReady();

    private void OnCancelCastPerformed(InputAction.CallbackContext _)
    {
        if (!this.CanOperateRod())
        {
            return;
        }

        this.CancelAndResetToIdle();
    }

    private void UpdateReadyAndSampling(float deltaTime)
    {
        this.PositionHookAtReady();

        float rawSpeed = Controlls.fishingRodActions.CastSpeed.ReadValue<float>();
        float speed = this.onlyPositiveCastSpeed ? Mathf.Max(0f, rawSpeed) : Mathf.Abs(rawSpeed);

        if (this.currentState == RodState.Ready)
        {
            if (speed >= this.minThrowSpeed)
            {
                this.peakSampleSpeed = speed;
                this.castSampleTimer = this.castSampleDuration;
                this.SetState(RodState.SamplingCast);
                Debug.Log($"[ClassFishingRod.UpdateReadyAndSampling] Started cast sampling (initialSpeed={speed:F3}, duration={this.castSampleDuration:F3}s).");

                if (this.castSampleTimer <= 0f)
                {
                    this.ExecuteCast(this.peakSampleSpeed);
                }
            }

            return;
        }

        if (this.currentState == RodState.SamplingCast)
        {
            this.peakSampleSpeed = Mathf.Max(this.peakSampleSpeed, speed);
            this.castSampleTimer -= deltaTime;

            if (this.castSampleTimer <= 0f)
            {
                this.ExecuteCast(this.peakSampleSpeed);
            }
        }
    }

    private void UpdateCastHookedAndReeling(float deltaTime)
    {
        bool isReeling = Controlls.fishingRodActions.Rell.IsPressed();

        if (isReeling)
        {
            this.isTravelingToCastTarget = false;
            this.SetState(RodState.Reeling);

            if (this.hookTransform == null)
            {
                this.CompleteReelIn();
                return;
            }

            float weight = this.activeBait != null ? Mathf.Max(0.25f, this.activeBait.WeightMultiplier) : 1f;
            float effectiveReelSpeed = Mathf.Max(0f, this.reelSpeed) / weight;

            Vector3 originPos = this.GetRodOriginPosition();
            Vector3 currentPos = this.hookTransform.position;
            Vector3 targetPos = new Vector3(originPos.x, originPos.y, currentPos.z);

            this.hookTransform.position = Vector3.MoveTowards(currentPos, targetPos, effectiveReelSpeed * deltaTime);

            if (Vector2.Distance(this.hookTransform.position, originPos) <= this.reelCompleteDistance)
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
                if (this.activeBait == null && !this.isTravelingToCastTarget)
                {
                    this.SpawnAndAttachBait();
                }

                this.SetState(RodState.Cast);
            }
        }

        if (this.currentState == RodState.Cast)
        {
            this.UpdateCast(deltaTime);
        }
    }

    private void UpdateCast(float deltaTime)
    {
        if (this.hookTransform == null)
        {
            return;
        }

        if (this.isTravelingToCastTarget)
        {
            Vector3 currentPos = this.hookTransform.position;
            Vector3 targetPos = new Vector3(this.castTravelTarget.x, this.castTravelTarget.y, currentPos.z);
            float step = Mathf.Max(0f, this.castTravelSpeed) * deltaTime;

            this.hookTransform.position = this.castTravelSpeed <= 0f
                ? targetPos
                : Vector3.MoveTowards(currentPos, targetPos, step);

            if (Vector2.Distance(this.hookTransform.position, targetPos) <= 0.01f)
            {
                this.hookTransform.position = targetPos;
                this.isTravelingToCastTarget = false;
                this.SpawnAndAttachBait();
            }

            return;
        }

        float baitFallSpeed = this.CurrentBaitFallSpeed;
        if (baitFallSpeed > 0f)
        {
            float minY = this.GetRodOriginPosition().y - this.AllowedMaxDepth;
            Vector3 pos = this.hookTransform.position;
            if (pos.y > minY)
            {
                pos.y = Mathf.Max(minY, pos.y - baitFallSpeed * deltaTime);
                this.hookTransform.position = pos;
            }
        }
    }

    private void SpawnAndAttachBait()
    {
        this.CleanupActiveBait();
        this.EnsureHookHierarchy();

        GameObject baitObject = new GameObject("HookBait");
        baitObject.transform.SetParent(this.hookTipTransform, false);

        RectTransform baitRect = baitObject.AddComponent<RectTransform>();
        baitRect.localPosition = Vector3.zero;
        baitRect.localScale = Vector3.one;

        SpriteRenderer baitRenderer = baitObject.AddComponent<SpriteRenderer>();
        baitRenderer.sortingLayerName = this.GetResolvedSortingLayerName(this.hookSortingLayerName);
        baitRenderer.sortingOrder = this.hookSortingOrder + 1;

        ClassBait bait = baitObject.AddComponent<ClassBait>();
        if (this.equippedBaitData != null)
        {
            bait.Init(this.equippedBaitData);
        }
        else
        {
            bait.Init(this.fallbackBaitName, this.fallbackAttractRadius, this.fallbackBaitFallSpeed, this.fallbackBaitWeight, null);
        }

        bait.AttachToHook(this.hookTipTransform);
        bait.OnFishClaimed += this.HandleFishClaimed;
        this.activeBait = bait;

        this.SetHookSprite(this.GetHookBaitedSprite());
        Debug.Log($"[ClassFishingRod.SpawnAndAttachBait] Spawned and attached bait '{bait.BaitName}' at hook tip.");
    }

    private void HandleFishClaimed(ClassFish fish)
    {
        if (fish == null)
        {
            return;
        }

        if (this.hookedFish != null)
        {
            if (this.hookedFish != fish)
            {
                Debug.LogWarning($"[ClassFishingRod.HandleFishClaimed] Ignored {fish.FishName}; already holding {this.hookedFish.FishName}.");
            }

            return;
        }

        if (this.currentState != RodState.Cast && this.currentState != RodState.Reeling)
        {
            Debug.LogWarning($"[ClassFishingRod.HandleFishClaimed] Ignored claim by {fish.FishName} while in state {this.currentState}.");
            return;
        }

        this.hookedFish = fish;
        this.SetHookSprite(this.GetHookFishSprite());
        this.SetState(RodState.Hooked);

        Debug.Log($"[ClassFishingRod.HandleFishClaimed] Hooked {fish.FishName} ({fish.FishTypeCurrent}, price={fish.DefaultPrice:F1}).");
    }

    private void CompleteReelIn()
    {
        ClassFish caughtFish = this.hookedFish;

        try
        {
            if (caughtFish != null)
            {
                Debug.Log($"[ClassFishingRod.CompleteReelIn] Reeled in {caughtFish.FishName} ({caughtFish.FishTypeCurrent}, price={caughtFish.DefaultPrice:F1}).");
                this.OnFishReeledIn?.Invoke(caughtFish);
            }
            else
            {
                Debug.Log($"[ClassFishingRod.CompleteReelIn] Reeled in empty hook on {this.gameObject.name}.");
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
            this.isTravelingToCastTarget = false;
            this.PositionHookAtReady();
            this.SetHookSprite(this.hookEmptySprite);
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
            this.DestroyRuntimeObject(baitGameObject);
        }
    }

    private void EnsureHookHierarchy()
    {
        if (this.hookTransform == null)
        {
            GameObject hookObject = new GameObject("FishingHook");
            hookObject.transform.SetParent(this.transform, false);
            this.hookTransform = hookObject.AddComponent<RectTransform>();
        }

        if (this.hookTransform != this.transform)
        {
            this.hookTransform.localScale = this.hookScale;
        }

        if (this.hookSpriteRenderer == null)
        {
            this.hookSpriteRenderer = this.hookTransform.GetComponent<SpriteRenderer>();
            if (this.hookSpriteRenderer == null)
            {
                this.hookSpriteRenderer = this.hookTransform.GetComponentInChildren<SpriteRenderer>(true);
            }

            if (this.hookSpriteRenderer == null)
            {
                this.hookSpriteRenderer = this.hookTransform.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        this.hookSpriteRenderer.sortingLayerName = this.GetResolvedSortingLayerName(this.hookSortingLayerName);
        this.hookSpriteRenderer.sortingOrder = this.hookSortingOrder;

        if (this.hookTipTransform == null)
        {
            Transform existingTip = this.hookTransform.Find("HookTip");
            if (existingTip != null)
            {
                this.hookTipTransform = existingTip;
            }
            else
            {
                GameObject tipObject = new GameObject("HookTip");
                tipObject.transform.SetParent(this.hookTransform, false);
                this.hookTipTransform = tipObject.transform;
                this.hookTipTransform.localPosition = new Vector3(this.hookTipLocalOffset.x, this.hookTipLocalOffset.y, 0f);
            }
        }
    }

    private void EnsureStringLineRenderer()
    {
        if (this.stringLineRenderer == null)
        {
            GameObject stringObject = new GameObject("FishingString");
            stringObject.transform.SetParent(this.transform, false);
            this.stringLineRenderer = stringObject.AddComponent<LineRenderer>();
        }

        this.stringLineRenderer.useWorldSpace = true;
        this.stringLineRenderer.positionCount = 2;
        this.stringLineRenderer.startWidth = this.stringWidth;
        this.stringLineRenderer.endWidth = this.stringWidth;
        this.stringLineRenderer.sortingLayerName = this.GetResolvedSortingLayerName(this.stringSortingLayerName);
        this.stringLineRenderer.sortingOrder = this.stringSortingOrder;

        if (this.stringLineRenderer.sharedMaterial == null)
        {
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                this.runtimeStringMaterial = new Material(defaultShader);
                this.stringLineRenderer.sharedMaterial = this.runtimeStringMaterial;
            }
            else
            {
                Debug.LogWarning($"[ClassFishingRod.EnsureStringLineRenderer] Could not find 'Sprites/Default' shader on {this.gameObject.name}.");
            }
        }

        this.ApplyStringLineColors(this.stringColor);
    }

    private void SetState(RodState newState)
    {
        if (this.currentState == newState)
        {
            this.UpdateVisualVisibility();
            return;
        }

        this.currentState = newState;
        this.UpdateVisualVisibility();
        this.OnStateChanged?.Invoke(this.currentState);
    }

    private bool ShouldShowHookAndString()
    {
        return !(this.hideHookWhenIdle && this.currentState == RodState.Idle);
    }

    private void UpdateVisualVisibility()
    {
        bool visible = this.ShouldShowHookAndString();

        if (this.hookTransform != null && this.hookTransform != this.transform)
        {
            this.hookTransform.gameObject.SetActive(visible);
        }

        if (this.hookSpriteRenderer != null)
        {
            this.hookSpriteRenderer.enabled = visible;
        }

        if (this.stringLineRenderer != null)
        {
            this.stringLineRenderer.enabled = visible;
        }
    }

    private void SyncLineRenderer()
    {
        if (this.stringLineRenderer == null)
        {
            return;
        }

        bool visible = this.ShouldShowHookAndString();
        this.stringLineRenderer.enabled = visible;

        if (!visible)
        {
            return;
        }

        Vector3 startPos = this.GetRodOriginPosition();
        Vector3 endPos = this.hookTipTransform != null
            ? this.hookTipTransform.position
            : (this.hookTransform != null ? this.hookTransform.position : startPos);

        endPos.z = startPos.z;

        if (this.stringLineRenderer.positionCount != 2)
        {
            this.stringLineRenderer.positionCount = 2;
        }

        this.stringLineRenderer.SetPosition(0, startPos);
        this.stringLineRenderer.SetPosition(1, endPos);
    }

    private void PositionHookAtReady()
    {
        if (this.hookTransform == null)
        {
            return;
        }

        this.hookTransform.position = this.GetReadyHookPosition();
    }

    private Vector3 GetReadyHookPosition()
    {
        Vector3 originPos = this.GetRodOriginPosition();
        return new Vector3(originPos.x + this.readyHookOffset.x, originPos.y + this.readyHookOffset.y, originPos.z);
    }

    private Vector3 GetRodOriginPosition()
    {
        return this.rodOrigin != null ? this.rodOrigin.position : this.transform.position;
    }

    private int GetFacingDirection()
    {
        if (Globals.boatInstance != null)
        {
            return Globals.boatInstance.FacingDirection < 0 ? -1 : 1;
        }

        return 1;
    }

    private void SetHookSprite(Sprite sprite)
    {
        if (this.hookSpriteRenderer == null)
        {
            return;
        }

        this.hookSpriteRenderer.sprite = sprite != null ? sprite : this.hookEmptySprite;
    }

    private Sprite GetHookBaitedSprite()
    {
        return this.hookBaitedSprite != null ? this.hookBaitedSprite : this.hookEmptySprite;
    }

    private Sprite GetHookFishSprite()
    {
        return this.hookFishSprite != null ? this.hookFishSprite : this.hookEmptySprite;
    }

    private Sprite LoadDefaultHookSprite()
    {
        Sprite sprite = Resources.Load<Sprite>("Game/HookEmpty");
        if (sprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("Game/HookEmpty");
            if (sprites != null && sprites.Length > 0)
            {
                sprite = sprites[0];
            }
        }

        if (sprite == null)
        {
            Debug.LogWarning($"[ClassFishingRod.LoadDefaultHookSprite] Could not load 'Game/HookEmpty' from Resources for {this.gameObject.name}.");
        }

        return sprite;
    }

    private void ApplyStringLineColors(Color color)
    {
        if (this.stringLineRenderer == null)
        {
            return;
        }

        this.stringLineRenderer.startColor = color;
        this.stringLineRenderer.endColor = color;
    }

    private string GetResolvedSortingLayerName(string layerName)
    {
        return string.IsNullOrEmpty(layerName) ? "Default" : layerName;
    }

    private void DestroyRuntimeObject(UnityEngine.Object obj)
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
