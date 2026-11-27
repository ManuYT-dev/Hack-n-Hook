using Assets.Scripts.FishMovement;
using UnityEngine;
using UnityEngine.UI;

public class ClassFish : MonoBehaviour
{
    #region Properties
    #region Positions
    private float roamRadius = 120f;
    private Vector3 roamCenter;
    private Vector3 targetPosition;
    private Vector3 currentDirection;
    #endregion

    #region Fish Properties
    private FishType fishType;
    private float speed = 20f;
    private float stopDistance = 0.5f;
    private float rotationSpeed = 4f;
    #endregion

    private Image image;

    #region Movement States
    private enum FishState { Moving, Waiting }
    private FishState currentState = FishState.Moving;
    private float stateTimer;

    private Quaternion targetRotation = Quaternion.identity;
    private bool isFlipped = false;
    #endregion

    #endregion

    #region Init Fish
    private void InitFish()
    {
        this.fishType = FishType.Barsch;
        this.roamCenter = this.transform.position;
        this.image = this.GetComponent<Image>();
        this.targetRotation = this.transform.rotation;

        this.GenerateNewTarget();
    }

    private void InitFish(FishType fishType, float x_pos, float y_pos)
    {
        this.fishType = fishType;
        this.roamCenter = new Vector3(x_pos, y_pos, this.transform.position.z);
        this.image = this.GetComponent<Image>();
        this.targetRotation = this.transform.rotation;

        this.GenerateNewTarget();
    }
    #endregion

    #region Unity Methods
    private void Start()
    {
        this.InitFish();
    }

    private void Update()
    {
        // Sanfte Rotation anwenden
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, this.targetRotation, this.rotationSpeed * Time.deltaTime);

        switch (this.currentState)
        {
            case FishState.Moving:
                this.UpdateMoving();
                break;
            case FishState.Waiting:
                this.UpdateWaiting();
                break;
        }
    }
    #endregion

    #region Moving and Waiting Logic
    private void UpdateMoving()
    {
        Vector3 directionToTarget = (this.targetPosition - this.transform.position).normalized;
        float distanceToTarget = Vector3.Distance(this.transform.position, this.targetPosition);

        if (distanceToTarget > this.stopDistance)
        {
            this.transform.position += directionToTarget * this.speed * Time.deltaTime;
            this.UpdateSpriteDirection(directionToTarget);
        }
        else
        {
            this.transform.position = this.targetPosition;
            this.currentState = FishState.Waiting;
            this.stateTimer = Random.Range(1f, 3f);
        }
    }

    private void UpdateWaiting()
    {
        this.stateTimer -= Time.deltaTime;
        if (this.stateTimer <= 0f)
        {
            this.GenerateNewTarget();
            this.currentState = FishState.Moving;
        }
    }

    private void GenerateNewTarget()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(0f, this.roamRadius);

        this.targetPosition = this.roamCenter + new Vector3(
            Mathf.Cos(randomAngle) * randomDistance,
            Mathf.Sin(randomAngle) * randomDistance,
            0f
        );
    }

    private void UpdateSpriteDirection(Vector3 direction)
    {
        if (this.image == null) return;

        // Bestimme Flip basierend auf X-Richtung (nicht auf Winkel)
        bool shouldFlip = direction.x < 0f;

        // Wende Flip auf lokalScale an
        Vector3 scale = this.transform.localScale;
        if (shouldFlip != this.isFlipped)
        {
            scale.x = shouldFlip ? -1f : 1f;
            this.transform.localScale = scale;
            this.isFlipped = shouldFlip;
        }

        // Berechne Zielwinkel basierend auf Bewegungsrichtung
        // Wenn geflippt, spiegeln wir die Richtung
        Vector3 adjustedDirection = shouldFlip ? new Vector3(-direction.x, direction.y, direction.z) : direction;
        float targetAngle = Mathf.Atan2(adjustedDirection.y, adjustedDirection.x) * Mathf.Rad2Deg;

        // Setze Zielrotation
        this.targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
    }
    #endregion
}
