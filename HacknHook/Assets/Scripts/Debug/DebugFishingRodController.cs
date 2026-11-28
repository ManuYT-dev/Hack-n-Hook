using System.Collections.Generic;
using UnityEngine;

public class DebugFishingRodController : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private ClassFishingRod fishingRod;
    [SerializeField] private Transform fishContainer;
    [SerializeField] private int initialFishCount = 3;
    [SerializeField] private bool respawnFishOnCatch = true;
    #endregion

    #region Private Variables
    private static readonly FishType[] DebugFishTypes = new FishType[]
    {
        FishType.Barsch,
        FishType.Forelle,
        FishType.Hecht
    };

    private static readonly Vector2[] SpawnOffsets = new Vector2[]
    {
        new Vector2(-180f, -60f),
        new Vector2(-60f, -180f),
        new Vector2(-260f, -220f)
    };

    private readonly List<ClassFish> spawnedFish = new List<ClassFish>();
    private int caughtCount;
    private string lastCatchMessage = "None";
    #endregion

    #region Unity Funcs
    private void Awake()
    {
        if (this.fishingRod == null)
        {
            this.fishingRod = this.GetComponent<ClassFishingRod>();
            if (this.fishingRod == null)
            {
                this.fishingRod = UnityEngine.Object.FindAnyObjectByType<ClassFishingRod>();
            }
        }

        if (this.fishContainer == null)
        {
            GameObject pondObject = new GameObject("DebugFishPond");
            pondObject.AddComponent<RectTransform>();
            this.fishContainer = pondObject.transform;
        }
    }

    private void OnEnable()
    {
        if (this.fishingRod != null)
        {
            this.fishingRod.OnFishReeledIn += this.HandleFishReeledIn;
        }
    }

    private void OnDisable()
    {
        if (this.fishingRod != null)
        {
            this.fishingRod.OnFishReeledIn -= this.HandleFishReeledIn;
        }
    }

    private void Start()
    {
        int count = Mathf.Max(1, this.initialFishCount);
        for (int i = 0; i < count; i++)
        {
            this.SpawnTestFish(i);
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 360f, 320f), "Debug Fishing Rod", GUI.skin.window);

        if (this.fishingRod == null)
        {
            GUILayout.Label("No ClassFishingRod assigned or found.");
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"State: {this.fishingRod.CurrentState} | InWater: {this.fishingRod.Physics.IsInWater}");
        GUILayout.Label($"Last Peak Speed: {this.fishingRod.LastPeakCastSpeed:F3} | Multiplier: {this.fishingRod.LastSpeedMultiplier:F2}x");
        GUILayout.Label($"Hooked Fish: {(this.fishingRod.HookedFish != null ? this.fishingRod.HookedFish.FishName : "None")}");
        GUILayout.Label($"Caught Count: {this.caughtCount} | Last: {this.lastCatchMessage}");

        GUILayout.Space(6f);
        GUILayout.Label("Controls: [X] Toggle Ready | [Swipe Mouse Right] Cast | [Hold RMB] Reel | [C] Cancel");

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Toggle Ready (X)"))
        {
            this.fishingRod.ToggleReady();
        }
        if (GUILayout.Button("Cancel (C)"))
        {
            this.fishingRod.CancelAndResetToIdle();
        }
        if (GUILayout.Button("Spawn Fish"))
        {
            this.SpawnTestFish(this.spawnedFish.Count);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cobweb"))
        {
            this.fishingRod.ApplyStringColor(StringType.Cobweb);
        }
        if (GUILayout.Button("String"))
        {
            this.fishingRod.ApplyStringColor(StringType.String);
        }
        if (GUILayout.Button("Gold"))
        {
            this.fishingRod.ApplyStringColor(StringType.GoldString);
        }
        if (GUILayout.Button("Crimson"))
        {
            this.fishingRod.SetStringColor(new Color(0.95f, 0.25f, 0.25f, 1f));
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
    #endregion

    #region Private Funcs
    private void HandleFishReeledIn(ClassFish caughtFish)
    {
        this.caughtCount++;
        this.lastCatchMessage = caughtFish != null
            ? $"{caughtFish.FishName} ({caughtFish.FishTypeCurrent}, {caughtFish.DefaultPrice:F0}g)"
            : "Empty Hook";

        Debug.Log($"[DebugFishingRodController.HandleFishReeledIn] Caught #{this.caughtCount}: {this.lastCatchMessage}");

        if (caughtFish != null && this.respawnFishOnCatch)
        {
            this.SpawnTestFish(this.caughtCount);
        }
    }

    private void SpawnTestFish(int index)
    {
        FishType fishType = DebugFishTypes[index % DebugFishTypes.Length];
        ScriptableFish scriptableFish = Resources.Load<ScriptableFish>($"Scriptables/{fishType}");
        if (scriptableFish == null)
        {
            Debug.LogError($"[DebugFishingRodController.SpawnTestFish] Missing ScriptableFish at Resources/Scriptables/{fishType}.");
            return;
        }

        Vector2 basePos = SpawnOffsets[index % SpawnOffsets.Length];
        Vector2 jitter = new Vector2(Random.Range(-40f, 40f), Random.Range(-40f, 40f));
        Vector2 spawnPos = basePos + jitter;

        GameObject fishObject = new GameObject($"Debug_{fishType}_{index + 1}");
        fishObject.transform.SetParent(this.fishContainer, false);

        RectTransform rectTransform = fishObject.AddComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(spawnPos.x, spawnPos.y, 0f);
        rectTransform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = fishObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = scriptableFish.SpriteFish;
        spriteRenderer.sortingLayerName = "Fishes";
        spriteRenderer.sortingOrder = 10;

        ClassFish fish = fishObject.AddComponent<ClassFish>();
        fish.InitFish(scriptableFish, spawnPos.x, spawnPos.y);
        fishObject.transform.localScale = new Vector3(200f, 200f, 1f);

        this.spawnedFish.Add(fish);
    }
    #endregion
}
