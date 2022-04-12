using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HUDScript : MonoBehaviour
{
    private GameObject player;
    PlayerBrain playerScript;
    public Image fillBar;
    public TextMeshProUGUI currentShields;
    public TextMeshProUGUI maxShields;
    public GameObject RKey;
    private GameObject shieldInfo;
    public GameObject MainPanel;
    public GameObject SettingsPanel;
    public bool paused;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        // Set the gameobjects you need to find for scripts...
        shieldInfo = GameObject.Find("Shield Info");
        playerScript = GameObject.Find("Player").GetComponent<PlayerBrain>();

        // And make sure the fill starts at 0
        fillBar.fillAmount = 0;
    }

    // Fixed update bc why do it a shit ton
    void FixedUpdate()
    {
        // If you have 0 max shields, then hey, you don't need a hud for shields...
        if(playerScript.shieldsList.Count - 1 == 0) 
        {
            // So remove it!
            shieldInfo.SetActive(false);
        }

        // But if you do need it
        else
        {
            // Enable that MF
            shieldInfo.SetActive(true);
        }

        // Holding R over time fills a box around the shield. Lets go!
        fillBar.fillAmount = 1 - playerScript.currentResetTime*2;

        // Show the max/current shields.
        currentShields.text = (playerScript.shieldsList.Count - playerScript.shieldsOut - 1).ToString();
        maxShields.text = (playerScript.shieldsList.Count - 1).ToString();

        // If your shields are at 0, show the R key
        if (playerScript.shieldsList.Count - 1 == playerScript.shieldsOut)
        {
            RKey.SetActive(true);
        }

        // Otherwise, remove the R key.
        else
        {
            RKey.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            if(paused) 
            {
                unpauseGame();
            }
            else
            {
                pauseGame();
            }
        }
    }

    public void pauseGame()
    {
        MainPanel.SetActive(true);
        Time.timeScale = 0.0f;
        paused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void unpauseGame()
    {
        MainPanel.SetActive(false);
        Time.timeScale = 1.0f;
        paused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void openSettings()
    {
        SettingsPanel.SetActive(true);
        MainPanel.SetActive(false);
    }

    public void gotoMainMenu()
    {
        // Loads the main menu
        SceneManager.LoadScene(0);
    }

    public void quitGame()
    {
        Application.Quit();
        Debug.Log("Quitting...");
    }
}
