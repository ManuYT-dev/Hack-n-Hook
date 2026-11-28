using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public interface IInteractable
{
    void Interact(ClassBoat boat);
}

public class ClassBoat : MonoBehaviour
{
    #region Hardcoded Boundaries
    private float MAX_LEFT = -3520f;
    private float MAX_RIGHT = 12350f;
    #endregion

    #region Inspector Fields
    [Header("Boat Data")]
    [SerializeField] private ScriptableBoat boatData;

    [Header("Stats (Overridden by ScriptableBoat when assigned)")]
    [SerializeField] private string boatName;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float boostedSpeed = 10f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float buyPrice;
    [SerializeField] private int maxDistance;
    [SerializeField] private Sprite sprite;

    [Header("Components & Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image boatImage;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private bool facesLeftByDefault;
    [SerializeField] private float inputDeadzone = 0.05f;
    [SerializeField] private float anchorDecelerationMultiplier = 2f;
    [SerializeField] private float interactRadius = 2f;
    [SerializeField] private LayerMask interactLayerMask = ~0;
    #endregion

    #region Private Variables
    private float currentVelocity;
    private float currentYawAngle;
    private int facingDirection = 1;
    private bool isAnchored;
    private bool isFishing;
    private bool isBoosting;
    private bool initialized;
    private bool isQuitting;
    private Vector3 spawnPosition;
    #endregion

    #region Events
    public event Action<bool> OnAnchorChanged;
    public event Action<bool> OnFishingModeChanged;
    public event Action OnInteract;
    public event Action<bool> OnBoostChanged;
    public event Action OnMaxDistanceReached;
    #endregion

    #region Properties
    public ScriptableBoat BoatData => this.boatData;
    public string BoatName => this.boatName;
    public float Speed => this.speed;
    public float BoostedSpeed => this.boostedSpeed;
    public float Acceleration => this.acceleration;
    public float TurnSpeed => this.turnSpeed;
    public float BuyPrice => this.buyPrice;
    public int MaxDistance => this.maxDistance;
    public float CurrentVelocity => this.currentVelocity;
    public float CurrentSpeed => Mathf.Abs(this.currentVelocity);
    public int FacingDirection => this.facingDirection;
    public bool IsAnchored => this.isAnchored;
    public bool IsFishing => this.isFishing;
    public bool IsBoosting => this.isBoosting;
    public bool IsMoving => Mathf.Abs(this.currentVelocity) > 0.01f;
    public bool IsAtMaxDistance { get; private set; }
    public float DistanceFromOrigin => Mathf.Max(0f, this.transform.position.x);
    #endregion

    #region Unity Funcs
    private void Awake()
    {
        this.ResolveVisualComponents();
        
        // Speichere die Spawn-Position beim Awake
        this.spawnPosition = this.transform.position;

        if (this.boatData != null && !this.initialized)
        {
            this.ApplyBoatData(this.boatData);
        }
        else
        {
            this.ApplySprite();
        }

        this.currentYawAngle = this.GetTargetYawAngle(this.facingDirection);
        this.ApplyVisualRotation();
    }

    private void OnValidate()
    {
        if (this.boatData != null)
        {
            this.ApplyBoatData(this.boatData);
        }
    }

    private void Start()
    {
        (int x, int y) = DefaultSettings.max_chunk;
        if (y == 1)
            this.MAX_RIGHT = 1000f;
        else if (y == 2)
            this.MAX_RIGHT = 6550f;
        else if (y == 2)
            this.MAX_RIGHT = 12350f;

        if (!this.initialized && this.boatData == null)
        {
            Debug.LogWarning($"[ClassBoat.Start] {this.gameObject.name} has no ScriptableBoat assigned; using inspector values.");
        }
    }

    private void OnEnable()
    {
        Controlls.boatActions.Anchor.performed += this.OnAnchorPerformed;
        Controlls.boatActions.Interact.performed += this.OnInteractPerformed;
        Controlls.boatActions.ToggleFishing.performed += this.OnToggleFishingPerformed;
        this.ApplyInputMode();
    }

    private void OnDisable()
    {
        Controlls.boatActions.Anchor.performed -= this.OnAnchorPerformed;
        Controlls.boatActions.Interact.performed -= this.OnInteractPerformed;
        Controlls.boatActions.ToggleFishing.performed -= this.OnToggleFishingPerformed;
        this.SetBoostState(false);

        if (!this.isQuitting && this.isFishing)
        {
            Controlls.fishingRodActions.Disable();
            Controlls.boatActions.Enable();
        }
    }

    private void OnApplicationQuit()
    {
        this.isQuitting = true;
    }

    private void Update()
    {
        this.StepMovement(Time.deltaTime);
    }
    #endregion

    #region Public Funcs
    public void Init(ScriptableBoat scriptableBoat)
    {
        if (scriptableBoat == null)
        {
            Debug.LogError($"[ClassBoat.Init] ScriptableBoat is null on {this.gameObject.name}.");
            return;
        }

        this.ResolveVisualComponents();
        this.ApplyBoatData(scriptableBoat);
    }

    public void SetAnchored(bool anchored)
    {
        if (this.isAnchored == anchored)
        {
            return;
        }

        this.isAnchored = anchored;
        if (this.isAnchored)
        {
            this.SetBoostState(false);
        }

        this.OnAnchorChanged?.Invoke(this.isAnchored);
    }

    public void ToggleAnchor()
    {
        this.SetAnchored(!this.isAnchored);
    }

    public void SetFishingMode(bool fishing)
    {
        if (this.isFishing == fishing)
        {
            return;
        }

        this.isFishing = fishing;
        if (this.isFishing)
        {
            this.SetBoostState(false);
        }

        this.ApplyInputMode();
        this.OnFishingModeChanged?.Invoke(this.isFishing);
    }

    public void ToggleFishing()
    {
        this.SetFishingMode(!this.isFishing);
    }

    public void Interact()
    {
        this.OnInteract?.Invoke();

        Collider2D[] hits = Physics2D.OverlapCircleAll(this.transform.position, this.interactRadius, this.interactLayerMask);
        IInteractable closestInteractable = null;
        float closestDistanceSqr = float.PositiveInfinity;

        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(this.transform))
            {
                continue;
            }

            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null || (interactable is Component comp && comp.transform.IsChildOf(this.transform)))
            {
                continue;
            }

            float distSqr = ((Vector2)hit.transform.position - (Vector2)this.transform.position).sqrMagnitude;
            if (distSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distSqr;
                closestInteractable = interactable;
            }
        }

