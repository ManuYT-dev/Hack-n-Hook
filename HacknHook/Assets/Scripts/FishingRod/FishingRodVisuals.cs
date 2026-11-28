using System;
using UnityEngine;

public enum HookVisualState
{
    Empty,
    Baited,
    Fish
}

[Serializable]
public class FishingRodVisuals
{
    #region Constants
    private const int DefaultHookSortingOrder = 15;
    private const int DefaultStringSortingOrder = 14;
    #endregion

    #region Inspector Fields
    [SerializeField] private Transform hookTransform;
    [SerializeField] private Transform hookTipTransform;
    [SerializeField] private Vector2 hookTipLocalOffset = Vector2.zero;
    [SerializeField] private SpriteRenderer hookSpriteRenderer;
    [SerializeField] private Sprite hookEmptySprite;
    [SerializeField] private Sprite hookBaitedSprite;
    [SerializeField] private Sprite hookFishSprite;
    [SerializeField] private Vector3 hookScale = Vector3.one;
    [SerializeField] private bool hideHookWhenIdle = true;
    [SerializeField] private Vector2 readyHookOffset = new Vector2(0f, -1f);

    [SerializeField] private LineRenderer stringLineRenderer;
    [SerializeField] private Color stringColor = Color.white;
    [SerializeField] private float stringWidth = 0.05f;
    #endregion

    #region Private Variables
    private Material runtimeStringMaterial;
    #endregion

    #region Properties
    public Transform HookTransform => this.hookTransform;
    public Transform HookTipTransform => this.hookTipTransform;
    public SpriteRenderer HookSpriteRenderer => this.hookSpriteRenderer;
    public LineRenderer StringLineRenderer => this.stringLineRenderer;
    public Vector2 ReadyHookOffset => this.readyHookOffset;
    public int BaitSortingOrder => DefaultHookSortingOrder + 1;
    #endregion

    #region Public Funcs
    public void Initialize(Transform owner, ScriptableString stringData)
    {
        if (this.hookEmptySprite == null)
        {
            this.hookEmptySprite = this.LoadDefaultHookSprite();
        }

        if (stringData != null)
        {
            this.stringColor = stringData.ColorString;
        }

        this.EnsureHookHierarchy(owner);
        this.EnsureStringLineRenderer(owner);
        this.SetHookSprite(HookVisualState.Empty);
    }

    public void PositionHookAtReady(Vector3 originPos)
    {
        if (this.hookTransform == null)
        {
            return;
        }

        this.hookTransform.position = new Vector3(
            originPos.x + this.readyHookOffset.x,
            originPos.y + this.readyHookOffset.y,
            originPos.z
        );
    }

    public void SetHookSprite(HookVisualState visualState)
    {
        if (this.hookSpriteRenderer == null)
        {
            return;
        }

        Sprite target = visualState switch
        {
            HookVisualState.Baited => this.hookBaitedSprite != null ? this.hookBaitedSprite : this.hookEmptySprite,
            HookVisualState.Fish => this.hookFishSprite != null ? this.hookFishSprite : this.hookEmptySprite,
            _ => this.hookEmptySprite
        };

        this.hookSpriteRenderer.sprite = target;
    }

    public void UpdateVisibility(RodState currentState)
    {
        bool visible = !(this.hideHookWhenIdle && currentState == RodState.Idle);

        if (this.hookTransform != null && this.hookTransform.gameObject.activeSelf != visible)
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

    public void SyncLine(Vector3 startPos, RodState currentState)
    {
        if (this.stringLineRenderer == null)
        {
            return;
        }

        bool visible = !(this.hideHookWhenIdle && currentState == RodState.Idle);
        this.stringLineRenderer.enabled = visible;
        if (!visible)
        {
            return;
        }

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

    public void SetStringColor(Color color)
    {
        this.stringColor = color;
        if (this.stringLineRenderer == null)
        {
            return;
        }

        this.stringLineRenderer.startColor = color;
        this.stringLineRenderer.endColor = color;
    }

    public void ApplyStringColor(FishingLineType stringType)
    {
        Color color = stringType switch
        {
            FishingLineType.Level0 => new Color(0.85f, 0.85f, 0.85f, 1f),
            FishingLineType.Level2 => new Color(1f, 0.84f, 0.2f, 1f),
            _ => Color.white
        };

        this.SetStringColor(color);
    }

    public void Cleanup(Action<UnityEngine.Object> destroyCallback)
    {
        if (this.runtimeStringMaterial != null)
        {
            destroyCallback?.Invoke(this.runtimeStringMaterial);
            this.runtimeStringMaterial = null;
        }
    }
    #endregion

    #region Private Funcs
    private void EnsureHookHierarchy(Transform owner)
    {
        if (this.hookTransform == null)
        {
            GameObject hookObject = new GameObject("FishingHook");
            hookObject.transform.SetParent(owner, false);
            this.hookTransform = hookObject.AddComponent<RectTransform>();
        }

        if (this.hookTransform != owner)
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

        this.hookSpriteRenderer.sortingOrder = DefaultHookSortingOrder;

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

    private void EnsureStringLineRenderer(Transform owner)
    {
        if (this.stringLineRenderer == null)
        {
            GameObject stringObject = new GameObject("FishingString");
            stringObject.transform.SetParent(owner, false);
            this.stringLineRenderer = stringObject.AddComponent<LineRenderer>();
        }

        this.stringLineRenderer.useWorldSpace = true;
        this.stringLineRenderer.positionCount = 2;
        this.stringLineRenderer.startWidth = this.stringWidth;
        this.stringLineRenderer.endWidth = this.stringWidth;
        this.stringLineRenderer.sortingOrder = DefaultStringSortingOrder;

        if (this.stringLineRenderer.sharedMaterial == null)
        {
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                this.runtimeStringMaterial = new Material(defaultShader);
                this.stringLineRenderer.sharedMaterial = this.runtimeStringMaterial;
            }
        }

        this.SetStringColor(this.stringColor);
    }

    private Sprite LoadDefaultHookSprite()
    {
        Sprite sprite = Resources.Load<Sprite>("Game/HookEmpty");
        if (sprite != null)
        {
            return sprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>("Game/HookEmpty");
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }
    #endregion
}
