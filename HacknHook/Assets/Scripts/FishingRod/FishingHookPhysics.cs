using System;
using UnityEngine;

public enum CastSamplingResult
{
    None,
    StartedSampling,
    ReadyToCast
}

[Serializable]
public class FishingHookPhysics
{
    #region Constants
    private const float CastAngleRadians = 45f * Mathf.Deg2Rad;
    #endregion

    #region Inspector Fields
    [Header("Throw / Cast Speed Sampling")]
    [SerializeField] private float minThrowSpeed = 0.25f;
    [SerializeField] private float maxThrowSpeed = 1.0f;
    [SerializeField] private float castSampleDuration = 0.15f;
    [SerializeField] private bool onlyPositiveCastSpeed = true;
    [SerializeField] private float baseCastDistance = 240f;
    [SerializeField] private float airGravity = 600f;

    [Header("Water & Reeling")]
    [SerializeField] private Transform waterSurfaceTransform;
    [SerializeField] private float waterSurfaceOffsetY = 0f;
    [SerializeField] private float waterHorizontalDrag = 4f;
    [SerializeField] private float waterSinkMultiplier = 0.25f;
    [SerializeField] private float reelSpeed = 500f;
    [SerializeField] private float reelCompleteDistance = 20f;
    #endregion

    #region Private Variables
    private float lastPeakCastSpeed;
    private float lastSpeedMultiplier = 1f;
    private float peakSampleSpeed;
    private float castSampleTimer;
    private Vector2 velocity;
    private bool isInWater;
    #endregion

    #region Properties
    public float LastPeakCastSpeed => this.lastPeakCastSpeed;
    public float LastSpeedMultiplier => this.lastSpeedMultiplier;
    public Vector2 Velocity => this.velocity;
    public bool IsInWater => this.isInWater;
    #endregion

    #region Public Funcs
    public void ResetState()
    {
        this.peakSampleSpeed = 0f;
        this.castSampleTimer = 0f;
        this.velocity = Vector2.zero;
        this.isInWater = false;
    }

    public void ClearVelocity()
    {
        this.velocity = Vector2.zero;
    }

    public float GetWaterSurfaceY(Vector3 rodOrigin)
    {
        return this.waterSurfaceTransform != null
            ? this.waterSurfaceTransform.position.y
            : rodOrigin.y + this.waterSurfaceOffsetY;
    }

    public CastSamplingResult StepSampling(RodState currentState, float rawCastSpeed, float deltaTime, out float sampledPeak)
    {
        float speed = this.onlyPositiveCastSpeed ? Mathf.Max(0f, rawCastSpeed) : Mathf.Abs(rawCastSpeed);
        sampledPeak = 0f;

        if (currentState == RodState.Ready)
        {
            if (speed < this.minThrowSpeed)
            {
                return CastSamplingResult.None;
            }

            this.peakSampleSpeed = speed;
            this.castSampleTimer = this.castSampleDuration;
            if (this.castSampleTimer <= 0f)
            {
                sampledPeak = this.peakSampleSpeed;
                return CastSamplingResult.ReadyToCast;
            }

            return CastSamplingResult.StartedSampling;
        }

        if (currentState == RodState.SamplingCast)
        {
            this.peakSampleSpeed = Mathf.Max(this.peakSampleSpeed, speed);
            this.castSampleTimer -= deltaTime;
            if (this.castSampleTimer <= 0f)
            {
                sampledPeak = this.peakSampleSpeed;
                return CastSamplingResult.ReadyToCast;
            }
        }

        return CastSamplingResult.None;
    }

    public void LaunchCast(Transform hookTransform, Vector3 rodOrigin, float peakSpeed, int facingDirection)
    {
        float t = Mathf.InverseLerp(this.minThrowSpeed, this.maxThrowSpeed, peakSpeed);
        float speedMultiplier = Mathf.Clamp(Mathf.Lerp(0.5f, 1.5f, t), 0.5f, 1.5f);
        float castDistance = Mathf.Max(0f, this.baseCastDistance * speedMultiplier);

        this.lastPeakCastSpeed = peakSpeed;
        this.lastSpeedMultiplier = speedMultiplier;
        this.peakSampleSpeed = 0f;
        this.castSampleTimer = 0f;

        if (hookTransform != null)
        {
            hookTransform.position = rodOrigin;
        }

        float gravity = Mathf.Max(1f, this.airGravity);
        float launchSpeed = Mathf.Sqrt(castDistance * gravity);

        float dirX = facingDirection < 0 ? -1f : 1f;
        this.velocity = new Vector2(
            Mathf.Cos(CastAngleRadians) * dirX * launchSpeed,
            Mathf.Sin(CastAngleRadians) * launchSpeed
        );
        this.isInWater = false;
    }

