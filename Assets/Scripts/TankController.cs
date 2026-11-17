using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour{
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float turnSpeed = 120f;

    [SerializeField] private Animator animator;

    // Set by FirstPersonLook while first-person is on: WASD then walks relative to this view instead of tank-turning.
    [System.NonSerialized] public Transform lookBasis;

    private Rigidbody rb;
    private float turnInput;
    private float strafeInput;
    private float moveInput;
    private bool runHeld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if(animator==null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        ReadInput();
        UpdateAnimator();
    }

    private void ReadInput()
    {
        var kb = Keyboard.current;
        if(kb==null) return;

        turnInput = (kb.dKey.isPressed ?1f:0f)-(kb.aKey.isPressed ? 1f:0f);
        moveInput = (kb.wKey.isPressed ? 1f:0f)-(kb.sKey.isPressed ? 1f:0f);
        // first-person: A/D strafe, and the mouse does the turning
        strafeInput = lookBasis != null ? turnInput : 0f;
        if(lookBasis != null) turnInput = 0f;
        // sprint only while moving forward, and only while there's stamina (GameManager owns it)
        bool wantsSprint = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) && moveInput > 0f;
        runHeld = GameManager.Instance != null
            ? GameManager.Instance.TickSprint(wantsSprint, Time.deltaTime)
            : wantsSprint;
    }

    private void FixedUpdate()
    {
        if(lookBasis != null) { MoveLookRelative(); return; }

        Quaternion rotation = rb.rotation*Quaternion.Euler(0f, turnInput * turnSpeed* Time.fixedDeltaTime,0f);
        rb.MoveRotation(rotation);

        float speed = moveInput > 0f ? (runHeld ? runSpeed:walkSpeed) : walkSpeed * 0.6f;
        Vector3 velocity = rotation*Vector3.forward*(moveInput*speed);
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    // Rotation is left to FirstPersonLook; this only walks along the view's flattened forward/right.
    private void MoveLookRelative()
    {
        Vector3 forward = Vector3.ProjectOnPlane(lookBasis.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(lookBasis.right, Vector3.up).normalized;
        Vector3 dir = Vector3.ClampMagnitude(forward*moveInput + right*strafeInput, 1f);

        float speed = moveInput < 0f ? walkSpeed * 0.6f : (runHeld ? runSpeed:walkSpeed);
        Vector3 velocity = dir*speed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void UpdateAnimator()
    {
        if(animator==null)return;

        Vector3 flat = rb.linearVelocity;
        flat.y=0f;
        animator.SetFloat("Speed", flat.magnitude, 0.1f, Time.deltaTime);
    }
}
