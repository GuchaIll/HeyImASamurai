using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour
{
    public enum MoveReferenceMode
    {
        World,
        StableReference
    }

    [Header("Config")]
    [SerializeField] private MovementConfig movementConfig;

    [Header("Direction Reference")]
    [SerializeField] private MoveReferenceMode referenceMode = MoveReferenceMode.World;
    [Tooltip("Use an empty GameObject rotated to the desired heading. Avoid using a live camera transform if you want stable controls.")]
    [SerializeField] private Transform stableReference;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Rigidbody")]
    [SerializeField] private bool freezeRotationX = true;
    [SerializeField] private bool freezeRotationZ = true;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool runHeld;

    public MovementConfig Config => movementConfig;
    public Rigidbody Rigidbody => rb;
    public bool MovementEnabled { get; set; } = true;
    public Vector2 CurrentMoveInput => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        var constraints = rb.constraints;
        if (freezeRotationX) constraints |= RigidbodyConstraints.FreezeRotationX;
        if (freezeRotationZ) constraints |= RigidbodyConstraints.FreezeRotationZ;
        rb.constraints = constraints;
    }

    public void SetInput(Vector2 input, bool isRunning)
    {
        moveInput = input;
        runHeld = isRunning;
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void SetRunHeld(bool isRunning)
    {
        runHeld = isRunning;
    }

    public void TickMotor(float deltaTime)
    {
        if (!MovementEnabled || rb == null || movementConfig == null)
            return;

        Vector3 moveDir = GetMoveDirection(moveInput);
        float targetSpeed = runHeld ? movementConfig.runSpeed : movementConfig.walkSpeed;
        Vector3 targetPlanarVelocity = moveDir * targetSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        bool grounded = IsGrounded();

        float speedRatio = movementConfig.maxPlanarSpeed > 0f
            ? Mathf.Clamp01(planarVelocity.magnitude / movementConfig.maxPlanarSpeed)
            : 0f;

        float moveStep = GetMoveStep(planarVelocity, targetPlanarVelocity, grounded, speedRatio, deltaTime);
        Vector3 smoothedPlanar = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, moveStep);

        if (smoothedPlanar.magnitude > movementConfig.maxPlanarSpeed)
        {
            smoothedPlanar = smoothedPlanar.normalized * movementConfig.maxPlanarSpeed;
        }

        rb.linearVelocity = new Vector3(smoothedPlanar.x, currentVelocity.y, smoothedPlanar.z);
    }

    public bool TryJump()
    {
        if (rb == null || movementConfig == null)
            return false;

        bool grounded = IsGrounded();
        if (!grounded)
            return false;

        rb.AddForce(Vector3.up * movementConfig.jumpForce, ForceMode.VelocityChange);
        return true;
    }

    public bool IsGrounded()
    {
        if (movementConfig == null) return false;

        Vector3 rayStart = transform.position;
        return Physics.Raycast(rayStart, Vector3.down, movementConfig.groundCheckDistance, groundLayer);
    }

    public Vector3 GetMoveDirection(Vector2 input)
    {
        if (movementConfig != null && input.magnitude < movementConfig.inputDeadzone)
            return Vector3.zero;

        if (input.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        if (referenceMode == MoveReferenceMode.World || stableReference == null)
        {
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        Vector3 forward = stableReference.forward;
        Vector3 right = stableReference.right;
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
        {
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (right * input.x + forward * input.y);
        return moveDir.sqrMagnitude > 1f ? moveDir.normalized : moveDir;
    }

    private float GetMoveStep(Vector3 planarVelocity, Vector3 targetPlanarVelocity, bool grounded, float speedRatio, float deltaTime)
    {
        bool hasInput = targetPlanarVelocity.sqrMagnitude > 0.0001f;
        float curveMultiplier;
        float baseRate;

        if (hasInput)
        {
            baseRate = grounded ? movementConfig.acceleration : movementConfig.airAcceleration;
            curveMultiplier = EvaluateCurve(movementConfig.accelerationCurve, speedRatio);

            if (planarVelocity.sqrMagnitude > 0.0001f)
            {
                float alignment = Vector3.Dot(planarVelocity.normalized, targetPlanarVelocity.normalized);
                if (alignment < 0f)
                {
                    baseRate *= movementConfig.turnBrakeMultiplier;
                }
            }
        }
        else
        {
            baseRate = grounded ? movementConfig.deceleration : movementConfig.airDeceleration;
            curveMultiplier = EvaluateCurve(movementConfig.decelerationCurve, speedRatio);
        }

        return Mathf.Max(0f, baseRate * curveMultiplier * deltaTime);
    }

    private static float EvaluateCurve(AnimationCurve curve, float t)
    {
        if (curve == null || curve.length == 0)
            return 1f;

        return Mathf.Max(0f, curve.Evaluate(Mathf.Clamp01(t)));
    }

    private void OnDrawGizmosSelected()
    {
        if (movementConfig == null) return;

        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Vector3 rayStart = transform.position;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * movementConfig.groundCheckDistance);
    }
}
