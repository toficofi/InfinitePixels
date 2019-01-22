using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenshotTaker : MonoBehaviour {
    public RawImage rawImage;
    WebCamTexture webcamTexture;

	// Use this for initialization
	void Start () {
        webcamTexture = new WebCamTexture();
        webcamTexture.Play();
        rawImage.texture = webcamTexture;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
