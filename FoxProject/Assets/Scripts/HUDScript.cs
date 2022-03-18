using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    // Start is called before the first frame update
    void Start()
    {
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
        fillBar.fillAmount = 1 - playerScript.currentResetTime;

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
}
