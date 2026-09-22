using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Hierarchy.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{

    // Movement
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;

    public float dashSpeed;
    public float dashSpeedChangeFactor;

    private float disiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private MovementState lastState;
    private bool keepMomentum;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

    // Jumping
    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier; // Speed Which Movement is applied in air based off moveSpeed
    bool readyToJump = true;
    public int numAirJumps;

    public int avalableAirJumps;

    // Crouching
    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    // KeyBinds
    [Header("KeyBinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    // Grounded State
    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;
    public float groundDrag;

    // Slopes
    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    // Orientation
    public Transform orientation;

    // Vertical And Horizontal Inputs
    float horizontalInput;
    float verticalInput;

    // Movement Direction
    Vector3 moveDirection;

    // Player RigidBody
    Rigidbody rb;

    // State of Movement
    public MovementState state;

    // States
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        sliding,
        dashing,
        air
    }

    public bool sliding;

    public bool dashing;

    // Calls Once Everytime The Program Starts
    void Start()
    {
        // Gets RigidBody From Stack for later use 
        // and freezes its rotation
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        avalableAirJumps = numAirJumps;

        startYScale = transform.localScale.y;
    }

    // Called on Every Frame of Program
    private void Update()
    {
        // Detects If the Player is Grounded
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        // Adds Damping/Friction if Player is Walking, Sprinting, or Crouching
        if (state == MovementState.walking 
        || state == MovementState.sprinting 
        || state == MovementState.crouching)
        {
            rb.linearDamping = groundDrag;
            numAirJumps = avalableAirJumps;
        }
        else
        //If Player is in air
            rb.linearDamping = 0;
    }

    // Updates at a set time (0.02 secs / 50 times a sec)
    private void FixedUpdate()
    {
        MovePlayer();
        SpeedControl();
    }

    // Logs Inputs to actions (Press jumpKey -> Jump())
    private void MyInput()
    {
        // Makes Inputs for Movement
        horizontalInput = Input.GetAxisRaw("Horizontal"); // Uses W & S
        verticalInput = Input.GetAxisRaw("Vertical"); // Uses A & D

        // Make Player Jump if Key is Pressed, Jump Cooldown is off, and Player is Grounded
        if(Input.GetKey(jumpKey) && readyToJump && (grounded || numAirJumps > 0))
        {
            // Makes it so you cant jump twice
            readyToJump = false;

            Jump();

            // 
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // Scales Players Y down and moves them down when Crouch key is pressed
        if(Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // Returns Players Y Height when crouch is released
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    // Handles The Diffrent Movement States (Crouch, Sprint, ect)
    private void StateHandler()
    {
        if(sliding)
        {
            state = MovementState.sliding;

            if(OnSlope() && rb.linearVelocity.y < 0.1)
                disiredMoveSpeed = slideSpeed;

            else
                disiredMoveSpeed = sprintSpeed;
        }

        else if (dashing)
        {
            state = MovementState.dashing;
            moveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedChangeFactor;
        }

        // Sets Crouch State
        else if(Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            disiredMoveSpeed = crouchSpeed;
        }
        // Sets Sprint State
        else if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            disiredMoveSpeed = sprintSpeed;
        }
        // Sets Walk State
        else if (grounded)
        {
            state = MovementState.walking;
            disiredMoveSpeed = walkSpeed;
        }
        // Sets in air State
        else
        {
            state = MovementState.air;

            if (disiredMoveSpeed < sprintSpeed)
                disiredMoveSpeed = walkSpeed;
            else
                disiredMoveSpeed = sprintSpeed;
        }

        if(Math.Abs(disiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            moveSpeed = disiredMoveSpeed;
        }

        bool desiredMoveSpeedHasChanged = disiredMoveSpeed != lastDesiredMoveSpeed;
        if (lastState == MovementState.dashing) keepMomentum = true;

        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                moveSpeed = disiredMoveSpeed;
            }
        }

        lastDesiredMoveSpeed = disiredMoveSpeed;
        lastState = state;
        // Removes Gravity When On Slope
        rb.useGravity = !OnSlope();
    }

    private float speedChangeFactor = 2;

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float diffrerence = Mathf.Abs(disiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        float boostFactor = speedChangeFactor;

        while (time < diffrerence)
        {
            moveSpeed = Mathf.Lerp(startValue, disiredMoveSpeed, time / diffrerence);

            time += Time.deltaTime * boostFactor;
            
            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime *speedIncreaseMultiplier *slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        moveSpeed = disiredMoveSpeed;
    }
    // Moves Player Based on Input
    private void MovePlayer()
    {
        // Makes Movement Variable Direction Based on Vertical and Horizontal input
        // And Current Facing Direcion
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // 
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            //
            if(rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        // Moves RigidBody on Player in moveDirection and moveSpeed while on ground
        if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // Like Movement above, but with airMultiplier For Air Movement
        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    // Maxs Speed Control based on Move Speed
    private void SpeedControl()
    {
        // Maxes Speed on Slope 
        if (OnSlope() && !exitingSlope)
        {
            if(rb.linearVelocity.magnitude > moveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }

        // Zeros 
        else
        {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        //
        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
        }
    }

    // Make Rigid Body move up based on jumpForce
    private void Jump()
    {
        // Make it so you can jump off slopes and return Gravity
        exitingSlope = true;

        //
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        //
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

        numAirJumps--;
    }

    // Resets the jump timer when called
    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    // Detects If Player is on Slope
    public bool OnSlope()
    {
        // On A Slope
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            // Doesnt Allow Player to be on slopes more then maxSlopeAngle
            // and Throws out negitive angles
            return angle < maxSlopeAngle && angle != 0;
        }
        // Not on a slope
        return false;
    }

    // Returns Direction Moveing on Slope
    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
}