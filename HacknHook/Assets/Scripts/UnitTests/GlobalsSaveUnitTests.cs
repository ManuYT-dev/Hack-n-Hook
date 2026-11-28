using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GlobalsSaveUnitTests : MonoBehaviour
{
    #region Private Variables
    private readonly List<string> _results = new();
    private int _passed;
    private int _failed;
    private Vector2 _scrollPos;
    #endregion

    #region Unity Funcs
    private IEnumerator Start()
    {
        yield return null;
        this.RunAllTests();
    }

    private void OnGUI()
    {
        float colWidth = Mathf.Max(180f, (Screen.width - 64f) / 3f);
        float x = 16f + (colWidth + 16f) * 2f;
        GUILayout.BeginArea(new Rect(x, 16f, colWidth, Screen.height - 32f));
        GUIStyle headerStyle = new(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = this._failed == 0 ? Color.green : Color.red;
        GUILayout.Label($"Globals & Save: {this._passed} passed, {this._failed} failed", headerStyle);
        this._scrollPos = GUILayout.BeginScrollView(this._scrollPos);
        foreach (string line in this._results)
        {
            GUILayout.Label(line);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    #endregion

    #region Public Funcs
    [ContextMenu("Re-run Globals & SerializeGame Unit Tests")]
    public void RunAllTests()
    {
        this._results.Clear();
        this._passed = 0;
        this._failed = 0;

        string savePath = SerializeGame.SaveFilePath;
        bool hadExistingSave = File.Exists(savePath);
        byte[] existingSaveBackup = hadExistingSave ? File.ReadAllBytes(savePath) : null;

        try
        {
            this.RunGlobalsDefaultsTests();
            this.RunGlobalsJsonTests();
            this.RunSerializeGameFileTests();
        }
        finally
        {
            Globals.ResetToDefaults();

            if (hadExistingSave)
            {
                File.WriteAllBytes(savePath, existingSaveBackup);
            }
            else if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }

        string summary = $"[GlobalsSaveUnitTests] Finished: {this._passed} passed, {this._failed} failed.";
        if (this._failed == 0)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogError(summary);
        }
    }
    #endregion

    #region Test Suites
    private void RunGlobalsDefaultsTests()
    {
        Globals.cash = 999f;
        Globals.isPaused = true;
        Globals.equippedEngine = EngineType.Level3;
        Globals.caughtFishCounts[FishType.Barsch] = 5;

        Globals.ResetToDefaults();

        this.Assert("ResetToDefaults -> cash == 0", Mathf.Approximately(Globals.cash, 0f));
        this.Assert("ResetToDefaults -> totalCashEarned == 0", Mathf.Approximately(Globals.totalCashEarned, 0f));
        this.Assert("ResetToDefaults -> equippedEngine == Level0", Globals.equippedEngine == EngineType.Level0);
        this.Assert("ResetToDefaults -> bestPurchasedEngine == Level0", Globals.bestPurchasedEngine == EngineType.Level0);
        this.Assert("ResetToDefaults -> equippedFishingLine == Level0", Globals.equippedFishingLine == FishingLineType.Level0);
        this.Assert("ResetToDefaults -> bestPurchasedFishingLine == Level0", Globals.bestPurchasedFishingLine == FishingLineType.Level0);
        this.Assert("ResetToDefaults -> equippedBait == Level0", Globals.equippedBait == BaitType.Level0);
        this.Assert("ResetToDefaults -> bestPurchasedBait == Level0", Globals.bestPurchasedBait == BaitType.Level0);
        this.Assert("ResetToDefaults -> totalFishCaught == 0", Globals.totalFishCaught == 0);
        this.Assert("ResetToDefaults -> caughtFishCounts empty", Globals.caughtFishCounts != null && Globals.caughtFishCounts.Count == 0);
        this.Assert("ResetToDefaults -> boatInstance == null", Globals.boatInstance == null);
        this.Assert("ResetToDefaults -> isPaused == false", !Globals.isPaused);
        this.Assert("ResetToDefaults -> isInShop == false", !Globals.isInShop);
    }

    private void RunGlobalsJsonTests()
    {
        SerializeGame.DeleteSave();
        Globals.ResetToDefaults();

        Globals.cash = 1234.5f;
        Globals.totalCashEarned = 5000f;
        Globals.equippedEngine = EngineType.Level2;
        Globals.bestPurchasedEngine = EngineType.Level3;
        Globals.equippedFishingLine = FishingLineType.Level2;
        Globals.bestPurchasedFishingLine = FishingLineType.Level3;
        Globals.equippedBait = BaitType.Level2;
        Globals.bestPurchasedBait = BaitType.Level3;
        Globals.totalFishCaught = 42;
        Globals.heaviestFishWeight = 18.75f;
        Globals.maxDistanceReached = 350f;
        Globals.maxDepthReached = 95.5f;
        Globals.caughtFishCounts[FishType.Barsch] = 10;
        Globals.caughtFishCounts[FishType.Hecht] = 3;

        // Defensive copy check on ToSaveData()
        GlobalsSaveData snapshot = Globals.ToSaveData();
        Globals.caughtFishCounts[FishType.Forelle] = 99;
        this.Assert("ToSaveData -> creates defensive copy of caughtFishCounts",
            snapshot.caughtFishCounts.Count == 2 && !snapshot.caughtFishCounts.ContainsKey(FishType.Forelle));
        Globals.caughtFishCounts.Remove(FishType.Forelle);

        // ToJson() must return JSON without writing to disk
        string json = Globals.ToJson(true);
        this.Assert("ToJson -> returns non-empty string", !string.IsNullOrWhiteSpace(json));
        this.Assert("ToJson -> does not write save file to disk", !SerializeGame.HasSave());
        this.Assert("ToJson -> excludes runtime-only fields",
            !json.Contains("boatInstance") && !json.Contains("isPaused") && !json.Contains("isInShop"));

        // Round-trip Load(json)
        Globals.ResetToDefaults();
        Globals.isPaused = true;
        Globals.sessionPlayTime = 77f;

        bool loaded = Globals.Load(json);
        this.Assert("Globals.Load(json) -> returns true", loaded);
        this.Assert("Globals.Load(json) -> restores cash", Mathf.Approximately(Globals.cash, 1234.5f));
        this.Assert("Globals.Load(json) -> restores totalCashEarned", Mathf.Approximately(Globals.totalCashEarned, 5000f));
        this.Assert("Globals.Load(json) -> restores equippedEngine", Globals.equippedEngine == EngineType.Level2);
        this.Assert("Globals.Load(json) -> restores bestPurchasedEngine", Globals.bestPurchasedEngine == EngineType.Level3);
        this.Assert("Globals.Load(json) -> restores equippedFishingLine", Globals.equippedFishingLine == FishingLineType.Level2);
        this.Assert("Globals.Load(json) -> restores bestPurchasedFishingLine", Globals.bestPurchasedFishingLine == FishingLineType.Level3);
        this.Assert("Globals.Load(json) -> restores equippedBait", Globals.equippedBait == BaitType.Level2);
        this.Assert("Globals.Load(json) -> restores bestPurchasedBait", Globals.bestPurchasedBait == BaitType.Level3);
        this.Assert("Globals.Load(json) -> restores totalFishCaught", Globals.totalFishCaught == 42);
        this.Assert("Globals.Load(json) -> restores heaviestFishWeight", Mathf.Approximately(Globals.heaviestFishWeight, 18.75f));
        this.Assert("Globals.Load(json) -> restores maxDistanceReached", Mathf.Approximately(Globals.maxDistanceReached, 350f));
        this.Assert("Globals.Load(json) -> restores maxDepthReached", Mathf.Approximately(Globals.maxDepthReached, 95.5f));
        this.Assert("Globals.Load(json) -> restores caughtFishCounts",
            Globals.caughtFishCounts.TryGetValue(FishType.Barsch, out int barsch) && barsch == 10 &&
            Globals.caughtFishCounts.TryGetValue(FishType.Hecht, out int hecht) && hecht == 3);
        this.Assert("Globals.Load(json) -> preserves runtime-only fields",
            Globals.isPaused && Mathf.Approximately(Globals.sessionPlayTime, 77f));

        // Invalid JSON handling (suppress expected warning/error logs from negative test cases)
        bool prevLogEnabled = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = false;
        bool loadedEmpty;
        bool loadedInvalid;
        try
        {
            loadedEmpty = Globals.Load(string.Empty);
            loadedInvalid = Globals.Load("{not valid json");
        }
        finally
        {
            Debug.unityLogger.logEnabled = prevLogEnabled;
        }
        this.Assert("Globals.Load(empty) -> returns false", !loadedEmpty);
        this.Assert("Globals.Load(invalid) -> returns false", !loadedInvalid);
    }

    private void RunSerializeGameFileTests()
    {
        SerializeGame.DeleteSave();
        this.Assert("SerializeGame.SaveFilePath -> ends with savegame.json",
            SerializeGame.SaveFilePath.EndsWith("savegame.json", StringComparison.OrdinalIgnoreCase));
        this.Assert("SerializeGame.HasSave -> false after DeleteSave", !SerializeGame.HasSave());
        this.Assert("SerializeGame.Load -> false when no save exists", !SerializeGame.Load());

        Globals.ResetToDefaults();
        Globals.cash = 450f;
        Globals.equippedEngine = EngineType.Level3;
        Globals.bestPurchasedEngine = EngineType.Level3;
        Globals.caughtFishCounts[FishType.Forelle] = 7;

        bool saved = SerializeGame.Save();
        this.Assert("SerializeGame.Save -> returns true", saved);
        this.Assert("SerializeGame.HasSave -> true after Save", SerializeGame.HasSave());

        Globals.ResetToDefaults();
        bool loaded = SerializeGame.Load();
        this.Assert("SerializeGame.Load -> returns true", loaded);
        this.Assert("SerializeGame.Load -> restores cash", Mathf.Approximately(Globals.cash, 450f));
        this.Assert("SerializeGame.Load -> restores equippedEngine", Globals.equippedEngine == EngineType.Level3);
        this.Assert("SerializeGame.Load -> restores caughtFishCounts",
            Globals.caughtFishCounts.TryGetValue(FishType.Forelle, out int forelle) && forelle == 7);

        // Overwrite same single save slot
        Globals.cash = 900f;
        Globals.equippedFishingLine = FishingLineType.Level3;
        this.Assert("SerializeGame.Save (overwrite) -> returns true", SerializeGame.Save());

        Globals.ResetToDefaults();
        this.Assert("SerializeGame.Load (after overwrite) -> returns true", SerializeGame.Load());
        this.Assert("SerializeGame.Load (after overwrite) -> cash == 900", Mathf.Approximately(Globals.cash, 900f));
        this.Assert("SerializeGame.Load (after overwrite) -> equippedFishingLine == Level3", Globals.equippedFishingLine == FishingLineType.Level3);

        bool deleted = SerializeGame.DeleteSave();
        this.Assert("SerializeGame.DeleteSave -> returns true", deleted);
        this.Assert("SerializeGame.HasSave -> false after DeleteSave", !SerializeGame.HasSave());
    }
    #endregion

    #region Helpers
    private void Assert(string testName, bool condition)
    {
        if (condition)
        {
            this._passed++;
            this._results.Add($"PASS: {testName}");
        }
        else
        {
            this._failed++;
            string message = $"FAIL: {testName}";
            this._results.Add(message);
            Debug.LogError($"[GlobalsSaveUnitTests] {message}");
        }
    }
    #endregion
}
