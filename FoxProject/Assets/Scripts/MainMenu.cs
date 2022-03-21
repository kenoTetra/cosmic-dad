using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 0.75f;

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
