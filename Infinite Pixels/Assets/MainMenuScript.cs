using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour {
    public AudioClip uiClick;

    private AudioSource audio;

    public void PlayClickSound()
    {
        audio.PlayOneShot(uiClick);
    }

	// Use this for initialization
	void Start () {
        audio = this.GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void EnterButtonClicked()
    {
        PlayClickSound();
        SceneManager.LoadScene("Main");
    }

    public void CreditsButtonClicked()
    {
        PlayClickSound();
        Application.OpenURL("https://jishaxe.github.io/InfinitePixels/credits");
    }
}