        if (closestInteractable == null)
        {
            float maxDistSqr = this.interactRadius * this.interactRadius;
            MonoBehaviour[] candidates = UnityEngine.Object.FindObjectsByType<MonoBehaviour>();

            foreach (MonoBehaviour candidate in candidates)
            {
                if (candidate == null || candidate.transform.IsChildOf(this.transform) || candidate is not IInteractable interactable)
                {
                    continue;
                }

                float distSqr = ((Vector2)candidate.transform.position - (Vector2)this.transform.position).sqrMagnitude;
                if (distSqr <= maxDistSqr && distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    closestInteractable = interactable;
                }
            }
        }

        closestInteractable?.Interact(this);
    }

    /// <summary>
    /// Advances boat turning, velocity, and clamped horizontal movement by <paramref name="deltaTime"/>.
    /// </summary>
    public void StepMovement(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float driveInput = this.ReadDriveInput();

        if (Mathf.Abs(driveInput) > 0f)
        {
            this.facingDirection = driveInput > 0f ? 1 : -1;
        }

        float targetYaw = this.GetTargetYawAngle(this.facingDirection);
        this.UpdateVisualTurn(targetYaw, deltaTime);

        bool canDrive = !this.isAnchored && !this.isFishing && Mathf.Abs(driveInput) > 0f;
        this.SetBoostState(canDrive && Controlls.boatActions.Boost.IsPressed());

        bool isTurningAround = canDrive &&
            (Mathf.Abs(targetYaw - this.currentYawAngle) > 90f || (this.currentVelocity * driveInput < -0.001f));

        if (this.isAnchored)
        {
            float baseRate = this.acceleration > 0f ? this.acceleration : Mathf.Max(this.speed, 10f);
            float anchorRate = baseRate * Mathf.Max(1f, this.anchorDecelerationMultiplier);
            this.currentVelocity = Mathf.MoveTowards(this.currentVelocity, 0f, anchorRate * deltaTime);
        }
        else if (!canDrive)
        {
            this.currentVelocity = this.acceleration > 0f
                ? Mathf.MoveTowards(this.currentVelocity, 0f, this.acceleration * deltaTime)
                : 0f;
        }
        else if (isTurningAround)
        {
            float turnBrakeRate = Mathf.Max(this.acceleration, this.turnSpeed);
            this.currentVelocity = turnBrakeRate > 0f
                ? Mathf.MoveTowards(this.currentVelocity, 0f, turnBrakeRate * deltaTime)
                : 0f;
        }
        else
        {
            float activeMaxSpeed = this.isBoosting && this.boostedSpeed > 0f ? this.boostedSpeed : this.speed;
            float targetVelocity = driveInput * activeMaxSpeed;
            this.currentVelocity = this.acceleration > 0f
                ? Mathf.MoveTowards(this.currentVelocity, targetVelocity, this.acceleration * deltaTime)
                : targetVelocity;
        }

        this.ApplyHorizontalPosition(deltaTime);
    }
    #endregion

    #region Private Funcs
    private void ApplyBoatData(ScriptableBoat data)
    {
        this.boatData = data;
        this.boatName = data.BoatName;
        this.speed = data.Speed;
        this.boostedSpeed = data.BoostedSpeed;
        this.acceleration = data.Acceleration;
        this.turnSpeed = data.TurnSpeed;
        this.buyPrice = data.BuyPrice;
        this.maxDistance = data.MaxDistance;
        this.sprite = data.SpriteBoat;
        this.initialized = true;

        this.ApplySprite();
    }

    private void ResolveVisualComponents()
    {
        // Zuerst versuchen Image zu finden
        if (this.boatImage == null)
        {
            this.boatImage = this.GetComponent<Image>();
            if (this.boatImage == null)
            {
                this.boatImage = this.GetComponentInChildren<Image>(true);
            }
        }

        // Dann SpriteRenderer versuchen
        if (this.spriteRenderer == null)
        {
            this.spriteRenderer = this.GetComponent<SpriteRenderer>();
            if (this.spriteRenderer == null)
            {
                this.spriteRenderer = this.GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (this.visualTransform == null)
        {
            if (this.boatImage != null)
            {
                this.visualTransform = this.boatImage.transform;
            }
            else if (this.spriteRenderer != null)
            {
                this.visualTransform = this.spriteRenderer.transform;
            }
            else
            {
                this.visualTransform = this.transform;
            }
        }
        this.interactLayerMask = (1 << LayerMask.NameToLayer("Interactable"));
    }

    private void ApplySprite()
    {
        if (this.boatImage != null && this.sprite != null)
        {
            this.boatImage.sprite = this.sprite;
        }
        else if (this.spriteRenderer != null && this.sprite != null)
        {
            this.spriteRenderer.sprite = this.sprite;
        }
    }

    private float ReadDriveInput()
    {
        if (this.isAnchored || this.isFishing)
        {
            return 0f;
        }

        float raw = Controlls.boatActions.Drive.ReadValue<float>();
        return Mathf.Abs(raw) < this.inputDeadzone ? 0f : Mathf.Clamp(raw, -1f, 1f);
    }

    private float GetTargetYawAngle(int direction)
    {
        bool flipped = (direction < 0) ^ this.facesLeftByDefault;
        return flipped ? 180f : 0f;
    }

    private void UpdateVisualTurn(float targetYaw, float deltaTime)
    {
        if (this.turnSpeed <= 0f)
        {
            this.currentYawAngle = targetYaw;
        }
        else
        {
            float alpha = 1f - Mathf.Exp(-this.turnSpeed * deltaTime);
            this.currentYawAngle = Mathf.Lerp(this.currentYawAngle, targetYaw, alpha);
            if (Mathf.Abs(this.currentYawAngle - targetYaw) < 0.1f)
            {
                this.currentYawAngle = targetYaw;
            }
        }

        this.ApplyVisualRotation();
    }

    private void ApplyVisualRotation()
    {
        Transform target = this.visualTransform != null ? this.visualTransform : this.transform;

        // Für Image Components: scale.x verwenden für Flip-Effekt
        if (this.boatImage != null)
        {
            Vector3 scale = target.localScale;
            scale.x = this.currentYawAngle > 90f ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            target.localScale = scale;
            return;
        }

        // Für SpriteRenderer: flipX verwenden
        if (target == this.transform && this.transform.childCount > 0)
        {
            if (this.spriteRenderer != null)
            {
                this.spriteRenderer.flipX = this.currentYawAngle > 90f;
            }

            return;
        }

        // Fallback: Rotation verwenden
        Quaternion rotation = Quaternion.Euler(0f, this.currentYawAngle, 0f);

        if (target == this.transform)
        {
            target.rotation = rotation;
        }
        else
        {
            target.localRotation = rotation;
        }
    }

    private void ApplyHorizontalPosition(float deltaTime)
    {
        Vector3 position = this.transform.position;
        position.x += this.currentVelocity * deltaTime;

        // Verwende hardcodierte Grenzen statt Konstanten
        if (position.x <= MAX_LEFT)
        {
            position.x = MAX_LEFT;
            if (this.currentVelocity < 0f)
            {
                this.currentVelocity = 0f;
            }
        }

        if (position.x >= MAX_RIGHT)
        {
            position.x = MAX_RIGHT;
            if (this.currentVelocity > 0f)
            {
                this.currentVelocity = 0f;
            }

            if (!this.IsAtMaxDistance)
            {
                this.IsAtMaxDistance = true;
                this.OnMaxDistanceReached?.Invoke();
            }
        }
        else
        {
            this.IsAtMaxDistance = false;
        }

        this.transform.position = position;
    }

    private void SetBoostState(bool boosting)
    {
        if (this.isBoosting == boosting)
        {
            return;
        }

        this.isBoosting = boosting;
        this.OnBoostChanged?.Invoke(this.isBoosting);
    }

    private void ApplyInputMode()
    {
        if (this.isFishing)
        {
            Controlls.boatActions.Drive.Disable();
            Controlls.boatActions.Anchor.Disable();
            Controlls.boatActions.Interact.Disable();
            Controlls.boatActions.Boost.Disable();
            Controlls.boatActions.ToggleFishing.Enable();
            Controlls.fishingRodActions.Enable();
        }
        else
        {
            Controlls.fishingRodActions.Disable();
            Controlls.boatActions.Enable();
        }
    }

    private void OnAnchorPerformed(InputAction.CallbackContext _) => this.ToggleAnchor();

    private void OnInteractPerformed(InputAction.CallbackContext _)
    {
        if (!this.isFishing)
        {
            this.Interact();
        }
    }

    private void OnToggleFishingPerformed(InputAction.CallbackContext _) => this.ToggleFishing();
    #endregion
}
