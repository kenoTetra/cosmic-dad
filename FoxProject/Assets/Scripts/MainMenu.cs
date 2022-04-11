using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 0.75f;
<<<<<<< Updated upstream
=======
    public GameObject MainPanel;
    public GameObject SettingsPanel;
    public GameObject CreditsPanel;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // reset stats when hitting the main menu.
        PlayerPrefs.SetInt("time", 0);
        PlayerPrefs.SetInt("min", 0);
        PlayerPrefs.SetInt("sec", 0);
        PlayerPrefs.SetInt("msec", 0);
        PlayerPrefs.SetInt("deaths", 0);
        PlayerPrefs.SetInt("shieldsThrown", 0);
        PlayerPrefs.SetInt("jumps", 0);
        PlayerPrefs.SetInt("walljumps", 0);
    }
>>>>>>> Stashed changes

    public void startGame()
    {
        StartCoroutine(loadLevelOne(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator loadLevelOne(int levelIndex)
    {
        // Play the load zone animations
        transition.SetTrigger("LevelChange");

        // Wait until the animation finishes
        yield return new WaitForSeconds(transitionTime);

        // Load the other scene!
        SceneManager.LoadScene(levelIndex);
    }

    public void openOptions()
    {

    }

    public void closeOptions()
    {

    }

    public void quitGame()
    {
        Application.Quit();
        Debug.Log("Quitting...");
    }
}
