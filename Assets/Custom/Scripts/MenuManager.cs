using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    [Header("Ui panels")]
    public GameObject pauseMenuCanvas;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    private bool isPaused = false;

    void Start()
    {
        
    }

    void Update()
    {
        // Detect ESC key to open/close menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseMenuCanvas.SetActive(true);
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);

        Time.timeScale = 0f; // Freeze game physics and movement
        Cursor.lockState = CursorLockMode.None; // Unlock mouse cursor
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuCanvas.SetActive(false);

        Time.timeScale = 1f; // Unfreeze game physics
        Cursor.lockState = CursorLockMode.Locked; // Relock mouse cursor to gameplay
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Always unfreeze time before reloading!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload current level
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    // --- Control Adjustments ---
    public void SetArrowKeysProfile()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ChangeControlScheme(0); // 0 = ArrowKeys enum
            // Debug.Log("Switched Controls to Arrow Keys");
        }
    }

    public void SetWASDProfile()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ChangeControlScheme(1); // 1 = WASD enum
            // Debug.Log("Switched Controls to WASD");
        }
    }
}
