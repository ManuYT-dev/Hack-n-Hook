using Assets.Scripts.FishMovement;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ScriptableFish", menuName = "Scriptable Objects/ScriptableFish")]
public class ScriptableFish : ScriptableObject
{
    #region Inspector Fields
    [SerializeField] private string fishName;
    [SerializeField] private float speed;
    [SerializeField] private float boostedSpeed;   
    [SerializeField] private float defaultPrice;
    [SerializeField] private float roamRadius;
    [SerializeField] private float huntRadius;
    [SerializeField] private FishType fishType;
    [SerializeField] private Sprite sprite;
    #endregion

    #region Code Fields
    public string FishName => fishName;
    public float Speed => speed;
    public float BoostedSpeed => boostedSpeed;
    public float DefaultPrice => defaultPrice;
    public float RoamRadius => roamRadius;
    public float HuntRadius => huntRadius;
    public FishType FishTypeCurrent => fishType;
    public Sprite SpriteFish => sprite;
    #endregion
}
