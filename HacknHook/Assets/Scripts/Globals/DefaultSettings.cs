using UnityEngine;

public static class DefaultSettings
{
    public static (int, int) max_chunk;

    static public void Init()
    {
        max_chunk = (1, 1);
    }
}
