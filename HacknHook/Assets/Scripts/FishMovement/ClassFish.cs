using Assets.Scripts.FishMovement;
using Unity.VisualScripting;
using UnityEngine;

public class ClassFish : MonoBehaviour
{
    #region Properties

    float defaultPrice;

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
    private float stopDistance = 0.3f;
    private float rotationSpeed = 4f;
    private float maxVerticalAngle = 30f;
    #endregion

    #region Bait Detection
    private ClassBait targetBait;
    private bool isHunting = false;
    private bool hasEaten = false;
    private float huntTimeout = 5f;
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

    #endregion

    #region Init Fish
    public void InitFish()
    {
        this.fishType = FishType.Barsch;
        ScriptableFish scriptableFish = Resources.Load<ScriptableFish>("Scriptables/Barsch");
        InitFish(scriptableFish);
    }

    public void InitFish(ScriptableFish scriptableFish, float weight_multiplier = 1, float x_pos = 0, float y_pos = 0)
    {
        this.fishType = scriptableFish.FishTypeCurrent;
        this.fishName = scriptableFish.FishName;
        this.speed = scriptableFish.Speed;
        this.boostedSpeed = scriptableFish.BoostedSpeed;
        this.defaultPrice = scriptableFish.DefaultPrice;
        this.roamRadius = scriptableFish.RoamRadius;
        this.huntRadius = scriptableFish.HuntRadius;
        this.fishType = scriptableFish.FishTypeCurrent;
        
        this.rectTransform = this.GetComponent<RectTransform>();
        this.rectTransform.anchoredPosition = new Vector2(x_pos, y_pos);
        this.roamCenter = new Vector2(x_pos, y_pos);
        this.targetRotation = this.transform.rotation;
        this.fishName = this.gameObject.name;

        Debug.Log($"[ClassFish.InitFish] {this.fishName} initialisiert als {this.fishType} an Position ({this.roamCenter.x:F1}, {this.roamCenter.y:F1})");
        this.GenerateNewTarget();
    }
    #endregion

    #region Unity Methods
    private void Start()
    {
        if (this.rectTransform == null)
        {
            this.rectTransform = this.GetComponent<RectTransform>();
            Debug.Log($"[ClassFish.Start] {this.fishName} RectTransform wurde in Start initialisiert");
        }
    }

    private void Update()
    {
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, this.targetRotation, this.rotationSpeed * Time.deltaTime);

        this.CheckForBait();

        // Jagd-Timer abziehen
        if (this.isHunting && !this.hasEaten)
        {
            this.huntTimer -= Time.deltaTime;
            if (this.huntTimer <= 0f)
            {
                Debug.Log($"[ClassFish.Update] {this.fishName} gibt Jagd auf Bait auf (Timeout nach {this.huntTimeout}s)");
                this.isHunting = false;
                this.targetBait = null;
                this.GenerateNewTarget();
                this.currentState = FishState.Moving;
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
    #endregion

    #region Bait Detection
    private void CheckForBait()
    {
        // Wenn bereits gegessen, bleibe am Bait kleben
        if (this.hasEaten && this.targetBait != null && !this.targetBait.IsDestroyed())
        {
            this.isHunting = true;
            this.currentState = FishState.Stuck;
            return;
        }

        ClassBait[] allBaits = UnityEngine.Object.FindObjectsByType<ClassBait>();
        ClassBait closestBait = null;
        float closestDistance = this.huntRadius;

        Vector2 fishPos = this.rectTransform.anchoredPosition;

        foreach (ClassBait bait in allBaits)
        {
            if (bait == null || bait.IsDestroyed()) continue;

            float distance = Vector2.Distance(fishPos, bait.GetPosition());
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
                this.isHunting = false;
                this.targetBait = null;
                this.GenerateNewTarget();
                this.currentState = FishState.Moving;
            }
        }
        else if (this.hasEaten && (this.targetBait == null || this.targetBait.IsDestroyed()))
        {
            // Bait despawned, aber wir haben gegessen - zurück zum Roamen
            Debug.Log($"[ClassFish.CheckForBait] {this.fishName} Bait ist weg - zurück zum Roamen");
            this.hasEaten = false;
            this.isHunting = false;
            this.targetBait = null;
            this.currentState = FishState.Moving;
            this.GenerateNewTarget();
        }
    }
    #endregion

    #region Moving and Waiting Logic
    private void UpdateMoving()
    {
        if (this.isHunting && this.targetBait != null)
        {
            this.targetPosition = this.targetBait.GetPosition();
        }

        Vector2 currentPos = this.rectTransform.anchoredPosition;
        Vector2 directionToTarget = (this.targetPosition - currentPos).normalized;
        float distanceToTarget = Vector2.Distance(currentPos, this.targetPosition);

        float currentSpeed = this.isHunting ? this.boostedSpeed : this.speed;

        if (distanceToTarget > this.stopDistance)
        {
            Vector2 newPos = currentPos + directionToTarget * currentSpeed * Time.deltaTime;
            this.rectTransform.anchoredPosition = newPos;
            this.UpdateSpriteDirection(directionToTarget);
        }
        else
        {
            if (this.isHunting && this.targetBait != null)
            {
                // Fisch hat das Bait erreicht - stuck machen
                Debug.Log($"[ClassFish.UpdateMoving] {this.fishName} hat Bait erreicht und bleibt kleben");
                this.hasEaten = true;
                this.currentState = FishState.Stuck;
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
        if (this.targetBait != null && !this.targetBait.IsDestroyed())
        {
            Vector2 baitPos = this.targetBait.GetPosition();
            this.rectTransform.anchoredPosition = baitPos;
        }
        else
        {
            // Bait despawned - zurück zum Roamen
            Debug.Log($"[ClassFish.UpdateStuck] {this.fishName} Bait despawned - zurück zum Roamen");
            this.hasEaten = false;
            this.isHunting = false;
            this.GenerateNewTarget();
            this.currentState = FishState.Moving;
        }
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
