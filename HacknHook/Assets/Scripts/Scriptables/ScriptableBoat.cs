using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableBoat", menuName = "Scriptable Objects/ScriptableBoat")]
public class ScriptableBoat : ScriptableObject
{
    #region Inspector Fields
    [SerializeField] private string boatName;
    [SerializeField] private float speed;
    [SerializeField] private float boostedSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float defaultPrice;
    [SerializeField] private int maxCapacity;
    [SerializeField] private Sprite sprite;
    #endregion

    #region Code Fields
    public string BoatName => this.boatName;
    public float Speed => this.speed;
    public float BoostedSpeed => this.boostedSpeed;
    public float Acceleration => this.acceleration;
    public float TurnSpeed => this.turnSpeed;
    public float DefaultPrice => this.defaultPrice;
    public int MaxCapacity => this.maxCapacity;
    public Sprite SpriteBoat => this.sprite;
    #endregion
}
