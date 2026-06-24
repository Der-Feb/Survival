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
    public float baseSpeed = 6f;

    [Header("Physics Settings")]
    public float gravity = -9.81f * 2f;
    public float jumpHeight = 3f;
    public float groundDistance = 0.4f;

    // Movement Lock Flag for Automated Sequences
    [HideInInspector] public bool isLocked = false;

    private Vector3 verticalVelocity;
    private bool isGrounded;

    private float inputX, inputZ;
    private float visualAnimSpeed = 1.0f;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        // 1. Ground Check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        // 2. Horizontal Movement (Bypassed if locked)
        if (!isLocked)
        {
            inputX = InputManager.Instance.Horizontal;
            inputZ = InputManager.Instance.Vertical;
        }
        else
        {
            inputX = 0f;
            inputZ = 0f;
        }

        Vector3 move = Vector3.zero;

        if (Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputZ) > 0.01f)
        {
            move = Accelerate();
        }
        else
        {
            if (!isLocked)
            {
                currentSpeed = 0f;
            }
            move = Vector3.zero;
        }

        // Send movement vector to controller
        controller.Move(move * Time.deltaTime);

        // 3. Update Animation States (Only manage auto-animations if not locked)
        if (!isLocked)
        {
            UpdateAnimation();
        }

        // 4. Jump Logic (Disabled when locked)
        if (!isLocked && Input.GetButtonDown("Jump") && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. Apply Gravity
        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    Vector3 Accelerate()
    {
        Vector3 inputDir = transform.right * inputX + transform.forward * inputZ;

        if (Input.GetKey(KeyCode.RightControl))
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, baseSpeed, 30f * Time.deltaTime);
        }

        UpdateAnimation();
        return inputDir.normalized * currentSpeed;
    }

    public void UpdateAnimation()
    {
        if (animator == null) return;

        if ((Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputZ) > 0.1f || isLocked) && currentSpeed > 0.1f)
        {
            animator.SetBool("isMoving", true);
            animator.SetBool("isStopped", false);

            float targetAnimSpeed = Mathf.Clamp(currentSpeed / maxSpeed, 0.5f, 1.2f);
            visualAnimSpeed = Mathf.MoveTowards(visualAnimSpeed, targetAnimSpeed, 2f * Time.deltaTime);
            animator.speed = visualAnimSpeed;
        }
        else
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isStopped", true);

            visualAnimSpeed = Mathf.MoveTowards(visualAnimSpeed, 1.0f, 4f * Time.deltaTime);
            animator.speed = visualAnimSpeed;
        }
    }
}