using Assets.Scripts.FishMovement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class InitFishes
{
    private static Canvas canvas;
    private static Sprite baitSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Start()
    {
        Debug.Log("[InitFishes] Initialisierung gestartet.");
        CreateFishes();
        StartBaitSpawner();
        Debug.Log("[InitFishes] Initialisierung abgeschlossen.");
    }

    private static List<ClassFish> CreateFishes()
    {
        Debug.Log("[InitFishes.CreateFishes] Erstelle 12 Fische...");
        List<ClassFish> fishes = new List<ClassFish>();
        Sprite fishSprite = Resources.Load<Sprite>("debug_Fish");

        if (fishSprite == null)
        {
            Debug.LogError("[InitFishes.CreateFishes] Fisch-Sprite nicht gefunden!");
        }

        canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[InitFishes.CreateFishes] Canvas nicht gefunden!");
            return fishes;
        }

        for (int i = 0; i < 12; i++)
        {
            GameObject obj = new GameObject($"Fish{i + 1}");
            obj.transform.SetParent(canvas.transform, false);

            Image image = obj.AddComponent<Image>();
            image.sprite = fishSprite;

            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(Random.Range(-800, 800), Random.Range(-400, 400));
            rectTransform.sizeDelta = new Vector2(100f, 100f);

            ClassFish fish = obj.AddComponent<ClassFish>();
            fish.InitFish(FishType.Forelle, rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y);
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
        spawnerObj.transform.SetParent(canvas.transform, false);

        BaitSpawner spawner = spawnerObj.AddComponent<BaitSpawner>();
        spawner.Initialize(canvas, baitSprite);
        Debug.Log("[InitFishes.StartBaitSpawner] BaitSpawner erfolgreich initialisiert.");
    }
}

public class BaitSpawner : MonoBehaviour
{
    private Canvas canvas;
    private Sprite baitSprite;
    private float spawnInterval = 5f;
    private float spawnTimer = 0f;
    private int baitCount = 0;

    public void Initialize(Canvas canvasRef, Sprite sprite)
    {
        this.canvas = canvasRef;
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
        Vector2 spawnPos = new Vector2(Random.Range(-800, 800), 500f);

        GameObject baitObj = new GameObject($"Bait{this.baitCount}");
        baitObj.transform.SetParent(this.canvas.transform, false);

        Image image = baitObj.AddComponent<Image>();
        image.sprite = this.baitSprite;

        RectTransform rectTransform = baitObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = spawnPos;
        rectTransform.sizeDelta = new Vector2(50f, 50f);

        ClassBait bait = baitObj.AddComponent<ClassBait>();
        Debug.Log($"[BaitSpawner.SpawnBait] Bait #{this.baitCount} gespawnt an Position ({spawnPos.x:F1}, {spawnPos.y:F1})");
    }
}
