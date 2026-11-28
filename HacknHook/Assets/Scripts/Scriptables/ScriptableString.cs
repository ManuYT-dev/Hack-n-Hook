using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableBait", menuName = "Scriptable Objects/ScriptableBait")]
public class ScriptableString : ScriptableObject
{
    #region Inspector Fields
    [SerializeField] private string stringName = "Baby Bait";
    [SerializeField] private float maxDepth = 100f;
    [SerializeField] private Color color = Color.floralWhite;
    #endregion

    #region Code Fields
    public string StringName => this.stringName;
    public float MaxDepth => this.maxDepth;
    public Color ColorString => this.color;
    #endregion
}
