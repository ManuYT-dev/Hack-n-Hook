using Assets.Scripts.FishMovement;
using UnityEngine;
using UnityEngine.UI;

public class ClassFish : MonoBehaviour
{
    #region Properties
    #region Positions
    private float roamRadius = 120f;
    private Vector2 roamCenter;
    private Vector2 targetPosition;
    private Vector3 currentDirection;
    #endregion

    #region Fish Properties
    private FishType fishType;
    private float speed = 20f;
    private float stopDistance = 0.5f;
    private float rotationSpeed = 4f;
    private float maxVerticalAngle = 30f;
    #endregion

    private Image image;
    private RectTransform rectTransform;

    #region Movement States
    private enum FishState { Moving, Waiting }
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
        Vector2 directionToTarget = (this.targetPosition - this.rectTransform.anchoredPosition).normalized;
        float distanceToTarget = Vector2.Distance(this.rectTransform.anchoredPosition, this.targetPosition);

        if (distanceToTarget > this.stopDistance)
        {
            this.rectTransform.anchoredPosition += directionToTarget * this.speed * Time.deltaTime;
            this.UpdateSpriteDirection(directionToTarget);
        }
        else
        {
            this.rectTransform.anchoredPosition = this.targetPosition;
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

        this.targetPosition = this.roamCenter + new Vector2(
            Mathf.Cos(randomAngle) * randomDistance,
            Mathf.Sin(randomAngle) * randomDistance
        );
    }

    private void UpdateSpriteDirection(Vector3 direction)
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
