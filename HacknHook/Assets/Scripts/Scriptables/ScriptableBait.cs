using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableBait", menuName = "Scriptable Objects/ScriptableBait")]
public class ScriptableBait : ScriptableObject
{
    #region Inspector Fields
    [SerializeField] private string baitName = "Baby Bait";
    [SerializeField] private float fallSpeed = 50f;
    [SerializeField] private float attractRadius = 100f;
    [SerializeField] private float weightMultiplier = 1f;
    [SerializeField] private Sprite sprite;
    public float StandardPrice = 0f;
    #endregion

    #region Code Fields
    public string BaitName => this.baitName;
    public float FallSpeed => this.fallSpeed;
    public float AttractRadius => this.attractRadius;
    public float WeightMultiplier => this.weightMultiplier;
    public Sprite SpriteBait => this.sprite;
    #endregion
}
