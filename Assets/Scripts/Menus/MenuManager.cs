using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuManager : MonoBehaviour
{
    [Header("Menus")]
    public GameObject mainMenuPanel;
    public GameObject optionsMenuPanel;
    
    // ----------------------- main menu -----------------------
    public void StartGame()
    {
        Debug.Log("Starting game...");
        SceneManager.LoadScene("GameScene");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
    
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsMenuPanel.SetActive(true);
    }
    
    // ----------------------- settings menu -----------------------
    public void CloseOptions()
    {
        optionsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}