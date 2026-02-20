using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager I { get; private set; }

    public Vector2 Move { get; private set; }
    public bool RunHeld { get; private set; }

    public event Action InteractPressed;
    public event Action JumpPressed;
    public event Action DashPressed;

    private PlayerInputActions actions;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        actions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        actions.Enable();

        actions.Player.Move.performed += OnMove;
        actions.Player.Move.canceled  += OnMove;

        actions.Player.Jump.performed += OnJump;

        actions.Player.Sprint.performed += _ => RunHeld = true;
        actions.Player.Sprint.canceled  += _ => RunHeld = false;

        actions.Player.Interact.performed += OnInteract;

        actions.Player.Dash.performed += OnDash;
        
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid leaks / double subscriptions.
        actions.Player.Move.performed -= OnMove;
        actions.Player.Move.canceled  -= OnMove;
        actions.Player.Interact.performed -= OnInteract;
        actions.Player.Jump.performed -= OnJump;
        actions.Player.Sprint.performed -= _ => RunHeld = true;
        actions.Player.Sprint.canceled  -= _ => RunHeld = false;

        actions.Player.Dash.performed -= OnDash;

        actions.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        JumpPressed?.Invoke();
    }

    // SendMessage-compatible overload
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            JumpPressed?.Invoke();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Move = ctx.ReadValue<Vector2>();
    }

    // SendMessage-compatible overload (used by PlayerInput component)
    public void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        InteractPressed?.Invoke();
    }

    // SendMessage-compatible overload
    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
            InteractPressed?.Invoke();
    }

    // SendMessage-compatible overload
    public void OnSprint(InputValue value)
    {
        RunHeld = value.isPressed;
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        Debug.Log("[InputManager] OnDash callback fired");
        DashPressed?.Invoke();
    }

    // SendMessage-compatible overload
    public void OnDash(InputValue value)
    {
        if (value.isPressed)
            DashPressed?.Invoke();
    }
}