using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    PlayerBrain playerScript;
    public Animator transition;
    public float transitionTime = 0.75f;

    void Start()
    {
        transition.ResetTrigger("LevelChange");
        playerScript = GameObject.Find("Player").GetComponent<PlayerBrain>();
    }

    void Update()
    {
        if(playerScript.loadZoneHit)
        {
            loadNextLevel();
        }
    }

    public void loadNextLevel()
    {
        // Loads the cronological next scene in the build index.
        StartCoroutine(loadLevel(SceneManager.GetActiveScene().buildIndex + 1));
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
}
