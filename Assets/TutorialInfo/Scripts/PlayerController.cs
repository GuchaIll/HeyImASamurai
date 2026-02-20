using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public enum MoveReferenceMode
    {
        World,          // WASD = world axes
        StableReference // WASD relative to a fixed transform (not the live camera)
    }

    [Header("Movement")]
    [SerializeField] float walkSpeed = 4.5f;
    [SerializeField] float runSpeed = 6.5f;
    [SerializeField] float acceleration = 30f;
    [SerializeField] float jumpForce = 5f;

    [Header("Direction Reference")]
    [SerializeField] MoveReferenceMode referenceMode = MoveReferenceMode.World;

    [Header("References")]
    [SerializeField] LayerMask groundLayer;


    [Tooltip("Use an empty GameObject you rotate once to match your desired camera heading. DO NOT assign the live Cinemachine/Main Camera here.")]
    [SerializeField] Transform stableReference; // optional

    [Header("Physics")]
    [SerializeField] float maxPlanarSpeed = 10f;
    [SerializeField] float groundCheckDistance = 1.1f;

    private GAS_AbilitySystemComponent gas;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        // Make sure this is set on the Rigidbody in Inspector too
        // rb.interpolation = RigidbodyInterpolation.Interpolate;

        gas = GetComponent<GAS_AbilitySystemComponent>();
    }

    void Start()
    {
        InputManager.I.JumpPressed += OnJump;
        InputManager.I.InteractPressed += OnInteract;
        InputManager.I.DashPressed += OnDash;
    }

    void OnDisable()
    {
        if (InputManager.I != null)
        {
            InputManager.I.JumpPressed -= OnJump;
            InputManager.I.InteractPressed -= OnInteract;
            InputManager.I.DashPressed -= OnDash;
        }
    }

    void FixedUpdate()
    {
        if (InputManager.I == null) return;

        Vector2 input = InputManager.I.Move;
        bool running = InputManager.I.RunHeld;

        Vector3 moveDir = GetMoveDirection(input);

        float targetSpeed = running ? runSpeed : walkSpeed;
        Vector3 targetPlanarVelocity = moveDir * targetSpeed;

        // Preserve Y velocity (gravity)
        Vector3 currentVel = rb.linearVelocity; // use rb.velocity for broad compatibility
        Vector3 planarVel = new(currentVel.x, 0f, currentVel.z);

        Vector3 smoothedPlanar = Vector3.MoveTowards(
            planarVel,
            targetPlanarVelocity,
            acceleration * Time.fixedDeltaTime
        );

        // Safety clamp
        if (smoothedPlanar.magnitude > maxPlanarSpeed)
            smoothedPlanar = smoothedPlanar.normalized * maxPlanarSpeed;

        rb.linearVelocity = new Vector3(smoothedPlanar.x, currentVel.y, smoothedPlanar.z);

    }

    Vector3 GetMoveDirection(Vector2 input)
    {
        // no input
        if (input.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        // Option 1: World-relative (recommended for top-down city)
        if (referenceMode == MoveReferenceMode.World || stableReference == null)
        {
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        // Option 2: Stable reference (camera-feel without camera feedback loop)
        Vector3 forward = stableReference.forward;
        Vector3 right = stableReference.right;
        forward.y = 0f;
        right.y = 0f;

        // If the reference is accidentally pointing straight up/down, fallback safely
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


    bool IsGrounded()
    {
        Vector3 rayStart = transform.position;
        return Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer);
    }

    void OnJump()
    {
        Debug.LogFormat("Jump pressed! Grounded: {0}", IsGrounded());
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    void OnDash()
    {
       Debug.Log("[PlayerController] OnDash called, attempting to activate Dash ability");
       bool result = gas.TryActivateAbility("Ability.Dash");
       Debug.Log($"[PlayerController] TryActivateAbility result: {result}");
    }

    void OnDrawGizmosSelected()
    {
        // Ground check ray
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Vector3 rayStart = transform.position;

        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundCheckDistance);     
    }

    void OnInteract()
    {
        // Placeholder for interact logic
        Debug.Log("Interact pressed!");
    }

}