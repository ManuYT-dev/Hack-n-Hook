using System;
using UnityEngine;

public class ClassBait : MonoBehaviour
{
    #region Private Variables
    private bool initialized = false;

    [Header("Bait Settings")]
    [SerializeField] private float fallSpeed;
    [SerializeField] private float attractRadius;
    [SerializeField] private float weightMultiplier;
    [SerializeField] private string baitName;

    private Sprite sprite;
    private RectTransform rectTransform;
    private Transform hookTipTransform;
    private bool isAttachedToHook = false;
    private bool warnIfMissingSprite = true;
    private ClassFish claimedByFish;
    #endregion

    #region Events
    public event Action<ClassFish> OnFishClaimed;
    #endregion

    #region Properties
    public bool IsAttachedToHook => this.isAttachedToHook && this.hookTipTransform != null;
    public float FallSpeed => this.fallSpeed;
    public float AttractRadius => this.attractRadius;
    public float WeightMultiplier => this.weightMultiplier;
    public string BaitName => this.baitName;
    public ClassFish ClaimedByFish => this.claimedByFish;
    public bool IsOccupied => this.claimedByFish != null && this.claimedByFish.gameObject != null;
    #endregion

    #region Unity Funcs
    private void Start()
    {
        try
        {
            if (!this.initialized)
            {
                Debug.LogError($"[ClassBait.Start] {this.gameObject.name} wurde nicht initialisiert - Komponente wird zerstört.");
                Destroy(this);
                return;
            }

            this.ApplySprite(this.warnIfMissingSprite);

            if (string.IsNullOrEmpty(this.baitName))
            {
                this.baitName = this.gameObject.name;
            }

            if (this.IsAttachedToHook)
            {
                this.SyncPositionWithHook();
            }

            if (this.TryGetComponent(out this.rectTransform))
            {
                Debug.Log($"[ClassBait.Start] {this.baitName} gestartet an Position ({this.rectTransform.anchoredPosition.x:F1}, {this.rectTransform.anchoredPosition.y:F1})");
            }
            else
            {
                Debug.Log($"[ClassBait.Start] {this.baitName} gestartet an Position ({this.transform.position.x:F1}, {this.transform.position.y:F1})");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ClassBait.Start] Unerwarteter Fehler beim Start von {this.gameObject.name}: {ex.Message}");
        }
    }

    private void Update()
    {
        if (!this.initialized)
        {
            return;
        }

        try
        {
            if (this.IsAttachedToHook)
            {
                this.SyncPositionWithHook();
                return;
            }

            if (this.rectTransform != null || this.TryGetComponent(out this.rectTransform))
            {
                this.rectTransform.anchoredPosition -= new Vector2(0f, this.fallSpeed * Time.deltaTime);
            }
            else if (this.fallSpeed != 0f)
            {
                this.transform.position -= new Vector3(0f, this.fallSpeed * Time.deltaTime, 0f);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ClassBait.Update] Fehler beim Aktualisieren von {this.baitName}: {ex.Message}");
        }
    }

    private void LateUpdate()
    {
        if (!this.initialized || !this.IsAttachedToHook)
        {
            return;
        }

        try
        {
            this.SyncPositionWithHook();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ClassBait.LateUpdate] Fehler beim Synchronisieren von {this.baitName}: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        string displayName = this.GetDisplayName();
        this.OnFishClaimed = null;
        this.claimedByFish = null;
        this.hookTipTransform = null;
        this.isAttachedToHook = false;
        Debug.Log($"[ClassBait.OnDestroy] {displayName} wurde zerstört");
    }
    #endregion

    #region Public Funcs
    public void Init(ScriptableBait scriptableBait)
    {
        try
        {
            if (scriptableBait == null)
            {
                Debug.LogError($"[ClassBait.Init] ScriptableBait ist null auf {this.gameObject.name} - Initialisierung abgebrochen.");
                this.initialized = false;
                return;
            }

            this.ApplyInitialization(
                scriptableBait.BaitName,
                scriptableBait.AttractRadius,
                scriptableBait.FallSpeed,
                scriptableBait.WeightMultiplier,
                scriptableBait.SpriteBait,
                warnIfMissingSprite: true);
        }
        catch (Exception ex)
        {
            this.initialized = false;
            Debug.LogError($"[ClassBait.Init] Fehler bei der Initialisierung von {this.gameObject.name}: {ex.Message}");
        }
    }

    public void Init(string baitName, float attractRadius, float fallSpeed = 0f, float weightMultiplier = 1f, Sprite baitSprite = null)
    {
        try
        {
            this.ApplyInitialization(
                baitName,
                attractRadius,
                fallSpeed,
                weightMultiplier,
                baitSprite,
                warnIfMissingSprite: false);
        }
        catch (Exception ex)
        {
            this.initialized = false;
            Debug.LogError($"[ClassBait.Init] Fehler bei der Initialisierung von {this.gameObject.name}: {ex.Message}");
        }
    }

