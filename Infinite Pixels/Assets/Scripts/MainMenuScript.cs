using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour {
    public AudioClip uiClick;
    public Text versionText;

    private AudioSource audio;

    public void PlayClickSound()
    {
        audio.PlayOneShot(uiClick);
    }

	// Use this for initialization
	void Start () {
        audio = this.GetComponent<AudioSource>();
        versionText.text = Application.version;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void EnterButtonClicked()
    {
        PlayClickSound();
        SceneManager.LoadScene("UserPolicyScene");
    }

    public void CreditsButtonClicked()
    {
        PlayClickSound();
        Application.OpenURL("https://jishaxe.github.io/InfinitePixels/credits");
    }

    public void TwitterButtonClicked()
    {
        PlayClickSound();
        Application.OpenURL("https://twitter.com/jshxe");
    }
}
