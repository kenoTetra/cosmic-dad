using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Linq;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public TMPro.TMP_Dropdown resolutionDropdown;
    public GameObject MainPanel;
    public GameObject SettingsPanel;
    Resolution[] resolutions;

    void Start ()
    {
        // Find all available screen sizes for the monitor used
        resolutions = Screen.resolutions;

        // Clear out the dropdown's default A, B...
        resolutionDropdown.ClearOptions();
        

        // Turn the resolutions into a string list (can't take in the resolution array)
        List<string> resolutionOptions = new List<string>();

        // Clear it out, y'know.
        resolutionOptions.Clear();

        int currentResIndex = 0;

        for(int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @" + resolutions[i].refreshRate + "Hz";
            resolutionOptions.Add(option);
            /*
            Sets the resolution in the Settings menu to the native screen size.
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
            */
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentResIndex = i;
            }
        }

        // Add the resolution string list to the dropdown.
        resolutionDropdown.AddOptions(resolutionOptions);

        // Set to current resolution of game size.
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void setResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void setVolume(float volume)
    {
        audioMixer.SetFloat("volume", volume);
    }

    public void setFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void closeSettings()
    {
        MainPanel.SetActive(true);
        SettingsPanel.SetActive(false);
    }
}
