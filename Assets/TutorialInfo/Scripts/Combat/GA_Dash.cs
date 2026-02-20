using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Directional dash ability that moves the character in the input direction.
/// Supports both Rigidbody and CharacterController movement.
/// </summary>
[CreateAssetMenu(menuName = "GAS/Abilities/Directional Dash")]
public class GA_Dash : GAS_Ability
{
    [Header("Dash Settings")]
    [Tooltip("Total distance covered by the dash")]
    public float dashDistance = 6f;
    
    [Tooltip("Time to complete the dash")]
    public float dashDuration = 0.15f;
    
    [Tooltip("Easing curve for dash speed (optional)")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    
    [Header("Direction")]
    [Tooltip("Use movement input direction. If false or no input, uses facing direction")]
    public bool useInputDirection = true;
    
    [Tooltip("If true, dash goes backward when no input is held")]
    public bool backwardOnNoInput = false;
    
    [Header("Physics")]
    [Tooltip("Ignore gravity during dash")]
    public bool ignoreGravity = true;
    
    [Tooltip("Preserve Y velocity after dash")]
    public bool preserveYVelocity = false;
    
    [Tooltip("Can dash in air")]
    public bool canDashInAir = true;
    
    [Header("Combat")]
    [Tooltip("Grant invincibility frames during dash")]
    public bool grantIFrames = true;
    
    [Tooltip("Tag applied during i-frames (e.g., State.Invulnerable)")]
    public GameplayTagReference iFrameTag;
    
    // Runtime state (per-activation, stored on owner via extension data)
    private Vector3 dashDirection;
    private Vector3 startPosition;
    private float elapsedTime;
    private Vector3 originalVelocity;
    
    public override float OnActivate(GAS_AbilitySystemComponent owner)
    {
        UnityEngine.Debug.Log("Activating Dash ability");
        // Determine dash direction
        dashDirection = GetDashDirection(owner);
        startPosition = owner.transform.position;
        elapsedTime = 0f;
        
        // Store original velocity
        var rb = owner.GetComponent<Rigidbody>();
        if (rb != null)
        {
            originalVelocity = rb.linearVelocity;
            
            if (ignoreGravity)
                rb.useGravity = false;
        }
        
        var cc = owner.GetComponent<CharacterController>();
        if (cc != null)
        {
            originalVelocity = cc.velocity;
        }
        
        return dashDuration;
    }
    
    public override void OnTick(GAS_AbilitySystemComponent owner, float deltaTime)
    {
        elapsedTime += deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / dashDuration);
        
        // Calculate speed from curve
        float curveValue = speedCurve.Evaluate(normalizedTime);
        float speed = (dashDistance / dashDuration) * curveValue;
        
        // Apply movement
        var rb = owner.GetComponent<Rigidbody>();
        var cc = owner.GetComponent<CharacterController>();
        
        if (rb != null)
        {
            Vector3 velocity = dashDirection * speed;
            if (!ignoreGravity)
                velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
        else if (cc != null)
        {
            Vector3 move = dashDirection * speed * deltaTime;
            if (!ignoreGravity && !cc.isGrounded)
                move.y -= 9.81f * deltaTime;
            cc.Move(move);
        }
        else
        {
            // Fallback: direct transform movement
            owner.transform.position += dashDirection * speed * deltaTime;
        }
    }
    
    public override void OnEnd(GAS_AbilitySystemComponent owner, bool wasCancelled)
    {
        var rb = owner.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (ignoreGravity)
                rb.useGravity = true;
            
            // Reset or preserve velocity
            if (preserveYVelocity)
            {
                rb.linearVelocity = new Vector3(0f, originalVelocity.y, 0f);
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
    
    private Vector3 GetDashDirection(GAS_AbilitySystemComponent owner)
    {
        Vector3 direction = Vector3.zero;
        
        if (useInputDirection)
        {
            // Try to get input from InputManager
            if (InputManager.I != null)
            {
                Vector2 input = InputManager.I.Move;
                if (input.sqrMagnitude > 0.01f)
                {
                    // Convert input to world direction based on camera
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 camForward = cam.transform.forward;
                        Vector3 camRight = cam.transform.right;
                        camForward.y = 0f;
                        camRight.y = 0f;
                        camForward.Normalize();
                        camRight.Normalize();
                        
                        direction = (camForward * input.y + camRight * input.x).normalized;
                    }
                    else
                    {
                        // No camera, use transform-relative
                        direction = (owner.transform.forward * input.y + owner.transform.right * input.x).normalized;
                    }
                }
            }
        }
        
        // Fallback to facing direction
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = backwardOnNoInput ? -owner.transform.forward : owner.transform.forward;
        }
        
        // Flatten if needed (keep horizontal dash)
        if (!canDashInAir || ignoreGravity)
        {
            direction.y = 0f;
            direction.Normalize();
        }
        
        return direction;
    }
    
    /// <summary>
    /// Get the final position the dash will reach (for prediction/UI)
    /// </summary>
    public Vector3 GetDashEndPosition(GAS_AbilitySystemComponent owner)
    {
        Vector3 dir = GetDashDirection(owner);
        return owner.transform.position + dir * dashDistance;
    }
}
