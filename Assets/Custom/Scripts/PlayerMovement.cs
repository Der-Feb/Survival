using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform groundCheck;
    public LayerMask groundMask;

    [Header("Movement Settings")]
    public float maxSpeed = 12f;
    public float acceleration = 10f;
    public float deacceleration = 15f;
    [HideInInspector] public float currentSpeed = 0f;

    [Header("Physics Settings")]
    public float gravity = -19.62f; // -9.81 * 2
    public float jumpHeight = 3f;
    public float groundDistance = 0.4f;

    private Vector3 verticalVelocity;
    private bool isGrounded;

    void Update()
    {
        // 1. Ground Check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 2. Horizontal Movement (Using our Accelerate method)
        Vector3 move = Accelerate();
        controller.Move(move * Time.deltaTime);

        // 3. Jump Logic
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Physics formula: v = sqrt(height * -2 * g)
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 4. Apply Gravity
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    // This method calculates the speed build-up and returns the movement vector
    Vector3 Accelerate()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculate direction relative to where the player is facing
        Vector3 inputDir = transform.right * x + transform.forward * z;

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

        // Return the direction multiplied by our calculated speed
        // .normalized ensures diagonal movement isn't faster than forward movement
        return inputDir.normalized * currentSpeed;
    }
}