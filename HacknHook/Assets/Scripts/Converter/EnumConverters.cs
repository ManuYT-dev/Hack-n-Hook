using UnityEngine;
using System;
using UnityEngine;

public static class BaitEnumToScriptables
{
    public static T Convert<T>(Enum value, string folder = "Scriptables/BaitScriptables")
        where T : ScriptableObject
    {
        string path = $"{folder}/Bait{value}";

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
    public static T Convert<T>(Enum value, string folder = "Scriptables/FishingLineScriptable")
        where T : ScriptableObject
    {
        string path = $"{folder}/FishingLine{value.ToString().Replace("Level", string.Empty)}";

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
    public static T Convert<T>(Enum value, string folder = "Scriptables/EngineScriptable")
        where T : ScriptableObject
    {
        string path = $"{folder}/Engine{value}";

        T scriptable = Resources.Load<T>(path);

        if (scriptable == null)
        {
            Debug.LogError($"ScriptableObject not found: {path}");
        }

        return scriptable;
    }
}