using UnityEngine;

public class AI_Movement : MonoBehaviour
{
    Animator animator;

    public float moveSpeed = 0.2f;

    float walkTime;
    public float walkCounter;
    float waitTime;
    public float waitCounter;

    int walkDirection;

    public bool isWalking;

    Vector3 stopPosition;

    void Start()
    {
        animator = GetComponent<Animator>();

        //So that all the prefabs don't move/stop at the same time
        walkTime = Random.Range(3,6);
        waitTime = Random.Range(5,7);

        waitCounter = waitTime;
        walkCounter = walkTime;
 
        ChooseDirection();
    }

    public void ChooseDirection()
    {
        walkDirection = Random.Range(0, 4);

        isWalking = true;
        walkCounter = walkTime;
    }

    void Update()
    {
        animator.SetBool("isRunning", true);
        walkCounter -= Time.deltaTime;

        // avoiding using switch cases
        float[] directions = {0f, 90f, -90f, 180f};

        transform.localRotation = Quaternion.Euler(0f, directions[walkDirection], 0f);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        if(walkCounter >= 0)
        {
            stopPosition = transform.position;
            isWalking = false;

            // stop walking
            transform.position = stopPosition;
            animator.SetBool("isRunning", false);

            //reset the waitCounter
            waitCounter = waitTime;
        }
        else
        {
            walkCounter -= Time.deltaTime;
            if (waitCounter <= 0) ChooseDirection();
        }
    }

}
