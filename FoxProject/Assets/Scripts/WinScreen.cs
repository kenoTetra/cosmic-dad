using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 0.75f;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void loadMainMenu()
    {
        // Loads the main menu
        StartCoroutine(loadLevel(0));
    }

    IEnumerator loadLevel(int levelIndex)
    {
        // Play the load zone animations
        transition.SetTrigger("LevelChange");

        // Wait until the animation finishes
        yield return new WaitForSeconds(transitionTime);

        // Load the other scene!
        SceneManager.LoadScene(levelIndex);
    }

    public void quitGame()
    {
        Application.Quit();
        Debug.Log("Quitting...");
    }
}
