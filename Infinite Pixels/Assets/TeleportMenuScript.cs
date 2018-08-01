using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeleportMenuScript : MonoBehaviour {
    public Text xPos;
    public Text yPos;

    public InputField xPosField;
    public InputField yPosField;

    public Text xSignText;
    public Text ySignText;

    public SelectorController selector;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 snappedSelectorPosition = selector.GetSnappedPosition();
        xPos.text = snappedSelectorPosition.x.ToString();
        yPos.text = snappedSelectorPosition.z.ToString();
	}

    public void XSignClicked()
    {
        SwitchSign(xSignText);
    }

    public void YSignClicked()
    {
        SwitchSign(ySignText);
    }

    // Swaps the sign in the button (+ to -, - to +)
    void SwitchSign(Text buttonText)
    {
        buttonText.text = (buttonText.text == "+" ? "-" : "+");
    }
}
