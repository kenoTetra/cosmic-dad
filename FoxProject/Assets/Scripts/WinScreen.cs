using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WinScreen : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 0.75f;
    public TextMeshProUGUI jumps;
    public TextMeshProUGUI walljumps;
    public TextMeshProUGUI shieldsThrown;
    public TextMeshProUGUI deaths;
    public TextMeshProUGUI timer;
    private int min;
    private int sec;
    private int msec;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Get timer variables
        min = PlayerPrefs.GetInt("min");
        sec = PlayerPrefs.GetInt("sec");
        msec = PlayerPrefs.GetInt("msec");

        // Set Stats
        timer.text = string.Format("{0:00}:{1:00}:{2:00}",min,sec,msec);
        jumps.text = (PlayerPrefs.GetInt("jumps")).ToString();
        walljumps.text = (PlayerPrefs.GetInt("walljumps")).ToString();
        shieldsThrown.text = (PlayerPrefs.GetInt("shieldsThrown")).ToString();
        deaths.text = (PlayerPrefs.GetInt("deaths")).ToString();
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
