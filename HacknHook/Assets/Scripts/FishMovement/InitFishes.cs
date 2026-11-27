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
        CreateFishes();
        StartBaitSpawner();
    }

    private static List<ClassFish> CreateFishes()
    {
        List<ClassFish> fishes = new List<ClassFish>();
        Sprite fishSprite = Resources.Load<Sprite>("debug_Fish");
        canvas = Object.FindAnyObjectByType<Canvas>();

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
        }
        return fishes;
    }

    private static void StartBaitSpawner()
    {
        baitSprite = Resources.Load<Sprite>("debug_bait");
        
        GameObject spawnerObj = new GameObject("BaitSpawner");
        spawnerObj.transform.SetParent(canvas.transform, false);
        
        BaitSpawner spawner = spawnerObj.AddComponent<BaitSpawner>();
        spawner.Initialize(canvas, baitSprite);
    }
}

public class BaitSpawner : MonoBehaviour
{
    private Canvas canvas;
    private Sprite baitSprite;
    private float spawnInterval = 5f;
    private float spawnTimer = 0f;

    public void Initialize(Canvas canvasRef, Sprite sprite)
    {
        this.canvas = canvasRef;
        this.baitSprite = sprite;
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
        GameObject baitObj = new GameObject("Bait");
        baitObj.transform.SetParent(this.canvas.transform, false);

        Image image = baitObj.AddComponent<Image>();
        image.sprite = this.baitSprite;

        RectTransform rectTransform = baitObj.GetComponent<RectTransform>();
        // Spawn oberhalb des Screens
        rectTransform.anchoredPosition = new Vector2(Random.Range(-800, 800), 500f);
        rectTransform.sizeDelta = new Vector2(50f, 50f);

        ClassBait bait = baitObj.AddComponent<ClassBait>();
    }
}
