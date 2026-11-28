using UnityEngine;
using System;
using UnityEngine;

public static class BaitEnumToScriptables
{
    public static T Convert<T>(Enum value, string folder = "Assets/Resources/Scriptables/BaitScriptables")
        where T : ScriptableObject
    {
        string path = $"{folder}/{value}";

        T scriptable = Resources.Load<T>(path);

        if (scriptable == null)
        {
            Debug.LogError($"ScriptableObject not found: {path}");
        }

        return scriptable;
    }
}

public static class FishingLineEnumToScriptables
{
    public static T Convert<T>(Enum value, string folder = "Assets/Resources/Scriptables/FishingLineScriptables")
        where T : ScriptableObject
    {
        string path = $"{folder}/{value}";

        T scriptable = Resources.Load<T>(path);

        if (scriptable == null)
        {
            Debug.LogError($"ScriptableObject not found: {path}");
        }

        return scriptable;
    }
}
public static class EngineEnumToScriptables
{
    public static T Convert<T>(Enum value, string folder = "Assets/Resources/Scriptables/FishingLineScriptables")
        where T : ScriptableObject
    {
        string path = $"{folder}/{value}";

        T scriptable = Resources.Load<T>(path);

        if (scriptable == null)
        {
            Debug.LogError($"ScriptableObject not found: {path}");
        }

        return scriptable;
    }
}