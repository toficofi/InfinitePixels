using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourManager : MonoBehaviour {
    public Color[] colours = new Color[15];
    public int selectedColour = 1;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnColourChanged(int colour)
    {
        GameObject.Find("selector").GetComponent<SelectorController>().ChangeSelectorColour(colours[colour]);
        this.selectedColour = colour;
    }
}
