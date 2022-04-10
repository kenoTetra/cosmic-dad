using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseScript : MonoBehaviour
{
    public GameObject MainPanel;
    public GameObject SettingsPanel;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 0.0f;
    }

    public void Close()
    {
        Time.timeScale = 1.0f;
    }

    public void openSettings()
    {
        SettingsPanel.SetActive(true);
        MainPanel.SetActive(false);
    }
}
