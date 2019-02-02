using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourManager : MonoBehaviour {
    public Color[] colours = new Color[15];
    public Material[] pixelCubeMaterials = new Material[15];
    public int selectedColour = 1;
    public GameObject selector;

	// Use this for initialization
	void Start () {
	   	for (int i = 0; i < 15; i++)
        {
            pixelCubeMaterials[i].color = colours[i];
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Material ColorToMaterial(int colour)
    {
        return pixelCubeMaterials[colour];
    }

    public void OnColourChanged(int colour)
    {
        selector.GetComponent<SelectorController>().ChangeSelectorColour(colours[colour]);
        this.selectedColour = colour;
    }
}
