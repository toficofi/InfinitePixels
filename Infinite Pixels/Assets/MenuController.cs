using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {
    public PostProcessingProfile profile;
    public GameObject settingsMenu;

    public bool menuIsOpen = false;
    public NetworkManagerScript networkManager;

    public GameObject zoomIn;
    public GameObject zoomOut;
    public GameObject reportButton;
    public GameObject settingsButton;
    public GameObject teleportButton;
    public TVDudeScript tvDude;

    public InputField nameField;
    public GameObject randomizeColourButton;

	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ClickSettingsButton()
    {
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

        nameField.text = networkManager.currentPlayerName;
        randomizeColourButton.GetComponent<Image>().color = tvDude.colour;
    }
    
    public void CloseMenu()
    {
        UnBlurBackground();
        zoomIn.SetActive(true);
        zoomOut.SetActive(true);
        reportButton.SetActive(true);
        teleportButton.SetActive(true);
        settingsButton.SetActive(true);

        settingsMenu.SetActive(false);
        menuIsOpen = false;
    }
    
    public void BlurBackground()
    {
        profile.depthOfField.enabled = true;
        profile.colorGrading.enabled = true;
    }

    public void UnBlurBackground()
    {
        profile.depthOfField.enabled = false;
        profile.colorGrading.enabled = false;
    }

    public void UpdateNameClicked()
    {
        string newName = nameField.text;
        if (newName.Length < 0 || newName.Length > 20) return;

        newName = newName.Trim();
        newName = Util.ConvertToASCII(newName);
        nameField.text = newName;

        tvDude.ChangeName(newName);
        networkManager.currentPlayerName = newName;
    }

    public void RandomizeColourClicked()
    {
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
