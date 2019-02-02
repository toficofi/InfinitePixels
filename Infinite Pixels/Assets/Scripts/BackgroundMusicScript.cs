using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMusicScript : MonoBehaviour {
    public float secondsBeforeStartingBackgroundMusic;
    public float fadeInTime;

    private AudioSource bg;
    private float targetVolume;

    IEnumerator StartPlayingMusic()
    {
        yield return new WaitForSeconds(secondsBeforeStartingBackgroundMusic);

        bg.Play();
        bg.volume = 0;

        float currentTime = 0;
        while (bg.volume < targetVolume)
        {
            yield return new WaitForSeconds(0.1f);
            currentTime += 0.1f;
            bg.volume = Mathf.Lerp(0, targetVolume, currentTime / fadeInTime);
        }
    }

	// Use this for initialization
	void Start () {
        DontDestroyOnLoad(this);
        bg = this.GetComponent<AudioSource>();
        targetVolume = bg.volume;
        StartCoroutine(StartPlayingMusic());
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
