using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputReader : MonoBehaviour, IPlayerInputSource
{
    public Vector2 Move { get; private set; }
    public bool RunHeld { get; private set; }

    public event Action InteractPressed;
    public event Action JumpPressed;
    public event Action DashPressed;

    private PlayerInputActions actions;
    
    protected virtual void Awake()
    {
        actions = new PlayerInputActions();
    }

    protected virtual void OnEnable()
    {
        if (actions == null)
        {
            actions = new PlayerInputActions();
        }

        actions.Enable();

        actions.Player.Move.performed += OnMove;
        actions.Player.Move.canceled += OnMove;
        actions.Player.Jump.performed += OnJump;
        actions.Player.Sprint.performed += OnSprintPerformed;
        actions.Player.Sprint.canceled += OnSprintCanceled;
        actions.Player.Interact.performed += OnInteract;
        actions.Player.Dash.performed += OnDash;
    }

    protected virtual void OnDisable()
    {
        if (actions == null) return;

        actions.Player.Move.performed -= OnMove;
        actions.Player.Move.canceled -= OnMove;
        actions.Player.Jump.performed -= OnJump;
        actions.Player.Sprint.performed -= OnSprintPerformed;
        actions.Player.Sprint.canceled -= OnSprintCanceled;
        actions.Player.Interact.performed -= OnInteract;
        actions.Player.Dash.performed -= OnDash;

        actions.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Move = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        JumpPressed?.Invoke();
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        InteractPressed?.Invoke();
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        DashPressed?.Invoke();
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        RunHeld = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        RunHeld = false;
    }

    // Optional PlayerInput SendMessage compatibility.
    public void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            JumpPressed?.Invoke();
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
            InteractPressed?.Invoke();
    }

    public void OnSprint(InputValue value)
    {
        RunHeld = value.isPressed;
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed)
            DashPressed?.Invoke();
    }
}