    public void AttachToHook(Transform hookTip)
    {
        if (this.IsDestroyed())
        {
            return;
        }

        this.hookTipTransform = hookTip;
        this.isAttachedToHook = hookTip != null;

        if (hookTip != null)
        {
            this.SyncPositionWithHook();
            Debug.Log($"[ClassBait.AttachToHook] {this.GetDisplayName()} an Hook '{hookTip.name}' befestigt.");
        }
        else
        {
            Debug.LogWarning($"[ClassBait.AttachToHook] HookTip ist null auf {this.GetDisplayName()} - nicht befestigt.");
        }
    }

    public void DetachFromHook()
    {
        this.hookTipTransform = null;
        this.isAttachedToHook = false;

        if (!this.IsDestroyed())
        {
            Debug.Log($"[ClassBait.DetachFromHook] {this.GetDisplayName()} vom Hook gelöst.");
        }
    }

    public Vector2 GetPosition()
    {
        try
        {
            if (this.rectTransform != null || this.TryGetComponent(out this.rectTransform))
            {
                return this.rectTransform.anchoredPosition;
            }

            return (Vector2)this.transform.position;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ClassBait.GetPosition] Fehler beim Abrufen der Position von {this.baitName}: {ex.Message}");
            return Vector2.zero;
        }
    }

    public Vector2 GetWorldPosition()
    {
        if (this.IsDestroyed())
        {
            return Vector2.zero;
        }

        return (Vector2)this.transform.position;
    }

    public bool TryClaim(ClassFish fish)
    {
        if (fish == null || fish.gameObject == null || this.IsDestroyed())
        {
            return false;
        }

        if (this.IsOccupied && this.claimedByFish != fish)
        {
            return false;
        }

        if (this.claimedByFish == fish)
        {
            return true;
        }

        this.claimedByFish = fish;
        Debug.Log($"[ClassBait.TryClaim] {this.GetDisplayName()} wurde von {fish.gameObject.name} beansprucht.");
        this.OnFishClaimed?.Invoke(fish);
        return true;
    }

    public void ReleaseClaim(ClassFish fish = null)
    {
        if (fish == null || this.claimedByFish == fish)
        {
            if (this.claimedByFish != null)
            {
                Debug.Log($"[ClassBait.ReleaseClaim] Beanspruchung von {this.GetDisplayName()} durch {this.claimedByFish.name} freigegeben.");
            }

            this.claimedByFish = null;
        }
    }

    public bool IsDestroyed()
    {
        return this == null || this.gameObject == null;
    }
    #endregion

    #region Private Funcs
    private void ApplyInitialization(
        string resolvedBaitName,
        float resolvedAttractRadius,
        float resolvedFallSpeed,
        float resolvedWeightMultiplier,
        Sprite baitSprite,
        bool warnIfMissingSprite)
    {
        this.baitName = string.IsNullOrWhiteSpace(resolvedBaitName) ? "DefaultBait" : resolvedBaitName;
        this.attractRadius = Mathf.Max(0f, resolvedAttractRadius);
        this.fallSpeed = resolvedFallSpeed;
        this.weightMultiplier = resolvedWeightMultiplier;
        this.sprite = baitSprite;
        this.warnIfMissingSprite = warnIfMissingSprite;
        this.initialized = true;

        if (warnIfMissingSprite && this.sprite == null)
        {
            Debug.LogWarning($"[ClassBait.Init] {this.baitName} hat kein Sprite in ScriptableBait.");
        }

        this.ApplySprite(false);

        Debug.Log($"[ClassBait.Init] {this.baitName} initialisiert (FallSpeed: {this.fallSpeed:F1}, AttractRadius: {this.attractRadius:F1}, WeightMultiplier: {this.weightMultiplier:F2})");
    }

    private void ApplySprite(bool logWarnings)
    {
        if (this.TryGetComponent(out SpriteRenderer sp))
        {
            if (this.sprite != null)
            {
                sp.sprite = this.sprite;
            }
            else if (logWarnings && sp.sprite == null)
            {
                Debug.LogWarning($"[ClassBait.Start] {this.baitName} hat kein Sprite zugewiesen.");
            }
        }
        else if (logWarnings)
        {
            Debug.LogWarning($"[ClassBait.Start] Kein SpriteRenderer auf {this.gameObject.name} gefunden.");
        }
    }

    private void SyncPositionWithHook()
    {
        if (this.hookTipTransform == null)
        {
            return;
        }

        Vector3 hookPosition = this.hookTipTransform.position;
        this.transform.position = new Vector3(hookPosition.x, hookPosition.y, this.transform.position.z);
    }

    private string GetDisplayName()
    {
        if (this.IsDestroyed())
        {
            return string.IsNullOrEmpty(this.baitName) ? "DestroyedBait" : this.baitName;
        }

        return string.IsNullOrEmpty(this.baitName) ? this.gameObject.name : this.baitName;
    }
    #endregion
}