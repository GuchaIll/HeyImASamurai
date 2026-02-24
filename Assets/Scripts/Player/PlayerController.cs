using Features.Dialogue;
using UnityEngine;
using VContainer;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerMotor))]
public class PlayerController : MonoBehaviour
{
    [Header("Ability Integration")]
    [SerializeField] private bool suppressMotorWhileDashActive = true;
    [SerializeField] private string dashAbilityTag = "Ability.Dash";

    private GAS_AbilitySystemComponent gas;
    private PlayerMotor playerMotor;
    private IPlayerInputSource injectedInput;
    private IPlayerInputSource activeInput;

    [Inject]
    public void Construct(IPlayerInputSource inputSource)
    {
        injectedInput = inputSource;
    }

    void Awake()
    {
        playerMotor ??= GetComponent<PlayerMotor>();
        gas = GetComponent<GAS_AbilitySystemComponent>();
    }

    void Start()
    {
        activeInput = injectedInput;

        if (activeInput == null)
        {
            Debug.LogWarning("[PlayerController] No injected input source found. Ensure VContainer scope registers IPlayerInputSource.", this);
            return;
        }

        activeInput.JumpPressed += OnJump;
        activeInput.InteractPressed += OnInteract;
        activeInput.DashPressed += OnDash;
    }

    void OnDisable()
    {
        if (activeInput == null) return;

        activeInput.JumpPressed -= OnJump;
        activeInput.InteractPressed -= OnInteract;
        activeInput.DashPressed -= OnDash;
    }

    void FixedUpdate()
    {
        if (playerMotor == null || activeInput == null) return;

        if (suppressMotorWhileDashActive && gas != null && !string.IsNullOrEmpty(dashAbilityTag) && gas.IsAbilityActive(dashAbilityTag))
        {
            return;
        }

        playerMotor.TickMotor(Time.fixedDeltaTime);
    }

    void Update()
    {
        if (playerMotor == null || activeInput == null) return;
        playerMotor.SetInput(activeInput.Move, activeInput.RunHeld);
    }

    void OnJump()
    {
        if (playerMotor == null) return;

        bool jumped = playerMotor.TryJump();
        Debug.LogFormat("Jump pressed! Grounded: {0}", jumped);
    }

    void OnDash()
    {
       if (gas == null) return;
       Debug.Log("[PlayerController] OnDash called, attempting to activate Dash ability");
       bool result = gas.TryActivateAbility(dashAbilityTag);
       Debug.Log($"[PlayerController] TryActivateAbility result: {result}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[PlayerController] OnTriggerEnter called");
        // Check if other tag is NPC
        if (other.CompareTag("NPC"))
        {
            Debug.Log("[PlayerController] NPC entered");
            // Get the DialogueController in its parent
            DialogueController npcDialogueController = other.gameObject.GetComponentInParent<DialogueController>();
            npcDialogueController.Show("Hi", 2.5f);
            Debug.Log("[PlayerController] Showing dialogue");
        }
    }

    void OnInteract()
    {
        // Placeholder for interact logic
        Debug.Log("Interact pressed!");
    }

}
