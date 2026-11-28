using UnityEngine;

public class ClassFish : MonoBehaviour
{
    #region Properties

    private const float MinWeightFactor = 0.5f;
    private const float MaxWeightFactor = 1.5f;

    private float defaultPrice;
    private float defaultWeight;
    private float fishCurWeight;
    private float fishMaxWeight;

    #region Positions
    private float roamRadius;
    private float huntRadius;
    private Vector2 roamCenter;
    private Vector2 targetPosition;
    private Vector2 currentDirection;
    #endregion

    #region Fish Properties
    private FishType fishType;
    private float speed;
    private float boostedSpeed;
    [Header("Movement Settings")]
    [SerializeField] private float stopDistance = 0.3f;
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private float maxVerticalAngle = 30f;
    #endregion

    #region Bait Detection
    private ClassBait targetBait;
    private bool isHunting = false;
    private bool hasEaten = false;
    [Header("Bait Detection")]
    [SerializeField] private float huntTimeout = 5f;
    private float huntTimer = 0f;
    #endregion

    private RectTransform rectTransform;
    private string fishName;

    #region Movement States
    private enum FishState { Moving, Waiting, Stuck }
    private FishState currentState = FishState.Moving;
    private float stateTimer;

    private Quaternion targetRotation = Quaternion.identity;
    private bool isFlipped = false;
    #endregion

    #region Public Properties
    public FishType FishTypeCurrent => this.fishType;
    public string FishName => this.fishName;
    public float DefaultPrice => this.defaultPrice;
    public float DefaultWeight => this.defaultWeight;
    public float FishCurWeight => this.fishCurWeight;
    public float FishMaxWeight => this.fishMaxWeight;
    public bool IsHooked => this.hasEaten && this.IsBaitAvailable(this.targetBait) && this.targetBait.ClaimedByFish == this;
    #endregion

    #endregion

    #region Init Fish
    public void InitFish()
    {
        this.fishType = FishType.Barsch;
        ScriptableFish scriptableFish = Resources.Load<ScriptableFish>("Scriptables/Barsch");
        if (scriptableFish == null)
        {
            Debug.LogError($"[ClassFish.InitFish] Scriptables/Barsch konnte nicht geladen werden für {this.gameObject.name}");
            return;
        }

        this.InitFish(scriptableFish);
    }

    public void InitFish(ScriptableFish scriptableFish, float x_pos = 0, float y_pos = 0)
    {
        if (scriptableFish == null)
        {
            Debug.LogError($"[ClassFish.InitFish] ScriptableFish ist null für {this.gameObject.name}");
            return;
        }

        this.fishType = scriptableFish.FishTypeCurrent;
        this.fishName = !string.IsNullOrEmpty(scriptableFish.FishName) ? scriptableFish.FishName : this.gameObject.name;
        this.speed = scriptableFish.Speed;
        this.boostedSpeed = scriptableFish.BoostedSpeed;
        this.defaultPrice = scriptableFish.DefaultPrice;
        this.defaultWeight = scriptableFish.DefaultWeight;
        this.fishMaxWeight = this.defaultWeight * MaxWeightFactor;
        this.fishCurWeight = Random.Range(this.defaultWeight * MinWeightFactor, this.fishMaxWeight);
        this.roamRadius = scriptableFish.RoamRadius;
        this.huntRadius = scriptableFish.HuntRadius;

        this.rectTransform = this.GetComponent<RectTransform>();
        if (this.rectTransform != null)
        {
            this.rectTransform.anchoredPosition = new Vector2(x_pos, y_pos);
        }

        this.roamCenter = new Vector2(x_pos, y_pos);
        this.targetRotation = this.transform.rotation;

        Debug.Log($"[ClassFish.InitFish] {this.fishName} initialisiert als {this.fishType} an Position ({this.roamCenter.x:F1}, {this.roamCenter.y:F1}), Gewicht: {this.fishCurWeight:F2}/{this.fishMaxWeight:F2}");
        this.GenerateNewTarget();
    }
    #endregion

