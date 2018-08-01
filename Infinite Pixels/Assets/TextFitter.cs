using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextFitter : MonoBehaviour {
    Text text;
    public int fullSize;

	// Use this for initialization
	void Start () {
        text = this.GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
        int digits = text.text.Length;

        text.fontSize = fullSize - digits;
    }
}
