using Assets.Scripts.FishMovement;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ClassFish : MonoBehaviour
{
    #region Properties
    #region Positions
    private float roamRadius = 100f;
    private float huntRadius = 180f;
    private Vector2 roamCenter;
    private Vector2 targetPosition;
    private Vector2 currentDirection;
    #endregion

    #region Fish Properties
    private FishType fishType;
    private float speed = 20f;
    private float boostedSpeed = 40f;
    private float stopDistance = 0.3f;
    private float rotationSpeed = 4f;
    private float maxVerticalAngle = 30f;
    #endregion

    #region Bait Detection
    private ClassBait targetBait;
    private bool isHunting = false;
    private bool hasEaten = false;
    private float huntTimeout = 5f; // Jagd-Timeout in Sekunden
    private float huntTimer = 0f;
    #endregion

    private Image image;
    private RectTransform rectTransform;

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
        this.rectTransform = this.GetComponent<RectTransform>();
        this.roamCenter = this.rectTransform.anchoredPosition;
        this.image = this.GetComponent<Image>();
        this.targetRotation = this.transform.rotation;

        this.GenerateNewTarget();
    }

    public void InitFish(FishType fishType, float x_pos, float y_pos)
    {
        this.fishType = fishType;
        this.rectTransform = this.GetComponent<RectTransform>();
        this.roamCenter = new Vector2(x_pos, y_pos);
        this.image = this.GetComponent<Image>();
        this.targetRotation = this.transform.rotation;

        this.GenerateNewTarget();
    }
    #endregion

    #region Unity Methods
    private void Start()
    {
        if (this.rectTransform == null)
        {
            this.rectTransform = this.GetComponent<RectTransform>();
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
                // Jagd aufgeben
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

        foreach (ClassBait bait in allBaits)
        {
            if (bait == null || bait.IsDestroyed()) continue;

            float distance = Vector2.Distance(this.rectTransform.anchoredPosition, bait.GetPosition());
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
        }
        else if (closestBait != null && this.isHunting && closestBait != this.targetBait)
        {
            // Anderer Bait näher - Ziel wechseln
            this.targetBait = closestBait;
            this.huntTimer = this.huntTimeout;
        }
        else if (this.isHunting && !this.hasEaten)
        {
            if (closestBait == null)
            {
                // Kein Bait mehr in Reichweite
                this.isHunting = false;
                this.targetBait = null;
                this.GenerateNewTarget();
                this.currentState = FishState.Moving;
            }
        }
        else if (this.hasEaten && (this.targetBait == null || this.targetBait.IsDestroyed()))
        {
            // Bait despawned, aber wir haben gegessen - zurück zum Roamen
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

        Vector2 directionToTarget = (this.targetPosition - this.rectTransform.anchoredPosition).normalized;
        float distanceToTarget = Vector2.Distance(this.rectTransform.anchoredPosition, this.targetPosition);

        float currentSpeed = this.isHunting ? this.boostedSpeed : this.speed;

        if (distanceToTarget > this.stopDistance)
        {
            this.rectTransform.anchoredPosition += directionToTarget * currentSpeed * Time.deltaTime;
            this.UpdateSpriteDirection(directionToTarget);
        }
        else
        {
            if (this.isHunting && this.targetBait != null)
            {
                // Fisch hat das Bait erreicht - stuck machen
                this.hasEaten = true;
                this.currentState = FishState.Stuck;
            }
            else if (!this.isHunting)
            {
                this.rectTransform.anchoredPosition = this.targetPosition;
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
            this.GenerateNewTarget();
            this.currentState = FishState.Moving;
        }
    }

    private void UpdateStuck()
    {
        // Fisch bleibt am Bait-Ort stecken
        if (this.targetBait != null && !this.targetBait.IsDestroyed())
        {
            this.rectTransform.anchoredPosition = this.targetBait.GetPosition();
        }
        else
        {
            // Bait despawned - zurück zum Roamen
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
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        if (this.image == null)
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
        }

        float rawAngle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
        float clampedAngle = Mathf.Clamp(rawAngle, -this.maxVerticalAngle, this.maxVerticalAngle);

        float targetAngle = shouldFlip ? -clampedAngle : clampedAngle;
        this.targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
    }
    #endregion
}
