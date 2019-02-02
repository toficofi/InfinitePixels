using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldTextFitter : MonoBehaviour {
    public InputField textField;
    public Text textElement;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        int digits = textField.text.Length;

        textElement.fontSize = 24 - digits;
	}
}
