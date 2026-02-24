using UnityEngine;

[CreateAssetMenu(menuName = "Player/Movement Config")]
public class MovementConfig : ScriptableObject
{
    [Header("Speed")]
    [Min(0f)] public float walkSpeed = 4.5f;
    [Min(0f)] public float runSpeed = 6.5f;
    [Min(0f)] public float maxPlanarSpeed = 10f;

    [Header("Acceleration")]
    [Min(0f)] public float acceleration = 30f;
    [Min(0f)] public float deceleration = 35f;
    [Min(0f)] public float airAcceleration = 12f;
    [Min(0f)] public float airDeceleration = 8f;
    [Min(0f)] public float turnBrakeMultiplier = 1.5f;

    [Tooltip("Multiplier curve evaluated by current speed ratio (0..1) while accelerating.")]
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Tooltip("Multiplier curve evaluated by current speed ratio (0..1) while decelerating.")]
    public AnimationCurve decelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Jump / Grounding")]
    [Min(0f)] public float jumpForce = 5f;
    [Min(0.01f)] public float groundCheckDistance = 1.1f;
    [Min(0f)] public float inputDeadzone = 0.05f;
}
