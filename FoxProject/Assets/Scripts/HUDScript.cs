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

    // Start is called before the first frame update
    void Start()
    {
        playerScript = GameObject.Find("Player").GetComponent<PlayerBrain>();
        fillBar.fillAmount = 0;
    }

    // Fixed update bc why do it a shit ton
    void FixedUpdate()
    {
        fillBar.fillAmount = 1 - playerScript.currentResetTime;
        currentShields.text = playerScript.shieldsOut.ToString();
        maxShields.text = (playerScript.shieldsList.Count - 1).ToString();
    }
}
