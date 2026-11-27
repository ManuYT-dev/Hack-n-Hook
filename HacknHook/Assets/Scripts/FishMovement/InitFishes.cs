using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public static class InitFishes
{
    private static Transform parent;
    private static Sprite baitSprite;
    private static Canvas canvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Start()
    {
        Debug.Log("[InitFishes] Initialisierung gestartet.");
        CreateCanvas();
        CreateFishes();
        StartBaitSpawner();
        Debug.Log("[InitFishes] Initialisierung abgeschlossen.");
    }

    private static void CreateCanvas()
    {
        // Suche nach existierendem Canvas
        canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            // Erstelle neuen Canvas falls keiner existiert
            GameObject canvasObj = new GameObject("GameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[InitFishes.CreateCanvas] Neuer Canvas erstellt.");
        }

        // Erstelle Container für Fische und Bait als Kinder des Canvas
        GameObject fishContainer = new GameObject("Fishes");
        fishContainer.transform.SetParent(canvas.transform, false);
        parent = fishContainer.transform;

        Debug.Log("[InitFishes.CreateCanvas] Canvas gefunden/erstellt.");
    }

    private static List<ClassFish> CreateFishes()
    {
        Debug.Log("[InitFishes.CreateFishes] Erstelle 12 Fische...");
        List<ClassFish> fishes = new List<ClassFish>();
        ScriptableFish scriptableFish = Resources.Load<ScriptableFish>("Scriptables/Forelle");

        if (scriptableFish.SpriteFish == null)
        {
            Debug.LogError("[InitFishes.CreateFishes] Fisch-Sprite nicht gefunden!");
            return fishes;
        }

        for (int i = 0; i < 12; i++)
        {
            GameObject obj = new GameObject($"Fish{i + 1}");
            obj.transform.SetParent(parent, false);

            Image image = obj.AddComponent<Image>();
            image.sprite = scriptableFish.SpriteFish;

            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 70f);
            rectTransform.anchoredPosition = new Vector2(Random.Range(-400f, 400f), Random.Range(-200f, 200f));

            ClassFish fish = obj.AddComponent<ClassFish>();
            fish.InitFish(scriptableFish, 1f, rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y);
            fishes.Add(fish);

            Debug.Log($"[InitFishes.CreateFishes] Fisch {i + 1} erstellt an Position ({rectTransform.anchoredPosition.x:F1}, {rectTransform.anchoredPosition.y:F1})");
        }
        Debug.Log($"[InitFishes.CreateFishes] {fishes.Count} Fische erfolgreich erstellt.");
        return fishes;
    }

    private static void StartBaitSpawner()
    {
        Debug.Log("[InitFishes.StartBaitSpawner] BaitSpawner wird initialisiert...");
        baitSprite = Resources.Load<Sprite>("debug_bait");

        if (baitSprite == null)
        {
            Debug.LogError("[InitFishes.StartBaitSpawner] Bait-Sprite nicht gefunden!");
            return;
        }

        GameObject spawnerObj = new GameObject("BaitSpawner");
        spawnerObj.transform.SetParent(parent, false);
        
        BaitSpawner spawner = spawnerObj.AddComponent<BaitSpawner>();
        spawner.Initialize(parent, baitSprite);
        Debug.Log("[InitFishes.StartBaitSpawner] BaitSpawner erfolgreich initialisiert.");
    }
}

public class BaitSpawner : MonoBehaviour
{
    private Transform parent;
    private Sprite baitSprite;
    private float spawnInterval = 5f;
    private float spawnTimer = 0f;
    private int baitCount = 0;

    public void Initialize(Transform parentTransform, Sprite sprite)
    {
        this.parent = parentTransform;
        this.baitSprite = sprite;
        this.spawnTimer = this.spawnInterval;
        Debug.Log($"[BaitSpawner.Initialize] BaitSpawner initialisiert mit Spawn-Intervall: {this.spawnInterval}s");
    }

    private void Update()
    {
        this.spawnTimer -= Time.deltaTime;
        if (this.spawnTimer <= 0f)
        {
            this.SpawnBait();
            this.spawnTimer = this.spawnInterval;
        }
    }

    private void SpawnBait()
    {
        this.baitCount++;

        GameObject baitObj = new GameObject($"Bait{this.baitCount}");
        baitObj.transform.SetParent(this.parent, false);

        Image image = baitObj.AddComponent<Image>();
        image.sprite = this.baitSprite;

        RectTransform rectTransform = baitObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(50f, 50f);
        rectTransform.anchoredPosition = new Vector2(Random.Range(-400f, 400f), 300f);

        ClassBait bait = baitObj.AddComponent<ClassBait>();
        Debug.Log($"[BaitSpawner.SpawnBait] Bait #{this.baitCount} gespawnt an Position ({rectTransform.anchoredPosition.x:F1}, {rectTransform.anchoredPosition.y:F1})");
    }
}