    #region Unity Methods
    private void Start()
    {
        if (string.IsNullOrEmpty(this.fishName))
        {
            this.fishName = this.gameObject.name;
        }

        if (this.rectTransform == null)
        {
            this.rectTransform = this.GetComponent<RectTransform>();
            Debug.Log($"[ClassFish.Start] {this.fishName} RectTransform wurde in Start initialisiert");
        }
    }

    private void Update()
    {
        if (this.rectTransform == null)
        {
            this.rectTransform = this.GetComponent<RectTransform>();
            if (this.rectTransform == null)
            {
                return;
            }
        }

        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, this.targetRotation, this.rotationSpeed * Time.deltaTime);

        this.CheckForBait();

        // Jagd-Timer abziehen
        if (this.isHunting && !this.hasEaten)
        {
            this.huntTimer -= Time.deltaTime;
            if (this.huntTimer <= 0f)
            {
                Debug.Log($"[ClassFish.Update] {this.fishName} gibt Jagd auf Bait auf (Timeout nach {this.huntTimeout}s)");
                this.ResetToRoam();
            }
        }

        switch (this.currentState)
        {
            case FishState.Moving:
                this.UpdateMoving();
                break;
            case FishState.Waiting:
                this.UpdateWaiting();
                break;
            case FishState.Stuck:
                this.UpdateStuck();
                break;
        }
    }

    private void OnDestroy()
    {
        if (this.targetBait != null && !this.targetBait.IsDestroyed())
        {
            this.targetBait.ReleaseClaim(this);
        }
    }
    #endregion

    #region Public Methods
    public void ReleaseFromHook()
    {
        if (this.targetBait != null && !this.targetBait.IsDestroyed())
        {
            this.targetBait.ReleaseClaim(this);
        }

        this.ResetToRoam();
        Debug.Log($"[ClassFish.ReleaseFromHook] {this.fishName} vom Haken gelöst - zurück zum Roamen");
    }

    public void CompleteCatch(bool destroyGameObject = true)
    {
        if (this.targetBait != null && !this.targetBait.IsDestroyed())
        {
            this.targetBait.ReleaseClaim(this);
        }

        this.hasEaten = false;
        this.isHunting = false;
        this.targetBait = null;
        this.currentState = FishState.Moving;

        this.SellFish();

        Debug.Log($"[ClassFish.CompleteCatch] {this.fishName} ({this.fishType}) wurde gefangen");

        if (destroyGameObject && this.gameObject != null)
        {
            Destroy(this.gameObject);
        }
        else if (!destroyGameObject)
        {
            this.GenerateNewTarget();
        }
    }
    #endregion

    #region Economy
    private void SellFish()
    {
        float price = this.fishMaxWeight > 0f
            ? (this.fishCurWeight / this.fishMaxWeight) * this.defaultPrice
            : this.defaultPrice;

        Globals.cash += price;
        Globals.totalCashEarned += price;

        Debug.Log($"[ClassFish.SellFish] {this.fishName} verkauft für {price:F2} (Gewicht: {this.fishCurWeight:F2}/{this.fishMaxWeight:F2}) -> Cash: {Globals.cash:F2}");
    }
    #endregion

    #region Bait Detection
    private Vector2 GetBaitPositionInFishSpace(ClassBait bait)
    {
        if (bait == null || bait.IsDestroyed())
        {
            return this.rectTransform != null ? this.rectTransform.anchoredPosition : Vector2.zero;
        }

        Vector2 baitWorldPos = bait.GetWorldPosition();
        if (this.rectTransform != null && this.rectTransform.parent != null)
        {
            Vector2 deltaInParent = (Vector2)this.rectTransform.parent.InverseTransformVector(baitWorldPos - (Vector2)this.transform.position);
            return this.rectTransform.anchoredPosition + deltaInParent;
        }

        return baitWorldPos;
    }

    private bool IsBaitAvailable(ClassBait bait)
    {
        return bait != null
            && !bait.IsDestroyed()
            && bait.gameObject.activeInHierarchy
            && (!bait.IsOccupied || bait.ClaimedByFish == this);
    }

    private void CheckForBait()
    {
        if (this.rectTransform == null)
        {
            return;
        }

        // Wenn bereits gegessen, prüfen ob der eigene Claim noch gültig ist
        if (this.hasEaten)
        {
            if (this.IsBaitAvailable(this.targetBait) && this.targetBait.ClaimedByFish == this)
            {
                this.isHunting = true;
                this.currentState = FishState.Stuck;
                return;
            }

            Debug.Log($"[ClassFish.CheckForBait] {this.fishName} Bait ist weg oder Claim verloren - zurück zum Roamen");
            this.ResetToRoam();
            return;
        }

        // Wenn das aktuell gejagte Bait zerstört oder bereits von einem anderen Fisch besetzt wurde, sofort aufgeben
        if (!this.hasEaten && this.targetBait != null && !this.IsBaitAvailable(this.targetBait))
        {
            Debug.Log($"[ClassFish.CheckForBait] {this.fishName} gibt aktuelles Ziel auf - Bait zerstört oder bereits besetzt");
            this.ResetToRoam();
        }

        ClassBait[] allBaits = UnityEngine.Object.FindObjectsByType<ClassBait>();
        ClassBait closestBait = null;
        float closestDistance = this.huntRadius;

        Vector2 fishPos = this.rectTransform.anchoredPosition;

        foreach (ClassBait bait in allBaits)
        {
            if (!this.IsBaitAvailable(bait))
            {
                continue;
            }

            float distance = Vector2.Distance(fishPos, this.GetBaitPositionInFishSpace(bait));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBait = bait;
            }
        }

        if (closestBait != null && !this.isHunting)
        {
            // Neu anvisierter Bait - Timer starten
            this.targetBait = closestBait;
            this.isHunting = true;
            this.huntTimer = this.huntTimeout;
            Debug.Log($"[ClassFish.CheckForBait] {this.fishName} startet Jagd auf {this.targetBait.gameObject.name} (Distanz: {closestDistance:F1})");
        }
        else if (closestBait != null && this.isHunting && closestBait != this.targetBait)
        {
            // Anderer Bait näher - Ziel wechseln
            Debug.Log($"[ClassFish.CheckForBait] {this.fishName} wechselt Jagdziel zu {closestBait.gameObject.name} (Distanz: {closestDistance:F1})");
            this.targetBait = closestBait;
            this.huntTimer = this.huntTimeout;
        }
        else if (this.isHunting && !this.hasEaten)
        {
            if (closestBait == null)
            {
                // Kein Bait mehr in Reichweite
                Debug.Log($"[ClassFish.CheckForBait] {this.fishName} kein Bait mehr in Reichweite - Jagd aufgegeben");
                this.ResetToRoam();
            }
        }
    }
    #endregion

    #region Moving and Waiting Logic
    private void UpdateMoving()
    {
        if (this.rectTransform == null)
        {
            return;
        }

        if (this.isHunting)
        {
            if (!this.IsBaitAvailable(this.targetBait))
            {
                Debug.Log($"[ClassFish.UpdateMoving] {this.fishName} gibt Jagd auf - Bait zerstört oder bereits besetzt");
                this.ResetToRoam();
            }
            else
            {
                this.targetPosition = this.GetBaitPositionInFishSpace(this.targetBait);
            }
        }

        Vector2 currentPos = this.rectTransform.anchoredPosition;
        Vector2 toTarget = this.targetPosition - currentPos;
        float distanceToTarget = toTarget.magnitude;
        Vector2 directionToTarget = distanceToTarget > 0.0001f ? toTarget / distanceToTarget : Vector2.zero;

        float currentSpeed = this.isHunting ? this.boostedSpeed : this.speed;

        if (distanceToTarget > this.stopDistance)
        {
            Vector2 newPos = Vector2.MoveTowards(currentPos, this.targetPosition, currentSpeed * Time.deltaTime);
            this.rectTransform.anchoredPosition = newPos;
            this.UpdateSpriteDirection(directionToTarget);
        }
        else
        {
            if (this.isHunting && this.targetBait != null)
            {
                if (this.targetBait.TryClaim(this))
                {
                    // Fisch hat das Bait als Erster erreicht und geclaimed
                    Debug.Log($"[ClassFish.UpdateMoving] {this.fishName} hat Bait geclaimed und hängt am Haken");
                    this.hasEaten = true;
                    this.currentState = FishState.Stuck;
                    Vector2 baitWorldPos = this.targetBait.GetWorldPosition();
                    this.transform.position = new Vector3(baitWorldPos.x, baitWorldPos.y, this.transform.position.z);
                }
                else
                {
                    // Ein anderer Fisch war schneller
                    Debug.Log($"[ClassFish.UpdateMoving] {this.fishName} konnte Bait nicht claimen - ein anderer Fisch war schneller");
                    this.ResetToRoam();
                }
            }
            else if (!this.isHunting)
            {
                this.rectTransform.anchoredPosition = this.targetPosition;
                Debug.Log($"[ClassFish.UpdateMoving] {this.fishName} erreicht Zielposition ({this.targetPosition.x:F1}, {this.targetPosition.y:F1}) - wartet");
                this.currentState = FishState.Waiting;
                this.stateTimer = Random.Range(1f, 3f);
            }
        }
    }

    private void UpdateWaiting()
    {
        if (this.isHunting)
        {
            this.currentState = FishState.Moving;
            return;
        }

        this.stateTimer -= Time.deltaTime;
        if (this.stateTimer <= 0f)
        {
            Debug.Log($"[ClassFish.UpdateWaiting] {this.fishName} hat gewartet - neues Ziel wird generiert");
            this.GenerateNewTarget();
            this.currentState = FishState.Moving;
        }
    }

    private void UpdateStuck()
    {
        // Fisch bleibt am Bait-Ort stecken
        if (this.IsBaitAvailable(this.targetBait))
        {
            Vector2 baitWorldPos = this.targetBait.GetWorldPosition();
            this.transform.position = new Vector3(baitWorldPos.x, baitWorldPos.y, this.transform.position.z);
        }
        else
        {
            // Bait despawned oder Claim verloren - zurück zum Roamen
            Debug.Log($"[ClassFish.UpdateStuck] {this.fishName} Bait despawned oder ungültig - zurück zum Roamen");
            this.ResetToRoam();
        }
    }

    private void ResetToRoam()
    {
        this.hasEaten = false;
        this.isHunting = false;
        this.targetBait = null;
        this.GenerateNewTarget();
        this.currentState = FishState.Moving;
    }

    private void GenerateNewTarget()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(0f, this.roamRadius);

        this.targetPosition = this.roamCenter + new Vector2(
            Mathf.Cos(randomAngle) * randomDistance,
            Mathf.Sin(randomAngle) * randomDistance
        );
        Debug.Log($"[ClassFish.GenerateNewTarget] {this.fishName} generiert neues Ziel: ({this.targetPosition.x:F1}, {this.targetPosition.y:F1})");
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        bool shouldFlip = direction.x < 0f;

        if (shouldFlip != this.isFlipped)
        {
            Vector3 scale = this.transform.localScale;
            scale.x = shouldFlip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            this.transform.localScale = scale;
            this.isFlipped = shouldFlip;
            Debug.Log($"[ClassFish.UpdateSpriteDirection] {this.fishName} wurde {(shouldFlip ? "links" : "rechts")} gespiegelt");
        }

        float rawAngle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
        float clampedAngle = Mathf.Clamp(rawAngle, -this.maxVerticalAngle, this.maxVerticalAngle);

        float targetAngle = shouldFlip ? -clampedAngle : clampedAngle;
        this.targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
    }
    #endregion
}