    public void StepAirborneAndWater(
        Transform hookTransform,
        Vector3 rodOrigin,
        float deltaTime,
        float baitFallSpeed,
        float allowedMaxDepth,
        Action onEnteredWater)
    {
        if (hookTransform == null)
        {
            return;
        }

        float waterY = this.GetWaterSurfaceY(rodOrigin);
        Vector3 pos = hookTransform.position;

        if (!this.isInWater)
        {
            float gravity = Mathf.Max(1f, this.airGravity);
            float y0 = pos.y;
            float vy0 = this.velocity.y;
            Vector2 prevVel = this.velocity;
            this.velocity.y -= gravity * deltaTime;
            Vector2 avgVel = (prevVel + this.velocity) * 0.5f;

            pos.x += avgVel.x * deltaTime;
            pos.y += avgVel.y * deltaTime;

            if (this.velocity.y <= 0f && pos.y <= waterY)
            {
                float discriminant = Mathf.Max(0f, (vy0 * vy0) - (2f * gravity * (waterY - y0)));
                float tAir = Mathf.Clamp((vy0 + Mathf.Sqrt(discriminant)) / gravity, 0f, deltaTime);
                float tWater = deltaTime - tAir;

                pos.x = hookTransform.position.x + (prevVel.x * tAir);
                pos.y = waterY;
                this.velocity.y = 0f;
                this.isInWater = true;
                hookTransform.position = pos;
                onEnteredWater?.Invoke();

                if (tWater > 0f)
                {
                    this.StepWaterPhase(hookTransform, waterY, tWater, baitFallSpeed, allowedMaxDepth);
                }

                return;
            }

            hookTransform.position = pos;
            return;
        }

        this.StepWaterPhase(hookTransform, waterY, deltaTime, baitFallSpeed, allowedMaxDepth);
    }

    private void StepWaterPhase(Transform hookTransform, float waterY, float deltaTime, float baitFallSpeed, float allowedMaxDepth)
    {
        Vector3 pos = hookTransform.position;

        float drag = Mathf.Max(0f, this.waterHorizontalDrag);
        this.velocity.x *= Mathf.Exp(-drag * deltaTime);
        if (Mathf.Abs(this.velocity.x) < 0.01f)
        {
            this.velocity.x = 0f;
        }

        pos.x += this.velocity.x * deltaTime;

        float sinkSpeed = Mathf.Max(0f, baitFallSpeed) * Mathf.Max(0f, this.waterSinkMultiplier);
        float minY = waterY - Mathf.Max(0f, allowedMaxDepth);
        if (sinkSpeed > 0f && pos.y > minY)
        {
            pos.y = Mathf.Max(minY, pos.y - sinkSpeed * deltaTime);
        }

        hookTransform.position = pos;
    }

    public bool StepReeling(Transform hookTransform, Vector3 rodOrigin, float deltaTime, ClassBait activeBait, ClassFish hookedFish)
    {
        if (hookTransform == null)
        {
            return true;
        }

        float baitWeight = activeBait != null ? Mathf.Max(0.25f, activeBait.WeightMultiplier) : 1f;
        float fishWeightFactor = 1f;
        if (hookedFish != null && hookedFish.FishCurWeight > 0f && hookedFish.DefaultWeight > 0f)
        {
            fishWeightFactor = Mathf.Clamp(hookedFish.FishCurWeight / hookedFish.DefaultWeight, 0.5f, 2f);
        }

        float effectiveReelSpeed = Mathf.Max(0f, this.reelSpeed) / (baitWeight * fishWeightFactor);
        Vector3 currentPos = hookTransform.position;
        Vector3 targetPos = new Vector3(rodOrigin.x, rodOrigin.y, currentPos.z);

        hookTransform.position = Vector3.MoveTowards(currentPos, targetPos, effectiveReelSpeed * deltaTime);
        return Vector2.Distance(hookTransform.position, rodOrigin) <= this.reelCompleteDistance;
    }
    #endregion
}
