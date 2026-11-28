using UnityEngine;

public static class DefaultSettings
{
    public static (int, int) max_chunk;

    public static void Init()
    {
        max_chunk = (3, 1);
    }
}
