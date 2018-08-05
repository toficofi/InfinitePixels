using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    Int64 xunCapped;
    Int64 yunCapped;

    public NetworkManagerScript networkManager;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 snappedSelectorPosition = selector.GetSnappedPosition();
        xPos.text = snappedSelectorPosition.x.ToString();
        yPos.text = snappedSelectorPosition.z.ToString();

        try
        {
            xPosField.text = Regex.Match(xPosField.text, @"\d+").Value;
            yPosField.text = Regex.Match(yPosField.text, @"\d+").Value;

            xunCapped = Convert.ToInt64(xPosField.text);
            yunCapped = Convert.ToInt64(yPosField.text);

            if (xunCapped > networkManager.worldSize) xunCapped = networkManager.worldSize;
            if (yunCapped > networkManager.worldSize) yunCapped = networkManager.worldSize;

            xPosField.text = xunCapped.ToString();
            yPosField.text = yunCapped.ToString();
        }
        catch (FormatException exception) { }

    }

    public bool IsXNegated()
    {
        return (xSignText.text.Contains("-"));
    }


    public bool IsYNegated()
    {
        return (ySignText.text.Contains("-"));
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

    public void CopyButtonClicked()
    {
        int x = Convert.ToInt32(xPos.text);
        int y = Convert.ToInt32(yPos.text);

        ClipboardHelper.clipBoard = "[" + x + "," + y + "]";
    }



    public bool AttemptToParseCoords(string text)
    {
        if (!text.Contains("[")) {  Debug.Log("Doesn't contain ["); return false; }
        if (!text.Contains("]")) {  Debug.Log("Doesn't contain ]"); return false; }
        if (text.IndexOf("]") > text.IndexOf("[") == false) {  Debug.Log("] is before ["); return false;  }// If ] is after [

        string[] splitText = text.Split(',');
        if (splitText.Length != 2) { Debug.Log("Split array length is not 2"); return false; } // If it splits in two at ,
        string xNumber = splitText[0];
        xNumber = Regex.Match(xNumber, @"-?[0-9]+").Value; // Leave only digits

        int x;
        if (!Int32.TryParse(xNumber, out x)) { Debug.Log("Couldn't parse " + xNumber + " as int"); return false; };

        xPosField.text = Math.Abs(x).ToString();
        if (x < 0) xSignText.text = "-";
        else xSignText.text = "+";

        string yNumber = splitText[1];
        yNumber = Regex.Match(yNumber, @"-?[0-9]+").Value; // Leave only digits

        int y;
        if (!Int32.TryParse(yNumber, out y)) { Debug.Log("Couldn't parse " + yNumber + " as int"); return false; }

        yPosField.text = Math.Abs(y).ToString();
        if (y < 0) ySignText.text = "-";
        else ySignText.text = "+";

        return true;
    }

    public void PasteButtonClicked()
    {
        if (AttemptToParseCoords(ClipboardHelper.clipBoard))
        {
            // Parsed successfully
        } else
        {
            Debug.Log("Couldn't parse " + ClipboardHelper.clipBoard + " for coords");
        }
    }

    public void TeleportButtonClicked()
    {
        int x = Convert.ToInt32(xPosField.text);
        int y = Convert.ToInt32(yPosField.text);
        if (IsXNegated()) x = -x;
        if (IsYNegated()) y = -y;

        selector.ChangePosition(new Vector3(x, 0, y));
    }
}
