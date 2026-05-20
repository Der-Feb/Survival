using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform groundCheck;
    public LayerMask groundMask;

    public Animator animator;

    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float acceleration = 10f;
    public float deacceleration = 120f;
    [HideInInspector] public float currentSpeed = 0f;

    [Header("Physics Settings")]
    public float gravity = -19.62f; // -9.81 * 2
    public float jumpHeight = 3f;
    public float groundDistance = 0.4f;

    private Vector3 verticalVelocity;
    private bool isGrounded;

    private float inputX, inputZ;

    void Update()
    {
        // 1. Ground Check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 2. Horizontal Movement (Using our Accelerate method)
        inputX = Input.GetAxisRaw("Horizontal");
        inputZ = Input.GetAxisRaw("Vertical");

        Vector3 move = Vector3.zero;

        if(Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputZ) > 0.01f)
        {
            move = Accelerate();
        }
        else
        {
            currentSpeed = 0f;
            move = Vector3.zero;
        }

        // send the final processed vector to the controller
        controller.Move(move * Time.deltaTime);

        // 3. Update Animation States
        UpdateAnimation();

        // 4. Jump Logic (Keep your existing code here...)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. Apply Gravity (Keep your existing code here...)
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // This method calculates the speed build-up and returns the movement vector
    Vector3 Accelerate()
    {
        // Calculate direction relative to where the player is facing
        Vector3 inputDir = transform.right * inputX + transform.forward * inputZ;

        // Check if the player is actually trying to move
        if (inputDir.magnitude > 0.1f)
        {
            // v = u + at
            currentSpeed += acceleration * Time.deltaTime;
        }
        else
        {
            // Apply friction/drag when no keys are pressed
            currentSpeed -= deacceleration * Time.deltaTime;
        }

        // Clamp speed between 0 and our maximum allowed speed
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        UpdateAnimation();

        // Return the direction multiplied by our calculated speed
        // .normalized ensures diagonal movement isn't faster than forward movement
        return inputDir.normalized * currentSpeed;
    }

    private float visualAnimSpeed = 1.0f;
    
    void UpdateAnimation()
    {
        if(animator == null) return;

        if((Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputZ) > 0.1f) && currentSpeed > 0.1f)
        {
            animator.SetBool("isMoving", true);
            animator.SetBool("isStopped", false);

            /// Calculate where the animation speed WANT to be based on physical speed
            float targetAnimSpeed = Mathf.Clamp(currentSpeed / maxSpeed, 0.5f, 1.2f);

            /**
             * SMOOTH STEP: Instead of snapping, smoothly drift toward the target speed over time
             * 2f controls how fast the animation transitions. Lower = smoother/slower adaptation.
            */
            visualAnimSpeed = Mathf.MoveTowards(visualAnimSpeed, targetAnimSpeed, 2f * Time.deltaTime);
            animator.speed = visualAnimSpeed;
        }
        else
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isStopped", true);

            // Smoothly return the animation clock back to a normal 1.0 speed when idling
            visualAnimSpeed = Mathf.MoveTowards(visualAnimSpeed, 1.0f, 4f * Time.deltaTime);
            animator.speed = visualAnimSpeed;
        }
    }
}