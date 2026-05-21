using UnityEngine;

public enum ControlScheme
{
    ArrowKeys, 
    WASD
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Profile Configuration")]
    public ControlScheme activeScheme = ControlScheme.ArrowKeys; 

    [Header("Global Input States")]
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsFocusing { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (activeScheme == ControlScheme.ArrowKeys)
        {
            // Movement via Arrow Keys
            Horizontal = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            Vertical = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            
            JumpPressed = Input.GetKeyDown(KeyCode.Return);     
            IsFocusing = Input.GetKey(KeyCode.RightShift);     

            // MAGIC HAPPENS HERE:
            // We tell the InputManager to check if Left Click OR Right Control is pressed.
            // This tricks your untouched PlayerMovement script into responding to Left Click automatically!
            IsSprinting = Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.RightControl); 
        }
        else if (activeScheme == ControlScheme.WASD)
        {
            Horizontal = Input.GetAxisRaw("Horizontal"); 
            Vertical = Input.GetAxisRaw("Vertical");     
            
            JumpPressed = Input.GetButtonDown("Jump");   
            IsSprinting = Input.GetKey(KeyCode.LeftShift);  
            IsFocusing = Input.GetKey(KeyCode.F);           
        }
    }
}