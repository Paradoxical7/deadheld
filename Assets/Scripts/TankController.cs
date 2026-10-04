using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour{
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float turnSpeed = 120f;

    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private float turnInput;
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
        runHeld = kb.leftShiftKey.isPressed;
    }

    private void FixedUpdate()
    {
        Quaternion rotation = rb.rotation*Quaternion.Euler(0f, turnInput * turnSpeed* Time.fixedDeltaTime,0f);
        rb.MoveRotation(rotation);

        float speed = moveInput > 0f ? (runHeld ? runSpeed:walkSpeed) : walkSpeed * 0.6f;
        Vector3 velocity = rotation*Vector3.forward*(moveInput*speed);
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
