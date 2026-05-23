using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Ui panels")]
    public GameObject pauseMenuCanvas;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject inventoryPanel; // Added Inventory Panel slot

    private bool isPaused = false;
    private bool isInventoryOpen = false;

    void Update()
    {
        // 1. Detect I key to toggle inventory directly during gameplay
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (isInventoryOpen)
            {
                ResumeGame();
            }
            else
            {
                OpenInventoryDirectly();
            }
        }

        // 2. Detect ESC key to handle standard menu logic
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused || isInventoryOpen)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        isInventoryOpen = false;

        pauseMenuCanvas.SetActive(true);
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        inventoryPanel.SetActive(false); // Make sure inventory starts hidden

        FreezeGameplayTime(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        isInventoryOpen = false;

        pauseMenuCanvas.SetActive(false);

        FreezeGameplayTime(false);
    }

    // Direct open call via 'I' key shortcut
    public void OpenInventoryDirectly()
    {
        isPaused = false;
        isInventoryOpen = true;

        pauseMenuCanvas.SetActive(true);
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        inventoryPanel.SetActive(true); // Show inventory panel directly

        FreezeGameplayTime(true);
    }

    // Navigational button call from inside the pause menu panel
    public void SwitchToInventoryPanel()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        inventoryPanel.SetActive(true);
    }

    // Navigational back button call from inside the inventory panel
    public void CloseInventoryPanelToMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        inventoryPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        inventoryPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        inventoryPanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    // Helper function to cleanly lock/unlock gameplay states
    private void FreezeGameplayTime(bool freeze)
    {
        Time.timeScale = freeze ? 0f : 1f;
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }

    // --- Control Adjustments ---
    public void SetArrowKeysProfile()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ChangeControlScheme(0); 
        }
    }

    public void SetWASDProfile()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ChangeControlScheme(1); 
        }
    }
}