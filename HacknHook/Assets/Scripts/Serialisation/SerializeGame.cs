using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles saving and loading the single-slot JSON save file from <see cref="Globals"/>.
/// </summary>
public static class SerializeGame
{
    #region Constants & Properties
    private const string SaveFileName = "savegame.json";

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    #endregion

    #region Public Funcs
    /// <summary>
    /// Serializes all saveable <see cref="Globals"/> data and writes it to the single save file.
    /// </summary>
    /// <param name="prettyPrint">Whether to format the JSON with indentation.</param>
    /// <returns>True if saving succeeded; otherwise false.</returns>
    public static bool Save(bool prettyPrint = true)
    {
        try
        {
            string json = Globals.ToJson(prettyPrint);
            string path = SaveFilePath;
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SerializeGame.Save] Failed to save game: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Reads the single save file from disk and loads it into <see cref="Globals"/>.
    /// </summary>
    /// <returns>True if the save file existed and loaded successfully; otherwise false.</returns>
    public static bool Load()
    {
        string path = SaveFilePath;
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SerializeGame.Load] No save file found at {path}.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            return Globals.Load(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SerializeGame.Load] Failed to read save file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Returns whether a save file currently exists.
    /// </summary>
    public static bool HasSave()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// Deletes the save file if it exists.
    /// </summary>
    /// <returns>True if the file was deleted or did not exist; false on error.</returns>
    public static bool DeleteSave()
    {
        string path = SaveFilePath;
        if (!File.Exists(path))
        {
            return true;
        }

        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SerializeGame.DeleteSave] Failed to delete save file: {ex.Message}");
            return false;
        }
    }
    #endregion
}
