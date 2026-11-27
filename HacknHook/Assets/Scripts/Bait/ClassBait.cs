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

            if (this.TryGetComponent(out SpriteRenderer sp))
            {
                if (this.sprite == null)
                {
                    Debug.LogWarning($"[ClassBait.Start] {this.baitName} hat kein Sprite zugewiesen.");
                }
                sp.sprite = this.sprite;
            }
            else
            {
                Debug.LogWarning($"[ClassBait.Start] Kein SpriteRenderer auf {this.gameObject.name} gefunden.");
            }

            if (!this.TryGetComponent(out this.rectTransform))
            {
                Debug.LogError($"[ClassBait.Start] Kein RectTransform auf {this.gameObject.name} gefunden - Komponente wird zerstört.");
                Destroy(this);
                return;
            }

            if (string.IsNullOrEmpty(this.baitName))
            {
                this.baitName = this.gameObject.name;
            }

            Debug.Log($"[ClassBait.Start] {this.baitName} gestartet an Position ({this.rectTransform.anchoredPosition.x:F1}, {this.rectTransform.anchoredPosition.y:F1})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ClassBait.Start] Unerwarteter Fehler beim Start von {this.gameObject.name}: {ex.Message}");
        }
    }

    private void Update()
    {
        if (!this.initialized || this.rectTransform == null)
        {
            return;
        }

        try
        {
            this.rectTransform.anchoredPosition -= new Vector2(0f, this.fallSpeed * Time.deltaTime);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ClassBait.Update] Fehler beim Aktualisieren von {this.baitName}: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        string displayName = string.IsNullOrEmpty(this.baitName) ? this.gameObject.name : this.baitName;
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

            this.baitName = scriptableBait.BaitName;
            this.fallSpeed = scriptableBait.FallSpeed;
            this.attractRadius = scriptableBait.AttractRadius;
            this.weightMultiplier = scriptableBait.WeightMultiplier;
            this.sprite = scriptableBait.SpriteBait;
            this.initialized = true;

            if (this.sprite == null)
            {
                Debug.LogWarning($"[ClassBait.Init] {this.baitName} hat kein Sprite in ScriptableBait.");
            }

            Debug.Log($"[ClassBait.Init] {this.baitName} initialisiert (FallSpeed: {this.fallSpeed:F1}, AttractRadius: {this.attractRadius:F1}, WeightMultiplier: {this.weightMultiplier:F2})");
        }
        catch (Exception ex)
        {
            this.initialized = false;
            Debug.LogError($"[ClassBait.Init] Fehler bei der Initialisierung von {this.gameObject.name}: {ex.Message}");
        }
    }

    public Vector2 GetPosition()
    {
        try
        {
            if (this.rectTransform == null && !this.TryGetComponent(out this.rectTransform))
            {
                Debug.LogError($"[ClassBait.GetPosition] RectTransform ist null auf {this.gameObject.name}.");
                return Vector2.zero;
            }

            return this.rectTransform.anchoredPosition;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ClassBait.GetPosition] Fehler beim Abrufen der Position von {this.baitName}: {ex.Message}");
            return Vector2.zero;
        }
    }

    public bool IsDestroyed()
    {
        return this == null || this.gameObject == null;
    }
    #endregion
}