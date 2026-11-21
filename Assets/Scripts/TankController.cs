using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour{
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4.5f;
    // crouch walking speed; tune in the Inspector
    [SerializeField] private float crouchSpeed=1f;

    // crouched capsule height 
    [SerializeField] private float crouchHeight=1.1f;
    [SerializeField] private float turnSpeed = 120f;

    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private float turnInput;
    private float moveInput;
    private bool runHeld;
    // * true while the player is crouched; toggled by the C key
    private bool isCrouching;

    // the capsule and its standing size, saved in Awake
    private CapsuleCollider capsule;
    private float standHeight;

    private Vector3 standCenter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        if(animator==null) animator = GetComponentInChildren<Animator>();

        // remember the standing size so we can restore the collider when out of crouched state
        capsule=GetComponent<CapsuleCollider>();
        standHeight=capsule.height;
        standCenter=capsule.center;
    }

    private void Update()
    {
        ReadInput();
        UpdateAnimator();
    }

    // resize the sams capsule collider so his feet stay on the ground
    private void ApplyCrouchCollider()
    {
        float h = isCrouching ? crouchHeight : standHeight;
        capsule.height=h;
        capsule.center=new Vector3(standCenter.x, h*0.5f, standCenter.z);
    }

    // crouch any time, but only stand up if nothing is overhead
    private void ToggleCrouch()
    {
        if(isCrouching && !CanStand()) return;
        isCrouching=!isCrouching;
        ApplyCrouchCollider();
    }

    // would a standing-size capsule fit here  without touching anything?
    private bool CanStand()
    {
        float r=capsule.radius*0.95f;
        Vector3 bottom=transform.position+Vector3.up*(r+0.05f);
        Vector3 top = transform.position+Vector3.up*(standHeight-r);
        foreach(var hit in Physics.OverlapCapsule(bottom, top, r, ~0, QueryTriggerInteraction.Ignore))
            if(hit.attachedRigidbody != rb) return false;
        return true;
    }

    private void ReadInput()
    {
        var kb = Keyboard.current;
        if(kb==null) return;

        turnInput = (kb.dKey.isPressed ?1f:0f)-(kb.aKey.isPressed ? 1f:0f);
        moveInput = (kb.wKey.isPressed ? 1f:0f)-(kb.sKey.isPressed ? 1f:0f);
        // flip crouch once per key press, not every frame it's held

        // Toggle Crouch also resizes the capsule and checks for a ceiling
        if(kb.cKey.wasPressedThisFrame) ToggleCrouch();

        // sprint only while moving forward, and only while there's stamina (GameManager owns it)
        // a crouched player never asks to sprint, so no energy is spent
        bool wantsSprint = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) && moveInput > 0f && !isCrouching;
            runHeld = GameManager.Instance != null        
        ? GameManager.Instance.TickSprint(wantsSprint, Time.deltaTime)
        : wantsSprint;
    }

    private void FixedUpdate()
    {
        Quaternion rotation = rb.rotation*Quaternion.Euler(0f, turnInput * turnSpeed* Time.fixedDeltaTime,0f);
        rb.MoveRotation(rotation);

        // crouched always uses crouchSpeed (no run, no extra back walk penalty)
        float speed=isCrouching ? crouchSpeed : (moveInput > 0f ? (runHeld? runSpeed:walkSpeed) : walkSpeed * 0.6f);
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
        // tells the animator when he's moving backward
        animator.SetBool("Backward", moveInput < 0f);
        // tells the animator when simon is crouched
        animator.SetBool("Crouch", isCrouching);

        
    }
}
