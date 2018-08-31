using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {
    public GameObject panel;
    public GameObject settingsMenu;
    public GameObject teleportMenu;
    public GameObject reportMenu;

    public bool menuIsOpen = false;
    public NetworkManagerScript networkManager;

    public GameObject zoomIn;
    public GameObject zoomOut;
    public GameObject reportButton;
    public GameObject settingsButton;
    public GameObject teleportButton;
    public GameObject swatches;
    public TVDudeScript tvDude;
    public CameraScript cameraScript;

    public InputField nameField;
    public GameObject randomizeColourButton;

    private AudioSource audioSource;

    public AudioClip uiClickSound;
    public AudioClip teleportSound;
    public AudioClip zoomOutSound;
    public AudioClip zoomInSound;

	// Use this for initialization
	void Start () {
        audioSource = this.GetComponent<AudioSource>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void PlayClickSound()
    {
        audioSource.PlayOneShot(uiClickSound);
    }

    public void PlayTeleportSound()
    {
        audioSource.PlayOneShot(teleportSound);
    }

    public void ZoomOutClicked()
    {
        audioSource.PlayOneShot(zoomOutSound);
        cameraScript.IncreaseCameraLevel();
    }

    public void ZoomInClicked()
    {
        audioSource.PlayOneShot(zoomInSound);
        cameraScript.DecreateCameraLevel();
    }

    public void ClickSettingsButton()
    {
        PlayClickSound();
        // If menu is already open, clicking button closes the menu
        if (menuIsOpen)
        {
            CloseMenu();
            return;
        }

        BlurBackground();
        settingsMenu.SetActive(true);
        menuIsOpen = true;

        zoomIn.SetActive(false);
        zoomOut.SetActive(false);
        reportButton.SetActive(false);
        teleportButton.SetActive(false);
        swatches.transform.localScale = new Vector3(0, 0, 0);

        nameField.text = networkManager.currentPlayerName;
        randomizeColourButton.GetComponent<Image>().color = tvDude.colour;
    }

    public void ClickReportButton()
    {
        PlayClickSound();
        // If menu is already open, clicking button closes the menu
        if (menuIsOpen)
        {
            CloseMenu();
            return;
        }

        BlurBackground();
        reportButton.SetActive(true);
        menuIsOpen = true;

        reportMenu.SetActive(true);

        zoomIn.SetActive(false);
        zoomOut.SetActive(false);
        teleportButton.SetActive(false);
        settingsButton.SetActive(false);

        swatches.transform.localScale = new Vector3(0, 0, 0);
    }

    public void ClickTeleportButton()
    {
        PlayClickSound();
        // If menu is already open, clicking button closes the menu
        if (menuIsOpen)
        {
            CloseMenu();
            return;
        }

        BlurBackground();
        teleportButton.SetActive(true);
        menuIsOpen = true;

        teleportMenu.SetActive(true);

        zoomIn.SetActive(false);
        zoomOut.SetActive(false);
        reportButton.SetActive(false);
        settingsButton.SetActive(false);

        swatches.transform.localScale = new Vector3(0, 0, 0);
    }

    public void CloseMenu()
    {
        PlayClickSound();
        UnBlurBackground();
        zoomIn.SetActive(true);
        zoomOut.SetActive(true);
        //reportButton.SetActive(true);
        teleportButton.SetActive(true);
        settingsButton.SetActive(true);
        swatches.transform.localScale = new Vector3(1, 1, 1);

        settingsMenu.SetActive(false);
        teleportMenu.SetActive(false);
        reportMenu.SetActive(false);
        menuIsOpen = false;
    }
    
    public void BlurBackground()
    {
        panel.SetActive(true);
        // Postprocessing removed for performance reasons
        /*
        profile.depthOfField.enabled = true;

        ColorGradingModel.Settings currentSettings = profile.colorGrading.settings;
        currentSettings.basic.postExposure = -1;

        profile.colorGrading.settings = currentSettings;
        //profile.colorGrading.enabled = true;*/
    }

    public void UnBlurBackground()
    {
        panel.SetActive(false);
        /*
        profile.depthOfField.enabled = false;
        //profile.colorGrading.enabled = false;

        ColorGradingModel.Settings currentSettings = profile.colorGrading.settings;
        currentSettings.basic.postExposure = 1.9f;

        profile.colorGrading.settings = currentSettings;*/
    }

    public void UpdateNameClicked()
    {
        PlayClickSound();
        nameField.Select();
    }

    public void SanitizeNameInput()
    {
        if (nameField.text.Length == 0) return;

        nameField.text = Util.ConvertToASCII(nameField.text);
        tvDude.ChangeName(nameField.text);
        networkManager.currentPlayerName = nameField.text;
    }

    public void RandomizeColourClicked()
    {
        PlayClickSound();
        Color color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f) / 2f;
        color.a = 1;
        tvDude.ChangeTVDudeColour(color);
        randomizeColourButton.GetComponent<Image>().color = color;
    }

    // Triggered when the screen was tapped outside of the menu, just close it
    public void ScreenWasClickedElsewhere()
    {
        if (menuIsOpen) CloseMenu();
    }

    public void OnDestroy()
    {
        UnBlurBackground();
    }
}
