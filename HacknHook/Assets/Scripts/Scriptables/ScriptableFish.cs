using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableFish", menuName = "Scriptable Objects/ScriptableFish")]
public class ScriptableFish : ScriptableObject
{
    #region Inspector Fields
    [SerializeField] private string fishName;
    [SerializeField] private float speed;
    [SerializeField] private float boostedSpeed;
    [SerializeField] private float defaultPrice;
    [SerializeField] private float defaultWeight;
    [SerializeField] private float roamRadius;
    [SerializeField] private float huntRadius;
    [SerializeField] private FishType fishType;
    [SerializeField] private Sprite sprite;
    #endregion

    #region Code Fields
    public string FishName => this.fishName;
    public float Speed => this.speed;
    public float BoostedSpeed => this.boostedSpeed;
    public float DefaultPrice => this.defaultPrice;
    public float DefaultWeight => this.defaultWeight;
    public float RoamRadius => this.roamRadius;
    public float HuntRadius => this.huntRadius;
    public FishType FishTypeCurrent => this.fishType;
    public Sprite SpriteFish => this.sprite;
    #endregion
}
