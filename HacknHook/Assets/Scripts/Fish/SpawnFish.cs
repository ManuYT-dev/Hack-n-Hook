using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnFish : MonoBehaviour
{
    private const string CHUNKS_CONTAINER_NAME = "Chunks";
    private const string CHUNK_PREFIX = "Chunk_";
    private const float CHUNK_WIDTH = 5760f;
    private const float CHUNK_HEIGHT = 3240f;
    private const int FISH_PER_CHUNK = 10;

    private static readonly Dictionary<string, FishSpawnConfig[]> ChunkConfigs =
        new Dictionary<string, FishSpawnConfig[]>();

    private class FishSpawnConfig
    {
        public FishType fishType;
        public int spawnCount;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += this.OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= this.OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            this.Start();
        }
    }

    public void Start()
    {
        InitializeChunkConfigs();

        GameObject chunksContainer = GameObject.Find(CHUNKS_CONTAINER_NAME);
        if (chunksContainer == null)
        {
            Debug.LogError($"Container '{CHUNKS_CONTAINER_NAME}' nicht gefunden!");
            return;
        }

        Debug.Log($"[SpawnFish] Max Chunks: {DefaultSettings.max_chunk}");

        int chunkCount = 0;
        foreach (Transform chunkTransform in chunksContainer.transform)
        {
            string chunkName = chunkTransform.gameObject.name;
            if (!chunkName.StartsWith(CHUNK_PREFIX))
                continue;

            if (!TryParseChunkCoordinates(chunkName, out int row, out int column))
            {
                Debug.LogWarning($"[SpawnFish] Konnte Koordinaten von {chunkName} nicht parsen");
                continue;
            }

            Debug.Log($"[SpawnFish] Verarbeite {chunkName}: Row={row}, Column={column}");

            if (!IsChunkInMaxBounds(row, column))
            {
                Debug.Log($"[SpawnFish] {chunkName} ist außerhalb der Grenzen ({row}, {column}) vs Max ({DefaultSettings.max_chunk})");
                continue;
            }

            chunkCount++;
            SpawnFishesInChunk(chunkTransform, row, column);
        }

        Debug.Log($"[SpawnFish] Insgesamt {chunkCount} Chunks verarbeitet");
    }

    private static void InitializeChunkConfigs()
    {
        ChunkConfigs.Clear();

        // Row 1: Hauptsächlich Barsche (8 Barsche, 1 Forelle, 1 Hecht)
        ChunkConfigs["1_1"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 8 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 1 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 1 }
        };

        ChunkConfigs["1_2"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 8 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 1 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 1 }
        };

        ChunkConfigs["1_3"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 8 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 1 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 1 }
        };

        // Row 2: Mischung mit mehr Hechten und Forellen (2 Barsche, 4 Forellen, 4 Hechte)
        ChunkConfigs["2_1"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 2 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 4 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 4 }
        };

        ChunkConfigs["2_2"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 2 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 4 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 4 }
        };

        ChunkConfigs["2_3"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 2 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 4 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 4 }
        };

        // Row 3: Viele Hechte und Forellen, wenig Barsche (1 Barsch, 4 Forellen, 5 Hechte)
        ChunkConfigs["3_1"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 1 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 4 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 5 }
        };

        ChunkConfigs["3_2"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 1 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 4 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 5 }
        };

        ChunkConfigs["3_3"] = new FishSpawnConfig[]
        {
            new FishSpawnConfig { fishType = FishType.Barsch, spawnCount = 1 },
            new FishSpawnConfig { fishType = FishType.Forelle, spawnCount = 4 },
            new FishSpawnConfig { fishType = FishType.Hecht, spawnCount = 5 }
        };
    }

    private static bool TryParseChunkCoordinates(string chunkName, out int row, out int column)
    {
        row = 0;
        column = 0;

        string[] parts = chunkName.Split('_');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[1], out row) || !int.TryParse(parts[2], out column))
            return false;

        return true;
    }

    private static bool IsChunkInMaxBounds(int row, int column)
    {
        bool isValid = row >= 1 &&
               column >= 1 &&
               row <= DefaultSettings.max_chunk.Item1 &&
               column <= DefaultSettings.max_chunk.Item2;

        return isValid;
    }

    private static void SpawnFishesInChunk(Transform chunkTransform, int row, int column)
    {
        string configKey = $"{row}_{column}";

        if (!ChunkConfigs.ContainsKey(configKey))
        {
            Debug.LogWarning($"[SpawnFishesInChunk] Keine Konfiguration für {configKey}");
            return;
        }

        FishSpawnConfig[] configs = ChunkConfigs[configKey];

        Bounds bounds = GetChunkBounds(chunkTransform);

        Debug.Log($"[SpawnFishesInChunk] Spawne Fische in {configKey}:");

        foreach (FishSpawnConfig config in configs)
        {
            for (int i = 0; i < config.spawnCount; i++)
            {
                Vector3 randomPosition = GetRandomPositionInBounds(
                    bounds,
                    chunkTransform.position,
                    row == 1
                );
                SpawnFishAtPosition(randomPosition, config.fishType, chunkTransform);
            }
            Debug.Log($"  - {config.fishType}: {config.spawnCount} Stück");
        }
    }

    private static Bounds GetChunkBounds(Transform chunkTransform)
    {
        Vector3 chunkCenter = chunkTransform.position;
        Vector3 chunkSize = new Vector3(CHUNK_WIDTH, CHUNK_HEIGHT, 0f);

        Bounds bounds = new Bounds(chunkCenter, chunkSize);

        return bounds;
    }

    private static Vector3 GetRandomPositionInBounds(Bounds bounds, Vector3 chunkCenter, bool bottomThreeQuarters)
    {
        float minX = chunkCenter.x - CHUNK_WIDTH / 2f;
        float maxX = chunkCenter.x + CHUNK_WIDTH / 2f;

        float minY = chunkCenter.y - CHUNK_HEIGHT / 2f;
        float maxY = chunkCenter.y + CHUNK_HEIGHT / 2f;

        if (bottomThreeQuarters)
        {
            // Oberes Viertel ausschließen
            maxY = minY + CHUNK_HEIGHT * 0.75f;
        }

        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        return new Vector3(randomX, randomY, chunkCenter.z);
    }

    private static void SpawnFishAtPosition(Vector3 worldPosition, FishType fishType, Transform parent)
    {
        // Konvertiere FishType zu String - mit expliziter Konvertierung
        string fishTypeName = fishType.ToString();
        string scriptablePath = $"Scriptables/{fishTypeName}";

        Debug.Log($"[SpawnFish] Versuche zu laden: {scriptablePath} für {fishTypeName}");

        ScriptableFish scriptableFish = Resources.Load<ScriptableFish>(scriptablePath);

        if (scriptableFish == null)
        {
            Debug.LogError($"[SpawnFish] ScriptableFish nicht gefunden: Assets/Resources/{scriptablePath}.asset");
            Debug.LogError($"[SpawnFish] FishType: {fishType}, konvertiert zu: {fishTypeName}");
            return;
        }

        if (scriptableFish.SpriteFish == null)
        {
            Debug.LogError($"[SpawnFish] Das ScriptableFish '{fishTypeName}' hat kein Sprite!");
            return;
        }

        Debug.Log($"[SpawnFish] Erfolgreich geladen: {fishTypeName}");

        GameObject fishObject = new GameObject($"{fishTypeName}_{Random.Range(1000, 9999)}");
        fishObject.transform.SetParent(parent, false);

        Vector3 localPosition = parent.InverseTransformPoint(worldPosition);

        RectTransform rectTransform = fishObject.AddComponent<RectTransform>();
        rectTransform.localPosition = localPosition;
        rectTransform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = fishObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = scriptableFish.SpriteFish;
        spriteRenderer.sortingLayerName = "Fishes";
        spriteRenderer.sortingOrder = 10;

        ClassFish fish = fishObject.AddComponent<ClassFish>();
        fish.InitFish(scriptableFish, localPosition.x, localPosition.y);

        fishObject.transform.localScale = new Vector3(200f, 200f, 1f);
    }
}
