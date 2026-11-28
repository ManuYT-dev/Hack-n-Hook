using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableFishingLine", menuName = "Scriptable Objects/ScriptableFishingLine")]
public class ScriptableFishingLine : ScriptableObject
{
    #region Inspector Fields
    [SerializeField] private float deapth;
    [SerializeField] private Sprite sprite;
    #endregion

    #region Code Fields
    public float Deapth => this.deapth;
    public Sprite SpriteFish => this.sprite;
    #endregion
}
