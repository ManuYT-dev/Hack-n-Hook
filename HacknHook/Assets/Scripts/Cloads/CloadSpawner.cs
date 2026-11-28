using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Die 5 Wolken-Sprites")]
    public Sprite[] cloudSprites = new Sprite[5];

    [Header("Spawn-Abstand in Sekunden")]
    public float minTimeBetweenSpawns = 1f;
    public float maxTimeBetweenSpawns = 3f;

    [Header("Zufällige Spawn-Höhe")]
    public float minSpawnYOffset = -3f;
    public float maxSpawnYOffset = 3f;

    [Header("Bewegung und Größe")]
    public float moveSpeed = 2f;
    [Min(0.01f)] public float cloudScale = 1f;
    public float despawnX = 12f;

    [Header("Sortierung")]
    public string sortingLayerName = "Fishes";
    public int sortingOrder = 0;

    private readonly List<Transform> spawnedClouds = new List<Transform>();
    private float timeUntilNextSpawn;

    private void Start()
    {
        timeUntilNextSpawn = GetRandomSpawnTime();
    }

    private void Update()
    {
        MoveClouds();

        timeUntilNextSpawn -= Time.deltaTime;

        if (timeUntilNextSpawn <= 0f)
        {
            SpawnCloud();
            timeUntilNextSpawn = GetRandomSpawnTime();
        }
    }

    private float GetRandomSpawnTime()
    {
        float min = Mathf.Max(0.05f, minTimeBetweenSpawns);
        float max = Mathf.Max(min, maxTimeBetweenSpawns);

        return Random.Range(min, max);
    }

    private void SpawnCloud()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
            return;

        Sprite chosenSprite = null;

        for (int attempt = 0; attempt < cloudSprites.Length; attempt++)
        {
            Sprite candidate = cloudSprites[Random.Range(0, cloudSprites.Length)];

            if (candidate != null)
            {
                chosenSprite = candidate;
                break;
            }
        }

        if (chosenSprite == null)
            return;

        float lowY = Mathf.Min(minSpawnYOffset, maxSpawnYOffset);
        float highY = Mathf.Max(minSpawnYOffset, maxSpawnYOffset);

        Vector3 spawnPosition = new Vector3(
            transform.position.x,
            transform.position.y + Random.Range(lowY, highY),
            transform.position.z
        );

        GameObject cloud = new GameObject("Cloud_" + chosenSprite.name);
        cloud.transform.position = spawnPosition;
        cloud.transform.localScale = Vector3.one * Mathf.Max(0.01f, cloudScale);

        SpriteRenderer cloudRenderer = cloud.AddComponent<SpriteRenderer>();
        cloudRenderer.sprite = chosenSprite;
        cloudRenderer.sortingLayerName = sortingLayerName;
        cloudRenderer.sortingOrder = sortingOrder;

        spawnedClouds.Add(cloud.transform);
    }

    private void MoveClouds()
    {
        for (int i = spawnedClouds.Count - 1; i >= 0; i--)
        {
            Transform cloud = spawnedClouds[i];

            if (cloud == null)
            {
                spawnedClouds.RemoveAt(i);
                continue;
            }

            cloud.localScale = Vector3.one * Mathf.Max(0.01f, cloudScale);
            cloud.position += Vector3.right * moveSpeed * Time.deltaTime;

            if (cloud.position.x >= despawnX)
            {
                Destroy(cloud.gameObject);
                spawnedClouds.RemoveAt(i);
            }
        }
    }
}