using UnityEngine;

public enum ControlScheme
{
    ArrowKeys, 
    WASD
}

public class InputManager : MonoBehaviour
{

    // making InputManager singleton
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
            IsSprinting = Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.RightControl);
        }
        else if (activeScheme == ControlScheme.WASD)
        {
            Horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            Vertical = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);    
            
            JumpPressed = Input.GetButtonDown("Jump");   
            IsSprinting = Input.GetKey(KeyCode.LeftShift);  
            IsFocusing = Input.GetKey(KeyCode.F);           
        }
    }

    public void ChangeControlScheme(int schemeIndex)
    {
        // 0 = ArrowKeys, 1 = WASD
        activeScheme = (ControlScheme)schemeIndex;
    }
}