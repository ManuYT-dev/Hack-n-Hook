using Assets.Scripts.FishMovement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class InitFishes
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Start()
    {
        CreateFishes();
    }

    private static List<ClassFish> CreateFishes()
    {
        List<ClassFish> fishes = new List<ClassFish>();
        Sprite fishSprite = Resources.Load<Sprite>("debug_Fish");
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

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
}
