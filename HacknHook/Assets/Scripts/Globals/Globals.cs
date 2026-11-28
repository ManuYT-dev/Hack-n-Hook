using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable snapshot of all saveable fields from <see cref="Globals"/>.
/// </summary>
[Serializable]
public struct GlobalsSaveData
{
    [Header("Economy")]
    public float cash;
    public float totalCashEarned;

    [Header("Unlocks & Loadout")]
    public BoatType equippedBoat;
    public BoatType bestBoat;
    public StringType equippedString;
    public StringType bestString;
    public BaitType equippedBait;
    public BaitType bestBait;

    [Header("Stats & Records")]
    public int totalFishCaught;
    public float heaviestFishWeight;
    public float maxDistanceReached;
    public float maxDepthReached;
    public Dictionary<FishType, int> caughtFishCounts;
}

/// <summary>
/// Central runtime and progression state for the game.
/// </summary>
public static class Globals
{
    #region Runtime Only (Not Saved)
    public static ClassBoat boatInstance;
    public static bool isPaused;
    public static bool isInShop;
    public static float sessionPlayTime;
    #endregion

    #region Saveable State
    // Economy
    public static float cash;
    public static float totalCashEarned;

    // Unlocks & Loadout
    public static BoatType equippedBoat = BoatType.FisherBoat;
    public static BoatType bestBoat = BoatType.FisherBoat;
    public static StringType equippedString = StringType.Cobweb;
    public static StringType bestString = StringType.Cobweb;
    public static BaitType equippedBait = BaitType.Level1;
    public static BaitType bestBait = BaitType.Level1;

    // Stats & Records
    public static int totalFishCaught;
    public static float heaviestFishWeight;
    public static float maxDistanceReached;
    public static float maxDepthReached;
    public static Dictionary<FishType, int> caughtFishCounts = new Dictionary<FishType, int>();
    #endregion

    static Globals()
    {
        ResetToDefaults();
    }

    #region Public Funcs
    /// <summary>
    /// Initializes global state to default values.
    /// </summary>
    public static void Init()
    {
        ResetToDefaults();
    }

    /// <summary>
    /// Resets both runtime and saveable global variables to their starting defaults.
    /// </summary>
    public static void ResetToDefaults()
    {
        boatInstance = null;
        isPaused = false;
        isInShop = false;
        sessionPlayTime = 0f;

        cash = 0f;
        totalCashEarned = 0f;

        equippedBoat = BoatType.FisherBoat;
        bestBoat = BoatType.FisherBoat;
        equippedString = StringType.Cobweb;
        bestString = StringType.Cobweb;
        equippedBait = BaitType.Level1;
        bestBait = BaitType.Level1;


        totalFishCaught = 0;
        heaviestFishWeight = 0f;
        maxDistanceReached = 0f;
        maxDepthReached = 0f;
        caughtFishCounts = new Dictionary<FishType, int>();
    }

    /// <summary>
    /// Loads saveable global variables from a JSON string.
    /// </summary>
    /// <param name="json">JSON representation of <see cref="GlobalsSaveData"/>.</param>
    /// <returns>True if deserialization succeeded; otherwise false.</returns>
    public static bool Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[Globals.Load] JSON string is null or empty.");
            return false;
        }

        try
        {
            GlobalsSaveData data = JsonConvert.DeserializeObject<GlobalsSaveData>(json);
            Load(data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Globals.Load] Failed to deserialize save data: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads saveable global variables from a <see cref="GlobalsSaveData"/> struct.
    /// </summary>
    public static void Load(GlobalsSaveData data)
    {
        cash = data.cash;
        totalCashEarned = data.totalCashEarned;

        equippedBoat = data.equippedBoat;
        bestBoat = data.bestBoat;
        equippedString = data.equippedString;
        bestString = data.bestString;
        equippedBait = data.equippedBait;
        bestBait = data.bestBait;

        totalFishCaught = data.totalFishCaught;
        heaviestFishWeight = data.heaviestFishWeight;
        maxDistanceReached = data.maxDistanceReached;
        maxDepthReached = data.maxDepthReached;
        caughtFishCounts = data.caughtFishCounts != null
            ? new Dictionary<FishType, int>(data.caughtFishCounts)
            : new Dictionary<FishType, int>();
    }

    /// <summary>
    /// Creates a <see cref="GlobalsSaveData"/> struct containing all saveable global variables.
    /// </summary>
    public static GlobalsSaveData ToSaveData()
    {
        return new GlobalsSaveData
        {
            cash = cash,
            totalCashEarned = totalCashEarned,
            equippedBoat = equippedBoat,
            bestBoat = bestBoat,
            equippedString = equippedString,
            bestString = bestString,
            equippedBait = equippedBait,
            bestBait = bestBait,
            totalFishCaught = totalFishCaught,
            heaviestFishWeight = heaviestFishWeight,
            maxDistanceReached = maxDistanceReached,
            maxDepthReached = maxDepthReached,
            caughtFishCounts = caughtFishCounts != null
                ? new Dictionary<FishType, int>(caughtFishCounts)
                : new Dictionary<FishType, int>()
        };
    }

    /// <summary>
    /// Converts the saveable global variables into a JSON string without writing to disk.
    /// </summary>
    public static string ToJson(bool prettyPrint = false)
    {
        GlobalsSaveData data = ToSaveData();
        return JsonConvert.SerializeObject(data, prettyPrint ? Formatting.Indented : Formatting.None);
    }
    #endregion
}
